namespace Atc.Rest.Client.Streaming;

/// <summary>
/// The bytes that were read ahead of a stream, and then the rest of that stream. The stream read from is not this
/// one's to close.
/// </summary>
internal sealed class PrefixedStream : Stream
{
    private readonly byte[] prefix;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "The stream belongs to the HTTP response, which disposes it.")]
    private readonly Stream inner;
    private int position;

    public PrefixedStream(
        byte[] prefix,
        Stream inner)
    {
        this.prefix = prefix;
        this.inner = inner;
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

    public override void Flush()
    {
    }

    public override int Read(
        byte[] buffer,
        int offset,
        int count)
        => TryReadPrefix(buffer.AsSpan(offset, count), out var read)
            ? read
            : inner.Read(buffer, offset, count);

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
        => TryReadPrefix(buffer.AsSpan(offset, count), out var read)
            ? Task.FromResult(read)
#if NET
            : inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
#else
            : inner.ReadAsync(buffer, offset, count, cancellationToken);
#endif

#if NET
    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
        => TryReadPrefix(buffer.Span, out var read)
            ? new ValueTask<int>(read)
            : inner.ReadAsync(buffer, cancellationToken);
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

    private bool TryReadPrefix(
        Span<byte> buffer,
        out int read)
    {
        read = Math.Min(buffer.Length, prefix.Length - position);
        if (read == 0)
        {
            return false;
        }

        prefix.AsSpan(position, read).CopyTo(buffer);
        position += read;
        return true;
    }
}