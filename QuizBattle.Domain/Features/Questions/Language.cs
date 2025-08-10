using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Questions
{
    public sealed class Language : ValueObject<Language>
    {
        public string Code { get; }
        public static Language Serbian => new("sr");
        public static Language English => new("en");

        public Language(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentNullException(nameof(code));
            Code = code;
        }
        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Code;
        }
    }
}
