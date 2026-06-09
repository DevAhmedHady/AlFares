namespace BuildingBlocks.Messaging;

// Marker: a write request returning Result<TResponse>.
public interface ICommand<TResponse>;

// Marker: a read-only request returning Result<TResponse>.
public interface IQuery<TResponse>;
