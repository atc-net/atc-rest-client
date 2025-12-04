namespace Atc.Rest.Client;

/// <summary>
/// Represents a binary file response from an endpoint.
/// </summary>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Binary content requires array for practical usage.")]
public interface IBinaryEndpointResponse
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
    /// Gets the binary content.
    /// </summary>
    byte[]? Content { get; }

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
}