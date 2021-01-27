using System.Net.Http;

namespace Atc.Rest.Client.Builder
{
    public interface IHttpMessageFactory
    {
        IMessageRequestBuilder FromTemplate(string template);

        IMessageResponseBuilder FromResponse(HttpResponseMessage? response);
    }
}