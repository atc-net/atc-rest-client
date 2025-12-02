namespace Atc.Rest.Client.Builder;

public interface IMessageResponseBuilder
{
    IMessageResponseBuilder AddSuccessResponse(
        HttpStatusCode statusCode);

    IMessageResponseBuilder AddSuccessResponse<TResponseContent>(
        HttpStatusCode statusCode);

    IMessageResponseBuilder AddErrorResponse(
        HttpStatusCode statusCode);

    IMessageResponseBuilder AddErrorResponse<TResponseContent>(
        HttpStatusCode statusCode);

    Task<TResult> BuildResponseAsync<TResult>(
        Func<EndpointResponse, TResult> factory,
        CancellationToken cancellationToken);

    Task<EndpointResponse<TSuccessContent>>
        BuildResponseAsync<TSuccessContent>(
            CancellationToken cancellationToken)
        where TSuccessContent : class;

    Task<EndpointResponse<TSuccessContent, TErrorContent>>
        BuildResponseAsync<TSuccessContent, TErrorContent>(
            CancellationToken cancellationToken)
        where TSuccessContent : class
        where TErrorContent : class;

    /// <summary>
    /// Builds a binary response from the HTTP response.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="BinaryEndpointResponse"/> containing the binary content.</returns>
    Task<BinaryEndpointResponse> BuildBinaryResponseAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds a streaming binary response from the HTTP response.
    /// </summary>
    /// <remarks>
    /// The caller is responsible for disposing the returned response.
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="StreamBinaryEndpointResponse"/> containing the content stream.</returns>
    Task<StreamBinaryEndpointResponse> BuildStreamBinaryResponseAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds a streaming response that yields items as they are deserialized from the response stream.
    /// </summary>
    /// <typeparam name="T">The type of items to deserialize.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of deserialized items, or an empty enumerable if the response is null or failed.</returns>
    IAsyncEnumerable<T?> BuildStreamingResponseAsync<T>(
        CancellationToken cancellationToken = default);
}