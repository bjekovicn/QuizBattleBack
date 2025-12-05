namespace QuizBattle.Domain.Shared.Exceptions
{
    public sealed record ValidationError(string PropertyName, string ErrorMessage);
}
