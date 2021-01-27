using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Atc.Rest.Client.Authentication
{
    public class AzureAuthenticationHandler : DelegatingHandler
    {
        private readonly IBearerTokenProvider tokenProvider;

        public AzureAuthenticationHandler(IBearerTokenProvider tokenProvider)
        {
            this.tokenProvider = tokenProvider;
        }

        protected async override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = await tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}