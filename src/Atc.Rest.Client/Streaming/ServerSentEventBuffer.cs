namespace Atc.Rest.Client.Streaming;

/// <summary>
/// The server-sent event being read: its name and its data, taken from the lines of the event as the HTML standard
/// describes. The lines of one event are limited in length together, so an event of many short lines is refused as
/// well as one long line.
/// </summary>
internal sealed class ServerSentEventBuffer
{
    private const string DefaultEventName = "message";

    private static readonly byte[] DataField = "data"u8.ToArray();
    private static readonly byte[] EventField = "event"u8.ToArray();

    private readonly int maxLength;
    private readonly string description;
    private readonly RecordBuffer data;
    private string? eventName;
    private bool hasData;
    private long length;

    public ServerSentEventBuffer(
        int maxLength,
        string description)
    {
        this.maxLength = maxLength;
        this.description = description;
        data = new RecordBuffer(maxLength, description);
    }

    /// <summary>
    /// Takes one line of the event: a field name, a colon and the value, of which one leading space is dropped.
    /// A line that starts with a colon is a comment, and a line without a colon is a field without a value.
    /// </summary>
    /// <param name="line">The line, without its line ending.</param>
    public void AddLine(ArraySegment<byte> line)
    {
        length += line.Count;
        if (length > maxLength)
        {
            throw StreamRecordLimit.CreateException(description, maxLength);
        }

        var span = line.AsSpan();
        var colon = span.IndexOf((byte)':');
        if (colon == 0)
        {
            return;
        }

        var name = colon < 0 ? span : span.Slice(0, colon);
        var valueStart = colon < 0 ? span.Length : colon + 1;
        if (valueStart < span.Length && span[valueStart] == (byte)' ')
        {
            valueStart++;
        }

        var value = new ArraySegment<byte>(line.Array!, line.Offset + valueStart, line.Count - valueStart);

        if (name.SequenceEqual(DataField))
        {
            if (hasData)
            {
                data.Append((byte)'\n');
            }

            data.Append(value);
            hasData = true;
        }
        else if (name.SequenceEqual(EventField))
        {
            eventName = Encoding.UTF8.GetString(value.Array!, value.Offset, value.Count);
        }
    }

    /// <summary>
    /// Whether the event is an item: it has data, and it is the unnamed event or one of the named item events.
    /// Event names are case-sensitive by the standard.
    /// </summary>
    /// <param name="itemEventNames">The names of the events that are items, besides the unnamed one.</param>
    /// <returns><see langword="true"/> when the data of the event is an item.</returns>
    public bool IsItem(IReadOnlyCollection<string>? itemEventNames)
    {
        if (!hasData || StreamRecordReaders.IsWhiteSpace(data.GetSegment().AsSpan()))
        {
            return false;
        }

        return string.IsNullOrEmpty(eventName) ||
               string.Equals(eventName, DefaultEventName, StringComparison.Ordinal) ||
               (itemEventNames is not null && itemEventNames.Contains(eventName, StringComparer.Ordinal));
    }

    public byte[] GetData()
        => data.ToArray();

    /// <summary>
    /// Forgets the event at the blank line that ends it - its name too, also when the event had no data.
    /// </summary>
    public void Reset()
    {
        data.Clear();
        eventName = null;
        hasData = false;
        length = 0;
    }
}