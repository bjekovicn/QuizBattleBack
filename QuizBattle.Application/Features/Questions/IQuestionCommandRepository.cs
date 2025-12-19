using QuizBattle.Domain.Features.Questions;

namespace QuizBattle.Application.Features.Questions
{
    public interface IQuestionCommandRepository
    {
        Task AddAsync(Question entity, CancellationToken cancellationToken = default);
        void Update(Question entity);
        void Delete(Question entity);
        Task<Question?> GetByIdAsync(QuestionId id, CancellationToken cancellationToken = default);
        Task AddRangeAsync(IEnumerable<Question> entities, CancellationToken cancellationToken = default);
    }

}
