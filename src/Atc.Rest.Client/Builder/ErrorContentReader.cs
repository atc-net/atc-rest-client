namespace Atc.Rest.Client.Builder;

/// <summary>
/// Reads the body of an error response as text, up to a limit. An error body is diagnostic text for the caller; a
/// peer that answers an error with an endless body must not be able to grow the memory of the process with it.
/// </summary>
internal static class ErrorContentReader
{
    private const int InitialCapacity = 8192;

    // The longest array the runtime allocates (Array.MaxLength, which netstandard2.0 does not have)
    private const int MaxCapacity = 0x7FFFFFC7;

    // UTF-32 before UTF-16: its byte order mark starts with that of UTF-16
    private static readonly Encoding[] EncodingsWithByteOrderMark =
    [
        Encoding.UTF8,
        Encoding.UTF32,
        Encoding.Unicode,
        Encoding.BigEndianUnicode,
    ];

    public static int Validate(int maxErrorContentLength)
        => maxErrorContentLength > 0
            ? maxErrorContentLength
            : throw new ArgumentOutOfRangeException(
                nameof(maxErrorContentLength),
                maxErrorContentLength,
                "The most of an error response body that is read (MaxErrorContentLength of AtcRestClientOptions) must be greater than zero.");

    /// <summary>
    /// Reads the body up to the limit, and one byte more to know whether there was more. The rest is not read.
    /// </summary>
    /// <param name="content">The content of the error response.</param>
    /// <param name="maxLength">The most of the body that is read, in bytes.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The text, decoded with the charset of the Content-Type (UTF-8 without one), and whether the body was longer
    /// than the limit. A text that was cut ends at a character boundary.
    /// </returns>
    public static async Task<(string Text, bool IsTruncated)> ReadAsync(
        HttpContent content,
        int maxLength,
        CancellationToken cancellationToken)
    {
        var stream = await content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        // A content without a stream - a test double of HttpContent - has no body
        if (stream is null)
        {
            return (string.Empty, false);
        }

        // In long: a limit of int.MaxValue has no byte after it
        var capacityLimit = Math.Min((long)maxLength + 1, MaxCapacity);
        var body = new byte[Math.Min(InitialCapacity, capacityLimit)];
        var length = 0;

        while (length <= maxLength)
        {
            if (length == body.Length)
            {
                if (body.Length == capacityLimit)
                {
                    break;
                }

                Array.Resize(ref body, (int)Math.Min(body.Length * 2L, capacityLimit));
            }

#if NET
            var read = await stream.ReadAsync(body.AsMemory(length), cancellationToken).ConfigureAwait(false);
#else
            var read = await stream.ReadAsync(body, length, body.Length - length, cancellationToken).ConfigureAwait(false);
#endif
            if (read == 0)
            {
                break;
            }

            length += read;
        }

        var isTruncated = length > maxLength;
        if (isTruncated)
        {
            length = maxLength;
        }

        return (Decode(body, length, content.Headers.ContentType?.CharSet, isTruncated), isTruncated);
    }

    private static string Decode(
        byte[] bytes,
        int length,
        string? charset,
        bool isTruncated)
    {
        var encoding = GetEncoding(bytes, length, charset);
        var preamble = encoding.GetPreamble();
        var offset = StartsWith(bytes, length, preamble)
            ? preamble.Length
            : 0;

        // Not flushed when the body was cut: the bytes of a character that the cut went through are left out
        var chars = new char[encoding.GetMaxCharCount(length - offset)];
        var count = encoding
            .GetDecoder()
            .GetChars(bytes, offset, length - offset, chars, 0, flush: !isTruncated);

        return new string(chars, 0, count);
    }

    private static Encoding GetEncoding(
        byte[] bytes,
        int length,
        string? charset)
    {
        var name = charset?.Trim('"');
        if (!string.IsNullOrWhiteSpace(name))
        {
            try
            {
                return Encoding.GetEncoding(name!);
            }
            catch (ArgumentException)
            {
                // A charset that is not known must not turn an error response into an exception
            }
        }

        return Array.Find(EncodingsWithByteOrderMark, x => StartsWith(bytes, length, x.GetPreamble()))
            ?? Encoding.UTF8;
    }

    private static bool StartsWith(
        byte[] bytes,
        int length,
        byte[] preamble)
        => preamble.Length > 0 &&
           length >= preamble.Length &&
           bytes.AsSpan(0, preamble.Length).SequenceEqual(preamble);
}