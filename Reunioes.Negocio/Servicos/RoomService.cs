using Reunioes.Dados.Entidades;
using Reunioes.Dados.Repositorios;
using Reunioes.Negocio.DTOs;
using Reunioes.Negocio.Resultados;
using Reunioes.Negocio.Validacoes;

namespace Reunioes.Negocio.Servicos;

public sealed class RoomService(RoomRepository roomRepository, ReservationRepository reservationRepository)
{
    private const int PageSize = 10;
    private static readonly TimeOnly OpeningTime = new(8, 0);
    private static readonly TimeOnly ClosingTime = new(19, 0);

    public async Task<OperationResult<RoomResponse>> CreateAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request);
        if (validationError is not null)
            return validationError;

        var room = new Room
        {
            Name = request.Name!.Trim(),
            Floor = request.Floor!.Value,
            Capacity = request.Capacity!.Value
        };

        await roomRepository.InsertAsync(room, cancellationToken);
        return OperationResult<RoomResponse>.Success(ToResponse(room));
    }

    public async Task<OperationResult<RoomResponse>> GetAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.GetAsync(roomId, cancellationToken);
        return room is null
            ? NotFound()
            : OperationResult<RoomResponse>.Success(ToResponse(room));
    }

    public async Task<OperationResult<RoomResponse>> UpdateAsync(
        Guid roomId,
        UpdateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.GetAsync(roomId, cancellationToken);
        if (room is null)
            return NotFound();

        var validationError = Validate(request);
        if (validationError is not null)
            return validationError;

        room.Name = request.Name!.Trim();
        room.Floor = request.Floor!.Value;
        room.Capacity = request.Capacity!.Value;
        await roomRepository.UpdateAsync(room, cancellationToken);
        return OperationResult<RoomResponse>.Success(ToResponse(room));
    }

    public async Task<OperationResult<RoomResponse>> DeleteAsync(
        Guid roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.GetAsync(roomId, cancellationToken);
        if (room is null)
            return NotFound();

        if (await roomRepository.HasReservationsAsync(roomId, cancellationToken))
            return OperationResult<RoomResponse>.Failure(
                OperationError.Conflict,
                "Não é possível excluir uma sala que possui reservas vinculadas.");

        await roomRepository.DeleteAsync(roomId, cancellationToken);
        return OperationResult<RoomResponse>.Success(ToResponse(room));
    }

    public async Task<OperationResult<RoomListResponse>> ListAsync(
        int page = 1,
        string? name = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            return OperationResult<RoomListResponse>.Failure(
                OperationError.Validation, "A página deve ser maior ou igual a 1.");

        var totalCount = await roomRepository.CountAsync(name, cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
        var offset = page > totalPages ? totalCount : (page - 1) * PageSize;
        var rooms = await roomRepository.ListAsync(name, offset, PageSize, cancellationToken);
        var items = rooms.Select(ToResponse).ToList();

        return OperationResult<RoomListResponse>.Success(new RoomListResponse(
            items, page, PageSize, totalCount, totalPages));
    }

    public async Task<OperationResult<IReadOnlyList<RoomAvailabilityResponse>>> GetAvailabilityAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            return OperationResult<IReadOnlyList<RoomAvailabilityResponse>>.Failure(
                OperationError.Validation, "A data final deve ser igual ou posterior à data inicial.");

        var rangeStart = startDate.ToDateTime(OpeningTime);
        var rangeEnd = endDate.ToDateTime(ClosingTime);
        var rooms = await roomRepository.ListAllAsync(cancellationToken);
        var reservations = await reservationRepository.ListOverlappingAsync(rangeStart, rangeEnd, cancellationToken);
        var totalMinutes = (decimal)(endDate.DayNumber - startDate.DayNumber + 1) * 11 * 60;

        var availability = rooms.Select(room =>
        {
            var intervals = reservations
                .Where(item => item.RoomId == room.Id)
                .Select(item => (
                    Start: item.StartTime < rangeStart ? rangeStart : item.StartTime,
                    End: item.EndTime > rangeEnd ? rangeEnd : item.EndTime))
                .Where(interval => interval.End > interval.Start)
                .OrderBy(interval => interval.Start)
                .ToList();

            decimal occupiedMinutes = 0;
            if (intervals.Count > 0)
            {
                var occupiedStart = intervals[0].Start;
                var occupiedEnd = intervals[0].End;
                foreach (var interval in intervals.Skip(1))
                {
                    if (interval.Start <= occupiedEnd)
                        occupiedEnd = interval.End > occupiedEnd ? interval.End : occupiedEnd;
                    else
                    {
                        occupiedMinutes += (decimal)(occupiedEnd - occupiedStart).TotalMinutes;
                        occupiedStart = interval.Start;
                        occupiedEnd = interval.End;
                    }
                }
                occupiedMinutes += (decimal)(occupiedEnd - occupiedStart).TotalMinutes;
            }

            return new RoomAvailabilityResponse(
                room.Id, room.Name, startDate, endDate, Math.Max(0, totalMinutes - occupiedMinutes) / 60);
        }).ToList();

        return OperationResult<IReadOnlyList<RoomAvailabilityResponse>>.Success(availability);
    }

    private static OperationResult<RoomResponse>? Validate(RoomRequest request)
    {
        var errors = RequestValidator.Validate(request);
        if (errors.Count == 0)
            return null;

        var message = string.Join(" ", errors
            .Select(error => error.ErrorMessage)
            .Where(message => !string.IsNullOrWhiteSpace(message)));
        return OperationResult<RoomResponse>.Failure(OperationError.Validation, message);
    }

    private static OperationResult<RoomResponse> NotFound() =>
        OperationResult<RoomResponse>.Failure(OperationError.NotFound, "A sala informada não foi encontrada.");

    private static RoomResponse ToResponse(Room room) =>
        new(room.Id, room.Name, room.Floor, room.Capacity);
}
