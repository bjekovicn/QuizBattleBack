using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Questions
{

    public sealed record GetRandomQuestionsQuery(
        string LanguageCode,
        int Count = 10) : IQuery<IReadOnlyList<QuestionResponse>>;

    internal sealed class GetRandomQuestionsQueryHandler : IQueryHandler<GetRandomQuestionsQuery, IReadOnlyList<QuestionResponse>>
    {
        private readonly IQuestionQueryRepository _repository;

        public GetRandomQuestionsQueryHandler(IQuestionQueryRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<IReadOnlyList<QuestionResponse>>> Handle(
            GetRandomQuestionsQuery query,
            CancellationToken cancellationToken)
        {
            var questions = await _repository.GetRandomQuestionsAsync(
                query.LanguageCode,
                query.Count,
                cancellationToken);

            if (questions.Count < query.Count)
            {
                return Result.Failure<IReadOnlyList<QuestionResponse>>(Error.NotEnoughQuestions);
            }

            return Result.Success(questions);
        }
    }
}
