namespace Atc.Rest.Client.Tests.TestTypes;

public enum OperatorRole
{
    None = 0,

    [EnumMember(Value = "owner")]
    Owner = 1,

    [EnumMember(Value = "admin")]
    Admin = 2,
}