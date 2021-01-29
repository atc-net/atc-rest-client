using System;
using Azure.Core;
using Azure.Identity;

namespace Atc.Rest.Client.Options
{
    public abstract class AtcRestClientOptions
    {
        private TokenCredential? credential;

        public virtual string? EnvironmentName { get; set; }

        public virtual EnvironmentType EnvironmentType { get; set; }

        public virtual Uri? BaseAddress { get; set; }

        public abstract string? IdentityScope { get; }

        public abstract string? TenantId { get; }

        public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        public TokenCredential Credential
        {
            get
            {
                if (credential is null)
                {
                    credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
                    {
                        VisualStudioTenantId = TenantId,
                        SharedTokenCacheTenantId = TenantId,
                        VisualStudioCodeTenantId = TenantId,
                        InteractiveBrowserTenantId = TenantId,
                    });
                }

                return credential;
            }
        }

        public void UseCredentials(TokenCredential credential)
            => this.credential = credential ?? throw new ArgumentNullException(nameof(credential));
    }
}
