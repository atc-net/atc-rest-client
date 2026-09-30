namespace Atc.Rest.Client;

/// <summary>
/// Represents a binary file response from an endpoint.
/// </summary>
[SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Binary content requires array for practical usage.")]
public class BinaryEndpointResponse : IBinaryEndpointResponse
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
    /// <param name="errorContent">The error content if the request failed.</param>
    public BinaryEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        byte[]? content,
        string? contentType,
        string? fileName,
        long? contentLength,
        string? errorContent = null)
        : this(
            isSuccess,
            statusCode,
            content,
            contentType,
            fileName,
            contentLength,
            errorContent,
            errorContentObject: null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BinaryEndpointResponse"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the request was successful.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="content">The binary content.</param>
    /// <param name="contentType">The content type.</param>
    /// <param name="fileName">The file name from Content-Disposition header.</param>
    /// <param name="contentLength">The content length.</param>
    /// <param name="errorContent">The error content if the request failed.</param>
    /// <param name="errorContentObject">The error content deserialized to the type registered for the status code.</param>
    /// <param name="headers">The response and content headers.</param>
    public BinaryEndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        byte[]? content,
        string? contentType,
        string? fileName,
        long? contentLength,
        string? errorContent,
        object? errorContentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Content = content;
        ContentType = contentType;
        FileName = fileName;
        ContentLength = contentLength;
        ErrorContent = errorContent;
        ErrorContentObject = errorContentObject;
        Headers = headers;
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

    /// <summary>
    /// Gets the error content if the request failed.
    /// </summary>
    public string? ErrorContent { get; }

    /// <summary>
    /// Gets the error content deserialized to the type registered for the status code
    /// (e.g. with <c>AddErrorResponse&lt;T&gt;</c>), or <see langword="null"/> when the request succeeded,
    /// no type is registered, or deserialization failed. The raw text is always in <see cref="ErrorContent"/>.
    /// </summary>
    public object? ErrorContentObject { get; }

    /// <summary>
    /// Gets the response and content headers.
    /// </summary>
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

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
}