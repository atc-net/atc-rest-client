namespace Atc.Rest.Client;

/// <summary>
/// Options for reading one streamed response.
/// </summary>
public class StreamReadOptions
{
    /// <summary>
    /// Gets or sets the longest record (a line, a record, an event, a part or an array element) that is read, in
    /// bytes. When <see langword="null"/>, <c>AtcRestClientOptions.MaxStreamRecordLength</c> applies.
    /// </summary>
    public int? MaxRecordLength { get; set; }

    /// <summary>
    /// Gets or sets the names of the server-sent events that are items, besides the unnamed event (which the
    /// standard also calls <c>message</c>). When <see langword="null"/> or empty, only the unnamed event is an item.
    /// </summary>
    /// <remarks>
    /// An event stream carries more than items: a heartbeat, a control frame, a closing event. They are told apart
    /// by the event name, so an event with a name that is not listed here is read past without being deserialized.
    /// A listed event must be an item: data that is no JSON for the item type throws from the enumeration, as for
    /// the unnamed event. Names are compared ordinally; the standard is case-sensitive.
    /// Used by <see cref="StreamFraming.ServerSentEvents"/> only.
    /// </remarks>
    public IReadOnlyCollection<string>? ItemEventNames { get; set; }

    /// <summary>
    /// Gets or sets what is called for every server-sent event that is yielded as an item, before it is yielded:
    /// with the id of the last event read, the name of the event and the retry time the server last asked for.
    /// </summary>
    /// <remarks>
    /// It is not called for an event that is read past - a heartbeat, a control frame, an event without data -
    /// though the <c>id:</c> and <c>retry:</c> of such an event count for the items after it. It is called on the
    /// thread that enumerates the stream, and an exception it throws fails the enumeration with that exception.
    /// Used by <see cref="StreamFraming.ServerSentEvents"/> only.
    /// </remarks>
    public Action<ServerSentEventInfo>? OnEvent { get; set; }
}