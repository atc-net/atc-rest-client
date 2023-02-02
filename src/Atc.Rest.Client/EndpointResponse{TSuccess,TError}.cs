using System.Collections.Generic;
using System.Net;

namespace Atc.Rest.Client
{
    public class EndpointResponse<TSuccess, TError>
        : EndpointResponse
        where TSuccess : class
        where TError : class
    {
        public EndpointResponse(EndpointResponse response)
            : base(response)
        {
        }

        public EndpointResponse(
            bool isSuccess,
            HttpStatusCode statusCode,
            string content,
            object? contentObject,
            IReadOnlyDictionary<string, IEnumerable<string>> headers)
            : base(isSuccess, statusCode, content, contentObject, headers)
        {
        }

        public TSuccess? SuccessContent => IsSuccess ? CastContent<TSuccess>() : null;

        public TError? ErrorContent => !IsSuccess ? CastContent<TError>() : null;
    }
}