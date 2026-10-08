namespace Atc.Rest.Client.Tests.TestTypes;

public sealed record TaskItem(
    Priority Priority,
    Priority? OptionalPriority,
    TestStatus Status);