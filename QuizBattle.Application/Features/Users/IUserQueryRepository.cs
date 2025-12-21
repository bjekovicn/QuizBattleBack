using System.Security.Cryptography;
using QuizBattle.Domain.Features.Users;

namespace QuizBattle.Application.Features.Users
{

    public interface IUserQueryRepository 
    {
        Task<UserResponse?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(UserId id, CancellationToken cancellationToken = default);
        Task<UserResponse?> GetByGoogleIdAsync(string googleId, CancellationToken cancellationToken = default);
        Task<UserResponse?> GetByAppleIdAsync(string appleId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserResponse>> GetLeaderboardAsync(int take = 10, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserResponse>> GetAllAsync(int? skip, int? take, CancellationToken cancellationToken = default);
        Task<UserWithTokensResponse?> GetByIdWithTokensAsync(UserId id, CancellationToken cancellationToken = default);
    }

    public sealed record UserResponse(
        int Id,
        string? GoogleId,
        string? AppleId,
        string? FirstName,
        string? LastName,
        string? Photo,
        string? Email,
        int Coins,
        int Tokens,
        int GamesWon,
        int GamesLost,
        DateTime CreatedAt,
        DateTime? LastLoginAt)
    {
        public string FullName => $"{FirstName} {LastName}".Trim();
        public int TotalGames => GamesWon + GamesLost;
        public double WinRate => TotalGames > 0 ? (double)GamesWon / TotalGames * 100 : 0;
    }

    public sealed record UserWithTokensResponse
    {
        public int Id { get; init; }
        public string? GoogleId { get; init; }
        public string? AppleId { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Photo { get; init; }
        public string? Email { get; init; }
        public int Coins { get; init; }
        public int Tokens { get; init; }
        public int GamesWon { get; init; }
        public int GamesLost { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
        public List<DeviceTokenResponse>? DeviceTokens { get; init; }

        // Parameterless constructor for Dapper
        public UserWithTokensResponse() { }

        // Full constructor
        public UserWithTokensResponse(
            int id,
            string? googleId,
            string? appleId,
            string? firstName,
            string? lastName,
            string? photo,
            string? email,
            int coins,
            int tokens,
            int gamesWon,
            int gamesLost,
            DateTime createdAt,
            DateTime? lastLoginAt,
            List<DeviceTokenResponse>? deviceTokens)
        {
            Id = id;
            GoogleId = googleId;
            AppleId = appleId;
            FirstName = firstName;
            LastName = lastName;
            Photo = photo;
            Email = email;
            Coins = coins;
            Tokens = tokens;
            GamesWon = gamesWon;
            GamesLost = gamesLost;
            CreatedAt = createdAt;
            LastLoginAt = lastLoginAt;
            DeviceTokens = deviceTokens;
        }
    }

    public sealed record DeviceTokenResponse(
        string Token,
        DevicePlatform Platform);
}
