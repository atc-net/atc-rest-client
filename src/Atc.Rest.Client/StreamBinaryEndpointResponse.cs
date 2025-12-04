namespace Atc.Rest.Client;

/// <summary>
/// Represents a streaming binary response from an endpoint.
/// </summary>
public sealed class StreamBinaryEndpointResponse : IStreamBinaryEndpointResponse
{
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamBinaryEndpointResponse"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the request was successful.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="contentStream">The content stream.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="fileName">The file name from Content-Disposition header.</param>
    /// <param name="contentLength">The content length.</param>
    public StreamBinaryEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        Stream? contentStream,
        string? contentType,
        string? fileName,
        long? contentLength)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        ContentStream = contentStream;
        ContentType = contentType;
        FileName = fileName;
        ContentLength = contentLength;
    }

    /// <summary>
    /// Gets a value indicating whether the request was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the status code indicates OK (200).
    /// </summary>
    public bool IsOk => StatusCode == HttpStatusCode.OK;

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the content stream.
    /// </summary>
    public Stream? ContentStream { get; }

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// Gets the file name from the Content-Disposition header.
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    /// Gets the content length.
    /// </summary>
    public long? ContentLength { get; }

    /// <summary>
    /// Disposes the content stream.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    private void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            ContentStream?.Dispose();
        }

        disposed = true;
    }
}