using System.Data;
using System.Data.Common;
using Reunioes.Dados.Conexao;
using Reunioes.Dados.Entidades;

namespace Reunioes.Dados.Repositorios;

public sealed class ReservationRepository(SqliteConnectionFactory connectionFactory)
{
    public async Task<bool> RoomExistsAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM Rooms WHERE Id = @RoomId LIMIT 1";
        DatabaseCommand.AddParameter(command, "@RoomId", roomId);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<Reservation?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, RoomId, StartTime, EndTime, CreatedAt FROM Reservations WHERE Id = @Id";
        DatabaseCommand.AddParameter(command, "@Id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadReservation(reader) : null;
    }

    public async Task<bool> CreateIfAvailableAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        if (await HasConflictAsync(connection, transaction, reservation.RoomId, reservation.StartTime, reservation.EndTime, null, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await InsertAsync(connection, transaction, reservation, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RescheduleIfAvailableAsync(Reservation reservation, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        if (await HasConflictAsync(connection, transaction, reservation.RoomId, reservation.StartTime, reservation.EndTime, reservation.Id, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE Reservations SET StartTime = @StartTime, EndTime = @EndTime WHERE Id = @Id";
        DatabaseCommand.AddParameter(command, "@StartTime", reservation.StartTime);
        DatabaseCommand.AddParameter(command, "@EndTime", reservation.EndTime);
        DatabaseCommand.AddParameter(command, "@Id", reservation.Id);
        var updated = await command.ExecuteNonQueryAsync(cancellationToken) == 1;
        if (updated)
            await transaction.CommitAsync(cancellationToken);
        else
            await transaction.RollbackAsync(cancellationToken);
        return updated;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Reservations WHERE Id = @Id";
        DatabaseCommand.AddParameter(command, "@Id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<List<Reservation>> ListOverlappingAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, RoomId, StartTime, EndTime, CreatedAt FROM Reservations WHERE StartTime < @End AND EndTime > @Start";
        DatabaseCommand.AddParameter(command, "@Start", start);
        DatabaseCommand.AddParameter(command, "@End", end);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var reservations = new List<Reservation>();
        while (await reader.ReadAsync(cancellationToken))
            reservations.Add(ReadReservation(reader));
        return reservations;
    }

    public async Task<int> CountCreatedBetweenAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Reservations WHERE CreatedAt >= @From AND CreatedAt <= @To";
        DatabaseCommand.AddParameter(command, "@From", from);
        DatabaseCommand.AddParameter(command, "@To", to);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private static async Task<bool> HasConflictAsync(
        DbConnection connection,
        DbTransaction transaction,
        Guid roomId,
        DateTime start,
        DateTime end,
        Guid? ignoredId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT 1 FROM Reservations WHERE RoomId = @RoomId AND (@IgnoredId IS NULL OR Id <> @IgnoredId) AND StartTime < @End AND EndTime > @Start LIMIT 1";
        DatabaseCommand.AddParameter(command, "@RoomId", roomId);
        DatabaseCommand.AddParameter(command, "@IgnoredId", ignoredId);
        DatabaseCommand.AddParameter(command, "@Start", start);
        DatabaseCommand.AddParameter(command, "@End", end);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    private static async Task InsertAsync(
        DbConnection connection,
        DbTransaction transaction,
        Reservation reservation,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT INTO Reservations (Id, RoomId, StartTime, EndTime, CreatedAt) VALUES (@Id, @RoomId, @StartTime, @EndTime, @CreatedAt)";
        DatabaseCommand.AddParameter(command, "@Id", reservation.Id);
        DatabaseCommand.AddParameter(command, "@RoomId", reservation.RoomId);
        DatabaseCommand.AddParameter(command, "@StartTime", reservation.StartTime);
        DatabaseCommand.AddParameter(command, "@EndTime", reservation.EndTime);
        DatabaseCommand.AddParameter(command, "@CreatedAt", reservation.CreatedAt);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static Reservation ReadReservation(DbDataReader reader) => new()
    {
        Id = ReadGuid(reader.GetValue(0)),
        RoomId = ReadGuid(reader.GetValue(1)),
        StartTime = ReadDateTime(reader.GetValue(2)),
        EndTime = ReadDateTime(reader.GetValue(3)),
        CreatedAt = DateTime.SpecifyKind(ReadDateTime(reader.GetValue(4)), DateTimeKind.Utc)
    };

    private static Guid ReadGuid(object value) => value is Guid id ? id : Guid.Parse(Convert.ToString(value)!);

    private static DateTime ReadDateTime(object value) => value is DateTime dateTime
        ? dateTime
        : DateTime.Parse(Convert.ToString(value)!, System.Globalization.CultureInfo.InvariantCulture);
}
