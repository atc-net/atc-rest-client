namespace Atc.Rest.Client.SseSample.Contracts;

public sealed record PriceTick(
    string Symbol,
    decimal Price,
    int Sequence);