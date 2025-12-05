using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Auth
{
    public sealed class RefreshTokenId : ValueObject<RefreshTokenId>
    {
        public Guid Value { get; }

        public RefreshTokenId(Guid value)
        {
            Value = value;
        }

        public static RefreshTokenId NewId() => new(Guid.NewGuid());
        public static RefreshTokenId Create(Guid value) => new(value);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();
    }
}