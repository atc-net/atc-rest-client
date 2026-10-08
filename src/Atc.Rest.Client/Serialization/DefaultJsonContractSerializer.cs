namespace Atc.Rest.Client.Serialization;

/// <summary>
/// Default JSON contract serializer using System.Text.Json.
/// </summary>
/// <remarks>
/// All JSON metadata is resolved through <see cref="JsonSerializerOptions.GetTypeInfo(Type)"/>, so the serializer
/// is trim- and Native AOT-safe when the options carry a <see cref="JsonSerializerContext"/> for the types in use.
/// </remarks>
public class DefaultJsonContractSerializer : IContractSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = CreateReadOnly(CreateDefaultOptions());

    private readonly JsonSerializerOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultJsonContractSerializer"/> class.
    /// </summary>
    /// <param name="options">
    /// Optional JSON serializer options. If not provided, default options are used.
    /// The options are made read-only by this constructor.
    /// </param>
    public DefaultJsonContractSerializer(
        JsonSerializerOptions? options = default)
    {
        this.options = options is null
            ? DefaultOptions
            : CreateReadOnly(options);
    }

    /// <summary>
    /// Creates a new, mutable instance of the default JSON serializer options:
    /// camel case property names, null values omitted and indented output.
    /// </summary>
    /// <remarks>
    /// When reflection-based serialization is enabled (<see cref="JsonSerializer.IsReflectionEnabledByDefault"/>),
    /// the options resolve metadata through <see cref="DefaultJsonTypeInfoResolver"/> and write enums as strings,
    /// except an enum that declares its own <see cref="JsonConverterAttribute"/> (for example
    /// <c>JsonNumberEnumConverter&lt;T&gt;</c>), which keeps that converter.
    /// Otherwise (trimmed and Native AOT apps) they start with an empty resolver chain; insert a
    /// <see cref="JsonSerializerContext"/> into <see cref="JsonSerializerOptions.TypeInfoResolverChain"/>
    /// for the types to serialize.
    /// </remarks>
    /// <returns>A new <see cref="JsonSerializerOptions"/> instance.</returns>
    public static JsonSerializerOptions CreateDefaultOptions()
    {
        var defaultOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            AddReflectionDefaults(defaultOptions);
        }
        else
        {
            defaultOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine();
        }

        return defaultOptions;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only reached when reflection-based serialization is enabled; the feature switch removes this call in trimmed apps.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only reached when reflection-based serialization is enabled; the feature switch removes this call in trimmed apps.")]
    private static void AddReflectionDefaults(JsonSerializerOptions options)
    {
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
        options.Converters.Add(new EnumConverterFallbackFactory(new JsonStringEnumConverter()));
    }

    private static JsonSerializerOptions CreateReadOnly(
        JsonSerializerOptions options)
    {
        if (!options.IsReadOnly)
        {
            if (options.TypeInfoResolver is null && JsonSerializer.IsReflectionEnabledByDefault)
            {
                MakeReadOnlyWithReflectionResolver(options);
            }
            else
            {
                options.MakeReadOnly();
            }
        }

        return options;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Only reached when reflection-based serialization is enabled; the feature switch removes this call in trimmed apps.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Only reached when reflection-based serialization is enabled; the feature switch removes this call in trimmed apps.")]
    private static void MakeReadOnlyWithReflectionResolver(
        JsonSerializerOptions options)
        => options.MakeReadOnly(populateMissingResolver: true);

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <param name="value">The object to serialize.</param>
    /// <returns>A JSON string representation of the object.</returns>
    public string Serialize(object value)
        => JsonSerializer.Serialize(
            value,
            options.GetTypeInfo(value?.GetType() ?? typeof(object)));

    /// <summary>
    /// Deserializes a JSON string to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public T? Deserialize<T>(string json)
        => JsonSerializer.Deserialize(
            json,
            GetTypeInfo<T>());

    /// <summary>
    /// Deserializes a UTF-8 encoded JSON byte array to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="utf8Json">The UTF-8 encoded JSON byte array.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public T? Deserialize<T>(byte[] utf8Json)
        => JsonSerializer.Deserialize(
            utf8Json,
            GetTypeInfo<T>());

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
            options.GetTypeInfo(returnType));

    /// <summary>
    /// Deserializes a UTF-8 encoded JSON byte array to the specified type.
    /// </summary>
    /// <param name="utf8Json">The UTF-8 encoded JSON byte array.</param>
    /// <param name="returnType">The type to deserialize to.</param>
    /// <returns>The deserialized object, or null if deserialization fails.</returns>
    public object? Deserialize(
        byte[] utf8Json,
        Type returnType)
        => JsonSerializer.Deserialize(
            utf8Json,
            options.GetTypeInfo(returnType));

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
        => JsonSerializer.DeserializeAsyncEnumerable(
            stream,
            GetTypeInfo<T>(),
            cancellationToken);

    private JsonTypeInfo<T> GetTypeInfo<T>()
        => (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
}