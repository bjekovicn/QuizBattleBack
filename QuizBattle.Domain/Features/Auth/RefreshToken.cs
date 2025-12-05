using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Auth
{
    public sealed class RefreshToken : Entity<RefreshTokenId>
    {
        public UserId UserId { get; private set; } = null!;
        public string TokenHash { get; private set; } = string.Empty;
        public string? DeviceInfo { get; private set; }
        public string? IpAddress { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public RefreshTokenId? ReplacedByTokenId { get; private set; }
        public string? RevokedReason { get; private set; }

        private RefreshToken()
        {
        }

        private RefreshToken(
            RefreshTokenId id,
            UserId userId,
            string tokenHash,
            DateTime expiresAt,
            string? deviceInfo,
            string? ipAddress) : base(id)
        {
            UserId = userId;
            TokenHash = tokenHash ?? throw new ArgumentNullException(nameof(tokenHash));
            DeviceInfo = deviceInfo;
            IpAddress = ipAddress;
            CreatedAt = DateTime.UtcNow;
            ExpiresAt = expiresAt;
        }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt.HasValue;
        public bool IsActive => !IsRevoked && !IsExpired;

        public void Revoke(string? reason = null, RefreshTokenId? replacedByTokenId = null)
        {
            if (IsRevoked)
                return;

            RevokedAt = DateTime.UtcNow;
            RevokedReason = reason;
            ReplacedByTokenId = replacedByTokenId;
        }

        public static RefreshToken Create(
            UserId userId,
            string tokenHash,
            int expirationDays,
            string? deviceInfo = null,
            string? ipAddress = null)
        {
            return new RefreshToken(
                RefreshTokenId.NewId(), 
                userId,
                tokenHash,
                DateTime.UtcNow.AddDays(expirationDays),
                deviceInfo,
                ipAddress);
        }
    }
}