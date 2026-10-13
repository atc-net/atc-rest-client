namespace Showcase.Contracts;

/// <summary>
/// Source-generated JSON metadata for the contracts, so the Blazor app needs no reflection when it is trimmed.
/// </summary>
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PriceTick))]
[JsonSerializable(typeof(Problem))]
public sealed partial class ShowcaseJsonContext : JsonSerializerContext;