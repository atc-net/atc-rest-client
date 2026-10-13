namespace Showcase.BlazorApp.Models;

/// <summary>
/// One item of the stream and when it arrived, counted from the start of the stream.
/// </summary>
public sealed record LogEntry(
    long ElapsedMilliseconds,
    PriceTick Tick);