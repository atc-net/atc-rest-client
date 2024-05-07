namespace Atc.Rest.Client.Builder;

internal class HttpMessageFactory : IHttpMessageFactory
{
    private readonly IContractSerializer serializer;

    public HttpMessageFactory(
        IContractSerializer serializer)
    {
        this.serializer = serializer;
    }

    public IMessageRequestBuilder FromTemplate(
        string pathTemplate)
        => new MessageRequestBuilder(
            pathTemplate,
            serializer);

    public IMessageResponseBuilder FromResponse(
        HttpResponseMessage? response)
        => new MessageResponseBuilder(
            response,
            serializer);
}