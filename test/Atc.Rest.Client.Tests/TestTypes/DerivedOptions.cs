namespace Atc.Rest.Client.Tests.TestTypes;

internal sealed class DerivedOptions : AtcRestClientOptions
{
    public override Uri? BaseAddress { get; set; } = new Uri("https://override.example.com");

    public override TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
}