namespace Atc.Rest.Client;

/// <summary>
/// Represents a binary file response from an endpoint.
/// </summary>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Binary content requires array for practical usage.")]
public sealed class BinaryEndpointResponse : IBinaryEndpointResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryEndpointResponse"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the request was successful.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="content">The binary content.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="fileName">The file name from Content-Disposition header.</param>
    /// <param name="contentLength">The content length.</param>
    public BinaryEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        byte[]? content,
        string? contentType,
        string? fileName,
        long? contentLength)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Content = content;
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
    /// Gets the binary content.
    /// </summary>
    public byte[]? Content { get; }

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
}