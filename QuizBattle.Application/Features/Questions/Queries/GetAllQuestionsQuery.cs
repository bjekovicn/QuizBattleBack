using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Questions.Queries
{

    public sealed record GetAllQuestionsQuery(
        string? LanguageCode = null,
        int? Skip = null,
        int? Take = null) : IQuery<IReadOnlyList<QuestionResponse>>;

    internal sealed class GetAllQuestionsQueryHandler : IQueryHandler<GetAllQuestionsQuery, IReadOnlyList<QuestionResponse>>
    {
        private readonly IQuestionQueryRepository _repository;

        public GetAllQuestionsQueryHandler(IQuestionQueryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyList<QuestionResponse>>> Handle(
            GetAllQuestionsQuery query,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<QuestionResponse> questions;

            if (!string.IsNullOrWhiteSpace(query.LanguageCode))
            {
                questions = await _repository.GetByLanguageAsync(
                    query.LanguageCode,
                    query.Skip,
                    query.Take,
                    cancellationToken);
            }
            else
            {
                questions = await _repository.GetAllAsync(query.Skip, query.Take, cancellationToken);
            }

            return Result.Success(questions);
        }
    }
}
