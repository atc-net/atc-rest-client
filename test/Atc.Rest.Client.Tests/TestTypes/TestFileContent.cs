namespace Atc.Rest.Client.Tests.TestTypes;

internal sealed class TestFileContent : IFileContent
{
    private readonly byte[] data;

    public TestFileContent(
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

    public Stream OpenReadStream() => new MemoryStream(data);
}