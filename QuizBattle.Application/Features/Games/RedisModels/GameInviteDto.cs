namespace QuizBattle.Application.Features.Games.RedisModels
{
    public sealed class GameInviteDto
    {
        public string Id { get; set; } = string.Empty;
        public string RoomId { get; set; } = string.Empty;
        public int HostUserId { get; set; }
        public string HostDisplayName { get; set; } = string.Empty;
        public string? HostPhotoUrl { get; set; }
        public int InvitedUserId { get; set; }
        public string InvitedDisplayName { get; set; } = string.Empty;
        public string? InvitedPhotoUrl { get; set; }
        public int Status { get; set; }
        public long SentAt { get; set; }
        public long? RespondedAt { get; set; }
        public long ExpiresAt { get; set; }
    }

    public sealed record InviteStatusDto(
        int FriendId,
        string FriendName,
        string? FriendPhotoUrl,
        int Status);
}
