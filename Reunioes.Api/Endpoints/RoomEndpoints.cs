using Reunioes.Negocio.DTOs;
using Reunioes.Negocio.Resultados;
using Reunioes.Negocio.Servicos;

namespace Reunioes.Api.Endpoints;

public static class RoomEndpoints
{
    public static IEndpointRouteBuilder MapRoomEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var rooms = endpoints.MapGroup("/api/rooms").WithTags("Salas");

        rooms.MapPost("/", async (
            CreateRoomRequest request,
            RoomService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.CreateAsync(request, cancellationToken);
            return result.IsSuccess
                ? (IResult)Results.Created($"/api/rooms/{result.Value!.Id}", result.Value)
                : MapError(result.Error, result.Message);
        })
        .WithName("CreateRoom")
        .WithSummary("Cadastra uma sala.")
        .Produces<RoomResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest);

        rooms.MapGet("/", async (
            int? page,
            string? name,
            RoomService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.ListAsync(page ?? 1, name, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error, result.Message);
        })
        .WithName("ListRooms")
        .WithSummary("Lista salas em páginas de 10, com filtro por nome e ordenação por andar.")
        .Produces<RoomListResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        rooms.MapGet("/{roomId:guid}", async (
            Guid roomId,
            RoomService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAsync(roomId, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error, result.Message);
        })
        .WithName("GetRoom")
        .WithSummary("Consulta uma sala pelo identificador.")
        .Produces<RoomResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound);

        rooms.MapPut("/{roomId:guid}", async (
            Guid roomId,
            UpdateRoomRequest request,
            RoomService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.UpdateAsync(roomId, request, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error, result.Message);
        })
        .WithName("UpdateRoom")
        .WithSummary("Atualiza os dados cadastrais de uma sala.")
        .Produces<RoomResponse>()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound);

        rooms.MapDelete("/{roomId:guid}", async (
            Guid roomId,
            RoomService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.DeleteAsync(roomId, cancellationToken);
            return result.IsSuccess ? Results.Ok(result.Value) : MapError(result.Error, result.Message);
        })
        .WithName("DeleteRoom")
        .WithSummary("Exclui uma sala que não tenha reservas vinculadas.")
        .Produces<RoomResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);

        rooms.MapGet("/availability", async (
            DateOnly startDate,
            DateOnly endDate,
            RoomService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.GetAvailabilityAsync(startDate, endDate, cancellationToken);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : MapError(result.Error, result.Message);
        })
        .WithName("GetRoomsAvailability")
        .WithSummary("Calcula as horas livres de cada sala no expediente de 08:00 a 19:00, incluindo as datas inicial e final.")
        .Produces<IReadOnlyList<RoomAvailabilityResponse>>()
        .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static IResult MapError(OperationError? error, string? message) =>
        error switch
        {
            OperationError.Validation => Results.Problem(
                title: "Dados inválidos", detail: message, statusCode: StatusCodes.Status400BadRequest),
            OperationError.NotFound => Results.Problem(
                title: "Sala não encontrada", detail: message, statusCode: StatusCodes.Status404NotFound),
            OperationError.Conflict => Results.Problem(
                title: "Sala possui reservas", detail: message, statusCode: StatusCodes.Status409Conflict),
            _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError)
        };
}
