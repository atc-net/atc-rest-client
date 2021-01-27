using System.Net.Http;

namespace Atc.Rest.Client.Builder
{
    public interface IMessageRequestBuilder
    {
        IMessageRequestBuilder WithPathParameter(string name, string value);

        IMessageRequestBuilder WithQueryParameter(string name, string? value);

        IMessageRequestBuilder WithQueryParameter<T>(string name, T value)
            where T : struct;

        IMessageRequestBuilder WithBody<TBody>(TBody body);

        HttpRequestMessage Build(HttpMethod method);
    }
}