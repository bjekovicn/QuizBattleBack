using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Users
{
    public sealed class UserId : ValueObject<UserId>, IEquatable<UserId>
    {
        public int Value { get; }

        public UserId(int value)
        {
            Value = value;
        }

        public static UserId Create(int value) => new(value);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();
    }
}