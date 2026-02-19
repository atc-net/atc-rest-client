namespace Atc.Rest.Client.Tests.TestTypes;

/// <summary>
/// IBrowserFile duck-type shape that captures the arguments passed to OpenReadStream.
/// </summary>
[SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Mimics IBrowserFile.OpenReadStream() method shape for duck-typing test")]
internal sealed class CapturingBrowserFileLike
{
    private readonly byte[] data;

    public CapturingBrowserFileLike(
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

    public long? CapturedMaxAllowedSize { get; private set; }

    public CancellationToken? CapturedCancellationToken { get; private set; }

    public Stream OpenReadStream(
        long maxAllowedSize = 512000,
        CancellationToken cancellationToken = default)
    {
        CapturedMaxAllowedSize = maxAllowedSize;
        CapturedCancellationToken = cancellationToken;
        return new MemoryStream(data);
    }
}