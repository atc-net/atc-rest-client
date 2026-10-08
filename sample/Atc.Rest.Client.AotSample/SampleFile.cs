namespace Atc.Rest.Client.AotSample;

public sealed class SampleFile : IFileContent
{
    private readonly byte[] data;

    public SampleFile(
        string fileName,
        string? contentType,
        byte[] data)
    {
        FileName = fileName;
        ContentType = contentType;
        this.data = data;
    }

    public string FileName { get; }

    public string? ContentType { get; }

    public Stream OpenReadStream()
        => new MemoryStream(data, writable: false);
}