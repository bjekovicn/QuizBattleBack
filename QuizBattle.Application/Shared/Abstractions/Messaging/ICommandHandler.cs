using QuizBattle.Domain.Shared.Abstractions;

namespace QuizBattle.Application.Shared.Abstractions.Messaging
{
    public interface ICommandHandler<TCommand> 
        where TCommand : ICommand
    {
        Task<Result> Handle(TCommand command, CancellationToken token);
    }

    public interface ICommandHandler<TCommand, TResponse> 
        where TCommand : ICommand<TResponse>
    {
        Task<Result<TResponse>> Handle(TCommand command, CancellationToken token);
    }

}
