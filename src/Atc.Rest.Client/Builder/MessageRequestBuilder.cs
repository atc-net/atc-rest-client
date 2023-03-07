using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Atc.Rest.Client.Serialization;
using Microsoft.AspNetCore.Http;

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
        private List<IFormFile>? contentFormFiles;

        public MessageRequestBuilder(string pathTemplate, IContractSerializer serializer)
        {
            this.template = pathTemplate ?? throw new ArgumentNullException(nameof(pathTemplate));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            pathMapper = new Dictionary<string, string>(StringComparer.Ordinal);
            headerMapper = new Dictionary<string, string>(StringComparer.Ordinal);
            queryMapper = new Dictionary<string, string>(StringComparer.Ordinal);
            WithHeaderParameter("accept", "application/json");
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "OK - ByteArrayContent can't be disposed.")]
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
            else if (contentFormFiles is not null)
            {
                var formDataContent = new MultipartFormDataContent();
                foreach (var formFile in contentFormFiles)
                {
                    byte[] bytes;
                    using (var binaryReader = new BinaryReader(formFile.OpenReadStream()))
                    {
                        bytes = binaryReader.ReadBytes((int)formFile.OpenReadStream().Length);
                    }

                    var bytesContent = new ByteArrayContent(bytes);
                    formDataContent.Add(bytesContent, "Request", formFile.FileName);
                }

                message.Headers.Remove("accept");
                message.Headers.Add("accept", "application/octet-stream");
                message.Content = formDataContent;
            }

            return message;
        }

        public IMessageRequestBuilder WithBody<TBody>(TBody body)
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            switch (body)
            {
                case IFormFile formFile:
                    contentFormFiles = new List<IFormFile>
                    {
                        formFile,
                    };
                    break;
                case List<IFormFile> formFiles:
                    contentFormFiles = new List<IFormFile>();
                    contentFormFiles.AddRange(formFiles);
                    break;
                default:
                    content = serializer.Serialize(body);
                    break;
            }

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

            var valueType = value.GetType();
            if (valueType.IsArray || valueType.IsGenericType)
            {
                var objects = ((IEnumerable)value).Cast<object>().ToArray();

                if (objects.Length == 0)
                {
                    return this;
                }

                var sb = new StringBuilder();
                for (int i = 0; i < objects.Length; i++)
                {
                    sb.Append(i == 0
                        ? Uri.EscapeDataString(objects[i].ToString())
                        : $"&{name}={Uri.EscapeDataString(objects[i].ToString())}");
                }

                queryMapper["#" + name] = sb.ToString();
            }
            else
            {
                queryMapper[name] = value.ToString();
            }

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
                urlBuilder.Append(string.Join("&", queryMapper.Select(BuildQueryKeyEqualValue)));
            }

            return new Uri(urlBuilder.ToString(), UriKind.RelativeOrAbsolute);
        }

        private string BuildQueryKeyEqualValue(KeyValuePair<string, string> pair)
        {
            return pair.Key.StartsWith("#", StringComparison.Ordinal)
                ? $"{pair.Key.Replace("#", string.Empty)}={pair.Value}"
                : $"{pair.Key}={Uri.EscapeDataString(pair.Value)}";
        }
    }
}
