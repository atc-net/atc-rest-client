namespace Atc.Rest.Client.Builder;

internal class MessageResponseBuilder : IMessageResponseBuilder
{
    private static readonly EndpointResponse EmptyResponse
        = new(
            isSuccess: false,
            HttpStatusCode.InternalServerError,
            string.Empty,
            contentObject: null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase));

    private readonly HttpResponseMessage? response;
    private readonly IContractSerializer serializer;
    private readonly Dictionary<HttpStatusCode, (ContentSerializerDelegate Serializer, Type TargetType)> responseSerializers;
    private readonly Dictionary<HttpStatusCode, bool> responseCodes;
    private readonly int maxStreamRecordLength;

    public MessageResponseBuilder(
        HttpResponseMessage? response,
        IContractSerializer serializer,
        int maxStreamRecordLength = AtcRestClientOptions.DefaultMaxStreamRecordLength)
    {
        this.response = response;
        this.serializer = serializer;
        this.maxStreamRecordLength = maxStreamRecordLength;
        responseSerializers = [];
        responseCodes = [];
    }

    private delegate object? ContentSerializerDelegate(string content);

    public IMessageResponseBuilder AddErrorResponse(HttpStatusCode statusCode)
        => AddEmptyResponse(statusCode, isSuccess: false);

    public IMessageResponseBuilder AddErrorResponse<TResponseContent>(
        HttpStatusCode statusCode)
        => AddTypedResponse<TResponseContent>(statusCode, isSuccess: false);

    public IMessageResponseBuilder AddSuccessResponse(HttpStatusCode statusCode)
        => AddEmptyResponse(statusCode, isSuccess: true);

    public IMessageResponseBuilder AddSuccessResponse<TResponseContent>(
        HttpStatusCode statusCode)
        => AddTypedResponse<TResponseContent>(statusCode, isSuccess: true);

    public IMessageResponseBuilder AddSuccessTextResponse(
        HttpStatusCode statusCode)
        => AddTextResponse(statusCode, isSuccess: true);

    public IMessageResponseBuilder AddErrorTextResponse(
        HttpStatusCode statusCode)
        => AddTextResponse(statusCode, isSuccess: false);

    public async Task<TResult> BuildResponseAsync<TResult>(
        Func<EndpointResponse, TResult> factory,
        CancellationToken cancellationToken)
    {
        if (response is null)
        {
            return factory(EmptyResponse);
        }

        // Error bodies are diagnostic text, so they are always read as a string regardless of media type.
        if (!IsSuccessStatus(response) ||
            UseReadAsStringFromContentDependingOnContentType(response.Content.Headers.ContentType))
        {
            var content = await response
                .Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            object? contentResponse = content;
            var serializerInfo = GetSerializer(response.StatusCode);
            if (serializerInfo is not null)
            {
                try
                {
                    contentResponse = serializerInfo.Value.Serializer.Invoke(content);
                }
                catch (Exception ex)
                {
                    if (IsSuccessStatus(response))
                    {
                        throw new RestClientDeserializationException(
                            $"Failed to deserialize response content to {serializerInfo.Value.TargetType.Name} for status code {(int)response.StatusCode} ({response.StatusCode})",
                            ex,
                            response.StatusCode,
                            content,
                            serializerInfo.Value.TargetType);
                    }
                }
            }

            return factory(new EndpointResponse(
                IsSuccessStatus(response),
                response.StatusCode,
                content,
                contentResponse,
                GetHeaders(response)));
        }

        var contentObject = await response
            .Content
            .ReadAsByteArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return factory(new EndpointResponse(
                IsSuccessStatus(response),
                response.StatusCode,
                string.Empty,
                contentObject,
                GetHeaders(response)));
    }

    public Task<EndpointResponse<TSuccessContent>> BuildResponseAsync<TSuccessContent>(
        CancellationToken cancellationToken) =>
        BuildResponseAsync(
            r => new EndpointResponse<TSuccessContent>(r),
            cancellationToken);

    public Task<EndpointResponse<TSuccessContent, TErrorContent>>
        BuildResponseAsync<TSuccessContent, TErrorContent>(
            CancellationToken cancellationToken) =>
        BuildResponseAsync(
            r => new EndpointResponse<TSuccessContent, TErrorContent>(r),
            cancellationToken);

    public async Task<BinaryEndpointResponse> BuildBinaryResponseAsync(
        CancellationToken cancellationToken)
    {
        if (response is null)
        {
            return new BinaryEndpointResponse(
                isSuccess: false,
                HttpStatusCode.InternalServerError,
                content: null,
                contentType: null,
                fileName: null,
                contentLength: null,
                errorContent: null);
        }

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var contentLength = response.Content.Headers.ContentLength;

        if (!response.IsSuccessStatusCode)
        {
            var (errorContent, errorContentObject) = await ReadErrorContentAsync(response, cancellationToken).ConfigureAwait(false);

            return new BinaryEndpointResponse(
                isSuccess: false,
                response.StatusCode,
                content: null,
                contentType,
                fileName: null,
                contentLength,
                errorContent,
                errorContentObject,
                GetHeaders(response));
        }

        var content = await response
            .Content
            .ReadAsByteArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return new BinaryEndpointResponse(
            isSuccess: true,
            response.StatusCode,
            content,
            contentType,
            GetFileName(response.Content.Headers.ContentDisposition),
            contentLength,
            errorContent: null,
            errorContentObject: null,
            GetHeaders(response));
    }

    public async Task<StreamBinaryEndpointResponse> BuildStreamBinaryResponseAsync(
        CancellationToken cancellationToken)
    {
        if (response is null)
        {
            return new StreamBinaryEndpointResponse(
                isSuccess: false,
                HttpStatusCode.InternalServerError,
                contentStream: null,
                contentType: null,
                fileName: null,
                contentLength: null,
                errorContent: null);
        }

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var contentLength = response.Content.Headers.ContentLength;

        if (!response.IsSuccessStatusCode)
        {
            var (errorContent, errorContentObject) = await ReadErrorContentAsync(response, cancellationToken).ConfigureAwait(false);

            return new StreamBinaryEndpointResponse(
                isSuccess: false,
                response.StatusCode,
                contentStream: null,
                contentType,
                fileName: null,
                contentLength,
                errorContent,
                errorContentObject,
                GetHeaders(response));
        }

        var contentStream = await response
            .Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        return new StreamBinaryEndpointResponse(
            isSuccess: true,
            response.StatusCode,
            contentStream,
            contentType,
            GetFileName(response.Content.Headers.ContentDisposition),
            contentLength,
            errorContent: null,
            errorContentObject: null,
            GetHeaders(response));
    }

    public async IAsyncEnumerable<T?> BuildStreamingResponseAsync<T>(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (response is null || !response.IsSuccessStatusCode)
        {
            yield break;
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var maxRecordLength = StreamRecordLimit.Validate(maxStreamRecordLength);

        await foreach (var item in StreamRecordReaders.ReadJsonArrayAsync<T>(stream, serializer, maxRecordLength, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public Task<StreamingEndpointResponse<T>> BuildStreamingEndpointResponseAsync<T>(
        CancellationToken cancellationToken = default)
        => BuildStreamingEndpointResponseAsync<T>(StreamFraming.JsonArray, options: null, cancellationToken);

    public Task<StreamingEndpointResponse<T>> BuildStreamingEndpointResponseAsync<T>(
        StreamFraming framing,
        CancellationToken cancellationToken = default)
        => BuildStreamingEndpointResponseAsync<T>(framing, options: null, cancellationToken);

    public async Task<StreamingEndpointResponse<T>> BuildStreamingEndpointResponseAsync<T>(
        StreamFraming framing,
        StreamReadOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (response is null)
        {
            return new StreamingEndpointResponse<T>(
                isSuccess: false,
                HttpStatusCode.InternalServerError,
                content: null,
                errorContent: null,
                httpResponse: null);
        }

        // The result owns the HTTP response; without a result, nothing else would dispose it.
        try
        {
            var maxRecordLength = StreamRecordLimit.Validate(options?.MaxRecordLength ?? maxStreamRecordLength);
            if (!Enum.IsDefined(typeof(StreamFraming), framing))
            {
                throw new ArgumentOutOfRangeException(nameof(framing), framing, "Unknown stream framing.");
            }

            if (!response.IsSuccessStatusCode)
            {
                var (errorContent, errorContentObject) = await ReadErrorContentAsync(response, cancellationToken).ConfigureAwait(false);

                return new StreamingEndpointResponse<T>(
                    isSuccess: false,
                    response.StatusCode,
                    content: null,
                    errorContent,
                    httpResponse: response,
                    errorContentObject,
                    GetHeaders(response));
            }

            var content = await ReadStreamContentAsync<T>(response, framing, maxRecordLength, options?.ItemEventNames, cancellationToken).ConfigureAwait(false);

            return new StreamingEndpointResponse<T>(
                isSuccess: true,
                response.StatusCode,
                content,
                errorContent: null,
                httpResponse: response,
                errorContentObject: null,
                GetHeaders(response));
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }

    public Task<TResult> BuildStreamingEndpointResponseAsync<T, TResult>(
        Func<StreamingEndpointResponse<T>, TResult> factory,
        CancellationToken cancellationToken = default)
        where TResult : StreamingEndpointResponse<T>
        => BuildStreamingEndpointResponseAsync<T, TResult>(factory, StreamFraming.JsonArray, options: null, cancellationToken);

    public Task<TResult> BuildStreamingEndpointResponseAsync<T, TResult>(
        Func<StreamingEndpointResponse<T>, TResult> factory,
        StreamFraming framing,
        CancellationToken cancellationToken = default)
        where TResult : StreamingEndpointResponse<T>
        => BuildStreamingEndpointResponseAsync<T, TResult>(factory, framing, options: null, cancellationToken);

    public async Task<TResult> BuildStreamingEndpointResponseAsync<T, TResult>(
        Func<StreamingEndpointResponse<T>, TResult> factory,
        StreamFraming framing,
        StreamReadOptions? options,
        CancellationToken cancellationToken = default)
        where TResult : StreamingEndpointResponse<T>
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        var streamingResponse = await BuildStreamingEndpointResponseAsync<T>(framing, options, cancellationToken).ConfigureAwait(false);

        try
        {
            return factory(streamingResponse);
        }
        catch
        {
            streamingResponse.Dispose();
            throw;
        }
    }

    private async Task<IAsyncEnumerable<T?>> ReadStreamContentAsync<T>(
        HttpResponseMessage responseMessage,
        StreamFraming framing,
        int maxRecordLength,
        IReadOnlyCollection<string>? itemEventNames,
        CancellationToken cancellationToken)
    {
        var boundary = framing == StreamFraming.MultipartMixed
            ? GetMultipartBoundary(responseMessage)
            : string.Empty;

        var stream = await responseMessage
            .Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        return framing switch
        {
            StreamFraming.JsonLines => StreamRecordReaders.ReadJsonLinesAsync<T>(stream, serializer, maxRecordLength, cancellationToken),
            StreamFraming.JsonSequence => StreamRecordReaders.ReadJsonSequenceAsync<T>(stream, serializer, maxRecordLength, cancellationToken),
            StreamFraming.ServerSentEvents => StreamRecordReaders.ReadServerSentEventsAsync<T>(stream, serializer, maxRecordLength, itemEventNames, cancellationToken),
            StreamFraming.MultipartMixed => StreamRecordReaders.ReadMultipartMixedAsync<T>(stream, boundary, serializer, maxRecordLength, cancellationToken),
            _ => StreamRecordReaders.ReadJsonArrayAsync<T>(stream, serializer, maxRecordLength, cancellationToken),
        };
    }

    private static string GetMultipartBoundary(
        HttpResponseMessage responseMessage)
    {
        var boundary = responseMessage
            .Content
            .Headers
            .ContentType?
            .Parameters
            .FirstOrDefault(x => string.Equals(x.Name, "boundary", StringComparison.OrdinalIgnoreCase))?
            .Value?
            .Trim('"');

        return string.IsNullOrWhiteSpace(boundary)
            ? throw new InvalidDataException(
                "The Content-Type of the response has no boundary parameter, which a multipart/mixed stream is split on " +
                $"(Content-Type: {responseMessage.Content.Headers.ContentType?.ToString() ?? "none"}).")
            : boundary!;
    }

    private static bool UseReadAsStringFromContentDependingOnContentType(
        MediaTypeHeaderValue? headersContentType)
        => headersContentType?.MediaType is null ||
           headersContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase) ||
           headersContentType.MediaType.Contains("text", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Prefers the RFC 5987 <c>filename*</c> parameter (already decoded, e.g. <c>UTF-8''Rapport-%C3%85.pdf</c>)
    /// over the plain <c>filename</c>, which servers often send as a mangled ASCII fallback.
    /// </summary>
    private static string? GetFileName(
        ContentDispositionHeaderValue? contentDisposition)
        => string.IsNullOrEmpty(contentDisposition?.FileNameStar)
            ? contentDisposition?.FileName?.Trim('"')
            : contentDisposition!.FileNameStar;

    private static IReadOnlyDictionary<string, IEnumerable<string>> GetHeaders(
        HttpResponseMessage responseMessage)
    {
        var headers = responseMessage.Headers.ToDictionary(h => h.Key, h => h.Value, StringComparer.OrdinalIgnoreCase);

        if (responseMessage.Content?.Headers is null)
        {
            return headers;
        }

        foreach (var item_ in responseMessage.Content.Headers)
        {
            headers[item_.Key] = item_.Value;
        }

        return headers;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "IContractSerializer is pluggable and may throw any exception; an undeserializable error body must not fail the call.")]
    private async Task<(string ErrorContent, object? ErrorContentObject)> ReadErrorContentAsync(
        HttpResponseMessage responseMessage,
        CancellationToken cancellationToken)
    {
        var errorContent = await responseMessage
            .Content
            .ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        var serializerInfo = GetSerializer(responseMessage.StatusCode);
        if (serializerInfo is null)
        {
            return (errorContent, null);
        }

        try
        {
            return (errorContent, serializerInfo.Value.Serializer.Invoke(errorContent));
        }
        catch (Exception)
        {
            // The raw text stays available in ErrorContent.
            return (errorContent, null);
        }
    }

    private bool IsSuccessStatus(HttpResponseMessage responseMessage)
        => responseCodes.TryGetValue(responseMessage.StatusCode, out var isSuccess)
            ? isSuccess
            : responseMessage.IsSuccessStatusCode;

    private (ContentSerializerDelegate Serializer, Type TargetType)? GetSerializer(
        HttpStatusCode statusCode)
        => responseSerializers.TryGetValue(statusCode, out var deserializer)
            ? deserializer
            : null;

    private IMessageResponseBuilder AddEmptyResponse(
        HttpStatusCode statusCode,
        bool isSuccess)
    {
        responseSerializers[statusCode] = (_ => null, typeof(object));
        responseCodes[statusCode] = isSuccess;

        return this;
    }

    private IMessageResponseBuilder AddTypedResponse<T>(
        HttpStatusCode statusCode,
        bool isSuccess)
    {
        responseSerializers[statusCode] = (
            content => string.IsNullOrWhiteSpace(content)
                ? null
                : serializer.Deserialize<T>(content),
            typeof(T));
        responseCodes[statusCode] = isSuccess;

        return this;
    }

    private IMessageResponseBuilder AddTextResponse(
        HttpStatusCode statusCode,
        bool isSuccess)
    {
        responseSerializers[statusCode] = (
            content => string.IsNullOrWhiteSpace(content) ? null : content,
            typeof(string));
        responseCodes[statusCode] = isSuccess;

        return this;
    }
}