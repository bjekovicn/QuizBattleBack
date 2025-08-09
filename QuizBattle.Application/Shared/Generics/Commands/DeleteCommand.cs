using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Application.Shared.Abstractions.Repositories;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Generics.Commands
{
    public record DeleteCommand<TId>(TId Id) : ICommand where TId : IEquatable<TId>;

    public sealed class DeleteCommandHandler<TId> : ICommandHandler<DeleteCommand<TId>>
      where TId : IEquatable<TId>
    {
        private readonly IBaseQueryRepository<Entity<TId>, TId> _queryRepository;
        private readonly IBaseCommandRepository<Entity<TId>, TId> _commandRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteCommandHandler(
          IBaseQueryRepository<Entity<TId>, TId> queryRepository,
          IBaseCommandRepository<Entity<TId>, TId> commandRepository,
          IUnitOfWork unitOfWork
          )
        {
            _queryRepository = queryRepository;
            _commandRepository = commandRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(DeleteCommand<TId> command, CancellationToken token)
        {
            var entity = await _queryRepository.GetByIdAsync(command.Id);
            if (entity is null)
            {
                return Result.Failure(new Error("NotFound", $"Entity with ID {command.Id} not found."));
            }

            await _commandRepository.DeleteAsync(entity);
            await _unitOfWork.SaveChangesAsync(token);
            return Result.Success();
        }
    }
}