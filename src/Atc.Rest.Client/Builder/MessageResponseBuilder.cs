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
    private readonly Dictionary<HttpStatusCode, ContentSerializerDelegate> responseSerializers;
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

    private delegate object? ContentSerializerDelegate(
        string content);

    public IMessageResponseBuilder AddErrorResponse(
        HttpStatusCode statusCode)
        => AddEmptyResponse(statusCode, isSuccess: false);

    public IMessageResponseBuilder AddErrorResponse<TResponseContent>(
        HttpStatusCode statusCode)
        => AddTypedResponse<TResponseContent>(statusCode, isSuccess: false);

    public IMessageResponseBuilder AddSuccessResponse(
        HttpStatusCode statusCode)
        => AddEmptyResponse(statusCode, isSuccess: true);

    public IMessageResponseBuilder AddSuccessResponse<TResponseContent>(
        HttpStatusCode statusCode)
        => AddTypedResponse<TResponseContent>(statusCode, isSuccess: true);

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK.")]
    public async Task<TResult> BuildResponseAsync<TResult>(
        Func<EndpointResponse, TResult> factory,
        CancellationToken cancellationToken)
    {
        if (response is null)
        {
            return factory(EmptyResponse);
        }

        if (UseReadAsStringFromContentDependingOnContentType(response.Content.Headers.ContentType))
        {
            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            object? contentResponse = content;
            var contentSerializerDelegate = GetSerializer(response.StatusCode);
            if (contentSerializerDelegate is not null)
            {
                try
                {
                    contentResponse = contentSerializerDelegate.Invoke(content);
                }
                catch
                {
                    // Swallow
                }
            }

            var endpointResponse = new EndpointResponse(
                IsSuccessStatus(response),
                response.StatusCode,
                content,
                contentResponse,
                GetHeaders(response));

            return factory(endpointResponse);
        }

        var contentObject = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

        return factory(
            new EndpointResponse(
                IsSuccessStatus(response),
                response.StatusCode,
                string.Empty,
                contentObject,
                GetHeaders(response)));
    }

    public Task<EndpointResponse<TSuccessContent>> BuildResponseAsync<TSuccessContent>(
        CancellationToken cancellationToken)
        where TSuccessContent : class =>
        BuildResponseAsync(
            r => new EndpointResponse<TSuccessContent>(r),
            cancellationToken);

    public Task<EndpointResponse<TSuccessContent, TErrorContent>>
        BuildResponseAsync<TSuccessContent, TErrorContent>(
            CancellationToken cancellationToken)
        where TSuccessContent : class
        where TErrorContent : class =>
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
                contentLength: null);
        }

        var content = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
        var contentLength = response.Content.Headers.ContentLength;

        return new BinaryEndpointResponse(
            response.IsSuccessStatusCode,
            response.StatusCode,
            content,
            contentType,
            fileName,
            contentLength);
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
                contentLength: null);
        }

        var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
        var contentLength = response.Content.Headers.ContentLength;

        return new StreamBinaryEndpointResponse(
            response.IsSuccessStatusCode,
            response.StatusCode,
            contentStream,
            contentType,
            fileName,
            contentLength);
    }

    public async IAsyncEnumerable<T?> BuildStreamingResponseAsync<T>(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (response is null || !response.IsSuccessStatusCode)
        {
            yield break;
        }

        var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        await foreach (var item in serializer.DeserializeAsyncEnumerable<T>(stream, cancellationToken).ConfigureAwait(false))
        {
            yield return item;
        }
    }

    private static bool UseReadAsStringFromContentDependingOnContentType(
        MediaTypeHeaderValue? headersContentType)
        => headersContentType?.MediaType is null ||
           headersContentType.MediaType.Contains("json") ||
           headersContentType.MediaType.Contains("text");

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

    private bool IsSuccessStatus(
        HttpResponseMessage responseMessage)
        => responseCodes.TryGetValue(responseMessage.StatusCode, out var isSuccess)
            ? isSuccess
            : responseMessage.IsSuccessStatusCode;

    private ContentSerializerDelegate? GetSerializer(
        HttpStatusCode statusCode)
        => responseSerializers.TryGetValue(statusCode, out var deserializer)
            ? deserializer
            : null;

    private IMessageResponseBuilder AddEmptyResponse(
        HttpStatusCode statusCode,
        bool isSuccess)
    {
        responseSerializers[statusCode] = content => null;
        responseCodes[statusCode] = isSuccess;

        return this;
    }

    private IMessageResponseBuilder AddTypedResponse<T>(
        HttpStatusCode statusCode, bool isSuccess)
    {
        responseSerializers[statusCode] = content => serializer.Deserialize<T>(content);
        responseCodes[statusCode] = isSuccess;

        return this;
    }
}