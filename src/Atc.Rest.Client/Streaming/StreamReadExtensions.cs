namespace Atc.Rest.Client.Streaming;

internal static class StreamReadExtensions
{
    public static ValueTask<int> ReadChunkAsync(
        this Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
#if NET
        => stream.ReadAsync(buffer.AsMemory(), cancellationToken);
#else
        => new(stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken));
#endif
}