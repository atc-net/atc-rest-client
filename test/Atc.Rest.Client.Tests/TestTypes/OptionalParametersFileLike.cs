namespace Atc.Rest.Client.Tests.TestTypes;

/// <summary>
/// File-like duck-type shape whose OpenReadStream has only optional parameters,
/// but not the IBrowserFile (long, CancellationToken) signature.
/// </summary>
internal sealed class OptionalParametersFileLike
{
    private readonly byte[] data;

    public OptionalParametersFileLike(
        string fileName,
        byte[] data)
    {
        FileName = fileName;
        this.data = data;
    }

    public string FileName { get; }

    public int? CapturedBufferSize { get; private set; }

    public string? CapturedMode { get; private set; }

    public Stream OpenReadStream(
        int bufferSize = 4096,
        string mode = "read")
    {
        CapturedBufferSize = bufferSize;
        CapturedMode = mode;
        return new MemoryStream(data);
    }
}