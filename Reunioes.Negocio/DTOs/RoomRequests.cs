using System.ComponentModel.DataAnnotations;

namespace Reunioes.Negocio.DTOs;

public abstract class RoomRequest
{
    [Required(ErrorMessage = "O nome da sala é obrigatório.")]
    [StringLength(120, ErrorMessage = "O nome da sala deve ter no máximo 120 caracteres.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "O andar é obrigatório.")]
    public int? Floor { get; set; }

    [Required(ErrorMessage = "A quantidade de assentos é obrigatória.")]
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade de assentos deve ser maior que zero.")]
    public int? Capacity { get; set; }
}

public sealed class CreateRoomRequest : RoomRequest { }
public sealed class UpdateRoomRequest : RoomRequest { }

public sealed record RoomResponse(Guid Id, string Name, int Floor, int Capacity);

public sealed record RoomListResponse(
    IReadOnlyList<RoomResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record RoomAvailabilityResponse(
    Guid RoomId,
    string RoomName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal FreeHours);

public sealed record RecentReservationsResponse(DateTime From, DateTime To, int TotalCount);
