namespace Atc.Rest.Client.Streaming;

/// <summary>
/// What the reader of a server-sent events stream has seen so far, shared with the response that hands out the
/// stream - and with the result that takes that response over.
/// </summary>
internal sealed class ServerSentEventState
{
    private volatile string? lastEventId;

    public string? LastEventId
    {
        get => lastEventId;
        set => lastEventId = value;
    }
}