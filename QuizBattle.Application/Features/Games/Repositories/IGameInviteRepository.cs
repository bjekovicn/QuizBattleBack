using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Games.Repositories
{
    public interface IGameInviteRepository
    {
        Task<Result<GameInviteDto>> CreateInviteAsync(
            Guid roomId,
            int hostUserId,
            string hostDisplayName,
            string? hostPhotoUrl,
            int invitedUserId,
            string invitedDisplayName,
            string? invitedPhotoUrl,
            TimeSpan expiresIn,
            CancellationToken ct = default);

        Task<GameInviteDto?> GetInviteByIdAsync(Guid inviteId, CancellationToken ct = default);

        Task<List<GameInviteDto>> GetRoomInvitesAsync(Guid roomId, CancellationToken ct = default);

        Task<List<GameInviteDto>> GetUserPendingInvitesAsync(int userId, CancellationToken ct = default);

        Task<Result<GameInviteDto>> UpdateInviteStatusAsync(Guid inviteId, int newStatus, CancellationToken ct = default);

        Task<Result> CancelRoomInvitesAsync(Guid roomId, CancellationToken ct = default);

        Task<bool> InviteExistsAsync(Guid roomId, int invitedUserId, CancellationToken ct = default);

        Task<Result> DeleteInviteAsync(Guid inviteId, CancellationToken ct = default);
    }





}