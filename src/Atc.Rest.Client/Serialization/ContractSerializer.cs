using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atc.Rest.Client.Serialization
{
    public class ContractSerializer : IContractSerializer
    {
        private static readonly JsonSerializerOptions DefaultOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(),
            },
        };

        private readonly JsonSerializerOptions options;

        public ContractSerializer(JsonSerializerOptions? options = default)
        {
            this.options = options ?? DefaultOptions;
        }

        public string Serialize(object value)
            => JsonSerializer.Serialize(value, options);

        public T? Deserialize<T>(string json)
            => JsonSerializer.Deserialize<T>(json, options);

        public T? Deserialize<T>(byte[] utf8Json)
            => JsonSerializer.Deserialize<T>(utf8Json, options);

        public object? Deserialize(string json, Type returnType)
            => JsonSerializer.Deserialize(json, returnType, options);

        public object? Deserialize(byte[] utf8Json, Type returnType)
            => JsonSerializer.Deserialize(utf8Json, returnType, options);
    }
}
