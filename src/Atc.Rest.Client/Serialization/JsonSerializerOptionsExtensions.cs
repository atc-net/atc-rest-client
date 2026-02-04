namespace Atc.Rest.Client.Serialization;

/// <summary>
/// Extension methods for <see cref="JsonSerializerOptions"/>.
/// </summary>
public static class JsonSerializerOptionsExtensions
{
    /// <summary>
    /// Creates a copy of the JSON serializer options without the specified converters.
    /// </summary>
    /// <param name="source">The source options to copy.</param>
    /// <param name="converters">The converters to exclude from the copy.</param>
    /// <returns>A new <see cref="JsonSerializerOptions"/> instance without the specified converters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
    public static JsonSerializerOptions WithoutConverter(
        this JsonSerializerOptions source,
        params JsonConverter[] converters)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var result = new JsonSerializerOptions
        {
            AllowTrailingCommas = source.AllowTrailingCommas,
            DefaultBufferSize = source.DefaultBufferSize,
            DictionaryKeyPolicy = source.DictionaryKeyPolicy,
            Encoder = source.Encoder,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IgnoreReadOnlyProperties = source.IgnoreReadOnlyProperties,
            MaxDepth = source.MaxDepth,
            PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive,
            PropertyNamingPolicy = source.PropertyNamingPolicy,
            ReadCommentHandling = source.ReadCommentHandling,
            WriteIndented = source.WriteIndented,
        };

        foreach (var converter in source.Converters.Except(converters))
        {
            result.Converters.Add(converter);
        }

        return result;
    }
}