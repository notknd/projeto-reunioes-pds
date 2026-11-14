using System.ComponentModel.DataAnnotations;

namespace Reunioes.Negocio.Validacoes;

public static class RequestValidator
{
    public static IReadOnlyList<ValidationResult> Validate<TRequest>(TRequest request)
        where TRequest : class
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), errors, validateAllProperties: true);
        return errors;
    }
}
