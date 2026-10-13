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
    IMessageResponseBuilder AddSuccessResponse(HttpStatusCode statusCode);

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
    IMessageResponseBuilder AddErrorResponse(HttpStatusCode statusCode);

    /// <summary>
    /// Registers a status code as an error response with typed content deserialization.
    /// </summary>
    /// <typeparam name="TResponseContent">The type to deserialize the error content to.</typeparam>
    /// <param name="statusCode">The HTTP status code to treat as error.</param>
    /// <returns>The <see cref="IMessageResponseBuilder"/>.</returns>
    IMessageResponseBuilder AddErrorResponse<TResponseContent>(
        HttpStatusCode statusCode);

    /// <summary>
    /// Registers a status code as a success response whose body is plain text.
    /// The body is returned verbatim as a <see cref="string"/> without any JSON
    /// or other deserialization.
    /// </summary>
    /// <remarks>
    /// Use this for endpoints that declare a <c>text/plain</c> (or other <c>text/*</c>) response.
    /// The body is read via <see cref="HttpContent.ReadAsStringAsync()"/>, which honors the
    /// charset declared in the <c>Content-Type</c> header.
    /// An empty or whitespace-only body yields a <see langword="null"/> <c>ContentObject</c>,
    /// matching the behavior of <see cref="AddSuccessResponse{TResponseContent}(HttpStatusCode)"/>.
    /// </remarks>
    /// <param name="statusCode">The HTTP status code to treat as success.</param>
    /// <returns>The <see cref="IMessageResponseBuilder"/>.</returns>
    IMessageResponseBuilder AddSuccessTextResponse(HttpStatusCode statusCode);

    /// <summary>
    /// Registers a status code as an error response whose body is plain text.
    /// The body is returned verbatim as a <see cref="string"/> without any JSON
    /// or other deserialization.
    /// </summary>
    /// <remarks>
    /// Use this for endpoints that declare a <c>text/plain</c> (or other <c>text/*</c>) error response.
    /// The body is read via <see cref="HttpContent.ReadAsStringAsync()"/>, which honors the
    /// charset declared in the <c>Content-Type</c> header.
    /// An empty or whitespace-only body yields a <see langword="null"/> <c>ContentObject</c>,
    /// matching the behavior of <see cref="AddErrorResponse{TResponseContent}(HttpStatusCode)"/>.
    /// </remarks>
    /// <param name="statusCode">The HTTP status code to treat as error.</param>
    /// <returns>The <see cref="IMessageResponseBuilder"/>.</returns>
    IMessageResponseBuilder AddErrorTextResponse(HttpStatusCode statusCode);

    /// <summary>
    /// Builds the response using a custom factory function.
    /// </summary>
    /// <remarks>
    /// Success bodies with a JSON or text media type (or no Content-Type) are read as a string; other success
    /// media types are read as a <c>byte[]</c> into <see cref="EndpointResponse.ContentObject"/>
    /// with an empty <see cref="EndpointResponse.Content"/>. Error bodies are always read as a string,
    /// decoded by the Content-Type charset (UTF-8 when none is given), whatever the media type.
    /// </remarks>
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
    Task<EndpointResponse<TSuccessContent>> BuildResponseAsync<TSuccessContent>(
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds a typed endpoint response with both success and error content.
    /// </summary>
    /// <typeparam name="TSuccessContent">The type of the success content.</typeparam>
    /// <typeparam name="TErrorContent">The type of the error content.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="EndpointResponse{TSuccess, TError}"/> with the typed content.</returns>
    Task<EndpointResponse<TSuccessContent, TErrorContent>>
        BuildResponseAsync<TSuccessContent, TErrorContent>(
            CancellationToken cancellationToken);

    /// <summary>
    /// Builds a binary response from the HTTP response.
    /// </summary>
    /// <remarks>
    /// <see cref="BinaryEndpointResponse.IsSuccess"/> follows the HTTP status (2xx). On an error, the body is read
    /// as text into <see cref="BinaryEndpointResponse.ErrorContent"/> and deserialized into
    /// <see cref="BinaryEndpointResponse.ErrorContentObject"/> with the type registered for the status code
    /// (e.g. <c>AddErrorResponse&lt;T&gt;</c>). <see cref="BinaryEndpointResponse.FileName"/> prefers the
    /// RFC 5987 <c>filename*</c> parameter over <c>filename</c>.
    /// </remarks>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="BinaryEndpointResponse"/> containing the binary content.</returns>
    Task<BinaryEndpointResponse> BuildBinaryResponseAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Builds a streaming binary response from the HTTP response.
    /// </summary>
    /// <remarks>
    /// The caller is responsible for disposing the returned response.
    /// Errors, typed error content and file names are handled as for <see cref="BuildBinaryResponseAsync"/>.
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
    /// For most use cases, prefer <see cref="BuildStreamingEndpointResponseAsync{T}(CancellationToken)"/> which automatically
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
    /// <item><description>Access to <see cref="StreamingEndpointResponse{T}.ErrorContentObject"/>, the error deserialized to the type registered for the status code (e.g. <c>AddErrorResponse&lt;T&gt;</c>)</description></item>
    /// <item><description>Access to <see cref="StreamingEndpointResponse{T}.Headers"/></description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <see cref="StreamingEndpointResponse{T}.IsSuccess"/> follows the HTTP status (2xx). An error body that is empty
    /// or cannot be deserialized to the registered type does not throw: the object is <see langword="null"/> and
    /// the text stays in <see cref="StreamingEndpointResponse{T}.ErrorContent"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of items to deserialize from the stream.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="StreamingEndpointResponse{T}"/> containing the streaming content and response metadata.</returns>
    Task<StreamingEndpointResponse<T>> BuildStreamingEndpointResponseAsync<T>(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a streaming endpoint response using a custom factory function, e.g. for a result class derived from
    /// <see cref="StreamingEndpointResponse{T}"/>.
    /// </summary>
    /// <remarks>
    /// The response is built as by <see cref="BuildStreamingEndpointResponseAsync{T}(CancellationToken)"/> and handed
    /// to <paramref name="factory"/>. A result created with the copy constructor of
    /// <see cref="StreamingEndpointResponse{T}"/> takes over the HTTP response; the caller disposes the result.
    /// If the factory throws, the HTTP response is disposed.
    /// </remarks>
    /// <typeparam name="T">The type of items to deserialize from the stream.</typeparam>
    /// <typeparam name="TResult">The type of result to create.</typeparam>
    /// <param name="factory">A factory function that creates the result from the streaming endpoint response.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result created by the factory function.</returns>
    Task<TResult> BuildStreamingEndpointResponseAsync<T, TResult>(
        Func<StreamingEndpointResponse<T>, TResult> factory,
        CancellationToken cancellationToken = default)
        where TResult : StreamingEndpointResponse<T>;

    /// <summary>
    /// Builds a streaming endpoint response whose items are framed as given: a JSON array, JSON Lines, a JSON text
    /// sequence, server-sent events or multipart/mixed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The caller names the framing, as the API specification does. It is not guessed from the Content-Type of the
    /// response, which an error page or a proxy can change. For <see cref="StreamFraming.MultipartMixed"/> the
    /// boundary is read from the Content-Type; a response without one is an <see cref="InvalidDataException"/>.
    /// </para>
    /// <para>
    /// An item is yielded as soon as its record is complete; nothing is read ahead of it. A record that is no JSON
    /// for <typeparamref name="T"/> throws the serializer's exception from the enumeration, and a record longer than
    /// <c>AtcRestClientOptions.MaxStreamRecordLength</c> throws an <see cref="InvalidDataException"/> from it.
    /// </para>
    /// <para>
    /// Errors, typed error content and headers are as for
    /// <see cref="BuildStreamingEndpointResponseAsync{T}(CancellationToken)"/>. If the response cannot be built, the
    /// HTTP response is disposed before the exception leaves this method.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of items to deserialize from the stream.</typeparam>
    /// <param name="framing">How the items are framed on the wire.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="StreamingEndpointResponse{T}"/> containing the streaming content and response metadata.</returns>
    Task<StreamingEndpointResponse<T>> BuildStreamingEndpointResponseAsync<T>(
        StreamFraming framing,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a streaming endpoint response whose items are framed as given, with options for this one response.
    /// </summary>
    /// <remarks>
    /// As <see cref="BuildStreamingEndpointResponseAsync{T}(StreamFraming, CancellationToken)"/>;
    /// <see cref="StreamReadOptions.MaxRecordLength"/> replaces <c>AtcRestClientOptions.MaxStreamRecordLength</c>
    /// when it is set.
    /// </remarks>
    /// <typeparam name="T">The type of items to deserialize from the stream.</typeparam>
    /// <param name="framing">How the items are framed on the wire.</param>
    /// <param name="options">Options for reading this response, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="StreamingEndpointResponse{T}"/> containing the streaming content and response metadata.</returns>
    Task<StreamingEndpointResponse<T>> BuildStreamingEndpointResponseAsync<T>(
        StreamFraming framing,
        StreamReadOptions? options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a streaming endpoint response whose items are framed as given, using a custom factory function.
    /// </summary>
    /// <remarks>
    /// The response is built as by <see cref="BuildStreamingEndpointResponseAsync{T}(StreamFraming, CancellationToken)"/>
    /// and handed to <paramref name="factory"/>, as by
    /// <see cref="BuildStreamingEndpointResponseAsync{T, TResult}(Func{StreamingEndpointResponse{T}, TResult}, CancellationToken)"/>.
    /// </remarks>
    /// <typeparam name="T">The type of items to deserialize from the stream.</typeparam>
    /// <typeparam name="TResult">The type of result to create.</typeparam>
    /// <param name="factory">A factory function that creates the result from the streaming endpoint response.</param>
    /// <param name="framing">How the items are framed on the wire.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result created by the factory function.</returns>
    Task<TResult> BuildStreamingEndpointResponseAsync<T, TResult>(
        Func<StreamingEndpointResponse<T>, TResult> factory,
        StreamFraming framing,
        CancellationToken cancellationToken = default)
        where TResult : StreamingEndpointResponse<T>;

    /// <summary>
    /// Builds a streaming endpoint response whose items are framed as given, using a custom factory function and
    /// options for this one response.
    /// </summary>
    /// <typeparam name="T">The type of items to deserialize from the stream.</typeparam>
    /// <typeparam name="TResult">The type of result to create.</typeparam>
    /// <param name="factory">A factory function that creates the result from the streaming endpoint response.</param>
    /// <param name="framing">How the items are framed on the wire.</param>
    /// <param name="options">Options for reading this response, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result created by the factory function.</returns>
    Task<TResult> BuildStreamingEndpointResponseAsync<T, TResult>(
        Func<StreamingEndpointResponse<T>, TResult> factory,
        StreamFraming framing,
        StreamReadOptions? options,
        CancellationToken cancellationToken = default)
        where TResult : StreamingEndpointResponse<T>;
}