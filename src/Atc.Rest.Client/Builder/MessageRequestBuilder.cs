using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Atc.Rest.Client.Serialization;

namespace Atc.Rest.Client.Builder
{
    internal class MessageRequestBuilder : IMessageRequestBuilder
    {
        private readonly string template;
        private readonly IContractSerializer serializer;
        private readonly Dictionary<string, string> pathMapper;
        private readonly Dictionary<string, string> queryMapper;
        private string content = string.Empty;

        public MessageRequestBuilder(string template, IContractSerializer serializer)
        {
            this.template = template;
            this.serializer = serializer;
            pathMapper = new Dictionary<string, string>(StringComparer.Ordinal);
            queryMapper = new Dictionary<string, string>(StringComparer.Ordinal);
        }

        public HttpRequestMessage Build(HttpMethod method)
        {
            var message = new HttpRequestMessage();

            message.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/json"));
            message.RequestUri = BuildRequestUri();
            message.Content = new StringContent(content);
            message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            message.Method = method;

            return message;
        }

        public IMessageRequestBuilder WithBody<TBody>(TBody body)
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            content = serializer.Serialize(body);

            return this;
        }

        public IMessageRequestBuilder WithPathParameter(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException($"'{nameof(value)}' cannot be null or whitespace", nameof(value));
            }

            pathMapper[name] = value;

            return this;
        }

        public IMessageRequestBuilder WithQueryParameter(string name, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                queryMapper[name] = value;
            }

            return this;
        }

        public IMessageRequestBuilder WithQueryParameter<T>(string name, T value)
            where T : struct
            => WithQueryParameter(name, value.ToString());

        private Uri BuildRequestUri()
        {
            var urlBuilder = new StringBuilder();

            urlBuilder.Append(template);
            foreach (var parameter in pathMapper)
            {
                urlBuilder.Replace($"{{{parameter.Key}}}", Uri.EscapeDataString(parameter.Value));
            }

            if (queryMapper.Any())
            {
                urlBuilder.Append('?');
                urlBuilder.Append(string.Join("&", queryMapper.Select(q => $"{q.Key}={Uri.EscapeDataString(q.Value)}")));
            }

            return new Uri(urlBuilder.ToString(), UriKind.RelativeOrAbsolute);
        }
    }
}