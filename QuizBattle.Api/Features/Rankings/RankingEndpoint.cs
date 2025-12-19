using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuizBattle.Api.Shared.Abstractions;
using QuizBattle.Api.Shared.Extensions;
using QuizBattle.Application.Features.Users.Queries;

namespace QuizBattle.Api.Features.Rankings
{
    public sealed class RankingEndpoints : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/rankings")
                .WithTags("Rankings")
                .RequireRateLimiting("api");

            group.MapGet("/all-time", GetAllTimeLeaderboard)
                .WithName("GetAllTimeLeaderboard")
                .WithOpenApi();
        }

        private static async Task<IResult> GetAllTimeLeaderboard(
            [FromQuery] int? take,
            ISender sender,
            CancellationToken ct)
        {
            var query = new GetLeaderboardQuery(take ?? 100);
            var result = await sender.Send(query, ct);
            return result.ToHttpResult();
        }
    }
}
