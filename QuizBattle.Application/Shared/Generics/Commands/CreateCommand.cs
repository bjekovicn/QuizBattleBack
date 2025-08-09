using QuizBattle.Domain.Shared.Abstractions;
using QuizBattle.Application.Shared.Abstractions.Messaging;
using QuizBattle.Application.Shared.Abstractions.Repositories;

namespace QuizBattle.Application.Shared.Generics.Commands
{
    public abstract record CreateCommand<TEntity, TId> : ICommand
        where TEntity : Entity<TId>
        where TId : IEquatable<TId>
    {
        public abstract TEntity ToDomainModel();
    }

    public sealed class CreateCommandHandler<TEntity, TId> : ICommandHandler<CreateCommand<TEntity, TId>>
        where TEntity : Entity<TId>
        where TId : IEquatable<TId>
    {
        private readonly IBaseCommandRepository<TEntity, TId> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateCommandHandler(IBaseCommandRepository<TEntity, TId> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<Result> Handle(CreateCommand<TEntity, TId> command, CancellationToken token)
        {
            var entity = command.ToDomainModel();

            await _repository.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync(token);

            return Result.Success();
        }
    }
}