namespace Atc.Rest.Client.Serialization;

public interface IContractSerializer
{
    string Serialize(
        object value);

    T? Deserialize<T>(
        string json);

    T? Deserialize<T>(
        byte[] utf8Json);

    object? Deserialize(
        string json,
        Type returnType);

    object? Deserialize(
        byte[] utf8Json,
        Type returnType);

    /// <summary>
    /// Deserializes a stream as an async enumerable sequence of items.
    /// </summary>
    /// <typeparam name="T">The type of items to deserialize.</typeparam>
    /// <param name="stream">The stream containing JSON array data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of deserialized items.</returns>
    IAsyncEnumerable<T?> DeserializeAsyncEnumerable<T>(
        Stream stream,
        CancellationToken cancellationToken = default);
}