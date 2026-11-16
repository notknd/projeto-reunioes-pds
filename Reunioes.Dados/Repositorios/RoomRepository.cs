using System.Data.Common;
using Reunioes.Dados.Conexao;
using Reunioes.Dados.Entidades;

namespace Reunioes.Dados.Repositorios;

public sealed class RoomRepository(SqliteConnectionFactory connectionFactory)
{
    public async Task InsertAsync(Room room, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Rooms (Id, Name, Floor, Capacity) VALUES (@Id, @Name, @Floor, @Capacity)";
        DatabaseCommand.AddParameter(command, "@Id", room.Id);
        DatabaseCommand.AddParameter(command, "@Name", room.Name);
        DatabaseCommand.AddParameter(command, "@Floor", room.Floor);
        DatabaseCommand.AddParameter(command, "@Capacity", room.Capacity);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Room?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Floor, Capacity FROM Rooms WHERE Id = @Id";
        DatabaseCommand.AddParameter(command, "@Id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadRoom(reader) : null;
    }

    public async Task<bool> UpdateAsync(Room room, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Rooms SET Name = @Name, Floor = @Floor, Capacity = @Capacity WHERE Id = @Id";
        DatabaseCommand.AddParameter(command, "@Name", room.Name);
        DatabaseCommand.AddParameter(command, "@Floor", room.Floor);
        DatabaseCommand.AddParameter(command, "@Capacity", room.Capacity);
        DatabaseCommand.AddParameter(command, "@Id", room.Id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> HasReservationsAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM Reservations WHERE RoomId = @RoomId LIMIT 1";
        DatabaseCommand.AddParameter(command, "@RoomId", roomId);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Rooms WHERE Id = @Id";
        DatabaseCommand.AddParameter(command, "@Id", id);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<int> CountAsync(string? name, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Rooms WHERE (@Pattern IS NULL OR Name LIKE @Pattern)";
        DatabaseCommand.AddParameter(command, "@Pattern", string.IsNullOrWhiteSpace(name) ? null : $"%{name.Trim()}%");
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<List<Room>> ListAsync(string? name, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Floor, Capacity FROM Rooms WHERE (@Pattern IS NULL OR Name LIKE @Pattern) ORDER BY Floor, Name, Id LIMIT @PageSize OFFSET @Offset";
        DatabaseCommand.AddParameter(command, "@Pattern", string.IsNullOrWhiteSpace(name) ? null : $"%{name.Trim()}%");
        DatabaseCommand.AddParameter(command, "@PageSize", pageSize);
        DatabaseCommand.AddParameter(command, "@Offset", offset);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rooms = new List<Room>();
        while (await reader.ReadAsync(cancellationToken))
            rooms.Add(ReadRoom(reader));
        return rooms;
    }

    public async Task<List<Room>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Floor, Capacity FROM Rooms ORDER BY Floor, Name, Id";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var rooms = new List<Room>();
        while (await reader.ReadAsync(cancellationToken))
            rooms.Add(ReadRoom(reader));
        return rooms;
    }

    private static Room ReadRoom(DbDataReader reader) => new()
    {
        Id = reader.GetValue(0) is Guid id ? id : Guid.Parse(Convert.ToString(reader.GetValue(0))!),
        Name = reader.GetString(1),
        Floor = reader.GetInt32(2),
        Capacity = reader.GetInt32(3)
    };
}
