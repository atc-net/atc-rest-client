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
        private readonly Dictionary<string, string> headerMapper;
        private readonly Dictionary<string, string> queryMapper;
        private string? content;

        public MessageRequestBuilder(string pathTemplate, IContractSerializer serializer)
        {
            this.template = pathTemplate ?? throw new ArgumentNullException(nameof(pathTemplate));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            pathMapper = new Dictionary<string, string>(StringComparer.Ordinal);
            headerMapper = new Dictionary<string, string>(StringComparer.Ordinal);
            queryMapper = new Dictionary<string, string>(StringComparer.Ordinal);
            WithHeaderParameter("accept", "application/json");
        }

        public HttpRequestMessage Build(HttpMethod method)
        {
            var message = new HttpRequestMessage();
            foreach (var parameter in headerMapper)
            {
                message.Headers.Add(parameter.Key, parameter.Value);
            }

            message.RequestUri = BuildRequestUri();
            message.Method = method;

            if (content is not null)
            {
                message.Content = new StringContent(content);
                message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
            }

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

        public IMessageRequestBuilder WithPathParameter(string name, object? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
            }

            if (value is null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                throw new ArgumentException($"'{nameof(value)}' cannot be null or whitespace", nameof(value));
            }

            pathMapper[name] = value.ToString();

            return this;
        }

        public IMessageRequestBuilder WithHeaderParameter(string name, object? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
            }

            if (value is null)
            {
                return this;
            }

            headerMapper[name] = value.ToString();

            return this;
        }

        public IMessageRequestBuilder WithQueryParameter(string name, object? value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace", nameof(name));
            }

            if (value is null)
            {
                return this;
            }

            queryMapper[name] = value.ToString();

            return this;
        }

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