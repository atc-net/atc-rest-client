namespace Showcase.Contracts;

/// <summary>
/// The error body of the API (the fields of RFC 9457 problem details that the showcase uses).
/// </summary>
public sealed record Problem(
    string Title,
    int Status,
    string Detail);