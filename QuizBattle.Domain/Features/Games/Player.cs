using QuizBattle.Domain.Features.Users;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Games
{
    public sealed class Player : ValueObject<Player>
    {
        public UserId UserId { get; }
        public string Color { get; }
        public int Score { get; }
        public string? Answer { get; }
        public TimeSpan? AnswerTime { get; }

        public Player(UserId userId, string color, int score, string? answer, TimeSpan? answerTime)
        {
            UserId = userId;
            Color = color;
            Score = score;
            Answer = answer;
            AnswerTime = answerTime;
        }


        public Player AddPoints(int points)
        {
            return new Player(UserId, Color, Score + points, Answer, AnswerTime);
        }


        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return UserId;
            yield return Color;
            yield return Score;
            yield return Answer;
            yield return AnswerTime;
        }
    }
}
