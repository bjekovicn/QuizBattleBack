using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Users
{
    public sealed class DeviceToken : ValueObject<DeviceToken>
    {
        public string Token { get; }
        public DevicePlatform? Platform { get; }
        public DateTime CreatedAt { get; }

        private DeviceToken()
        {
            Token = string.Empty;
            Platform = null;
            CreatedAt = DateTime.UtcNow;
        }

        public DeviceToken(
            string token,
            DevicePlatform? platform = null,
            DateTime? createdAt = null)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            Platform = platform;
            CreatedAt = createdAt ?? DateTime.UtcNow;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Token;
            yield return Platform;
        }
    }

}
