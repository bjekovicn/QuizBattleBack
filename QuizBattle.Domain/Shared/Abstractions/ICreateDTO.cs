namespace QuizBattle.Domain.Shared.Abstractions
{
    public interface ICreateDto<TEntity, TId>
        where TEntity : Entity<TId>
        where TId : IEquatable<TId>
    {
        TEntity MapToDomain();
    }
}
