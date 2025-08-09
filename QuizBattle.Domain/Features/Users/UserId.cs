using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Users
{
    public sealed class UserId : ValueObject<UserId>
    {
        public int Value { get; }
        public UserId(int value)
        {
            if (value <= 0) throw new ArgumentException("User ID must be a positive integer.", nameof(value));
            Value = value;
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
