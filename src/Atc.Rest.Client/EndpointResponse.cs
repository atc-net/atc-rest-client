// ReSharper disable ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
namespace Atc.Rest.Client;

public class EndpointResponse : IEndpointResponse
{
    public EndpointResponse(EndpointResponse response)
        : this(
            response?.IsSuccess ?? throw new ArgumentNullException(nameof(response)),
            response.StatusCode,
            response.Content,
            response.ContentObject,
            response.Headers)
    {
    }

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

    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the status code indicates OK (200).
    /// </summary>
    public bool IsOk => StatusCode == HttpStatusCode.OK;

    public HttpStatusCode StatusCode { get; }

    public string Content { get; }

    public object? ContentObject { get; }

    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    protected TResult CastContent<TResult>()
        where TResult : class
        => ContentObject as TResult ??
           throw new InvalidCastException($"ContentObject is not of type {typeof(TResult).Name}");

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