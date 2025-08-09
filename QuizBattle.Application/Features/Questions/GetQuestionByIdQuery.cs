using QuizBattle.Application.Shared.Generics.Queries;
using QuizBattle.Domain.Features.Questions;

namespace QuizBattle.Application.Features.Questions
{
    public sealed record GetQuestionByIdQuery(QuestionId QuestionId) : GetByIdQuery<Question, QuestionId>(QuestionId);
}
