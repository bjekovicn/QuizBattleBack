using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Questions
{
    public sealed class QuestionId : ValueObject<QuestionId>
    {
        public int Value { get; }
        public QuestionId(int value)
        {
            if (value <= 0) throw new ArgumentException("Question ID must be a positive integer.", nameof(value));
            Value = value;
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
