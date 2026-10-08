namespace Atc.Rest.Client.Builder;

internal class MessageResponseBuilder : IMessageResponseBuilder
{
    private static readonly EndpointResponse EmptyResponse
        = new(
            isSuccess: false,
            HttpStatusCode.InternalServerError,
            string.Empty,
            contentObject: null,
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

    private readonly HttpResponseMessage? response;
    private readonly IContractSerializer serializer;
    private readonly Dictionary<HttpStatusCode, (ContentSerializerDelegate Serializer, Type TargetType)> responseSerializers;
    private readonly Dictionary<HttpStatusCode, bool> responseCodes;

    public MessageResponseBuilder(
        HttpResponseMessage? response,
        IContractSerializer serializer)
    {
        this.response = response;
        this.serializer = serializer;
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
            var (errorContent, errorContentObject) = await ReadErrorContentAsync(response).ConfigureAwait(false);

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
            var (errorContent, errorContentObject) = await ReadErrorContentAsync(response).ConfigureAwait(false);

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

        await foreach (var item in serializer.DeserializeAsyncEnumerable<T>(stream, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    public async Task<StreamingEndpointResponse<T>> BuildStreamingEndpointResponseAsync<T>(
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

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response
                .Content
                .ReadAsStringAsync(cancellationToken)
                .ConfigureAwait(false);

            return new StreamingEndpointResponse<T>(
                isSuccess: false,
                response.StatusCode,
                content: null,
                errorContent,
                httpResponse: response);
        }

        var stream = await response
            .Content
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        var content = serializer.DeserializeAsyncEnumerable<T>(stream, cancellationToken);

        return new StreamingEndpointResponse<T>(
            isSuccess: true,
            response.StatusCode,
            content,
            errorContent: null,
            httpResponse: response);
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
        var headers = responseMessage.Headers.ToDictionary(h => h.Key, h => h.Value, StringComparer.Ordinal);

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
        HttpResponseMessage responseMessage)
    {
        var errorContent = await responseMessage
            .Content
            .ReadAsStringAsync()
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