namespace Atc.Rest.Client.Builder;

internal class HttpMessageFactory : IHttpMessageFactory
{
    private readonly IContractSerializer serializer;
    private readonly int maxStreamRecordLength;
    private readonly int maxErrorContentLength;

    public HttpMessageFactory(IContractSerializer serializer)
    {
        this.serializer = serializer;
        maxStreamRecordLength = AtcRestClientOptions.DefaultMaxStreamRecordLength;
        maxErrorContentLength = AtcRestClientOptions.DefaultMaxErrorContentLength;
    }

    public HttpMessageFactory(
        IContractSerializer serializer,
        IOptions<AtcRestClientOptions> options)
    {
        this.serializer = serializer;
        maxStreamRecordLength = options.Value.MaxStreamRecordLength;
        maxErrorContentLength = options.Value.MaxErrorContentLength;
    }

    public IMessageRequestBuilder FromTemplate(string pathTemplate)
        => new MessageRequestBuilder(
            pathTemplate,
            serializer);

    public IMessageResponseBuilder FromResponse(HttpResponseMessage? response)
        => new MessageResponseBuilder(
            response,
            serializer,
            maxStreamRecordLength,
            maxErrorContentLength);
}