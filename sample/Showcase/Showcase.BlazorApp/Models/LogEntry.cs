namespace Showcase.BlazorApp.Models;

/// <summary>
/// One item of the stream and when it arrived, counted from the start of the stream - and whether it is the
/// first item after the stream was resumed.
/// </summary>
public sealed record LogEntry(
    long ElapsedMilliseconds,
    PriceTick Tick,
    bool IsResumed);