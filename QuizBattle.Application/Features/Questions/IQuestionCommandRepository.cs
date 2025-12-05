using QuizBattle.Application.Shared.Abstractions.Repositories;
using QuizBattle.Domain.Features.Questions;

namespace QuizBattle.Application.Features.Questions
{
    public interface IQuestionCommandRepository : ICommandRepository<Question, QuestionId>
    {
        Task AddRangeAsync(IEnumerable<Question> entities, CancellationToken cancellationToken = default);
    }

}
