using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using QuizBattle.Application.Shared.Abstractions.Auth;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Infrastructure.Features.Auth.Services
{

    internal sealed class AppleAuthService : IAppleAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientId;
        private readonly ILogger<AppleAuthService> _logger;
        private const string AppleKeysUrl = "https://appleid.apple.com/auth/keys";

        public AppleAuthService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AppleAuthService> logger)
        {
            _httpClient = httpClient;
            _clientId = configuration["Apple:ClientId"]
                ?? throw new InvalidOperationException("Apple:ClientId not configured.");
            _logger = logger;
        }

        public async Task<Result<AppleUserInfo>> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
        {
            try
            {
                // Get Apple's public keys
                var keysResponse = await _httpClient.GetFromJsonAsync<AppleKeysResponse>(AppleKeysUrl, ct);
                if (keysResponse?.Keys is null || keysResponse.Keys.Count == 0)
                {
                    return Result.Failure<AppleUserInfo>(Error.InvalidAppleToken);
                }

                // Parse token header to get key ID
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(idToken);
                var kid = jwtToken.Header.Kid;

                // Find matching key
                var appleKey = keysResponse.Keys.FirstOrDefault(k => k.Kid == kid);
                if (appleKey is null)
                {
                    return Result.Failure<AppleUserInfo>(Error.InvalidAppleToken);
                }

                // Build RSA key from Apple's public key
                var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters
                {
                    Modulus = Base64UrlDecode(appleKey.N),
                    Exponent = Base64UrlDecode(appleKey.E)
                });

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "https://appleid.apple.com",
                    ValidateAudience = true,
                    ValidAudience = _clientId,
                    ValidateLifetime = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa)
                };

                var principal = tokenHandler.ValidateToken(idToken, validationParameters, out _);

                var appleId = principal.FindFirst("sub")?.Value
                    ?? throw new InvalidOperationException("Apple ID not found in token");
                var email = principal.FindFirst("email")?.Value;

                var userInfo = new AppleUserInfo(appleId, email, null, null);

                return Result.Success(userInfo);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "Invalid Apple ID token");
                return Result.Failure<AppleUserInfo>(Error.InvalidAppleToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Apple ID token");
                return Result.Failure<AppleUserInfo>(Error.InvalidAppleToken);
            }
        }

        private static byte[] Base64UrlDecode(string input)
        {
            var output = input
                .Replace('-', '+')
                .Replace('_', '/');

            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }

            return Convert.FromBase64String(output);
        }

        private sealed class AppleKeysResponse
        {
            public List<AppleKey> Keys { get; set; } = new();
        }

        private sealed class AppleKey
        {
            public string Kty { get; set; } = string.Empty;
            public string Kid { get; set; } = string.Empty;
            public string Use { get; set; } = string.Empty;
            public string Alg { get; set; } = string.Empty;
            public string N { get; set; } = string.Empty;
            public string E { get; set; } = string.Empty;
        }
    }
}
