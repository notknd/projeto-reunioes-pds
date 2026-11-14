using Reunioes.Negocio.DTOs;
using Reunioes.Negocio.Resultados;
using Reunioes.Negocio.Servicos;

namespace Reunioes.Api.Endpoints;

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/reservations", async (
            CreateReservationRequest request,
            ReservationService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(request, cancellationToken);
            return result.IsSuccess
                ? (IResult)Results.Created((string?)null, result.Value)
                : MapError(result.Error, result.Message);
        })
        .WithName("CreateReservation")
        .WithTags("Reservas")
        .WithSummary("Cria uma reserva para uma sala.");

        endpoints.MapPut("/api/reservations/{reservationId:guid}", async (
            Guid reservationId,
            RescheduleReservationRequest request,
            ReservationService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RescheduleAsync(reservationId, request, cancellationToken);
            return result.IsSuccess
                ? (IResult)Results.Ok(result.Value)
                : MapError(result.Error, result.Message);
        })
        .WithName("RescheduleReservation")
        .WithTags("Reservas")
        .WithSummary("Reagenda uma reserva futura.");

        endpoints.MapDelete("/api/reservations/{reservationId:guid}", async (
            Guid reservationId,
            ReservationService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CancelAsync(reservationId, cancellationToken);
            return result.IsSuccess
                ? (IResult)Results.Ok(result.Value)
                : MapError(result.Error, result.Message);
        })
        .WithName("CancelReservation")
        .WithTags("Reservas")
        .WithSummary("Cancela uma reserva existente.");

        return endpoints;
    }

    private static IResult MapError(OperationError? error, string? message) =>
        error switch
        {
            OperationError.Validation => Results.Problem(
                title: "Dados ou regra de negócio inválidos",
                detail: message,
                statusCode: StatusCodes.Status400BadRequest),
            OperationError.NotFound => Results.Problem(
                title: "Reserva ou sala não encontrada",
                detail: message,
                statusCode: StatusCodes.Status404NotFound),
            OperationError.Conflict => Results.Problem(
                title: "Conflito de reserva",
                detail: message,
                statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
}
