namespace Atc.Rest.Client;

/// <summary>
/// Represents a streaming response from an endpoint that yields items as they are deserialized.
/// </summary>
/// <typeparam name="T">The type of items in the stream.</typeparam>
public interface IStreamingEndpointResponse<out T> : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the request was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the streaming content as an async enumerable.
    /// </summary>
    IAsyncEnumerable<T?>? Content { get; }

    /// <summary>
    /// Gets the error content if the request failed.
    /// </summary>
    string? ErrorContent { get; }

    /// <summary>
    /// Gets the object the error content was deserialized to, when the status code was registered with a type
    /// (e.g. <c>AddErrorResponse&lt;TError&gt;(status)</c>); otherwise <see langword="null"/>.
    /// </summary>
    object? ErrorContentObject { get; }

    /// <summary>
    /// Gets the response headers (and content headers). Names are looked up without regard to casing.
    /// </summary>
    IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }
}