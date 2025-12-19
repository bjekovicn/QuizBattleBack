using QuizBattle.Application.Features.Questions;
using QuizBattle.Domain.Features.Questions;
using QuizBattle.Infrastructure.Shared.Persistence;
using Microsoft.EntityFrameworkCore;

namespace QuizBattle.Infrastructure.Features.Questions
{
    internal sealed class QuestionCommandRepository : IQuestionCommandRepository
    {
        private readonly AppDbContext _dbContext;

        public QuestionCommandRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(Question entity, CancellationToken cancellationToken = default)
        {
            await _dbContext.Questions.AddAsync(entity, cancellationToken);
        }

        public async Task AddRangeAsync(IEnumerable<Question> entities, CancellationToken cancellationToken = default)
        {
            await _dbContext.Questions.AddRangeAsync(entities, cancellationToken);
        }

        public void Delete(Question entity)
        {
            _dbContext.Questions.Remove(entity);
        }

        public async Task<Question?> GetByIdAsync(
            QuestionId id,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Questions
                .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);
        }

        public void Update(Question entity)
        {
            _dbContext.Questions.Update(entity);
        }
    }
}
