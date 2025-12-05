using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Questions
{
    public sealed class QuestionId : ValueObject<QuestionId>, IEquatable<QuestionId>
    {
        public int Value { get; }

        public QuestionId(int value)
        {
            Value = value;
        }

        public static QuestionId Create(int value) => new(value);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();
    }
}