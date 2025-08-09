using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Application.Shared.Generics.Queries;
using QuizBattle.Domain.Features.Questions;

namespace QuizBattle.Application.Features.Questions
{
    public sealed record GetAllQuestionsQuery() : GetAllQuery<Question, QuestionId>, IQuery<IReadOnlyList<Question>>;
}
