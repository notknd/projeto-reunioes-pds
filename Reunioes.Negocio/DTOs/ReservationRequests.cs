using System.ComponentModel.DataAnnotations;

namespace Reunioes.Negocio.DTOs;

public sealed class CreateReservationRequest : IValidatableObject
{
    [Required(ErrorMessage = "A sala da reserva é obrigatória.")]
    public Guid? RoomId { get; set; }

    [Required(ErrorMessage = "A data e hora de início são obrigatórias.")]
    public DateTime? StartTime { get; set; }

    [Required(ErrorMessage = "A data e hora de término são obrigatórias.")]
    public DateTime? EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RoomId == Guid.Empty)
            yield return new ValidationResult("A sala da reserva deve ser válida.", [nameof(RoomId)]);

        if (StartTime.HasValue && EndTime.HasValue && EndTime.Value <= StartTime.Value)
            yield return new ValidationResult("O término deve ser posterior ao início.", [nameof(EndTime)]);
    }
}

public sealed class RescheduleReservationRequest : IValidatableObject
{
    [Required(ErrorMessage = "A data e hora de início são obrigatórias.")]
    public DateTime? StartTime { get; set; }

    [Required(ErrorMessage = "A data e hora de término são obrigatórias.")]
    public DateTime? EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartTime.HasValue && EndTime.HasValue && EndTime.Value <= StartTime.Value)
            yield return new ValidationResult("O término deve ser posterior ao início.", [nameof(EndTime)]);
    }
}

public sealed record ReservationResponse(
    Guid Id,
    Guid RoomId,
    DateTime StartTime,
    DateTime EndTime,
    DateTime CreatedAt);
