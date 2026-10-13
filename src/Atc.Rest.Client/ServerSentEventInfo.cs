namespace Atc.Rest.Client;

/// <summary>
/// What a server-sent event says besides its data: handed to <see cref="StreamReadOptions.OnEvent"/> for every
/// event that is yielded as an item.
/// </summary>
public readonly record struct ServerSentEventInfo
{
    /// <summary>
    /// Gets the id of the last event read: the <c>id:</c> of this event, or of an earlier one when this event has
    /// none - by the standard an id stays until another is sent. <see langword="null"/> when the stream has sent
    /// no id, and empty when the server has reset it with an <c>id:</c> without a value.
    /// </summary>
    public string? LastEventId { get; init; }

    /// <summary>
    /// Gets the name of the event (<c>event:</c>), or <see langword="null"/> for the unnamed event.
    /// </summary>
    public string? EventName { get; init; }

    /// <summary>
    /// Gets the time the server last asked a client to wait before it reconnects (<c>retry:</c>), or
    /// <see langword="null"/> when it has not asked. It stays until the server sends another.
    /// </summary>
    public TimeSpan? Retry { get; init; }
}