using System.Collections.Generic;
using System.Net;

namespace Atc.Rest.Client
{
    public class TypedEndpointResponse<T>
        : EndpointResponse
        where T : class
    {
        public TypedEndpointResponse(EndpointResponse response)
            : base(response)
        {
        }

        public TypedEndpointResponse(
            bool isSuccess,
            HttpStatusCode statusCode,
            string content,
            T? contentObject,
            IReadOnlyDictionary<string, IEnumerable<string>> headers)
            : base(isSuccess, statusCode, content, contentObject, headers)
        {
        }

        public T? SuccessContent => IsSuccess ? CastContent<T>() : null;
    }

    public class TypedEndpointResponse<TSuccessContent, TFailureContent>
        : EndpointResponse
        where TSuccessContent : class
        where TFailureContent : class
    {
        public TypedEndpointResponse(EndpointResponse response)
            : base(response)
        {
        }

        public TypedEndpointResponse(
            bool isSuccess,
            HttpStatusCode statusCode,
            string content,
            object? contentObject,
            IReadOnlyDictionary<string, IEnumerable<string>> headers)
            : base(isSuccess, statusCode, content, contentObject, headers)
        {
        }

        public TSuccessContent? SuccessContent => IsSuccess ? CastContent<TSuccessContent>() : null;

        public TFailureContent? FailureContent => !IsSuccess ? CastContent<TFailureContent>() : null;
    }
}