using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Atc.Rest.Client.Serialization;

namespace Atc.Rest.Client.Builder
{
    internal class MessageResponseBuilder : IMessageResponseBuilder
    {
        [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1000:Keywords should be spaced correctly", Justification = "False positive. new() syntax requires it.")]
        private static readonly EndpointResponse EmptyResponse
            = new(isSuccess: false, HttpStatusCode.InternalServerError, string.Empty, contentObject: null, new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

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
            => AddEmptyResponse(statusCode, isSuccess: false);

        public IMessageResponseBuilder AddErrorResponse<TResponseContent>(HttpStatusCode statusCode)
            => AddTypedResponse<TResponseContent>(statusCode, isSuccess: false);

        public IMessageResponseBuilder AddSuccessResponse(HttpStatusCode statusCode)
            => AddEmptyResponse(statusCode, isSuccess: true);

        public IMessageResponseBuilder AddSuccessResponse<TResponseContent>(HttpStatusCode statusCode)
            => AddTypedResponse<TResponseContent>(statusCode, isSuccess: true);

        public async Task<TResult> BuildResponseAsync<TResult>(Func<EndpointResponse, TResult> factory, CancellationToken cancellationToken)
        {
            if (response is null)
            {
                return factory(EmptyResponse);
            }

            if (UseReadAsStringFromContentDependingOnContentType(response.Content.Headers.ContentType))
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                return factory(
                    new EndpointResponse(
                        IsSuccessStatus(response),
                        response.StatusCode,
                        content,
                        GetSerializer(response.StatusCode)?.Invoke(content),
                        GetHeaders(response)));
            }

            var contentObject = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            return factory(
                new EndpointResponse(
                    IsSuccessStatus(response),
                    response.StatusCode,
                    string.Empty,
                    contentObject,
                    GetHeaders(response)));
        }

        public async Task<EndpointResponse<TSuccessContent>> BuildResponseAsync<TSuccessContent>(
            CancellationToken cancellationToken)
            where TSuccessContent : class
        {
            if (response is null)
            {
                return new EndpointResponse<TSuccessContent>(
                    false,
                    HttpStatusCode.InternalServerError,
                    string.Empty,
                    null,
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));
            }

            if (UseReadAsStringFromContentDependingOnContentType(response.Content.Headers.ContentType))
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                return new EndpointResponse<TSuccessContent>(
                    IsSuccessStatus(response),
                    response.StatusCode,
                    content,
                    GetSerializer(response.StatusCode)?.Invoke(content) as TSuccessContent,
                    GetHeaders(response));
            }

            var contentObject = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            return new EndpointResponse<TSuccessContent>(
                IsSuccessStatus(response),
                response.StatusCode,
                string.Empty,
                contentObject as TSuccessContent,
                GetHeaders(response));
        }

        public async Task<EndpointResponse<TSuccessContent, TErrorContent>>
            BuildResponseAsync<TSuccessContent, TErrorContent>(CancellationToken cancellationToken)
            where TSuccessContent : class
            where TErrorContent : class
        {
            if (response is null)
            {
                return new EndpointResponse<TSuccessContent, TErrorContent>(
                    false,
                    HttpStatusCode.InternalServerError,
                    string.Empty,
                    null,
                    new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));
            }

            var isSuccessStatus = IsSuccessStatus(response);
            if (UseReadAsStringFromContentDependingOnContentType(response.Content.Headers.ContentType))
            {
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var serialized = GetSerializer(response.StatusCode)?.Invoke(content);

                return new EndpointResponse<TSuccessContent, TErrorContent>(
                    isSuccessStatus,
                    response.StatusCode,
                    content,
                    isSuccessStatus ? serialized as TSuccessContent : serialized as TErrorContent,
                    GetHeaders(response));
            }

            var contentObject = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            return new EndpointResponse<TSuccessContent, TErrorContent>(
                isSuccessStatus,
                response.StatusCode,
                string.Empty,
                isSuccessStatus ? contentObject as TSuccessContent : contentObject as TErrorContent,
                GetHeaders(response));
        }

        private static bool UseReadAsStringFromContentDependingOnContentType(MediaTypeHeaderValue? headersContentType)
            => headersContentType?.MediaType is null ||
               headersContentType.MediaType.Contains("json") ||
               headersContentType.MediaType.Contains("text");

        private static IReadOnlyDictionary<string, IEnumerable<string>> GetHeaders(HttpResponseMessage responseMessage)
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

        private bool IsSuccessStatus(HttpResponseMessage responseMessage)
            => responseCodes.TryGetValue(responseMessage.StatusCode, out var isSuccess)
                ? isSuccess
                : responseMessage.IsSuccessStatusCode;

        private ContentSerializerDelegate? GetSerializer(HttpStatusCode statusCode)
            => responseSerializers.TryGetValue(statusCode, out var deserializer)
             ? deserializer
             : null;

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