namespace Atc.Rest.Client.Tests.TestTypes;

/// <summary>
/// IBrowserFile duck-type shape that returns a non-seekable stream.
/// End-to-end test of duck-typing + long.MaxValue + non-seekable stream.
/// </summary>
[SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Mimics IBrowserFile.OpenReadStream() method shape for duck-typing test")]
internal sealed class NonSeekableBrowserFileLike
{
    private readonly byte[] data;

    public NonSeekableBrowserFileLike(
        string name,
        string contentType,
        byte[] data)
    {
        Name = name;
        ContentType = contentType;
        this.data = data;
    }

    public string Name { get; }

    public string ContentType { get; }

    public Stream OpenReadStream(
        long maxAllowedSize = 512000,
        CancellationToken cancellationToken = default) => new NonSeekableStream(data);
}