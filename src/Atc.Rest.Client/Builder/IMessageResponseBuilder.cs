namespace Atc.Rest.Client.Builder;

/// <summary>
/// A message response builder used to process and deserialize HTTP responses.
/// </summary>
public interface IMessageResponseBuilder
{
    /// <summary>
    /// Registers a status code as a success response without deserialization.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to treat as success.</param>
    /// <returns>The <see cref="IMessageResponseBuilder"/>.</returns>
    IMessageResponseBuilder AddSuccessResponse(
        HttpStatusCode statusCode);

    /// <summary>
    /// Registers a status code as a success response with typed content deserialization.
    /// </summary>
    /// <typeparam name="TResponseContent">The type to deserialize the response content to.</typeparam>
    /// <param name="statusCode">The HTTP status code to treat as success.</param>
    /// <returns>The <see cref="IMessageResponseBuilder"/>.</returns>
    IMessageResponseBuilder AddSuccessResponse<TResponseContent>(
        HttpStatusCode statusCode);

    /// <summary>
    /// Registers a status code as an error response without deserialization.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to treat as error.</param>
    /// <returns>The <see cref="IMessageResponseBuilder"/>.</returns>
    IMessageResponseBuilder AddErrorResponse(
        HttpStatusCode statusCode);

    /// <summary>
    /// Registers a status code as an error response with typed content deserialization.
    /// </summary>
    /// <typeparam name="TResponseContent">The type to deserialize the error content to.</typeparam>
    /// <param name="statusCode">The HTTP status code to treat as error.</param>
    /// <returns>The <see cref="IMessageResponseBuilder"/>.</returns>
    IMessageResponseBuilder AddErrorResponse<TResponseContent>(
        HttpStatusCode statusCode);

    /// <summary>
    /// Builds the response using a custom factory function.
    /// </summary>
    /// <typeparam name="TResult">The type of result to create.</typeparam>
    /// <param name="factory">A factory function that creates the result from the endpoint response.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result created by the factory function.</returns>
    Task<TResult> BuildResponseAsync<TResult>(
        Func<EndpointResponse, TResult> factory,
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds a typed endpoint response with success content.
    /// </summary>
    /// <typeparam name="TSuccessContent">The type of the success content.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="EndpointResponse{TSuccess}"/> with the typed success content.</returns>
    Task<EndpointResponse<TSuccessContent>>
        BuildResponseAsync<TSuccessContent>(
            CancellationToken cancellationToken)
        where TSuccessContent : class;

    /// <summary>
    /// Builds a typed endpoint response with both success and error content.
    /// </summary>
    /// <typeparam name="TSuccessContent">The type of the success content.</typeparam>
    /// <typeparam name="TErrorContent">The type of the error content.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="EndpointResponse{TSuccess, TError}"/> with the typed content.</returns>
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