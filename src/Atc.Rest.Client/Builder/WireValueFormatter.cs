namespace Atc.Rest.Client.Builder;

/// <summary>
/// Converts path, header, query and form values to their culture-independent wire text.
/// </summary>
internal static class WireValueFormatter
{
    /// <summary>
    /// Cache for enum member wire names to avoid repeated reflection.
    /// Key: (EnumType, MemberName), Value: the [EnumMember] or [JsonStringEnumMemberName] value, or null if none.
    /// </summary>
    private static readonly ConcurrentDictionary<(Type EnumType, string MemberName), string?> EnumMemberCache = new();

    /// <summary>
    /// Cache of whether an enum type declares a JsonNumberEnumConverter, so it is written as its number.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, bool> NumberEnumCache = new();

    /// <summary>
    /// Gets the wire text of a value:
    /// enums as their [EnumMember]/[JsonStringEnumMemberName] value, their number when the enum type declares
    /// a JsonNumberEnumConverter, else their name; DateTime and DateTimeOffset as ISO 8601 ("o");
    /// DateOnly as yyyy-MM-dd; TimeOnly as HH:mm:ss.FFFFFFF; other IFormattable values with the invariant culture;
    /// everything else with ToString().
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The wire text of <paramref name="value"/>.</returns>
    public static string ToWireString(object value)
        => value switch
        {
            string text => text,
            Enum enumValue => FormatEnum(enumValue),
            DateTime dateTime => dateTime.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("o", CultureInfo.InvariantCulture),
            IFormattable formattable => formattable.ToString(GetFormat(value), CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    // DateOnly and TimeOnly don't exist in netstandard2.0, so they are recognized by name.
    private static string? GetFormat(object value)
        => value.GetType().FullName switch
        {
            "System.DateOnly" => "yyyy-MM-dd",
            "System.TimeOnly" => "HH:mm:ss.FFFFFFF",
            _ => null,
        };

    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "The trimmer keeps every field of an enum type that is kept, so the public fields of the value's enum type are available.")]
    private static string FormatEnum(Enum value)
    {
        var enumType = value.GetType();
        var memberName = value.ToString();

        var wireName = GetEnumMemberValue(enumType, memberName);
        if (wireName is not null)
        {
            return wireName;
        }

        return IsNumberEnum(enumType)
            ? ((IFormattable)value).ToString("D", CultureInfo.InvariantCulture)
            : memberName;
    }

    private static string? GetEnumMemberValue(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type enumType,
        string memberName)
    {
        var key = (enumType, memberName);
        if (EnumMemberCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var field = enumType.GetField(memberName, BindingFlags.Public | BindingFlags.Static);
        var value = field?.GetCustomAttribute<EnumMemberAttribute>(inherit: false)?.Value
                    ?? field?.GetCustomAttribute<JsonStringEnumMemberNameAttribute>(inherit: false)?.Name;

        return EnumMemberCache.GetOrAdd(key, value);
    }

    private static bool IsNumberEnum(Type enumType)
    {
        if (NumberEnumCache.TryGetValue(enumType, out var cached))
        {
            return cached;
        }

        var converterType = enumType.GetCustomAttribute<JsonConverterAttribute>(inherit: false)?.ConverterType;
        var isNumber = converterType is { IsGenericType: true } &&
                       converterType.GetGenericTypeDefinition() == typeof(JsonNumberEnumConverter<>);

        return NumberEnumCache.GetOrAdd(enumType, isNumber);
    }
}