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

            if (result.IsSuccess)
                return Results.Created((string?)null, result.Value);

            return result.Error switch
            {
                OperationError.Validation => Results.Problem(
                    title: "Dados inválidos",
                    detail: result.Message,
                    statusCode: StatusCodes.Status400BadRequest),
                OperationError.NotFound => Results.Problem(
                    title: "Sala não encontrada",
                    detail: result.Message,
                    statusCode: StatusCodes.Status404NotFound),
                OperationError.Conflict => Results.Problem(
                    title: "Conflito de reserva",
                    detail: result.Message,
                    statusCode: StatusCodes.Status409Conflict),
                _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
            };
        })
        .WithName("CreateReservation")
        .WithTags("Reservas")
        .WithSummary("Cria uma reserva para uma sala.");

        return endpoints;
    }
}
