namespace Atc.Rest.Client;

/// <summary>
/// How the items of a streamed response are framed on the wire.
/// </summary>
public enum StreamFraming
{
    /// <summary>
    /// One JSON array, its elements being the items (<c>application/json</c>). The default.
    /// </summary>
    JsonArray,

    /// <summary>
    /// One JSON value per line (<c>application/jsonl</c>, <c>application/x-ndjson</c>).
    /// </summary>
    JsonLines,

    /// <summary>
    /// A JSON text sequence, RFC 7464 (<c>application/json-seq</c>): each record starts with the record separator
    /// <c>0x1E</c> and ends with a line feed.
    /// </summary>
    JsonSequence,

    /// <summary>
    /// Server-sent events (<c>text/event-stream</c>): the JSON is the <c>data:</c> of an event.
    /// </summary>
    ServerSentEvents,

    /// <summary>
    /// One JSON value per part (<c>multipart/mixed</c>); the boundary is read from the response Content-Type.
    /// </summary>
    MultipartMixed,
}