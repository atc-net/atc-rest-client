using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Atc.Rest.Client.Builder
{
    public interface IMessageResponseBuilder
    {
        IMessageResponseBuilder AddSuccessResponse(HttpStatusCode statusCode);

        IMessageResponseBuilder AddSuccessResponse<TResponseContent>(HttpStatusCode statusCode);

        IMessageResponseBuilder AddErrorResponse(HttpStatusCode statusCode);

        IMessageResponseBuilder AddErrorResponse<TResponseContent>(HttpStatusCode statusCode);

        Task<TResult> BuildResponseAsync<TResult>(
            Func<EndpointResponse, TResult> factory,
            CancellationToken cancellationToken);

        Task<EndpointResponse<TSuccessContent>>
            BuildResponseAsync<TSuccessContent>(CancellationToken cancellationToken)
            where TSuccessContent : class;

        Task<EndpointResponse<TSuccessContent, TFailureContent>>
            BuildResponseAsync<TSuccessContent, TFailureContent>(CancellationToken cancellationToken)
            where TSuccessContent : class
            where TFailureContent : class;
    }
}