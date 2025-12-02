namespace Atc.Rest.Client.Serialization;

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

    public DefaultJsonContractSerializer(
        JsonSerializerOptions? options = default)
    {
        this.options = options ?? DefaultOptions;
    }

    public string Serialize(
        object value)
        => JsonSerializer.Serialize(
            value,
            options);

    public T? Deserialize<T>(
        string json)
        => JsonSerializer.Deserialize<T>(
            json,
            options);

    public T? Deserialize<T>(
        byte[] utf8Json)
        => JsonSerializer.Deserialize<T>(
            utf8Json,
            options);

    public object? Deserialize(
        string json,
        Type returnType)
        => JsonSerializer.Deserialize(
            json,
            returnType,
            options);

    public object? Deserialize(
        byte[] utf8Json, Type returnType)
        => JsonSerializer.Deserialize(
            utf8Json,
            returnType,
            options);

    public IAsyncEnumerable<T?> DeserializeAsyncEnumerable<T>(
        Stream stream,
        CancellationToken cancellationToken = default)
        => JsonSerializer.DeserializeAsyncEnumerable<T>(
            stream,
            options,
            cancellationToken);
}