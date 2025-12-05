using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Domain.Features.Questions
{
    public sealed class Question : Entity<QuestionId>
    {
        public Language Language { get; private set; } = null!;
        public string Text { get; private set; } = string.Empty;
        public string AnswerA { get; private set; } = string.Empty;
        public string AnswerB { get; private set; } = string.Empty;
        public string AnswerC { get; private set; } = string.Empty;

        private Question()
        {
        }

        public Question(
            Language language,
            string text,
            string answerA,
            string answerB,
            string answerC)
        {
            Language = language ?? throw new ArgumentNullException(nameof(language));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            AnswerA = answerA ?? throw new ArgumentNullException(nameof(answerA));
            AnswerB = answerB ?? throw new ArgumentNullException(nameof(answerB));
            AnswerC = answerC ?? throw new ArgumentNullException(nameof(answerC));
        }

        public string CorrectAnswer => AnswerA;

        public bool IsCorrectAnswer(string answer) =>
            string.Equals(AnswerA, answer, StringComparison.OrdinalIgnoreCase);

        public void Update(Language language, string text, string answerA, string answerB, string answerC)
        {
            Language = language ?? throw new ArgumentNullException(nameof(language));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            AnswerA = answerA ?? throw new ArgumentNullException(nameof(answerA));
            AnswerB = answerB ?? throw new ArgumentNullException(nameof(answerB));
            AnswerC = answerC ?? throw new ArgumentNullException(nameof(answerC));
        }
    }
}