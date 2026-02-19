namespace Atc.Rest.Client.Tests.TestTypes;

/// <summary>
/// IFileContent implementation that returns a non-seekable stream.
/// Tests that BuildFormFileContent uses CopyTo (not stream.Length).
/// </summary>
internal sealed class NonSeekableFileContent : IFileContent
{
    private readonly byte[] data;

    public NonSeekableFileContent(
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

    public Stream OpenReadStream() => new NonSeekableStream(data);
}