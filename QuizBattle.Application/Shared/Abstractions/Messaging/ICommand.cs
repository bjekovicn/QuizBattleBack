namespace QuizBattle.Application.Shared.Abstractions.Messaging
{
    public interface ICommand : IBaseCommand;

    public interface ICommand<TReponse> : IBaseCommand;

    public interface IBaseCommand;
}
