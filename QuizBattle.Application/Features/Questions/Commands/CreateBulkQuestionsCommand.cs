using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Questions;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Questions
{
    public sealed record CreateBulkQuestionsCommand(
        List<CreateQuestionDto> Questions) : ICommand<int>;

    public sealed record CreateQuestionDto(
        string LanguageCode,
        string Text,
        string AnswerA,
        string AnswerB,
        string AnswerC);

    internal sealed class CreateBulkQuestionsCommandHandler : ICommandHandler<CreateBulkQuestionsCommand, int>
    {
        private readonly IQuestionCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateBulkQuestionsCommandHandler(IQuestionCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<int>> Handle(CreateBulkQuestionsCommand command, CancellationToken cancellationToken)
        {
            var questions = command.Questions.Select(q => new Question(
                new Language(q.LanguageCode),
                q.Text,
                q.AnswerA,
                q.AnswerB,
                q.AnswerC)).ToList();

            await _repository.AddRangeAsync(questions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(questions.Count);
        }
    }
}