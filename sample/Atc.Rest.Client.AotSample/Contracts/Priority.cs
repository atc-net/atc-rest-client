namespace Atc.Rest.Client.AotSample.Contracts;

[JsonConverter(typeof(JsonNumberEnumConverter<Priority>))]
public enum Priority
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}