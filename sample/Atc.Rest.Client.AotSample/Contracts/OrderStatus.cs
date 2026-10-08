namespace Atc.Rest.Client.AotSample.Contracts;

[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
public enum OrderStatus
{
    [EnumMember(Value = "open")]
    [JsonStringEnumMemberName("open")]
    Open,

    [EnumMember(Value = "shipped")]
    [JsonStringEnumMemberName("shipped")]
    Shipped,
}