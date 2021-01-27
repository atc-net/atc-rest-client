using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Atc.Rest.Client.Options;
using Azure.Core;

namespace Atc.Rest.Client.Authentication
{
    public class BearerTokenProvider : IBearerTokenProvider
    {
        private readonly TokenRequestContext context;
        private readonly TokenCredential credential;

        public BearerTokenProvider(AtcRestClientOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.IdentityScope))
            {
                throw new ArgumentException($"The {nameof(AtcRestClientOptions.IdentityScope)} in {nameof(AtcRestClientOptions)} cannot be null or whitespace.", nameof(options));
            }

            credential = options.Credential;
            context = new TokenRequestContext(new string[] { options.IdentityScope });
        }

        public async Task<AuthenticationHeaderValue> GetTokenAsync(CancellationToken cancellationToken)
        {
            var token = await credential.GetTokenAsync(context, cancellationToken).ConfigureAwait(false);
            return new AuthenticationHeaderValue("Bearer", token.Token);
        }
    }
}