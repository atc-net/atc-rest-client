namespace Atc.Rest.Client;

/// <summary>
/// Represents a response from an HTTP endpoint with typed success and error content.
/// </summary>
/// <typeparam name="TSuccess">The type of the success content.</typeparam>
/// <typeparam name="TError">The type of the error content.</typeparam>
public class EndpointResponse<TSuccess, TError>
    : EndpointResponse
    where TSuccess : class
    where TError : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointResponse{TSuccess, TError}"/> class by copying from another response.
    /// </summary>
    /// <param name="response">The response to copy from.</param>
    public EndpointResponse(EndpointResponse response)
        : base(response)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointResponse{TSuccess, TError}"/> class.
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
        : base(isSuccess, statusCode, content, contentObject, headers)
    {
    }

    /// <summary>
    /// Gets the success content if the request was successful; otherwise, null.
    /// </summary>
    public TSuccess? SuccessContent => IsSuccess ? CastContent<TSuccess>() : null;

    /// <summary>
    /// Gets the error content if the request failed; otherwise, null.
    /// </summary>
    public TError? ErrorContent => !IsSuccess ? CastContent<TError>() : null;
}