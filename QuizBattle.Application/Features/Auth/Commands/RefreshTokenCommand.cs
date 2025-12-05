using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Auth;
using QuizBattle.Domain.Shared.Abstractions;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Domain.Features.Auth;
using QuizBattle.Application.Features.Auth.Commands.DTOs;

namespace QuizBattle.Application.Features.Auth.Commands
{

    public sealed record RefreshTokenCommand(
        string AccessToken,
        string RefreshToken,
        string? DeviceInfo,
        string? IpAddress) : ICommand<AuthResponse>;

    internal sealed class RefreshTokenCommandHandler : ICommandHandlerMediatR<RefreshTokenCommand, AuthResponse>
    {
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ITokenHashService _tokenHashService;
        private readonly IUserQueryRepository _userQueryRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RefreshTokenCommandHandler(
            IJwtTokenService jwtTokenService,
            ITokenHashService tokenHashService,
            IUserQueryRepository userQueryRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork)
        {
            _jwtTokenService = jwtTokenService;
            _tokenHashService = tokenHashService;
            _userQueryRepository = userQueryRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<AuthResponse>> Handle(
            RefreshTokenCommand command,
            CancellationToken cancellationToken)
        {
            // 1. Validate expired access token to get user ID (don't validate lifetime)
            var userId = _jwtTokenService.ValidateAccessToken(command.AccessToken, validateLifetime: false);
            if (!userId.HasValue)
                return Result.Failure<AuthResponse>(Error.InvalidToken);

            // 2. Find refresh token by hash
            var refreshTokenHash = _tokenHashService.HashToken(command.RefreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(refreshTokenHash, cancellationToken);

            if (storedToken is null)
                return Result.Failure<AuthResponse>(Error.RefreshTokenNotFound);

            // 3. Validate token belongs to user
            if (storedToken.UserId.Value != userId.Value)
                return Result.Failure<AuthResponse>(Error.InvalidToken);

            // 4. Check if token was already used (TOKEN REUSE DETECTION)
            if (storedToken.IsRevoked)
            {
                // Security breach! Revoke ALL tokens for this user
                await _refreshTokenRepository.RevokeAllByUserIdAsync(
                    storedToken.UserId,
                    "Token reuse detected - potential security breach",
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return Result.Failure<AuthResponse>(Error.RefreshTokenReused);
            }

            // 5. Check if token is expired
            if (storedToken.IsExpired)
                return Result.Failure<AuthResponse>(Error.RefreshTokenExpired);

            // 6. Get user
            var user = await _userQueryRepository.GetByIdAsync(new UserId(userId.Value), cancellationToken);
            if (user is null)
                return Result.Failure<AuthResponse>(Error.UserNotFound);

            // 7. Generate new tokens (TOKEN ROTATION)
            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshTokenPlain = _jwtTokenService.GenerateRefreshToken();
            var newRefreshTokenHash = _tokenHashService.HashToken(newRefreshTokenPlain);

            // 8. Create new refresh token
            var newRefreshToken = RefreshToken.Create(
                storedToken.UserId,
                newRefreshTokenHash,
                _jwtTokenService.GetRefreshTokenExpirationDays(),
                command.DeviceInfo ?? storedToken.DeviceInfo,
                command.IpAddress ?? storedToken.IpAddress);

            // 9. Revoke old token and link to new one
            storedToken.Revoke("Replaced by new token", newRefreshToken.Id);
            _refreshTokenRepository.Update(storedToken);

            // 10. Save new token
            await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

            // 11. Commit transaction
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var expiresAt = DateTimeOffset.UtcNow
                .AddDays(_jwtTokenService.GetRefreshTokenExpirationDays())
                .ToUnixTimeSeconds();

            return Result.Success(new AuthResponse(
                user,
                newAccessToken,
                newRefreshTokenPlain,
                expiresAt));
        }
    }
}
