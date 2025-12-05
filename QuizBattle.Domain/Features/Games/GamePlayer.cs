using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Games
{

    public sealed class GamePlayer : Entity<UserId>
    {
        public string DisplayName { get; private set; }
        public string? PhotoUrl { get; private set; }
        public PlayerColor Color { get; private set; }
        public int TotalScore { get; private set; }
        public int CurrentRoundScore { get; private set; }
        public bool IsReady { get; private set; }
        public bool IsConnected { get; private set; }
        public PlayerAnswer? CurrentAnswer { get; private set; }
        public DateTime JoinedAt { get; private set; }

        private GamePlayer() : base()
        {
            DisplayName = string.Empty;
            Color = PlayerColor.Red;
        }

        public GamePlayer(
            UserId userId,
            string displayName,
            string? photoUrl,
            PlayerColor color) : base(userId)
        {
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            PhotoUrl = photoUrl;
            Color = color ?? throw new ArgumentNullException(nameof(color));
            TotalScore = 0;
            CurrentRoundScore = 0;
            IsReady = false;
            IsConnected = true;
            CurrentAnswer = null;
            JoinedAt = DateTime.UtcNow;
        }

        public void SetReady(bool isReady) => IsReady = isReady;

        public void SetConnected(bool isConnected) => IsConnected = isConnected;

        public void SubmitAnswer(string answer, TimeSpan responseTime)
        {
            if (CurrentAnswer is not null)
                throw new InvalidOperationException("Player has already answered this round.");

            CurrentAnswer = new PlayerAnswer(answer, responseTime, DateTime.UtcNow);
        }

        public void AwardPoints(int points)
        {
            if (points < 0)
                throw new ArgumentException("Points cannot be negative.", nameof(points));

            CurrentRoundScore = points;
            TotalScore += points;
        }

        public void PrepareForNextRound()
        {
            CurrentAnswer = null;
            CurrentRoundScore = 0;
        }

        public bool HasAnswered => CurrentAnswer is not null;

        public bool HasCorrectAnswer(string correctAnswer) =>
            CurrentAnswer is not null &&
            string.Equals(CurrentAnswer.Answer, correctAnswer, StringComparison.OrdinalIgnoreCase);
    }
}
