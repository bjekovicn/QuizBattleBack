using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Users
{

    public sealed class DeviceToken : ValueObject<DeviceToken>
    {
        public string Token { get; }
        public DevicePlatform Platform { get; }
        public DateTime CreatedAt { get; }

        private DeviceToken()
        {
            Token = string.Empty;
            Platform = DevicePlatform.Android;
            CreatedAt = DateTime.UtcNow;
        }

        public DeviceToken(string token, DevicePlatform platform, DateTime createdAt)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            Platform = platform;
            CreatedAt = createdAt;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Token;
            yield return Platform;
        }
    }
}
