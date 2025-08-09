using QuizBattle.Application.Shared.Generics.Commands;
using QuizBattle.Domain.Features.Questions;

namespace QuizBattle.Application.Features.Questions.CreateQuestion
{
    public sealed record CreateQuestionCommand(
        string Language,
        string Text,
        string AnswerA,
        string AnswerB,
        string AnswerC) : CreateCommand<Question, QuestionId>
    {
        public override Question ToDomainModel()
        {
            var questionId = new QuestionId(0);
            var language = new Language(Language);

            return new Question(
                questionId,
                language,
                Text,
                AnswerA,
                AnswerB,
                AnswerC
            );
        }
    }
}

