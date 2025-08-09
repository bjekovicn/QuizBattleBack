using QuizBattle.Application.Shared.Generics.Commands;
using QuizBattle.Domain.Features.Questions;

namespace QuizBattle.Application.Features.Questions
{
    public sealed record DeleteQuestionCommand(QuestionId QuestionId) : DeleteCommand<QuestionId>(QuestionId);
}
