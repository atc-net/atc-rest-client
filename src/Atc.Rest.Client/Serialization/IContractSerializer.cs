using System;

namespace Atc.Rest.Client.Serialization
{
    public interface IContractSerializer
    {
        string Serialize(object value);

        T? Deserialize<T>(string json);

        T? Deserialize<T>(byte[] utf8Json);

        object? Deserialize(string json, Type returnType);

        object? Deserialize(byte[] utf8Json, Type returnType);
    }
}