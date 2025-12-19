using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuizBattle.Application.Shared.Abstractions.Auth;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Infrastructure.Features.Auth.Services
{
    internal sealed class GoogleAuthService : IGoogleAuthService
    {
        private readonly string _clientId;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger)
        {
            _clientId = configuration["Google:ClientId"]
                ?? throw new InvalidOperationException("Google:ClientId not configured.");
            _logger = logger;
        }

        public async Task<Result<GoogleUserInfo>> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                var userInfo = new GoogleUserInfo(
                    payload.Subject,
                    payload.Email,
                    payload.GivenName,
                    payload.FamilyName,
                    payload.Picture);

                return Result.Success(userInfo);
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Invalid Google ID token");
                return Result.Failure<GoogleUserInfo>(Error.InvalidGoogleToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google ID token");
                return Result.Failure<GoogleUserInfo>(Error.InvalidGoogleToken);
            }
        }
    }
}
