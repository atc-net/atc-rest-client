namespace Atc.Rest.Client;

/// <summary>
/// Represents a streaming binary response from an endpoint.
/// </summary>
public class StreamBinaryEndpointResponse : IStreamBinaryEndpointResponse
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
    /// <param name="errorContent">The error content if the request failed.</param>
    public StreamBinaryEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        Stream? contentStream,
        string? contentType,
        string? fileName,
        long? contentLength,
        string? errorContent = null)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Content = contentStream;
        ContentType = contentType;
        FileName = fileName;
        ContentLength = contentLength;
        ErrorContent = errorContent;
    }

    /// <summary>
    /// Gets a value indicating whether the request was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the content stream.
    /// </summary>
    public Stream? Content { get; }

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
    /// Gets the error content if the request failed.
    /// </summary>
    public string? ErrorContent { get; }

    /// <summary>
    /// Disposes the content stream.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates an exception for invalid content access with detailed error information.
    /// </summary>
    /// <param name="expectedStatusCode">The expected HTTP status code.</param>
    /// <param name="propertyName">The name of the property being accessed.</param>
    /// <returns>An <see cref="InvalidOperationException"/> with detailed error information.</returns>
    protected InvalidOperationException InvalidContentAccessException(
        HttpStatusCode expectedStatusCode,
        string propertyName)
        => new(
            $"Cannot access {propertyName}. " +
            $"Expected status {(int)expectedStatusCode} ({expectedStatusCode}), " +
            $"but got {(int)StatusCode} ({StatusCode}).");

    /// <summary>
    /// Disposes managed resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            Content?.Dispose();
        }

        disposed = true;
    }
}