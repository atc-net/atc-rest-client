namespace Atc.Rest.Client;

/// <summary>
/// Represents a response from an HTTP endpoint.
/// </summary>
public interface IEndpointResponse
{
    /// <summary>
    /// Gets a value indicating whether the request was successful.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the status code indicates OK (200).
    /// </summary>
    bool IsOk { get; }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the raw response content as a string.
    /// </summary>
    string Content { get; }

    /// <summary>
    /// Gets the deserialized content object.
    /// </summary>
    object? ContentObject { get; }

    /// <summary>
    /// Gets the response headers.
    /// </summary>
    IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }
}