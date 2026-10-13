namespace Atc.Rest.Client.SseSample.Contracts;

public sealed record Problem(
    string Title,
    int Status,
    string Detail);