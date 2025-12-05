using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Shared.Abstractions;
using QuizBattle.Api.Shared.Extensions;
using QuizBattle.Application.Features.Games;
using QuizBattle.Application.Features.Games.Commands;
using QuizBattle.Application.Features.Games.Queries;
using QuizBattle.Domain.Features.Games;

namespace QuizBattle.Api.Features.Games
{

    public sealed class GameEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/games")
                .WithTags("Games")
                .RequireAuthorization()
                .RequireRateLimiting("api");

            group.MapGet("/{roomId:guid}", GetGameRoom)
                .WithName("GetGameRoom");

            group.MapGet("/player/{userId:int}/current", GetPlayerCurrentRoom)
                .WithName("GetPlayerCurrentRoom");

            group.MapPost("/", CreateGameRoom)
                .WithName("CreateGameRoom");

            group.MapPost("/{roomId:guid}/join", JoinGameRoom)
                .WithName("JoinGameRoom");

            group.MapPost("/{roomId:guid}/leave", LeaveGameRoom)
                .WithName("LeaveGameRoom");

            group.MapPost("/{roomId:guid}/ready", SetPlayerReady)
                .WithName("SetPlayerReady");

            group.MapPost("/{roomId:guid}/start", StartGame)
                .WithName("StartGame");

            group.MapPost("/{roomId:guid}/round/start", StartRound)
                .WithName("StartRound");

            group.MapPost("/{roomId:guid}/answer", SubmitAnswer)
                .WithName("SubmitAnswer");

            group.MapPost("/{roomId:guid}/round/end", EndRound)
                .WithName("EndRound");

            group.MapPost("/{roomId:guid}/end", EndGame)
                .WithName("EndGame");
        }

        private static async Task<IResult> GetGameRoom(
            Guid roomId,
            ISender sender,
            CancellationToken ct)
        {
            var query = new GetGameRoomQuery(roomId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> GetPlayerCurrentRoom(
            int userId,
            ISender sender,
            CancellationToken ct)
        {
            var query = new GetPlayerCurrentRoomQuery(userId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> CreateGameRoom(
            [FromBody] CreateGameRoomRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new CreateGameRoomCommand(
                (GameType)request.GameType,
                request.LanguageCode,
                request.TotalRounds);

            var result = await sender.Send(command, ct);

            return result.IsSuccess
                ? Results.Created($"/api/games/{result.Value.Id}", result.Value)
                : result.ToHttpResult();
        }

        private static async Task<IResult> JoinGameRoom(
            Guid roomId,
            [FromBody] JoinGameRoomRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new JoinGameRoomCommand(roomId, request.UserId, request.DisplayName, request.PhotoUrl);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> LeaveGameRoom(
            Guid roomId,
            [FromBody] LeaveGameRoomRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new LeaveGameRoomCommand(roomId, request.UserId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> SetPlayerReady(
            Guid roomId,
            [FromBody] SetPlayerReadyRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new SetPlayerReadyCommand(roomId, request.UserId, request.IsReady);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> StartGame(
            Guid roomId,
            ISender sender,
            CancellationToken ct)
        {
            var command = new StartGameCommand(roomId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> StartRound(
            Guid roomId,
            ISender sender,
            CancellationToken ct)
        {
            var command = new StartRoundCommand(roomId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> SubmitAnswer(
            Guid roomId,
            [FromBody] SubmitAnswerRequest request,
            ISender sender,
            CancellationToken ct)
        {
            var command = new SubmitAnswerCommand(roomId, request.UserId, request.Answer);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> EndRound(
            Guid roomId,
            ISender sender,
            CancellationToken ct)
        {
            var command = new EndRoundCommand(roomId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> EndGame(
            Guid roomId,
            ISender sender,
            CancellationToken ct)
        {
            var command = new EndGameCommand(roomId);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }
    }

    // Request DTOs
    public sealed record CreateGameRoomRequest(
        int GameType,
        string LanguageCode,
        int TotalRounds = 10);

    public sealed record JoinGameRoomRequest(
        int UserId,
        string DisplayName,
        string? PhotoUrl);

    public sealed record LeaveGameRoomRequest(int UserId);

    public sealed record SetPlayerReadyRequest(int UserId, bool IsReady);

    public sealed record SubmitAnswerRequest(int UserId, string Answer);
}
