using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Questions;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Questions.Queries
{

    public sealed record GetQuestionByIdQuery(int QuestionId) : IQuery<QuestionResponse>;

    internal sealed class GetQuestionByIdQueryHandler : IQueryHandler<GetQuestionByIdQuery, QuestionResponse>
    {
        private readonly IQuestionQueryRepository _repository;

        public GetQuestionByIdQueryHandler(IQuestionQueryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<QuestionResponse>> Handle(
            GetQuestionByIdQuery query,
            CancellationToken cancellationToken)
        {
            var question = await _repository.GetByIdAsync(new QuestionId(query.QuestionId), cancellationToken);

            return question is null
                ? Result.Failure<QuestionResponse>(Error.QuestionNotFound)
                : Result.Success(question);
        }
    }
}
