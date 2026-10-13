namespace Atc.Rest.Client.Builder;

internal class HttpMessageFactory : IHttpMessageFactory
{
    private readonly IContractSerializer serializer;
    private readonly int maxStreamRecordLength;

    public HttpMessageFactory(IContractSerializer serializer)
    {
        this.serializer = serializer;
        maxStreamRecordLength = AtcRestClientOptions.DefaultMaxStreamRecordLength;
    }

    public HttpMessageFactory(
        IContractSerializer serializer,
        IOptions<AtcRestClientOptions> options)
    {
        this.serializer = serializer;
        maxStreamRecordLength = options.Value.MaxStreamRecordLength;
    }

    public IMessageRequestBuilder FromTemplate(string pathTemplate)
        => new MessageRequestBuilder(
            pathTemplate,
            serializer);

    public IMessageResponseBuilder FromResponse(HttpResponseMessage? response)
        => new MessageResponseBuilder(
            response,
            serializer,
            maxStreamRecordLength);
}