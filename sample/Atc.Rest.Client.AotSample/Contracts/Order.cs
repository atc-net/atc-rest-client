namespace Atc.Rest.Client.AotSample.Contracts;

public sealed record Order(
    int Id,
    string Title,
    OrderStatus Status,
    Priority Priority,
    Priority? OptionalPriority);