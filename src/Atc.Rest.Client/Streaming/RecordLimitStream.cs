namespace Atc.Rest.Client.Streaming;

/// <summary>
/// What a parser reads through when the records of a stream are split by the parser itself (the JSON reader, the
/// server-sent events parser): the bytes are counted where they arrive, so a record longer than the limit is an
/// <see cref="InvalidDataException"/> before the parser behind it has buffered it.
/// </summary>
/// <remarks>
/// No more than the rest of the record's allowance and one byte is asked of the stream at a time. The bytes before
/// the one that takes a record past the limit are still handed on, so the records before it are read; the next read
/// throws. The stream read from is not this one's to close.
/// </remarks>
internal abstract class RecordLimitStream : Stream
{
    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "The stream belongs to the HTTP response, which disposes it.")]
    private readonly Stream inner;
    private readonly string description;
    private InvalidDataException? pendingException;

    protected RecordLimitStream(
        Stream inner,
        int maxLength,
        string description)
    {
        this.inner = inner;
        this.description = description;
        MaxLength = maxLength;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    protected int MaxLength { get; }

    /// <summary>
    /// Gets the length of the record the next byte belongs to. Never more than <see cref="MaxLength"/>.
    /// </summary>
    protected abstract long RecordLength { get; }

    public override void Flush()
    {
    }

    public override int Read(
        byte[] buffer,
        int offset,
        int count)
    {
        ThrowIfPastTheLimit();
        var read = inner.Read(buffer, offset, Limit(count));
        return Accept(buffer.AsSpan(offset, read));
    }

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        ThrowIfPastTheLimit();
#if NET
        var read = await inner.ReadAsync(buffer.AsMemory(offset, Limit(count)), cancellationToken).ConfigureAwait(false);
#else
        var read = await inner.ReadAsync(buffer, offset, Limit(count), cancellationToken).ConfigureAwait(false);
#endif
        return Accept(buffer.AsSpan(offset, read));
    }

#if NET
    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ThrowIfPastTheLimit();
        var read = await inner.ReadAsync(buffer.Slice(0, Limit(buffer.Length)), cancellationToken).ConfigureAwait(false);
        return Accept(buffer.Span.Slice(0, read));
    }
#endif

    public override long Seek(
        long offset,
        SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(
        byte[] buffer,
        int offset,
        int count)
        => throw new NotSupportedException();

    /// <summary>
    /// Counts the bytes that arrived.
    /// </summary>
    /// <param name="bytes">The bytes that arrived.</param>
    /// <returns>
    /// The number of bytes within the limit: the length of <paramref name="bytes"/>, or the index of the byte that
    /// takes a record past the limit.
    /// </returns>
    protected abstract int Count(ReadOnlySpan<byte> bytes);

    private int Limit(int count)
        => (int)Math.Min(count, MaxLength - RecordLength + 1);

    private void ThrowIfPastTheLimit()
    {
        if (pendingException is not null)
        {
            throw pendingException;
        }
    }

    private int Accept(ReadOnlySpan<byte> bytes)
    {
        var accepted = Count(bytes);
        if (accepted == bytes.Length)
        {
            return accepted;
        }

        pendingException = StreamRecordLimit.CreateException(description, MaxLength);
        if (accepted == 0)
        {
            throw pendingException;
        }

        return accepted;
    }
}