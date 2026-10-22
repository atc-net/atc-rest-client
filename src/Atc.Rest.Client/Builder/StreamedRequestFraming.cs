namespace Atc.Rest.Client.Builder;

/// <summary>
/// The media types and framing of a request body sent as a stream of records.
/// </summary>
internal static class StreamedRequestFraming
{
    /// <summary>
    /// The line feed that ends a record.
    /// </summary>
    public static readonly byte[] LineFeed = [0x0A];

    /// <summary>
    /// The record separator that starts a record of a JSON Text Sequence.
    /// </summary>
    public static readonly byte[] RecordSeparator = [0x1E];

    /// <summary>
    /// Whether the media type is JSON Lines or one of its aliases: one JSON value per line.
    /// </summary>
    /// <param name="mediaType">The media type, without parameters.</param>
    /// <returns><see langword="true"/> for <c>application/jsonl</c>, <c>application/x-ndjson</c> and
    /// <c>application/x-jsonlines</c>.</returns>
    public static bool IsJsonLines(string? mediaType)
        => string.Equals(mediaType, "application/jsonl", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(mediaType, "application/x-ndjson", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(mediaType, "application/x-jsonlines", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the media type is a JSON Text Sequence, RFC 7464.
    /// </summary>
    /// <param name="mediaType">The media type, without parameters.</param>
    /// <returns><see langword="true"/> for <c>application/json-seq</c> and a <c>+json-seq</c> type (RFC 8091).</returns>
    public static bool IsJsonSequence(string? mediaType)
        => mediaType is not null &&
           (mediaType.Equals("application/json-seq", StringComparison.OrdinalIgnoreCase) ||
            mediaType.EndsWith("+json-seq", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Writes the bytes to the stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="bytes">The bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The write.</returns>
    public static Task WriteAsync(
        Stream stream,
        byte[] bytes,
        CancellationToken cancellationToken)
#if NET
        => stream.WriteAsync(bytes.AsMemory(), cancellationToken).AsTask();
#else
        => stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
#endif
}