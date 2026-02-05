namespace Atc.Rest.Client;

/// <summary>
/// Represents a streaming response from an endpoint that yields items as they are deserialized.
/// </summary>
/// <typeparam name="T">The type of items in the stream.</typeparam>
public class StreamingEndpointResponse<T> : IStreamingEndpointResponse<T>
{
    private readonly HttpResponseMessage? httpResponse;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingEndpointResponse{T}"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the request was successful.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="content">The streaming content.</param>
    /// <param name="errorContent">The error content if the request failed.</param>
    /// <param name="httpResponse">The HTTP response message for lifecycle management.</param>
    public StreamingEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        IAsyncEnumerable<T?>? content,
        string? errorContent,
        HttpResponseMessage? httpResponse)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Content = content;
        ErrorContent = errorContent;
        this.httpResponse = httpResponse;
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
    /// Gets the streaming content as an async enumerable.
    /// </summary>
    public IAsyncEnumerable<T?>? Content { get; }

    /// <summary>
    /// Gets the error content if the request failed.
    /// </summary>
    public string? ErrorContent { get; }

    /// <summary>
    /// Disposes the underlying HTTP response.
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
            httpResponse?.Dispose();
        }

        disposed = true;
    }
}