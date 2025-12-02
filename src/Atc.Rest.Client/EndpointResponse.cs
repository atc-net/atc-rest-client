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

    public HttpStatusCode StatusCode { get; }

    public string Content { get; }

    public object? ContentObject { get; }

    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    protected TResult CastContent<TResult>()
        where TResult : class
    {
        return ContentObject as TResult ??
               throw new InvalidCastException($"ContentObject is not of type {typeof(TResult).Name}");
    }
}