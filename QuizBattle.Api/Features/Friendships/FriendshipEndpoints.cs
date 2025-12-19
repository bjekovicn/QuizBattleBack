using MediatR;
using QuizBattle.Api.Shared.Abstractions;
using QuizBattle.Api.Shared.Extensions;
using QuizBattle.Application.Features.Friendships.Commands;
using QuizBattle.Application.Features.Friendships.Queries;
using System.Security.Claims;

namespace QuizBattle.Api.Features.Friendships
{

    public sealed class FriendshipEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/friendships")
                .WithTags("Friendships")
                .RequireAuthorization()
                .RequireRateLimiting("api");

            group.MapGet("/accepted", GetAcceptedFriends)
                .WithName("GetAcceptedFriends")
                .WithOpenApi();

            group.MapPost("/{id:int}/add", AddFriend)
                .WithName("AddFriend")
                .WithOpenApi();

            group.MapDelete("/{id:int}/remove", RemoveFriend)
                .WithName("RemoveFriend")
                .WithOpenApi();
        }

        private static async Task<IResult> GetAcceptedFriends(
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct)
        {
            var userId = user.GetRequiredUserId();
            var query = new GetFriendsQuery(userId);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> AddFriend(
            int id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct)
        {
            var userId = user.GetRequiredUserId();
            var command = new AddFriendCommand(userId, id);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }

        private static async Task<IResult> RemoveFriend(
            int id,
            ClaimsPrincipal user,
            ISender sender,
            CancellationToken ct)
        {
            var userId = user.GetRequiredUserId();
            var command = new RemoveFriendCommand(userId, id);
            var result = await sender.Send(command, ct);
            return result.ToHttpResult();
        }
    }
}
