using QuizBattle.Application.Shared.Abstractions.Repositories;
using QuizBattle.Domain.Features.Questions;

namespace QuizBattle.Application.Features.Questions
{

    public interface IQuestionQueryRepository : IQueryRepository<QuestionResponse, QuestionId>
    {
        Task<IReadOnlyList<QuestionResponse>> GetRandomQuestionsAsync(
            string languageCode,
            int count,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QuestionResponse>> GetByLanguageAsync(
            string languageCode,
            int? skip = null,
            int? take = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QuestionResponse>> GetAllAsync(
            int? skip = null,
            int? take = null,
            CancellationToken cancellationToken = default);

        Task<int> GetCountAsync(string? languageCode = null, CancellationToken cancellationToken = default);
    }

    public sealed record QuestionResponse(
        int Id,
        string LanguageCode,
        string Text,
        string AnswerA,
        string AnswerB,
        string AnswerC);
}
