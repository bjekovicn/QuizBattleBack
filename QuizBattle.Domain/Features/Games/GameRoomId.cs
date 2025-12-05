using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Games
{

    public sealed class GameRoomId : ValueObject<GameRoomId>, IEquatable<GameRoomId>
    {
        public Guid Value { get; }

        private GameRoomId(Guid value, bool skipValidation)
        {
            Value = value;
        }

        public GameRoomId(Guid value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("GameRoom ID cannot be empty.", nameof(value));
            Value = value;
        }

        public static GameRoomId Empty => new(Guid.Empty, skipValidation: true);
        public static GameRoomId NewId() => new(Guid.NewGuid());
        public static GameRoomId Create(Guid value) => new(value);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }

        public override string ToString() => Value.ToString();
    }
}
