namespace Reunioes.Dados.Conexao;

public sealed class DatabaseInitializer(SqliteConnectionFactory connectionFactory)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        foreach (var statement in SqliteStatements)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = statement;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static readonly string[] SqliteStatements =
    [
        "CREATE TABLE IF NOT EXISTS Rooms (Id TEXT PRIMARY KEY, Name TEXT NOT NULL CHECK (length(Name) <= 120), Floor INTEGER NOT NULL, Capacity INTEGER NOT NULL CHECK (Capacity > 0))",
        "CREATE TABLE IF NOT EXISTS Reservations (Id TEXT PRIMARY KEY, RoomId TEXT NOT NULL REFERENCES Rooms(Id) ON DELETE RESTRICT, StartTime TEXT NOT NULL, EndTime TEXT NOT NULL, CreatedAt TEXT NOT NULL)",
        "CREATE INDEX IF NOT EXISTS IX_Reservations_RoomId_StartTime_EndTime ON Reservations (RoomId, StartTime, EndTime)"
    ];

}
