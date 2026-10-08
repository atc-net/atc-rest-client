namespace Atc.Rest.Client.AotSample.Contracts;

public enum ContactChannel
{
    [JsonStringEnumMemberName("e-mail")]
    Email,

    Sms,
}