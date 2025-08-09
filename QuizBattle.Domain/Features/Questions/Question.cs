using QuizBattle.Domain.Features.Questions;
using QuizBattle.Domain.Shared.Abstractions;
public sealed class Question : Entity<QuestionId>
{
    public Language Language { get; private set; }
    public string Text { get; private set; }
    public string AnswerA { get; private set; }
    public string AnswerB { get; private set; }
    public string AnswerC { get; private set; }

    private Question() : base(new QuestionId(0))
    {
        Language = Language.Serbian;
        Text = string.Empty;
        AnswerA = string.Empty;
        AnswerB = string.Empty;
        AnswerC = string.Empty;
    }
    public Question(QuestionId id, Language language, string text, string answerA, string answerB, string answerC) : base(id)
    {
        Language = language;
        Text = text;
        AnswerA = answerA;
        AnswerB = answerB;
        AnswerC = answerC;
    }
}