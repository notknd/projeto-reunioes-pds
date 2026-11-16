using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Reunioes.Dados.Conexao;

public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConnectionFactory(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString)
        {
            ForeignKeys = true
        };
        _connectionString = builder.ToString();
    }

    public DbConnection CreateConnection() => new SqliteConnection(_connectionString);
}
