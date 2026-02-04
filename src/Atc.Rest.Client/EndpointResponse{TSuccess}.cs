namespace Atc.Rest.Client;

/// <summary>
/// Represents a response from an HTTP endpoint with a typed success content.
/// </summary>
/// <typeparam name="TSuccess">The type of the success content.</typeparam>
public class EndpointResponse<TSuccess>
    : EndpointResponse
    where TSuccess : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointResponse{TSuccess}"/> class by copying from another response.
    /// </summary>
    /// <param name="response">The response to copy from.</param>
    public EndpointResponse(EndpointResponse response)
        : base(response)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointResponse{TSuccess}"/> class.
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
        TSuccess? contentObject,
        IReadOnlyDictionary<string, IEnumerable<string>> headers)
        : base(isSuccess, statusCode, content, contentObject, headers)
    {
    }

    /// <summary>
    /// Gets the success content if the request was successful; otherwise, null.
    /// </summary>
    public TSuccess? SuccessContent => IsSuccess ? CastContent<TSuccess>() : null;
}