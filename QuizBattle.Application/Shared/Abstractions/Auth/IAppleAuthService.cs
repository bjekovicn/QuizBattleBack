using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Abstractions.Auth
{
    public interface IAppleAuthService
    {
        Task<Result<AppleUserInfo>> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
    }

    public sealed record AppleUserInfo(
        string AppleId,
        string? Email,
        string? FirstName,
        string? LastName);
}
