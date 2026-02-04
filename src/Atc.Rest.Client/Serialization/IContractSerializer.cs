namespace Atc.Rest.Client.Serialization;

/// <summary>
/// Interface for serializing and deserializing objects.
/// </summary>
public interface IContractSerializer
{
    /// <summary>
    /// Serializes an object to a string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A string representation of the object.</returns>
    string Serialize(
        object value);

    /// <summary>
    /// Deserializes a string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The string to deserialize.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    T? Deserialize<T>(
        string json);

    /// <summary>
    /// Deserializes a UTF-8 encoded byte array to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Json">The UTF-8 encoded byte array to deserialize.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    T? Deserialize<T>(
        byte[] utf8Json);

    /// <summary>
    /// Deserializes a string to the specified type.
    /// </summary>
    /// <param name="json">The string to deserialize.</param>
    /// <param name="returnType">The type to deserialize to.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    object? Deserialize(
        string json,
        Type returnType);

    /// <summary>
    /// Deserializes a UTF-8 encoded byte array to the specified type.
    /// </summary>
    /// <param name="utf8Json">The UTF-8 encoded byte array to deserialize.</param>
    /// <param name="returnType">The type to deserialize to.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
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