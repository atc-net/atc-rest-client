using System.Net.Http;
using Atc.Rest.Client.Serialization;

namespace Atc.Rest.Client.Builder
{
    public class HttpMessageFactory : IHttpMessageFactory
    {
        private readonly IContractSerializer serializer;

        public HttpMessageFactory(IContractSerializer serializer)
        {
            this.serializer = serializer;
        }

        public IMessageRequestBuilder FromTemplate(string template)
            => new MessageRequestBuilder(template, serializer);

        public IMessageResponseBuilder FromResponse(HttpResponseMessage? response)
            => new MessageResponseBuilder(response, serializer);
    }
}