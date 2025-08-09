using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Application.Shared.Abstractions.Repositories;
using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Generics.Commands
{
    public sealed record UpdateCommand<TEntity, TId>(TEntity Entity) : ICommand where TEntity : Entity<TId>;

    public sealed class UpdateCommandHandler<TEntity, TId> : ICommandHandler<UpdateCommand<TEntity, TId>>
        where TEntity : Entity<TId>
        where TId : IEquatable<TId>
    {
        private readonly IBaseCommandRepository<TEntity, TId> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateCommandHandler(IBaseCommandRepository<TEntity, TId> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(UpdateCommand<TEntity, TId> command, CancellationToken token)
        {
            await _repository.UpdateAsync(command.Entity);
            await _unitOfWork.SaveChangesAsync(token);
            return Result.Success();
        }
    }
}

