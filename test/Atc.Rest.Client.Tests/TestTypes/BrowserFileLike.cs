namespace Atc.Rest.Client.Tests.TestTypes;

/// <summary>
/// Mimics the shape of IBrowserFile: Name property + OpenReadStream(long, CancellationToken) with defaults.
/// </summary>
[SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Mimics IBrowserFile.OpenReadStream() method shape for duck-typing test")]
internal sealed class BrowserFileLike
{
    private readonly byte[] data;

    public BrowserFileLike(
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
        CancellationToken cancellationToken = default) => new MemoryStream(data);
}