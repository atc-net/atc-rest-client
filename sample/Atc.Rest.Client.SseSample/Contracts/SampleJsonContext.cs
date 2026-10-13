namespace Atc.Rest.Client.SseSample.Contracts;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(PriceTick))]
[JsonSerializable(typeof(Problem))]
public sealed partial class SampleJsonContext : JsonSerializerContext;