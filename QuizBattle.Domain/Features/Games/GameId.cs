using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Games
{
    public sealed class GameId : ValueObject
    {
        public Guid Value { get; }
        public GameId(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException("Game ID cannot be empty.", nameof(value));
            Value = value;
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
