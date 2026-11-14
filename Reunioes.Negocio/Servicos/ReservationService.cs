using Microsoft.EntityFrameworkCore;
using Reunioes.Dados.Context;
using Reunioes.Dados.Entidades;
using Reunioes.Negocio.DTOs;
using Reunioes.Negocio.Resultados;
using Reunioes.Negocio.Validacoes;

namespace Reunioes.Negocio.Servicos;

public sealed class ReservationService(ReunioesDbContext context)
{
    private static readonly TimeSpan OpeningTime = new(8, 0, 0);
    private static readonly TimeSpan ClosingTime = new(19, 0, 0);

    public async Task<OperationResult<ReservationResponse>> CreateAsync(
        CreateReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = RequestValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            var message = string.Join(" ", validationErrors
                .Select(error => error.ErrorMessage)
                .Where(error => !string.IsNullOrWhiteSpace(error)));
            return OperationResult<ReservationResponse>.Failure(OperationError.Validation, message);
        }

        var roomId = request.RoomId!.Value;
        var startTime = request.StartTime!.Value;
        var endTime = request.EndTime!.Value;

        var intervalError = ValidateInterval(startTime, endTime);
        if (intervalError is not null)
            return OperationResult<ReservationResponse>.Failure(OperationError.Validation, intervalError);

        var roomExists = await context.Rooms
            .AnyAsync(room => room.Id == roomId, cancellationToken);
        if (!roomExists)
            return OperationResult<ReservationResponse>.Failure(
                OperationError.NotFound, "A sala informada não foi encontrada.");

        var hasConflict = await context.Reservations.AnyAsync(reservation =>
            reservation.RoomId == roomId &&
            reservation.StartTime < endTime &&
            reservation.EndTime > startTime,
            cancellationToken);

        if (hasConflict)
            return OperationResult<ReservationResponse>.Failure(
                OperationError.Conflict, "Já existe uma reserva sobreposta para esta sala.");

        var reservation = new Reservation
        {
            RoomId = roomId,
            StartTime = startTime,
            EndTime = endTime,
            CreatedAt = DateTime.UtcNow
        };

        context.Reservations.Add(reservation);
        await context.SaveChangesAsync(cancellationToken);

        var response = new ReservationResponse(
            reservation.Id,
            reservation.RoomId,
            reservation.StartTime,
            reservation.EndTime,
            reservation.CreatedAt);

        return OperationResult<ReservationResponse>.Success(response);
    }

    internal static string? ValidateInterval(DateTime startTime, DateTime endTime)
    {
        if (startTime >= endTime)
            return "O término deve ser posterior ao início.";

        if (startTime.Date != endTime.Date)
            return "A reserva deve começar e terminar no mesmo dia.";

        if (startTime.TimeOfDay <= OpeningTime || endTime.TimeOfDay >= ClosingTime)
            return "A reserva deve ocorrer estritamente entre 08:00 e 19:00.";

        return null;
    }
}
