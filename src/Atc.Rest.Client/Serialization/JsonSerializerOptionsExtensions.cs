using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Atc.Rest.Client.Serialization
{
    public static class JsonSerializerOptionsExtensions
    {
        public static JsonSerializerOptions WithoutConverter(
            this JsonSerializerOptions source,
            params JsonConverter[] converters)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var result = new JsonSerializerOptions
            {
                AllowTrailingCommas = source.AllowTrailingCommas,
                DefaultBufferSize = source.DefaultBufferSize,
                DictionaryKeyPolicy = source.DictionaryKeyPolicy,
                Encoder = source.Encoder,
                IgnoreNullValues = source.IgnoreNullValues,
                IgnoreReadOnlyProperties = source.IgnoreReadOnlyProperties,
                MaxDepth = source.MaxDepth,
                PropertyNameCaseInsensitive = source.PropertyNameCaseInsensitive,
                PropertyNamingPolicy = source.PropertyNamingPolicy,
                ReadCommentHandling = source.ReadCommentHandling,
                WriteIndented = source.WriteIndented,
            };

            foreach (var converter in source.Converters.Except(converters))
            {
                result.Converters.Add(converter);
            }

            return result;
        }
    }
}