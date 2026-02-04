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
    /// <remarks>
    /// <para>
    /// <b>Warning:</b> This method returns a bare <see cref="IAsyncEnumerable{T}"/> without lifecycle management.
    /// The caller must ensure the underlying <see cref="HttpResponseMessage"/> is not disposed until
    /// enumeration completes. If the response is disposed prematurely, the enumeration will fail.
    /// </para>
    /// <para>
    /// For most use cases, prefer <see cref="BuildStreamingEndpointResponseAsync{T}"/> which automatically
    /// manages the HTTP response lifecycle and provides additional response metadata (status code, error content).
    /// </para>
    /// <para>
    /// This method returns an empty enumerable if the response is null or has a non-success status code.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of items to deserialize from the stream.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An async enumerable of deserialized items, or an empty enumerable if the response is null or failed.</returns>
    IAsyncEnumerable<T?> BuildStreamingResponseAsync<T>(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a streaming endpoint response that manages the HTTP response lifecycle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Recommended:</b> This is the preferred method for streaming responses. It wraps the streaming
    /// enumerable in a <see cref="StreamingEndpointResponse{T}"/> that manages the HTTP response lifecycle
    /// and provides access to response metadata.
    /// </para>
    /// <para>
    /// The caller must dispose the returned response after consuming the stream. Use a <c>using</c> statement
    /// or <c>await using</c> to ensure proper cleanup:
    /// </para>
    /// <code>
    /// await using var response = await responseBuilder.BuildStreamingEndpointResponseAsync&lt;MyItem&gt;(ct);
    /// if (response.IsSuccess)
    /// {
    ///     await foreach (var item in response.Content!)
    ///     {
    ///         // Process item
    ///     }
    /// }
    /// </code>
    /// <para>
    /// Unlike <see cref="BuildStreamingResponseAsync{T}"/>, this method provides:
    /// <list type="bullet">
    /// <item><description>Automatic HTTP response lifecycle management</description></item>
    /// <item><description>Access to <see cref="StreamingEndpointResponse{T}.IsSuccess"/> status</description></item>
    /// <item><description>Access to <see cref="StreamingEndpointResponse{T}.StatusCode"/></description></item>
    /// <item><description>Access to <see cref="StreamingEndpointResponse{T}.ErrorContent"/> on failure</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of items to deserialize from the stream.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="StreamingEndpointResponse{T}"/> containing the streaming content and response metadata.</returns>
    Task<StreamingEndpointResponse<T>> BuildStreamingEndpointResponseAsync<T>(
        CancellationToken cancellationToken = default);
}