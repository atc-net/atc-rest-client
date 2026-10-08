namespace Atc.Rest.Client.Options;

/// <summary>
/// Holds the JSON serializer options of the default <see cref="IContractSerializer"/>,
/// adjusted by <see cref="ServiceCollectionExtensions.ConfigureAtcRestClientJsonOptions"/>.
/// </summary>
internal sealed class AtcRestClientJsonOptions
{
    /// <summary>
    /// Gets the JSON serializer options, created by <see cref="DefaultJsonContractSerializer.CreateDefaultOptions"/>.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; } = DefaultJsonContractSerializer.CreateDefaultOptions();
}