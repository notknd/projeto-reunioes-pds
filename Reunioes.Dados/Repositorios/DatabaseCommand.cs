using System.Data.Common;

namespace Reunioes.Dados.Repositorios;

internal static class DatabaseCommand
{
    public static DbParameter AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value is DateTime dateTime
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified)
            : value ?? DBNull.Value;
        command.Parameters.Add(parameter);
        return parameter;
    }
}
