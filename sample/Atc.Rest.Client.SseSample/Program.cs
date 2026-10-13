var server = await SampleServer.StartAsync();
await using (server.ConfigureAwait(false))
{
    var services = new ServiceCollection();
    services.AddAtcRestClient("Prices", server.BaseAddress, TimeSpan.FromSeconds(30));
    services.ConfigureAtcRestClientJsonOptions(o => o.TypeInfoResolverChain.Insert(0, SampleJsonContext.Default));

    // The client is an application of its own: it shares no services with the server above.
#pragma warning disable ASP0000
    var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000
    await using (provider.ConfigureAwait(false))
    {
        Console.WriteLine($"Server-sent events with Atc.Rest.Client, against {server.BaseAddress}");

        var demos = new SseDemos(
            provider.GetRequiredService<IHttpClientFactory>(),
            provider.GetRequiredService<IHttpMessageFactory>());

        await demos.RunAsync(CancellationToken.None).ConfigureAwait(false);
    }
}