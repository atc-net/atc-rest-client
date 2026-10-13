namespace Atc.Rest.Client.Tests.TestTypes;

// A response body the test feeds chunk by chunk: one read returns (at most) one chunk, so the test decides where
// the reader sees a chunk boundary - which a socket does not guarantee - and how much it has been given.
internal sealed class FeedStream : Stream
{
    private readonly Channel<byte[]> chunks = Channel.CreateUnbounded<byte[]>();
    private byte[] current = [];
    private int position;
    private long bytesRead;

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

    public static FeedStream From(params byte[][] chunks)
    {
        var stream = new FeedStream();
        foreach (var chunk in chunks)
        {
            stream.Feed(chunk);
        }

        stream.Complete();
        return stream;
    }

    public static FeedStream FromText(string text)
        => From(Encoding.UTF8.GetBytes(text));

    public void Feed(string text)
        => Feed(Encoding.UTF8.GetBytes(text));

    public void Feed(byte[] chunk)
    {
        if (chunk.Length > 0 && !chunks.Writer.TryWrite(chunk))
        {
            throw new InvalidOperationException("The stream is completed.");
        }
    }

    public void Complete()
        => chunks.Writer.TryComplete();

    public override void Flush()
    {
    }

    public override int Read(
        byte[] buffer,
        int offset,
        int count)
        => throw new NotSupportedException("Read asynchronously.");

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        if (position == current.Length)
        {
            if (!await chunks.Reader.WaitToReadAsync(cancellationToken) ||
                !chunks.Reader.TryRead(out var next))
            {
                return 0;
            }

            current = next;
            position = 0;
        }

        var taken = Math.Min(buffer.Length, current.Length - position);
        current.AsMemory(position, taken).CopyTo(buffer);
        position += taken;
        Interlocked.Add(ref bytesRead, taken);
        return taken;
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