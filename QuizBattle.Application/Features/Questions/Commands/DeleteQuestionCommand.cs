using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Questions;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Questions.Commands
{

    public sealed record DeleteQuestionCommand(int QuestionId) : ICommand;

    internal sealed class DeleteQuestionCommandHandler : ICommandHandler<DeleteQuestionCommand>
    {
        private readonly IQuestionCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteQuestionCommandHandler(IQuestionCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(DeleteQuestionCommand command, CancellationToken cancellationToken)
        {
            var question = await _repository.GetByIdAsync(new QuestionId(command.QuestionId), cancellationToken);
            if (question is null)
                return Result.Failure(Error.QuestionNotFound);

            _repository.Delete(question);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
