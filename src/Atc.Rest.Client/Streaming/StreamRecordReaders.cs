namespace Atc.Rest.Client.Streaming;

/// <summary>
/// Reads the items of a streamed response in the framings besides a JSON array. Every reader yields an item as soon
/// as its record is complete, reads nothing ahead of it, and deserializes with the contract serializer - so a record
/// that is no JSON for the item type throws the serializer's exception from the enumeration.
/// </summary>
internal static class StreamRecordReaders
{
    private const int ReadBufferSize = 4096;
    private const byte RecordSeparator = 0x1E;
    private const byte LineFeed = (byte)'\n';
    private const string ServerSentEventDescription = "An event of the server-sent events stream";

    // A JSON array (application/json), read by the contract serializer. One element is limited in length as one
    // record of the other framings is.
    public static async IAsyncEnumerable<T?> ReadJsonArrayAsync<T>(
        Stream stream,
        IContractSerializer serializer,
        int maxRecordLength,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var elements = new JsonArrayElementLimitStream(stream, maxRecordLength);

        await foreach (var item in serializer.DeserializeAsyncEnumerable<T>(elements, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    // JSON Lines (application/jsonl, application/x-ndjson): one JSON value per line. An empty or
    // whitespace-only line is skipped; the last line needs no line feed.
    public static async IAsyncEnumerable<T?> ReadJsonLinesAsync<T>(
        Stream stream,
        IContractSerializer serializer,
        int maxRecordLength,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reader = new Utf8LineReader(stream, maxRecordLength, "A line of the JSON Lines stream");
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false))
        {
            var line = reader.Line;
            if (IsWhiteSpace(line))
            {
                continue;
            }

            yield return serializer.Deserialize<T>(ToArray(line));
        }
    }

    // JSON text sequence, RFC 7464 (application/json-seq): a record starts with the record separator and
    // ends with a line feed. The JSON of a record may hold line feeds of its own (a pretty-printed record), so a
    // record ends at the line feed where its JSON is balanced - not at the next record separator, which a server
    // that fails after a record never sends. What is left at the end of the stream is a record as well.
    public static async IAsyncEnumerable<T?> ReadJsonSequenceAsync<T>(
        Stream stream,
        IContractSerializer serializer,
        int maxRecordLength,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var buffer = new byte[ReadBufferSize];
        var record = new RecordBuffer(maxRecordLength, "A record of the JSON text sequence stream");
        var json = new JsonCompletenessTracker();

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var read = await stream.ReadChunkAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            for (var i = 0; i < read; i++)
            {
                var value = buffer[i];
                var endsRecord = value == RecordSeparator || (value == LineFeed && json.IsComplete);
                if (!endsRecord)
                {
                    record.Append(value);
                    json.Add(value);
                    continue;
                }

                if (json.HasContent)
                {
                    yield return serializer.Deserialize<T>(record.ToArray());
                }

                record.Clear();
                json.Reset();
            }
        }

        if (json.HasContent)
        {
            yield return serializer.Deserialize<T>(record.ToArray());
        }
    }

    // Server-sent events (text/event-stream), read as the HTML standard has them: an event is its fields, one per
    // line, and a blank line; a line ends at LF, CR or CRLF; a line that starts with a colon is a comment; several
    // data fields are joined with a line feed; the event name is forgotten at every blank line, also when the event
    // had no data. The JSON is the data of an event.
    // The item event is the unnamed one (which the standard also calls "message") and the events named in
    // itemEventNames; such an event must be an item. An event with another name - a heartbeat, a control frame - is
    // read past without being deserialized, and so is an event without data.
    public static async IAsyncEnumerable<T?> ReadServerSentEventsAsync<T>(
        Stream stream,
        IContractSerializer serializer,
        int maxRecordLength,
        IReadOnlyCollection<string>? itemEventNames,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var reader = new Utf8LineReader(stream, maxRecordLength, ServerSentEventDescription, carriageReturnEndsLine: true);
        var serverSentEvent = new ServerSentEventBuffer(maxRecordLength, ServerSentEventDescription);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false))
        {
            if (reader.Line.Count != 0)
            {
                serverSentEvent.AddLine(reader.Line);
                continue;
            }

            var json = serverSentEvent.IsItem(itemEventNames)
                ? serverSentEvent.GetData()
                : null;

            serverSentEvent.Reset();

            if (json is not null)
            {
                yield return serializer.Deserialize<T>(json);
            }
        }
    }

    // multipart/mixed: one JSON value per part. The preamble and a part's own headers are read past; a part
    // is yielded as soon as its body is balanced JSON, not at the next boundary; reading ends at the closing
    // boundary.
    public static async IAsyncEnumerable<T?> ReadMultipartMixedAsync<T>(
        Stream stream,
        string boundary,
        IContractSerializer serializer,
        int maxRecordLength,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var delimiter = Encoding.UTF8.GetBytes("--" + boundary);
        var reader = new Utf8LineReader(stream, maxRecordLength, "A line of the multipart stream");
        var part = new RecordBuffer(maxRecordLength, "A part of the multipart stream");
        var json = new JsonCompletenessTracker();
        var inPart = false;
        var inHeaders = false;

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false))
        {
            var line = reader.Line;
            var delimiterKind = GetDelimiterKind(line, delimiter);
            if (delimiterKind != MultipartDelimiterKind.None)
            {
                // A body that is not balanced when its part ends is the serializer's to refuse
                if (json.HasContent)
                {
                    yield return serializer.Deserialize<T>(part.ToArray());
                }

                if (delimiterKind == MultipartDelimiterKind.Close)
                {
                    yield break;
                }

                part.Clear();
                json.Reset();
                inPart = true;
                inHeaders = true;
                continue;
            }

            if (!inPart)
            {
                continue;
            }

            if (inHeaders)
            {
                inHeaders = line.Count != 0;
                continue;
            }

            AppendLine(part, json, line);

            if (json.IsComplete)
            {
                yield return serializer.Deserialize<T>(part.ToArray());
                part.Clear();
                json.Reset();
            }
        }

        if (json.HasContent)
        {
            yield return serializer.Deserialize<T>(part.ToArray());
        }
    }

    // The line feed between two lines of a body is kept: it is what separated two JSON tokens.
    private static void AppendLine(
        RecordBuffer part,
        JsonCompletenessTracker json,
        ArraySegment<byte> line)
    {
        if (part.Length > 0)
        {
            part.Append(LineFeed);
        }

        part.Append(line);
        json.Add(line);
        json.Add(LineFeed);
    }

    // A delimiter line is the delimiter itself, or the delimiter and "--" for the closing one, with nothing but
    // spaces and tabs after it. A line that only starts with the delimiter is not one.
    private static MultipartDelimiterKind GetDelimiterKind(
        ArraySegment<byte> line,
        byte[] delimiter)
    {
        var span = line.AsSpan();
        if (!span.StartsWith(delimiter))
        {
            return MultipartDelimiterKind.None;
        }

        var rest = span.Slice(delimiter.Length);
        var kind = MultipartDelimiterKind.Part;
        if (rest.Length >= 2 && rest[0] == (byte)'-' && rest[1] == (byte)'-')
        {
            rest = rest.Slice(2);
            kind = MultipartDelimiterKind.Close;
        }

        return IsWhiteSpace(rest)
            ? kind
            : MultipartDelimiterKind.None;
    }

    internal static bool IsWhiteSpace(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            if (value is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n'))
            {
                return false;
            }
        }

        return true;
    }

    private static byte[] ToArray(ArraySegment<byte> bytes)
        => bytes.AsSpan().ToArray();
}