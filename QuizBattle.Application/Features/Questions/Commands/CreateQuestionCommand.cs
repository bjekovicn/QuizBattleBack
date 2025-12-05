using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Questions;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Questions.Commands
{

    public sealed record CreateQuestionCommand(
        string LanguageCode,
        string Text,
        string AnswerA,  // Correct answer
        string AnswerB,
        string AnswerC) : ICommand<int>;

    internal sealed class CreateQuestionCommandHandler : ICommandHandlerMediatR<CreateQuestionCommand, int>
    {
        private readonly IQuestionCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateQuestionCommandHandler(IQuestionCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result<int>> Handle(CreateQuestionCommand command, CancellationToken cancellationToken)
        {
            var question = new Question(
                new Language(command.LanguageCode),
                command.Text,
                command.AnswerA,
                command.AnswerB,
                command.AnswerC);

            await _repository.AddAsync(question, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(question.Id.Value);
        }
    }
}

