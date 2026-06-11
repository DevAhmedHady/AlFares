namespace SharedKernel;

public abstract class Entity
{
    public Guid Id { get; protected set; }

    protected Entity(Guid id) => Id = id;

    protected Entity() { } // for EF materialization

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id && Id != Guid.Empty;

    public override int GetHashCode() => Id.GetHashCode();
}
