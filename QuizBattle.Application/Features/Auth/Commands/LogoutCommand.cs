using QuizBattle.Application.Shared.Abstractions.Auth;
using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Auth.Commands
{

    public sealed record LogoutCommand(string RefreshToken) : ICommand;

    internal sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
    {
        private readonly ITokenHashService _tokenHashService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;

        public LogoutCommandHandler(
            ITokenHashService tokenHashService,
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork)
        {
            _tokenHashService = tokenHashService;
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
        {
            var tokenHash = _tokenHashService.HashToken(command.RefreshToken);
            var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

            if (token is null)
                return Result.Failure(Error.RefreshTokenNotFound);

            if (!token.IsRevoked)
            {
                token.Revoke("User logged out");
                _refreshTokenRepository.Update(token);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
    }
}
