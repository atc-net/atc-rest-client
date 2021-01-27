using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Atc.Rest.Client.Authentication
{
    public interface IBearerTokenProvider
    {
        Task<AuthenticationHeaderValue> GetTokenAsync(CancellationToken cancellationToken);
    }
}