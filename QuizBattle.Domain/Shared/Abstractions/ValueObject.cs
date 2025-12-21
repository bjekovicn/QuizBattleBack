namespace QuizBattle.Domain.Shared.Abstractions;

public abstract class ValueObject<T> : IEquatable<T>
    where T : ValueObject<T> 
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is not T other || other is null)
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x != null ? x.GetHashCode() : 0)
            .Aggregate((x, y) => x ^ y);
    }

    public bool Equals(T? other)
    {
        return Equals((object?)other);
    }

    public static bool operator ==(ValueObject<T> left, ValueObject<T> right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
        {
            return true;
        }

        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(ValueObject<T> left, ValueObject<T> right)
    {
        return !(left == right);
    }
}