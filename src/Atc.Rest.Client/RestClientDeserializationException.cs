namespace Atc.Rest.Client;

/// <summary>
/// Exception thrown when the REST client fails to deserialize response content.
/// </summary>
public class RestClientDeserializationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestClientDeserializationException"/> class.
    /// </summary>
    public RestClientDeserializationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestClientDeserializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public RestClientDeserializationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestClientDeserializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused deserialization to fail.</param>
    public RestClientDeserializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RestClientDeserializationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception that caused deserialization to fail.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="rawContent">The raw content that failed to deserialize.</param>
    public RestClientDeserializationException(
        string message,
        Exception innerException,
        HttpStatusCode statusCode,
        string rawContent)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        RawContent = rawContent;
    }

    /// <summary>
    /// Gets the HTTP status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Gets the raw content that failed to deserialize.
    /// </summary>
    public string RawContent { get; } = string.Empty;
}