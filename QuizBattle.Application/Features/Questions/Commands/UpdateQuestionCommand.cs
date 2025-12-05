using QuizBattle.Application.Shared.Abstractions.Data;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Domain.Features.Questions;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Features.Questions
{
    public sealed record UpdateQuestionCommand(
        int QuestionId,
        string LanguageCode,
        string Text,
        string AnswerA,
        string AnswerB,
        string AnswerC) : ICommand;

    internal sealed class UpdateQuestionCommandHandler : ICommandHandlerMediatR<UpdateQuestionCommand>
    {
        private readonly IQuestionCommandRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateQuestionCommandHandler(IQuestionCommandRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdateQuestionCommand command, CancellationToken cancellationToken)
        {
            var question = await _repository.GetByIdAsync(new QuestionId(command.QuestionId), cancellationToken);
            if (question is null)
                return Result.Failure(Error.QuestionNotFound);

            question.Update(
                new Language(command.LanguageCode),
                command.Text,
                command.AnswerA,
                command.AnswerB,
                command.AnswerC);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}