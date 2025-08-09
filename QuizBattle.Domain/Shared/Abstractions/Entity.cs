namespace QuizBattle.Domain.Shared.Abstractions
{
    public abstract class Entity<IdType>
    {
        public IdType Id { get; protected set; }

        protected Entity(IdType id)
        {
            Id = id;
        }
    }
}
