using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Users
{
    public sealed class User : Entity<UserId>
    {
        public string? GoogleId { get; private set; }
        public string? AppleId { get; private set; }
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string? Photo { get; private set; }
        public string? Email { get; private set; }
        public int Coins { get; private set; }
        public int Tokens { get; private set; }
        public int GamesWon { get; private set; }
        public int GamesLost { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? LastLoginAt { get; private set; }

        private readonly List<DeviceToken> _deviceTokens = new();
        public IReadOnlyCollection<DeviceToken> DeviceTokens => _deviceTokens.AsReadOnly();

        private User()
        {
        }

        private User(
            string? googleId,
            string? appleId,
            string? firstName,
            string? lastName,
            string? photo,
            string? email)
        {
            GoogleId = googleId;
            AppleId = appleId;
            FirstName = firstName;
            LastName = lastName;
            Photo = photo;
            Email = email;
            Coins = 0;
            Tokens = 50;
            GamesWon = 0;
            GamesLost = 0;
            CreatedAt = DateTime.UtcNow;
        }

        public static User CreateWithGoogle(
            string googleId,
            string? firstName,
            string? lastName,
            string? photo,
            string? email)
        {
            if (string.IsNullOrWhiteSpace(googleId))
                throw new ArgumentException("Google ID is required.", nameof(googleId));

            return new User(googleId, null, firstName, lastName, photo, email);
        }

        public static User CreateWithApple(
            string appleId,
            string? firstName,
            string? lastName,
            string? email)
        {
            if (string.IsNullOrWhiteSpace(appleId))
                throw new ArgumentException("Apple ID is required.", nameof(appleId));

            return new User(null, appleId, firstName, lastName, null, email);
        }

        public string FullName => $"{FirstName} {LastName}".Trim();

        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
        }

        public void LinkGoogleAccount(string googleId)
        {
            if (string.IsNullOrWhiteSpace(googleId))
                throw new ArgumentException("Google ID is required.", nameof(googleId));
            GoogleId = googleId;
        }

        public void LinkAppleAccount(string appleId)
        {
            if (string.IsNullOrWhiteSpace(appleId))
                throw new ArgumentException("Apple ID is required.", nameof(appleId));
            AppleId = appleId;
        }

        public void AddDeviceToken(string token, DevicePlatform platform)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token is required.", nameof(token));

            var existing = _deviceTokens.FirstOrDefault(t => t.Token == token);
            if (existing is not null)
            {
                _deviceTokens.Remove(existing);
            }

            _deviceTokens.Add(new DeviceToken(token, platform, DateTime.UtcNow));
        }

        public void RemoveDeviceToken(string token)
        {
            var existing = _deviceTokens.FirstOrDefault(t => t.Token == token);
            if (existing is not null)
            {
                _deviceTokens.Remove(existing);
            }
        }

        public void AddCoins(int amount)
        {
            if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
            Coins += amount;
        }

        public bool TrySpendTokens(int amount)
        {
            if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
            if (Tokens < amount) return false;
            Tokens -= amount;
            return true;
        }

        public void AddTokens(int amount)
        {
            if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
            Tokens += amount;
        }

        public void RecordWin() => GamesWon++;

        public void RecordLoss() => GamesLost++;

        public void UpdateProfile(string? firstName, string? lastName, string? photo)
        {
            FirstName = firstName;
            LastName = lastName;
            Photo = photo;
        }
    }
}