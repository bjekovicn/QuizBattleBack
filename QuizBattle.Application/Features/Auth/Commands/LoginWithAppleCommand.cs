using QuizBattle.Application.Features.Auth.Commands.DTOs;
using QuizBattle.Application.Features.Users;
using QuizBattle.Application.Shared.Abstractions.Auth;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Auth;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Auth.Commands
{
    public sealed record LoginWithAppleCommand(
        string IdToken,
        string? FirstName,
        string? LastName,
        string? DeviceToken,
        DevicePlatform? DevicePlatform,
        string? DeviceInfo,
        string? IpAddress) : ICommand<AuthResponse>;

    internal sealed class LoginWithAppleCommandHandler : ICommandHandlerMediatR<LoginWithAppleCommand, AuthResponse>
    {
        private readonly IAppleAuthService _appleAuthService;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly ITokenHashService _tokenHashService;
        private readonly IUserCommandRepository _userCommandRepository;
        private readonly IUserQueryRepository _userQueryRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LoginWithAppleCommandHandler(
            IAppleAuthService appleAuthService,
            IJwtTokenService jwtTokenService,
            ITokenHashService tokenHashService,
            IUserCommandRepository userCommandRepository,
            IUserQueryRepository userQueryRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork)
        {
            _appleAuthService = appleAuthService;
            _jwtTokenService = jwtTokenService;
            _tokenHashService = tokenHashService;
            _userCommandRepository = userCommandRepository;
            _userQueryRepository = userQueryRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<AuthResponse>> Handle(
            LoginWithAppleCommand command,
            CancellationToken cancellationToken)
        {
            // 1. Validate Apple ID token
            var appleResult = await _appleAuthService.ValidateIdTokenAsync(command.IdToken, cancellationToken);
            if (appleResult.IsFailure)
                return Result.Failure<AuthResponse>(appleResult.Error);

            var appleUser = appleResult.Value;

            // 2. Find or create user
            var existingUser = await _userCommandRepository.GetByAppleIdAsync(appleUser.AppleId, cancellationToken);

            User user;
            if (existingUser is null)
            {
                user = User.CreateWithApple(
                    appleUser.AppleId,
                    command.FirstName ?? appleUser.FirstName,
                    command.LastName ?? appleUser.LastName,
                    appleUser.Email);

                await _userCommandRepository.AddAsync(user, cancellationToken);
            }
            else
            {
                user = existingUser;
            }

            // Always update last login for both new and existing users
            user.UpdateLastLogin();

            // 3. Register device token if provided
            if (!string.IsNullOrWhiteSpace(command.DeviceToken) && command.DevicePlatform.HasValue)
            {
                user.AddDeviceToken(command.DeviceToken, command.DevicePlatform.Value);
            }

            // 4. Save user first (this generates user.Id for new users)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Generate refresh token AFTER user is persisted
            var refreshTokenPlain = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenHash = _tokenHashService.HashToken(refreshTokenPlain);

            // 6. Create and save refresh token with valid user.Id
            var refreshToken = RefreshToken.Create(
                user.Id,
                refreshTokenHash,
                _jwtTokenService.GetRefreshTokenExpirationDays(),
                command.DeviceInfo,
                command.IpAddress);

            await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 7. Get fresh user response with generated ID and all properties populated
            var finalUserResponse = await _userQueryRepository.GetByIdAsync(user.Id, cancellationToken);
            if (finalUserResponse is null)
                return Result.Failure<AuthResponse>(Error.UserNotFound);

            // 8. Generate access token with the complete user data
            var accessToken = _jwtTokenService.GenerateAccessToken(finalUserResponse);

            var expiresAt = DateTimeOffset.UtcNow
                .AddDays(_jwtTokenService.GetRefreshTokenExpirationDays())
                .ToUnixTimeSeconds();

            return Result.Success(new AuthResponse(
                finalUserResponse,
                accessToken,
                refreshTokenPlain,
                expiresAt));
        }
    }
}