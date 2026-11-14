using System.ComponentModel.DataAnnotations;

namespace Reunioes.Negocio.DTOs;

public abstract class RoomRequest
{
    [Required(ErrorMessage = "O nome da sala é obrigatório.")]
    [StringLength(120, ErrorMessage = "O nome da sala deve ter no máximo 120 caracteres.")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "O andar é obrigatório.")]
    [Range(0, 200, ErrorMessage = "O andar deve estar entre 0 e 200.")]
    public int? Floor { get; set; }

    [Required(ErrorMessage = "A quantidade de assentos é obrigatória.")]
    [Range(1, 500, ErrorMessage = "A quantidade de assentos deve ser maior que zero.")]
    public int? Capacity { get; set; }
}

public sealed class CreateRoomRequest : RoomRequest { }
public sealed class UpdateRoomRequest : RoomRequest { }

public sealed record RoomResponse(Guid Id, string Name, int Floor, int Capacity);
