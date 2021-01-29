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
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.IdentityScope))
            {
                throw new ArgumentException($"The {nameof(AtcRestClientOptions.IdentityScope)} in {nameof(AtcRestClientOptions)} cannot be null or whitespace.", nameof(options));
            }

            credential = options.Credential;

            // !BANG added to make nullable analyzer happy, since .netstandard2
            // doesn't include annotations in IsNullOrWhiteSpace that tells the analyzer
            // that options.IdentityScope cannot be null if it returns true.
            context = new TokenRequestContext(new string[] { options.IdentityScope! });
        }

        public async Task<AuthenticationHeaderValue> GetTokenAsync(CancellationToken cancellationToken)
        {
            var token = await credential.GetTokenAsync(context, cancellationToken).ConfigureAwait(false);
            return new AuthenticationHeaderValue("Bearer", token.Token);
        }
    }
}