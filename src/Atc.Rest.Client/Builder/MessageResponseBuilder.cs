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

            return factory(
                new EndpointResponse(
                    IsSuccessStatus(response),
                    response.StatusCode,
                    content,
                    GetSerializer(response.StatusCode)?.Invoke(content),
                    GetHeaders(response)));
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