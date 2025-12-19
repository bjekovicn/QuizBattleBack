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

    public sealed record UserWithTokensResponse(
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
    DateTime? LastLoginAt,
    List<DeviceTokenResponse>? DeviceTokens);

    public sealed record DeviceTokenResponse(
        string Token,
        DevicePlatform Platform);
}
