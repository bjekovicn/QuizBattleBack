using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Auth.Queries
{

    public sealed record GetActiveSessionsQuery(int UserId) : IQuery<IReadOnlyList<SessionResponse>>;

    internal sealed class GetActiveSessionsQueryHandler
        : IQueryHandler<GetActiveSessionsQuery, IReadOnlyList<SessionResponse>>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public GetActiveSessionsQueryHandler(IRefreshTokenRepository refreshTokenRepository)
        {
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<Result<IReadOnlyList<SessionResponse>>> Handle(
            GetActiveSessionsQuery query,
            CancellationToken cancellationToken)
        {
            var tokens = await _refreshTokenRepository.GetActiveByUserIdAsync(
                new UserId(query.UserId),
                cancellationToken);

            var sessions = tokens.Select(t => new SessionResponse(
                t.Id.Value,
                t.DeviceInfo,
                t.IpAddress,
                t.CreatedAt,
                t.ExpiresAt)).ToList();

            return Result.Success<IReadOnlyList<SessionResponse>>(sessions);
        }
    }

    public sealed record SessionResponse(
        Guid Id,
        string? DeviceInfo,
        string? IpAddress,
        DateTime CreatedAt,
        DateTime ExpiresAt);
}
