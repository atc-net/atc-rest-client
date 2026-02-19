namespace Atc.Rest.Client.Tests.TestTypes;

public sealed record NestedModel(TestModel? Parent, TestModel? Child);