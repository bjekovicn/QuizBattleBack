using Dapper;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Domain.Features.Users;
namespace QuizBattle.Infrastructure.Features.Users
{
    internal sealed class UserQueryRepository : IUserQueryRepository
    {
        private readonly ISqlConnectionFactory _connectionFactory;

        public UserQueryRepository(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        private const string SelectColumns = """
        user_id AS Id,
        google_id AS GoogleId,
        apple_id AS AppleId,
        first_name AS FirstName,
        last_name AS LastName,
        photo_url AS Photo,
        email AS Email,
        coins AS Coins,
        tokens AS Tokens,
        games_won AS GamesWon,
        games_lost AS GamesLost,
        created_at AS CreatedAt,
        last_login_at AS LastLoginAt
        """;

        public async Task<UserResponse?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = $"SELECT {SelectColumns} FROM users WHERE user_id = @UserId";

            return await connection.QueryFirstOrDefaultAsync<UserResponse>(sql, new { UserId = id.Value });
        }

        public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await GetAllAsync(null, null, cancellationToken);
        }

        public async Task<IReadOnlyList<UserResponse>> GetAllAsync(
            int? skip,
            int? take,
            CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = $"""
            SELECT {SelectColumns} 
            FROM users 
            ORDER BY user_id
            """;

            if (take.HasValue)
            {
                sql += " LIMIT @Take";
                if (skip.HasValue)
                    sql += " OFFSET @Skip";
            }

            var users = await connection.QueryAsync<UserResponse>(sql, new { Skip = skip, Take = take });
            return users.ToList();
        }

        public async Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string sql = "SELECT EXISTS(SELECT 1 FROM users WHERE user_id = @UserId)";

            return await connection.ExecuteScalarAsync<bool>(sql, new { UserId = id.Value });
        }

        public async Task<UserResponse?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = $"SELECT {SelectColumns} FROM users WHERE google_id = @GoogleId";

            return await connection.QueryFirstOrDefaultAsync<UserResponse>(sql, new { GoogleId = googleId });
        }

        public async Task<UserResponse?> GetByAppleIdAsync(string appleId, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = $"SELECT {SelectColumns} FROM users WHERE apple_id = @AppleId";

            return await connection.QueryFirstOrDefaultAsync<UserResponse>(sql, new { AppleId = appleId });
        }

        public async Task<IReadOnlyList<UserResponse>> GetLeaderboardAsync(
            int take = 10,
            CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            var sql = $"""
            SELECT {SelectColumns}
            FROM users
            ORDER BY games_won DESC, coins DESC
            LIMIT @Take
            """;

            var users = await connection.QueryAsync<UserResponse>(sql, new { Take = take });
            return users.ToList();
        }
        public async Task<UserWithTokensResponse?> GetByIdWithTokensAsync(UserId id, CancellationToken cancellationToken = default)
        {
            using var connection = _connectionFactory.CreateConnection();

            const string userSql = $"""
        SELECT 
            user_id AS Id,
            google_id AS GoogleId,
            apple_id AS AppleId,
            first_name AS FirstName,
            last_name AS LastName,
            photo_url AS Photo,
            email AS Email,
            coins AS Coins,
            tokens AS Tokens,
            games_won AS GamesWon,
            games_lost AS GamesLost,
            created_at AS CreatedAt,
            last_login_at AS LastLoginAt
        FROM users
        WHERE user_id = @UserId
        """;

            const string tokensSql = """
        SELECT 
            token AS Token,
            platform AS Platform
        FROM user_device_tokens
        WHERE user_id = @UserId
        """;

            var user = await connection.QueryFirstOrDefaultAsync<UserWithTokensResponse>(userSql, new { UserId = id.Value });
            if (user is null) return null;

            var tokens = await connection.QueryAsync<DeviceTokenResponse>(tokensSql, new { UserId = id.Value });

            return user with { DeviceTokens = tokens.ToList() };
        }
    }
}
