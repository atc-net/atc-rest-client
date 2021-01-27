using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Atc.Rest.Client.Serialization;

namespace Atc.Rest.Client.Builder
{
    public class MessageResponseBuilder : IMessageResponseBuilder
    {
        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1000:Keywords should be spaced correctly", Justification = "False positive. new() syntax requires it.")]
        private static readonly EndpointResponse EmptyResponse
            = new(false, HttpStatusCode.InternalServerError, string.Empty, null, new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

        private readonly HttpResponseMessage? response;
        private readonly IContractSerializer serializer;
        private readonly Dictionary<HttpStatusCode, ContentSerializerDelegate> responseSerializers;
        private readonly Dictionary<HttpStatusCode, bool> responseCodes;

        public MessageResponseBuilder(HttpResponseMessage? response, IContractSerializer serializer)
        {
            this.response = response;
            this.serializer = serializer;
            responseSerializers = new Dictionary<HttpStatusCode, ContentSerializerDelegate>();
            responseCodes = new Dictionary<HttpStatusCode, bool>();
        }

        private delegate object? ContentSerializerDelegate(string content);

        public IMessageResponseBuilder AddErrorResponse(HttpStatusCode statusCode)
            => AddEmptyResponse(statusCode, false);

        public IMessageResponseBuilder AddErrorResponse<TResponseContent>(HttpStatusCode statusCode)
            => AddTypedResponse<TResponseContent>(statusCode, false);

        public IMessageResponseBuilder AddSuccessResponse(HttpStatusCode statusCode)
            => AddEmptyResponse(statusCode, true);

        public IMessageResponseBuilder AddSuccessResponse<TResponseContent>(HttpStatusCode statusCode)
            => AddTypedResponse<TResponseContent>(statusCode, true);

        public async Task<TResult> BuildResponseAsync<TResult>(Func<EndpointResponse, TResult> factory, CancellationToken cancellationToken)
        {
            if (response is null)
            {
                return factory(EmptyResponse);
            }

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            return factory(
                new EndpointResponse(
                    IsSuccessStatus(response),
                    response.StatusCode,
                    content,
                    GetSerializer(response.StatusCode)?.Invoke(content),
                    GetHeaders(response)));
        }

        private bool IsSuccessStatus(HttpResponseMessage responseMessage)
            => responseCodes.TryGetValue(responseMessage.StatusCode, out var isSuccess)
                ? isSuccess
                : responseMessage.IsSuccessStatusCode;

        private ContentSerializerDelegate? GetSerializer(HttpStatusCode statusCode)
            => responseSerializers.TryGetValue(statusCode, out var deserializer)
             ? deserializer
             : null;

        private IReadOnlyDictionary<string, IEnumerable<string>> GetHeaders(HttpResponseMessage responseMessage)
        {
            var headers = responseMessage.Headers.ToDictionary(h => h.Key, h => h.Value, StringComparer.Ordinal);

            if (responseMessage.Content?.Headers is not null)
            {
                foreach (var item_ in responseMessage.Content.Headers)
                {
                    headers[item_.Key] = item_.Value;
                }
            }

            return headers;
        }

        private IMessageResponseBuilder AddEmptyResponse(HttpStatusCode statusCode, bool isSuccess)
        {
            responseSerializers[statusCode] = content => null;
            responseCodes[statusCode] = isSuccess;

            return this;
        }

        private IMessageResponseBuilder AddTypedResponse<T>(HttpStatusCode statusCode, bool isSuccess)
        {
            responseSerializers[statusCode] = content => serializer.Deserialize<T>(content);
            responseCodes[statusCode] = isSuccess;

            return this;
        }
    }
}