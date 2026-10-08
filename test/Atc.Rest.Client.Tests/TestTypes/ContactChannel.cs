namespace Atc.Rest.Client.Tests.TestTypes;

public enum ContactChannel
{
    [JsonStringEnumMemberName("e-mail")]
    Email,

    Sms,
}