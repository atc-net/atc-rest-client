namespace Atc.Rest.Client.Serialization;

/// <summary>
/// Default JSON contract serializer using System.Text.Json.
/// </summary>
public class DefaultJsonContractSerializer : IContractSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(),
        },
    };

    private readonly JsonSerializerOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultJsonContractSerializer"/> class.
    /// </summary>
    /// <param name="options">Optional JSON serializer options. If not provided, default options are used.</param>
    public DefaultJsonContractSerializer(
        JsonSerializerOptions? options = default)
    {
        this.options = options ?? DefaultOptions;
    }

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    public string Serialize(
        object value)
        => JsonSerializer.Serialize(
            value,
            options);

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public T? Deserialize<T>(
        string json)
        => JsonSerializer.Deserialize<T>(
            json,
            options);

    /// <summary>
    /// Deserializes a UTF-8 encoded JSON byte array to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Json">The UTF-8 encoded JSON byte array.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public T? Deserialize<T>(
        byte[] utf8Json)
        => JsonSerializer.Deserialize<T>(
            utf8Json,
            options);

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="returnType">The type to deserialize to.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public object? Deserialize(
        string json,
        Type returnType)
        => JsonSerializer.Deserialize(
            json,
            returnType,
            options);

    /// <summary>
    /// Deserializes a UTF-8 encoded JSON byte array to the specified type.
    /// </summary>
    /// <param name="utf8Json">The UTF-8 encoded JSON byte array.</param>
    /// <param name="returnType">The type to deserialize to.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public object? Deserialize(
        byte[] utf8Json, Type returnType)
        => JsonSerializer.Deserialize(
            utf8Json,
            returnType,
            options);

    /// <summary>
    /// Deserializes a stream of JSON data as an async enumerable.
    /// </summary>
    /// <typeparam name="T">The type of items to deserialize.</typeparam>
    /// <param name="stream">The stream containing JSON data.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>An async enumerable of deserialized items.</returns>
    public IAsyncEnumerable<T?> DeserializeAsyncEnumerable<T>(
        Stream stream,
        CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsyncEnumerable<T>(
            stream,
            options,
            cancellationToken);
}