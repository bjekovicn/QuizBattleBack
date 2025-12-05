using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Games
{

    public sealed class PlayerColor : ValueObject<PlayerColor>
    {
        public string HexCode { get; }
        public string Name { get; }

        private PlayerColor(string hexCode, string name)
        {
            HexCode = hexCode;
            Name = name;
        }

        public static PlayerColor Red => new("#FF4038", "Red");
        public static PlayerColor Blue => new("#2279EE", "Blue");
        public static PlayerColor Yellow => new("#FFE716", "Yellow");
        public static PlayerColor Green => new("#2DF726", "Green");
        public static PlayerColor Purple => new("#9B59B6", "Purple");

        public static PlayerColor[] All => new[] { Red, Blue, Yellow, Green, Purple };

        public static PlayerColor GetByIndex(int index) => All[index % All.Length];

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return HexCode;
        }

        public override string ToString() => Name;
    }
}
