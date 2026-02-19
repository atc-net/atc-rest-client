namespace Atc.Rest.Client.Tests.TestTypes;

/// <summary>
/// A stream wrapper that does not support Length, Position, or Seek.
/// Simulates the non-seekable stream returned by IBrowserFile.OpenReadStream().
/// </summary>
internal sealed class NonSeekableStream : Stream
{
    private readonly MemoryStream inner;

    public NonSeekableStream(byte[] data) => inner = new MemoryStream(data);

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(
        byte[] buffer,
        int offset,
        int count)
        => inner.Read(buffer, offset, count);

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

    public override void Flush()
        => inner.Flush();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            inner.Dispose();
        }

        base.Dispose(disposing);
    }
}