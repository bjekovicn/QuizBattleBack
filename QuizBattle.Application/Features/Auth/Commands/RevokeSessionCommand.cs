using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Auth;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Auth.Commands
{

    public sealed record RevokeSessionCommand(int UserId, Guid SessionId) : ICommand;

    internal sealed class RevokeSessionCommandHandler : ICommandHandlerMediatR<RevokeSessionCommand>
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RevokeSessionCommandHandler(
            IRefreshTokenRepository refreshTokenRepository,
            IUnitOfWork unitOfWork)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
        {
            var token = await _refreshTokenRepository.GetByIdAsync(
                RefreshTokenId.Create(command.SessionId),
                cancellationToken);

            if (token is null)
                return Result.Failure(Error.RefreshTokenNotFound);

            // Security: Ensure user can only revoke their own sessions
            if (token.UserId.Value != command.UserId)
                return Result.Failure(Error.Unauthorized);

            if (!token.IsRevoked)
            {
                token.Revoke("Session revoked by user");
                _refreshTokenRepository.Update(token);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Result.Success();
        }
    }
}
