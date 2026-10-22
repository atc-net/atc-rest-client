namespace Atc.Rest.Client.Builder;

/// <summary>
/// A request body sent as a stream of records: JSON Lines (one compact JSON value and a line feed per record) or a
/// JSON Text Sequence, RFC 7464 (a record separator, the value and a line feed). Each record is written as the source
/// yields it, with no length (chunked); what is written is sent whenever the source makes it wait, so a live source
/// reaches the server record by record and a fast one in full buffers.
/// </summary>
/// <remarks>
/// It is sent once: the records are read as they are written, so a retry or a redirect that sends it again is refused
/// rather than sending other records.
/// </remarks>
/// <typeparam name="T">The record.</typeparam>
internal sealed class StreamedRequestContent<T> : HttpContent
{
    private readonly IAsyncEnumerable<T> records;
    private readonly IContractSerializer serializer;
    private readonly bool isSequence;
    private int sent;

    public StreamedRequestContent(
        IAsyncEnumerable<T> records,
        IContractSerializer serializer,
        string contentType)
    {
        this.records = records ?? throw new ArgumentNullException(nameof(records));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        var mediaType = MediaTypeHeaderValue.Parse(contentType);
        isSequence = StreamedRequestFraming.IsJsonSequence(mediaType.MediaType);
        Headers.ContentType = mediaType;
    }

    protected override Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context)
        => SerializeToStreamAsync(stream, CancellationToken.None);

#if NET
    protected override Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context,
        CancellationToken cancellationToken)
        => SerializeToStreamAsync(stream, cancellationToken);
#endif

    protected override bool TryComputeLength(out long length)
    {
        length = 0;
        return false;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The exception is rethrown.")]
    private async Task SerializeToStreamAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref sent, 1) == 1)
        {
            throw new InvalidOperationException(
                "A streamed request body is sent once: its records are read as they are written, so a retry or a redirect cannot send them again. " +
                "The first attempt's failure is the cause - turn off the retries of an HttpClient resilience handler for this request.");
        }

        var enumerator = records.GetAsyncEnumerator(cancellationToken);
        var next = default(ValueTask<bool>);
        var pending = false;
        var disposeLater = false;
        try
        {
            next = enumerator.MoveNextAsync();
            pending = true;
            while (true)
            {
                // The source makes the request wait: what is written goes to the server now
                if (!next.IsCompleted)
                {
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }

                pending = false;
                if (!await next.ConfigureAwait(false))
                {
                    break;
                }

                await WriteRecordAsync(stream, enumerator.Current, cancellationToken).ConfigureAwait(false);
                next = enumerator.MoveNextAsync();
                pending = true;
            }
        }
        catch when (pending)
        {
            // The send failed while the source was still making its next record: the source is disposed once that
            // record has come (an iterator cannot be disposed while it runs), and the send's own error goes to the
            // caller - not one about the disposal
            disposeLater = true;
            var source = enumerator;
            _ = next.AsTask().ContinueWith(
                _ => source.DisposeAsync().AsTask(),
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default).Unwrap();
            throw;
        }
        finally
        {
            if (!disposeLater)
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }

        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteRecordAsync(
        Stream stream,
        T record,
        CancellationToken cancellationToken)
    {
        var json = record is null ? "null" : serializer.Serialize(record);
        if (!isSequence)
        {
            // A line break of an indenting serializer is whitespace (JSON escapes one inside a string), so a record
            // stays one line
            json = json.Replace('\r', ' ').Replace('\n', ' ');
        }

        var bytes = Encoding.UTF8.GetBytes(json);
        if (isSequence)
        {
            await StreamedRequestFraming.WriteAsync(stream, StreamedRequestFraming.RecordSeparator, cancellationToken).ConfigureAwait(false);
        }

        await StreamedRequestFraming.WriteAsync(stream, bytes, cancellationToken).ConfigureAwait(false);
        await StreamedRequestFraming.WriteAsync(stream, StreamedRequestFraming.LineFeed, cancellationToken).ConfigureAwait(false);
    }
}