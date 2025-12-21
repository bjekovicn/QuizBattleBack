using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Shared.Abstractions;
using QuizBattle.Api.Shared.Extensions;
using QuizBattle.Application.Features.Games.Services;


namespace QuizBattle.Api.Features.Game
{
    public sealed class GameInvitesEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/game/invites")
                .RequireAuthorization()
                .WithTags("Game Invites");

            group.MapPost("/{inviteId}/respond", RespondToInvite)
                .WithName("RespondToGameInvite")
                .WithOpenApi();
        }

        private static async Task<IResult> RespondToInvite(
            [FromRoute] Guid inviteId,
            [FromBody] RespondToInviteRequest request,
            [FromServices] IGameInviteService inviteService,
            [FromServices] IGameHubService hubService,
            HttpContext httpContext,
            CancellationToken ct)
        {
            var userId = httpContext.User.GetUserId();
            if (userId is null)
            {
                return Results.Unauthorized();
            }

            var result = await inviteService.RespondToInviteAsync(
                inviteId,
                userId.Value,
                request.Accept,
                ct);

            if (result.IsFailure)
            {
                return Results.BadRequest(new { error = result.Error.Message });
            }

            var invite = result.Value;

            // Notify host about response via SignalR
            await hubService.NotifyInviteResponseAsync(
                invite.HostUserId,
                userId.Value,
                request.Accept,
                invite,
                ct);

            return Results.Ok(new
            {
                success = true,
                roomId = invite.RoomId,
                accepted = request.Accept
            });
        }

        public sealed record RespondToInviteRequest(bool Accept);
    }
}