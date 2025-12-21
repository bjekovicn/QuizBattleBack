using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Friendships;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Application.Features.Games.Services;
using QuizBattle.Application.Features.Users;
using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Infrastructure.Features.Games.Services
{

    internal sealed class GameInviteService : IGameInviteService
    {
        private readonly IGameInviteRepository _inviteRepository;
        private readonly IFriendshipQueryRepository _friendshipRepository;
        private readonly IUserQueryRepository _userRepository;
        private readonly ILogger<GameInviteService> _logger;

        private static readonly TimeSpan DefaultInviteExpiration = TimeSpan.FromMinutes(5);

        public GameInviteService(
            IGameInviteRepository inviteRepository,
            IFriendshipQueryRepository friendshipRepository,
            IUserQueryRepository userRepository,
            ILogger<GameInviteService> logger)
        {
            _inviteRepository = inviteRepository;
            _friendshipRepository = friendshipRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<Result<GameInviteDto>> CreateInviteAsync(
            Guid roomId,
            int hostUserId,
            string hostDisplayName,
            string? hostPhotoUrl,
            int invitedUserId,
            CancellationToken ct = default)
        {
            _logger.LogInformation(
                "Creating invite for room {RoomId}, host {HostId} -> invited {InvitedId}",
                roomId, hostUserId, invitedUserId);

            if (hostUserId == invitedUserId)
            {
                return Result.Failure<GameInviteDto>(Error.CannotInviteSelf);
            }

            // Check if they are friends
            var friends = await _friendshipRepository.GetFriendsAsync(hostUserId, ct);
            var areFriends = friends.Any(f => f.Id == invitedUserId);

            if (!areFriends)
            {
                _logger.LogWarning(
                    "User {HostId} tried to invite non-friend {InvitedId}",
                    hostUserId, invitedUserId);
                return Result.Failure<GameInviteDto>(Error.NotFriends);
            }

            var inviteExists = await _inviteRepository.InviteExistsAsync(roomId, invitedUserId, ct);
            if (inviteExists)
            {
                return Result.Failure<GameInviteDto>(Error.InviteAlreadyExists);
            }

            var invitedUser = await _userRepository.GetByIdAsync(new UserId(invitedUserId), ct);
            if (invitedUser is null)
            {
                return Result.Failure<GameInviteDto>(Error.UserNotFound);
            }

            var result = await _inviteRepository.CreateInviteAsync(
                roomId,
                hostUserId,
                hostDisplayName,
                hostPhotoUrl,
                invitedUserId,
                invitedUser.FullName,
                invitedUser.Photo,
                DefaultInviteExpiration,
                ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully created invite {InviteId}", result.Value.Id);
            }

            return result;
        }

        public async Task<Result<GameInviteDto>> RespondToInviteAsync(
            Guid inviteId,
            int userId,
            bool accept,
            CancellationToken ct = default)
        {
            _logger.LogInformation(
                "User {UserId} responding to invite {InviteId}: {Response}",
                userId, inviteId, accept ? "ACCEPT" : "REJECT");

            var invite = await _inviteRepository.GetInviteByIdAsync(inviteId, ct);
            if (invite is null)
            {
                return Result.Failure<GameInviteDto>(Error.InviteNotFound);
            }

            if (invite.InvitedUserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} tried to respond to invite {InviteId} meant for {InvitedId}",
                    userId, inviteId, invite.InvitedUserId);
                return Result.Failure<GameInviteDto>(Error.Unauthorized);
            }

            if (invite.Status != (int)InviteStatus.Pending)
            {
                return Result.Failure<GameInviteDto>(Error.InviteAlreadyResponded);
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (now > invite.ExpiresAt)
            {
                await _inviteRepository.UpdateInviteStatusAsync(inviteId, (int)InviteStatus.Expired, ct);
                return Result.Failure<GameInviteDto>(Error.InviteExpired);
            }

            var newStatus = accept ? InviteStatus.Accepted : InviteStatus.Rejected;
            var result = await _inviteRepository.UpdateInviteStatusAsync(inviteId, (int)newStatus, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "User {UserId} {Response} invite {InviteId}",
                    userId, accept ? "accepted" : "rejected", inviteId);
            }

            return result;
        }

        public async Task<Result<List<InviteStatusDto>>> GetRoomInviteStatusesAsync(
            Guid roomId,
            CancellationToken ct = default)
        {
            var invites = await _inviteRepository.GetRoomInvitesAsync(roomId, ct);

            var statuses = invites.Select(i => new InviteStatusDto(
                i.InvitedUserId,
                i.InvitedDisplayName,
                i.InvitedPhotoUrl,
                i.Status
            )).ToList();

            return Result.Success(statuses);
        }

        public async Task<Result> CancelRoomInvitesAsync(Guid roomId, CancellationToken ct = default)
        {
            _logger.LogInformation("Cancelling all invites for room {RoomId}", roomId);
            return await _inviteRepository.CancelRoomInvitesAsync(roomId, ct);
        }

        public async Task<Result<List<GameInviteDto>>> GetUserPendingInvitesAsync(
            int userId,
            CancellationToken ct = default)
        {
            var invites = await _inviteRepository.GetUserPendingInvitesAsync(userId, ct);
            return Result.Success(invites);
        }
    }
}
