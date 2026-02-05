namespace Atc.Rest.Client;

/// <summary>
/// Represents a streaming binary response from an endpoint.
/// </summary>
public interface IStreamBinaryEndpointResponse : IDisposable
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
    /// Gets the content stream.
    /// </summary>
    Stream? Content { get; }

    /// <summary>
    /// Gets the content type.
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// Gets the file name from the Content-Disposition header.
    /// </summary>
    string? FileName { get; }

    /// <summary>
    /// Gets the content length.
    /// </summary>
    long? ContentLength { get; }

    /// <summary>
    /// Gets the error content if the request failed.
    /// </summary>
    string? ErrorContent { get; }
}