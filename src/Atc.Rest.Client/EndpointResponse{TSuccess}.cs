namespace Atc.Rest.Client;

/// <summary>
/// Represents a response from an HTTP endpoint with a typed success content.
/// </summary>
/// <typeparam name="TSuccess">The type of the success content.</typeparam>
public class EndpointResponse<TSuccess>
    : EndpointResponse
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
    /// <remarks>
    /// The <paramref name="contentObject"/> is typed as <see cref="object"/> to allow passing <see langword="null"/>
    /// even when <typeparamref name="TSuccess"/> is a value type (e.g., for failure responses).
    /// </remarks>
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
    public TSuccess? SuccessContent => IsSuccess ? CastContent<TSuccess>() : default;
}