using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Questions.Queries
{

    public sealed record GetQuestionCountQuery(string? LanguageCode = null) : IQuery<int>;

    internal sealed class GetQuestionCountQueryHandler : IQueryHandler<GetQuestionCountQuery, int>
    {
        private readonly IQuestionQueryRepository _repository;

        public GetQuestionCountQueryHandler(IQuestionQueryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<int>> Handle(GetQuestionCountQuery query, CancellationToken cancellationToken)
        {
            var count = await _repository.GetCountAsync(query.LanguageCode, cancellationToken);
            return Result.Success(count);
        }
    }
}
