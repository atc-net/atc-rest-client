using System.Collections.Generic;
using System.Net;

namespace Atc.Rest.Client
{
    public interface IEndpointResponse
    {
        bool IsSuccess { get; }

        HttpStatusCode StatusCode { get; }

        string Content { get; }

        object? ContentObject { get; }

        IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }
    }
}