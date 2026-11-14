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
            return InvalidRequest(validationErrors);

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

        var hasConflict = await HasConflictAsync(roomId, startTime, endTime, cancellationToken);
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

        return OperationResult<ReservationResponse>.Success(ToResponse(reservation));
    }

    public async Task<OperationResult<ReservationResponse>> RescheduleAsync(
        Guid reservationId,
        RescheduleReservationRequest request,
        CancellationToken cancellationToken = default)
    {
        var reservation = await context.Reservations
            .SingleOrDefaultAsync(item => item.Id == reservationId, cancellationToken);
        if (reservation is null)
            return OperationResult<ReservationResponse>.Failure(
                OperationError.NotFound, "A reserva informada não foi encontrada.");

        if (reservation.StartTime <= DateTime.Now)
            return OperationResult<ReservationResponse>.Failure(
                OperationError.Validation, "Somente reservas futuras podem ser reagendadas.");

        var validationErrors = RequestValidator.Validate(request);
        if (validationErrors.Count > 0)
            return InvalidRequest(validationErrors);

        var startTime = request.StartTime!.Value;
        var endTime = request.EndTime!.Value;

        var intervalError = ValidateInterval(startTime, endTime);
        if (intervalError is not null)
            return OperationResult<ReservationResponse>.Failure(OperationError.Validation, intervalError);

        var hasConflict = await HasConflictAsync(
            reservation.RoomId,
            startTime,
            endTime,
            cancellationToken,
            reservation.Id);
        if (hasConflict)
            return OperationResult<ReservationResponse>.Failure(
                OperationError.Conflict, "Já existe outra reserva sobreposta para esta sala.");

        reservation.StartTime = startTime;
        reservation.EndTime = endTime;
        await context.SaveChangesAsync(cancellationToken);

        return OperationResult<ReservationResponse>.Success(ToResponse(reservation));
    }

    public async Task<OperationResult<ReservationResponse>> CancelAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await context.Reservations
            .SingleOrDefaultAsync(item => item.Id == reservationId, cancellationToken);
        if (reservation is null)
            return OperationResult<ReservationResponse>.Failure(
                OperationError.NotFound, "A reserva informada não foi encontrada.");

        var response = ToResponse(reservation);
        context.Reservations.Remove(reservation);
        await context.SaveChangesAsync(cancellationToken);

        return OperationResult<ReservationResponse>.Success(response);
    }

    private Task<bool> HasConflictAsync(
        Guid roomId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken,
        Guid? ignoredReservationId = null) =>
        context.Reservations.AnyAsync(reservation =>
            reservation.RoomId == roomId &&
            (!ignoredReservationId.HasValue || reservation.Id != ignoredReservationId.Value) &&
            reservation.StartTime < endTime &&
            reservation.EndTime > startTime,
            cancellationToken);

    private static OperationResult<ReservationResponse> InvalidRequest(
        IReadOnlyList<System.ComponentModel.DataAnnotations.ValidationResult> validationErrors)
    {
        var message = string.Join(" ", validationErrors
            .Select(error => error.ErrorMessage)
            .Where(error => !string.IsNullOrWhiteSpace(error)));
        return OperationResult<ReservationResponse>.Failure(OperationError.Validation, message);
    }

    private static ReservationResponse ToResponse(Reservation reservation) =>
        new(
            reservation.Id,
            reservation.RoomId,
            reservation.StartTime,
            reservation.EndTime,
            reservation.CreatedAt);

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
