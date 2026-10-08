namespace Atc.Rest.Client.Tests.TestTypes;

[JsonSerializable(typeof(TestModel))]
public sealed partial class TestJsonContext : JsonSerializerContext;