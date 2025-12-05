
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Abstractions.Auth
{

    public interface IGoogleAuthService
    {
        Task<Result<GoogleUserInfo>> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);
    }

    public sealed record GoogleUserInfo(
        string GoogleId,
        string Email,
        string? FirstName,
        string? LastName,
        string? PhotoUrl);
}
