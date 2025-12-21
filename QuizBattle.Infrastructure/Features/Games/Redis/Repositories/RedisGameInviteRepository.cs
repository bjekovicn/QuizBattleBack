using Microsoft.Extensions.Logging;
using QuizBattle.Application.Features.Games.RedisModels;
using QuizBattle.Application.Features.Games.Repositories;
using QuizBattle.Domain.Shared.Abstractions;
using StackExchange.Redis;
using System.Text.Json;

namespace QuizBattle.Infrastructure.Features.Games.Redis.Repositories
{

    internal sealed class RedisGameInviteRepository : IGameInviteRepository
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisGameInviteRepository> _logger;

        private static readonly TimeSpan DefaultInviteTtl = TimeSpan.FromMinutes(5);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public RedisGameInviteRepository(
            IConnectionMultiplexer redis,
            ILogger<RedisGameInviteRepository> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        private IDatabase Db => _redis.GetDatabase();

        private static string InviteKey(Guid inviteId) => $"game:invite:{inviteId}";
        private static string RoomInvitesKey(Guid roomId) => $"game:room:{roomId}:invites";
        private static string UserInvitesKey(int userId) => $"game:user:{userId}:invites";
        private static string RoomUserInviteKey(Guid roomId, int userId) => $"game:room:{roomId}:user:{userId}:invite";

        public async Task<Result<GameInviteDto>> CreateInviteAsync(
            Guid roomId,
            int hostUserId,
            string hostDisplayName,
            string? hostPhotoUrl,
            int invitedUserId,
            string invitedDisplayName,
            string? invitedPhotoUrl,
            TimeSpan expiresIn,
            CancellationToken ct = default)
        {
            var inviteId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var expiresAt = DateTimeOffset.UtcNow.Add(expiresIn).ToUnixTimeMilliseconds();

            var existingInviteId = await Db.StringGetAsync(RoomUserInviteKey(roomId, invitedUserId));
            if (!existingInviteId.IsNullOrEmpty)
            {
                return Result.Failure<GameInviteDto>(Error.InviteAlreadyExists);
            }

            var invite = new GameInviteDto
            {
                Id = inviteId.ToString(),
                RoomId = roomId.ToString(),
                HostUserId = hostUserId,
                HostDisplayName = hostDisplayName,
                HostPhotoUrl = hostPhotoUrl,
                InvitedUserId = invitedUserId,
                InvitedDisplayName = invitedDisplayName,
                InvitedPhotoUrl = invitedPhotoUrl,
                Status = (int)InviteStatus.Pending,
                SentAt = now,
                RespondedAt = null,
                ExpiresAt = expiresAt
            };

            var inviteJson = JsonSerializer.Serialize(invite, JsonOptions);

            await Db.StringSetAsync(InviteKey(inviteId), inviteJson, expiresIn);
            await Db.SetAddAsync(RoomInvitesKey(roomId), inviteId.ToString());
            await Db.KeyExpireAsync(RoomInvitesKey(roomId), expiresIn);
            await Db.SetAddAsync(UserInvitesKey(invitedUserId), inviteId.ToString());
            await Db.KeyExpireAsync(UserInvitesKey(invitedUserId), expiresIn);
            await Db.StringSetAsync(RoomUserInviteKey(roomId, invitedUserId), inviteId.ToString(), expiresIn);

            _logger.LogInformation(
                "Created invite {InviteId} for room {RoomId}, host {HostId} -> invited {InvitedId}",
                inviteId, roomId, hostUserId, invitedUserId);

            return Result.Success(invite);
        }

        public async Task<GameInviteDto?> GetInviteByIdAsync(Guid inviteId, CancellationToken ct = default)
        {
            var inviteJson = await Db.StringGetAsync(InviteKey(inviteId));
            if (inviteJson.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<GameInviteDto>(inviteJson!, JsonOptions);
        }

        public async Task<List<GameInviteDto>> GetRoomInvitesAsync(Guid roomId, CancellationToken ct = default)
        {
            var inviteIds = await Db.SetMembersAsync(RoomInvitesKey(roomId));
            var invites = new List<GameInviteDto>();

            foreach (var inviteId in inviteIds)
            {
                var invite = await GetInviteByIdAsync(Guid.Parse(inviteId!), ct);
                if (invite is not null)
                {
                    invites.Add(invite);
                }
            }

            return invites;
        }

        public async Task<List<GameInviteDto>> GetUserPendingInvitesAsync(int userId, CancellationToken ct = default)
        {
            var inviteIds = await Db.SetMembersAsync(UserInvitesKey(userId));
            var invites = new List<GameInviteDto>();

            foreach (var inviteId in inviteIds)
            {
                var invite = await GetInviteByIdAsync(Guid.Parse(inviteId!), ct);
                if (invite is not null && invite.Status == (int)InviteStatus.Pending)
                {
                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    if (now < invite.ExpiresAt)
                    {
                        invites.Add(invite);
                    }
                    else
                    {
                        await UpdateInviteStatusAsync(Guid.Parse(invite.Id), (int)InviteStatus.Expired, ct);
                    }
                }
            }

            return invites;
        }

        public async Task<Result<GameInviteDto>> UpdateInviteStatusAsync(
            Guid inviteId,
            int newStatus,
            CancellationToken ct = default)
        {
            var invite = await GetInviteByIdAsync(inviteId, ct);
            if (invite is null)
            {
                return Result.Failure<GameInviteDto>(Error.InviteNotFound);
            }

            invite.Status = newStatus;
            invite.RespondedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var inviteJson = JsonSerializer.Serialize(invite, JsonOptions);

            var ttl = await Db.KeyTimeToLiveAsync(InviteKey(inviteId));
            if (ttl.HasValue)
            {
                await Db.StringSetAsync(InviteKey(inviteId), inviteJson, ttl.Value);
            }
            else
            {
                await Db.StringSetAsync(InviteKey(inviteId), inviteJson, DefaultInviteTtl);
            }

            if (newStatus != (int)InviteStatus.Pending)
            {
                await Db.SetRemoveAsync(UserInvitesKey(invite.InvitedUserId), inviteId.ToString());
            }

            _logger.LogInformation(
                "Updated invite {InviteId} status to {Status}",
                inviteId, (InviteStatus)newStatus);

            return Result.Success(invite);
        }

        public async Task<Result> CancelRoomInvitesAsync(Guid roomId, CancellationToken ct = default)
        {
            var invites = await GetRoomInvitesAsync(roomId, ct);

            foreach (var invite in invites)
            {
                if (invite.Status == (int)InviteStatus.Pending)
                {
                    await UpdateInviteStatusAsync(Guid.Parse(invite.Id), (int)InviteStatus.Cancelled, ct);
                }
            }

            _logger.LogInformation("Cancelled all pending invites for room {RoomId}", roomId);
            return Result.Success();
        }

        public async Task<bool> InviteExistsAsync(Guid roomId, int invitedUserId, CancellationToken ct = default)
        {
            var inviteId = await Db.StringGetAsync(RoomUserInviteKey(roomId, invitedUserId));
            return !inviteId.IsNullOrEmpty;
        }

        public async Task<Result> DeleteInviteAsync(Guid inviteId, CancellationToken ct = default)
        {
            var invite = await GetInviteByIdAsync(inviteId, ct);
            if (invite is null)
            {
                return Result.Failure(Error.InviteNotFound);
            }

            await Db.KeyDeleteAsync(InviteKey(inviteId));
            await Db.SetRemoveAsync(RoomInvitesKey(Guid.Parse(invite.RoomId)), inviteId.ToString());
            await Db.SetRemoveAsync(UserInvitesKey(invite.InvitedUserId), inviteId.ToString());
            await Db.KeyDeleteAsync(RoomUserInviteKey(Guid.Parse(invite.RoomId), invite.InvitedUserId));

            _logger.LogInformation("Deleted invite {InviteId}", inviteId);
            return Result.Success();
        }
    }
}