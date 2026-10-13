namespace Showcase.Contracts;

/// <summary>
/// One price of one symbol: an item of the price stream.
/// </summary>
public sealed record PriceTick(
    string Symbol,
    decimal Price,
    decimal Change,
    long Sequence);