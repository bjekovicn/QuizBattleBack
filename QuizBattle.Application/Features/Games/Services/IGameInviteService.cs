using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Services
{
    public interface IGameInviteService
    {
        Task<Result<GameInviteDto>> CreateInviteAsync(
            Guid roomId,
            int hostUserId,
            string hostDisplayName,
            string? hostPhotoUrl,
            int invitedUserId,
            CancellationToken ct = default);

        Task<Result<GameInviteDto>> RespondToInviteAsync(
            Guid inviteId,
            int userId,
            bool accept,
            CancellationToken ct = default);

        Task<Result<List<InviteStatusDto>>> GetRoomInviteStatusesAsync(
            Guid roomId,
            CancellationToken ct = default);

        Task<Result> CancelRoomInvitesAsync(Guid roomId, CancellationToken ct = default);

        Task<Result<List<GameInviteDto>>> GetUserPendingInvitesAsync(
            int userId,
            CancellationToken ct = default);
    }

}
