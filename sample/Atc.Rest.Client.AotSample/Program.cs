var services = new ServiceCollection();
services.AddAtcRestClientCore(o => o.TypeInfoResolverChain.Insert(0, SampleJsonContext.Default));

await using var provider = services.BuildServiceProvider();

var checks = new AotChecks(
    provider.GetRequiredService<IContractSerializer>(),
    provider.GetRequiredService<IHttpMessageFactory>());

return await checks.RunAsync();