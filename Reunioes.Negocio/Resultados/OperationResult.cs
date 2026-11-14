namespace Reunioes.Negocio.Resultados;

public enum OperationError
{
    Validation,
    NotFound,
    Conflict
}

public sealed record OperationResult<T>(T? Value, OperationError? Error, string? Message)
{
    public bool IsSuccess => Error is null;

    public static OperationResult<T> Success(T value) => new(value, null, null);

    public static OperationResult<T> Failure(OperationError error, string message) =>
        new(default, error, message);
}
