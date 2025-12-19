using Dapper;
using QuizBattle.Application.Features.Friendships;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Data;

namespace QuizBattle.Infrastructure.Features.Friendships.Repositories
{
    internal sealed class FriendshipQueryRepository : IFriendshipQueryRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public FriendshipQueryRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<IReadOnlyList<UserResponse>> GetFriendsAsync(int userId, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            SELECT DISTINCT
                u.user_id AS Id,
                u.google_id AS GoogleId,
                u.apple_id AS AppleId,
                u.first_name AS FirstName,
                u.last_name AS LastName,
                u.photo_url AS Photo,
                u.email AS Email,
                u.coins AS Coins,
                u.tokens AS Tokens,
                u.games_won AS GamesWon,
                u.games_lost AS GamesLost,
                u.created_at AS CreatedAt,
                u.last_login_at AS LastLoginAt
            FROM friendships f
            INNER JOIN users u ON (u.user_id = f.receiver_id OR u.user_id = f.sender_id)
            WHERE (f.sender_id = @UserId OR f.receiver_id = @UserId)
              AND u.user_id != @UserId
              AND f.status = 1
            ORDER BY u.first_name, u.last_name
            """;

            var users = await connection.QueryAsync<UserResponse>(sql, new { UserId = userId });
            return users.ToList();
        }

        public async Task<bool> ExistsAsync(int senderId, int receiverId, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = """
            SELECT EXISTS(
                SELECT 1 FROM friendships 
                WHERE (sender_id = @SenderId AND receiver_id = @ReceiverId)
                   OR (sender_id = @ReceiverId AND receiver_id = @SenderId)
            )
            """;

            return await connection.ExecuteScalarAsync<bool>(sql, new { SenderId = senderId, ReceiverId = receiverId });
        }
    }





}
