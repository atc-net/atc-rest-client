namespace Atc.Rest.Client.Tests.TestTypes;

// A response body from a peer that never ends a record: a prefix, then the same byte for as long as it is read.
internal sealed class EndlessStream : Stream
{
    private readonly byte[] prefix;
    private readonly byte filler;
    private long bytesRead;

    public EndlessStream(
        string prefix,
        char filler)
    {
        this.prefix = Encoding.UTF8.GetBytes(prefix);
        this.filler = (byte)filler;
    }

    public long BytesRead => Interlocked.Read(ref bytesRead);

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
    {
        var span = buffer.AsSpan(offset, count);
        var read = Interlocked.Read(ref bytesRead);
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = read + i < prefix.Length ? prefix[read + i] : filler;
        }

        Interlocked.Add(ref bytesRead, span.Length);
        return span.Length;
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
        => Task.FromResult(Read(buffer, offset, count));

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var array = new byte[buffer.Length];
        var read = Read(array, 0, array.Length);
        array.CopyTo(buffer);
        return new ValueTask<int>(read);
    }

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
}