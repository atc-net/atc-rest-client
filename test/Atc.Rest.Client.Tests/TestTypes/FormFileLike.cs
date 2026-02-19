namespace Atc.Rest.Client.Tests.TestTypes;

/// <summary>
/// Mimics the shape of IFormFile: FileName property + parameterless OpenReadStream().
/// </summary>
[SuppressMessage("Design", "CA1024:Use properties where appropriate", Justification = "Mimics IFormFile.OpenReadStream() method shape for duck-typing test")]
internal sealed class FormFileLike
{
    private readonly byte[] data;

    public FormFileLike(
        string fileName,
        string contentType,
        byte[] data)
    {
        FileName = fileName;
        ContentType = contentType;
        this.data = data;
    }

    public string FileName { get; }

    public string ContentType { get; }

    public Stream OpenReadStream() => new MemoryStream(data);
}