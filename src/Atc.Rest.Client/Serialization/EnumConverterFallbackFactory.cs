namespace Atc.Rest.Client.Serialization;

/// <summary>
/// Applies a fallback enum converter only to enums that don't declare their own <see cref="JsonConverterAttribute"/>.
/// </summary>
/// <remarks>
/// System.Text.Json takes a converter from the options before the one a type names in its own
/// <see cref="JsonConverterAttribute"/>. This factory declines such enums, so an enum declared with
/// <c>[JsonConverter(typeof(JsonNumberEnumConverter&lt;T&gt;))]</c> is written as its number.
/// </remarks>
internal sealed class EnumConverterFallbackFactory : JsonConverterFactory
{
    private readonly JsonConverterFactory fallback;

    public EnumConverterFallbackFactory(JsonConverterFactory fallback)
        => this.fallback = fallback;

    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsEnum &&
           typeToConvert.GetCustomAttribute<JsonConverterAttribute>(inherit: false) is null &&
           fallback.CanConvert(typeToConvert);

    public override JsonConverter? CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options)
        => fallback.CreateConverter(typeToConvert, options);
}