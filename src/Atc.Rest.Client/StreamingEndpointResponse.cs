namespace Atc.Rest.Client;

/// <summary>
/// Represents a streaming response from an endpoint that yields items as they are deserialized.
/// </summary>
/// <typeparam name="T">The type of items in the stream.</typeparam>
public class StreamingEndpointResponse<T> : IStreamingEndpointResponse<T>
{
    private readonly ServerSentEventState? eventState;
    private HttpResponseMessage? httpResponse;
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
        : this(
            isSuccess,
            statusCode,
            content,
            errorContent,
            httpResponse,
            errorContentObject: null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingEndpointResponse{T}"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the request was successful.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="content">The streaming content.</param>
    /// <param name="errorContent">The error content if the request failed.</param>
    /// <param name="httpResponse">The HTTP response message for lifecycle management.</param>
    /// <param name="errorContentObject">The error content deserialized to the type registered for the status code.</param>
    /// <param name="headers">The response and content headers.</param>
    public StreamingEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        IAsyncEnumerable<T?>? content,
        string? errorContent,
        HttpResponseMessage? httpResponse,
        object? errorContentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
        : this(
            isSuccess,
            statusCode,
            content,
            errorContent,
            httpResponse,
            errorContentObject,
            headers,
            eventState: null)
    {
    }

    internal StreamingEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        IAsyncEnumerable<T?>? content,
        string? errorContent,
        HttpResponseMessage? httpResponse,
        object? errorContentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> headers,
        ServerSentEventState? eventState)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Content = content;
        ErrorContent = errorContent;
        ErrorContentObject = errorContentObject;
        Headers = headers ?? new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        this.httpResponse = httpResponse;
        this.eventState = eventState;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamingEndpointResponse{T}"/> class that takes over the items,
    /// the error and the ownership of the HTTP response from another instance.
    /// </summary>
    /// <remarks>
    /// The HTTP response is moved, not shared: disposing the new instance disposes it, and disposing
    /// <paramref name="response"/> afterwards does not.
    /// </remarks>
    /// <param name="response">The response to take over.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is null.</exception>
    protected StreamingEndpointResponse(StreamingEndpointResponse<T> response)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        IsSuccess = response.IsSuccess;
        StatusCode = response.StatusCode;
        Content = response.Content;
        ErrorContent = response.ErrorContent;
        ErrorContentObject = response.ErrorContentObject;
        Headers = response.Headers;
        httpResponse = response.httpResponse;
        eventState = response.eventState;
        disposed = response.disposed;
        response.httpResponse = null;
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
    /// Gets the object the error content was deserialized to, when the status code was registered with a type
    /// (e.g. <c>AddErrorResponse&lt;TError&gt;(status)</c>); otherwise <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// It is also <see langword="null"/> when the error content is empty or cannot be deserialized to the registered
    /// type. The text stays available in <see cref="ErrorContent"/>.
    /// </remarks>
    public object? ErrorContentObject { get; }

    /// <summary>
    /// Gets the response headers (and content headers). Never <see langword="null"/>; names are looked up without
    /// regard to casing.
    /// </summary>
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    /// <summary>
    /// Gets the id of the last server-sent event read so far - what to send as the <c>Last-Event-ID</c> request
    /// header to continue the stream after it.
    /// </summary>
    /// <remarks>
    /// It follows the enumeration of <see cref="Content"/>: while an item is handled, it is the id that was current
    /// when that item was yielded, and it is also set by an event that is no item (a heartbeat with an id).
    /// <see langword="null"/> when no event with an id has been read and for a stream that is not server-sent
    /// events; empty when the server has reset the id, in which case no header is to be sent.
    /// It is <see langword="null"/> also when the response was not built by this library's reader - with a public
    /// constructor, around a reader of the caller's own. <see cref="ServerSentEventReader.Read{T}"/> builds a
    /// response that has it from a stream and a deserializer.
    /// </remarks>
    public string? LastEventId => eventState?.LastEventId;

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
            httpResponse = null;
        }

        disposed = true;
    }
}