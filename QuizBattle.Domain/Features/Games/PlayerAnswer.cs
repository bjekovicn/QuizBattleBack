using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Games
{

    public sealed class PlayerAnswer : ValueObject<PlayerAnswer>
    {
        public string Answer { get; }
        public TimeSpan ResponseTime { get; }
        public DateTime AnsweredAt { get; }

        private PlayerAnswer()
        {
            Answer = string.Empty;
            ResponseTime = TimeSpan.Zero;
            AnsweredAt = DateTime.UtcNow;
        }

        public PlayerAnswer(string answer, TimeSpan responseTime, DateTime answeredAt)
        {
            Answer = answer ?? throw new ArgumentNullException(nameof(answer));
            ResponseTime = responseTime;
            AnsweredAt = answeredAt;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Answer;
            yield return ResponseTime;
            yield return AnsweredAt;
        }
    }
}
