namespace Atc.Rest.Client;

public class EndpointResponse<TSuccess>
    : EndpointResponse
    where TSuccess : class
{
    public EndpointResponse(EndpointResponse response)
        : base(response)
    {
    }

    public EndpointResponse(
        bool isSuccess,
        HttpStatusCode statusCode,
        string content,
        TSuccess? contentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
        : base(isSuccess, statusCode, content, contentObject, headers)
    {
    }

    public TSuccess? SuccessContent => IsSuccess ? CastContent<TSuccess>() : null;
}