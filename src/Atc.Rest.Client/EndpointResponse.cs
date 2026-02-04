// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
namespace Atc.Rest.Client;

/// <summary>
/// Represents a response from an HTTP endpoint.
/// </summary>
public class EndpointResponse : IEndpointResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointResponse"/> class by copying from another response.
    /// </summary>
    /// <param name="response">The response to copy from.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is null.</exception>
    public EndpointResponse(EndpointResponse response)
        : this(
            response?.IsSuccess ?? throw new ArgumentNullException(nameof(response)),
            response.StatusCode,
            response.Content,
            response.ContentObject,
            response.Headers)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointResponse"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the request was successful.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <param name="content">The raw response content as a string.</param>
    /// <param name="contentObject">The deserialized content object.</param>
    /// <param name="headers">The response headers.</param>
    public EndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        string content,
        object? contentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
    {
        IsSuccess = isSuccess;
        StatusCode = statusCode;
        Content = content;
        ContentObject = contentObject;
        Headers = headers;
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
    /// Gets the raw response content as a string.
    /// </summary>
    public string Content { get; }

    /// <summary>
    /// Gets the deserialized content object.
    /// </summary>
    public object? ContentObject { get; }

    /// <summary>
    /// Gets the response headers.
    /// </summary>
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    /// <summary>
    /// Casts the content object to the specified type.
    /// </summary>
    /// <typeparam name="TResult">The type to cast to.</typeparam>
    /// <returns>The content object cast to the specified type.</returns>
    /// <exception cref="InvalidCastException">Thrown when the content object cannot be cast to the specified type.</exception>
    protected TResult CastContent<TResult>()
        where TResult : class
        => ContentObject as TResult ??
           throw new InvalidCastException($"ContentObject is not of type {typeof(TResult).Name}");

    /// <summary>
    /// Creates an exception for invalid content access with detailed error information.
    /// </summary>
    /// <typeparam name="TExpected">The expected content type.</typeparam>
    /// <param name="expectedStatusCode">The expected HTTP status code.</param>
    /// <param name="propertyName">The name of the property being accessed.</param>
    /// <returns>An <see cref="InvalidOperationException"/> with detailed error information.</returns>
    protected InvalidOperationException InvalidContentAccessException<TExpected>(
        HttpStatusCode expectedStatusCode,
        string propertyName)
        where TExpected : class
    {
        var actualType = ContentObject?.GetType().Name ?? "null";
        var expectedType = typeof(TExpected).Name;

        return new InvalidOperationException(
            $"Cannot access {propertyName}. " +
            $"Expected status {(int)expectedStatusCode} ({expectedStatusCode}) with {expectedType} content, " +
            $"but got {(int)StatusCode} ({StatusCode}) with {actualType} content. " +
            $"Response: {Content}");
    }
}