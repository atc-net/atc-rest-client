namespace Showcase.BlazorApp.Models;

/// <summary>
/// The last price of a symbol, as the page shows it.
/// </summary>
public sealed record Quote(
    string Symbol,
    decimal Price,
    decimal Change,
    int Ticks);