var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// The API serves this app, so its address is the address of the page
builder.Services.AddAtcRestClient(
    PriceStreamClient.HttpClientName,
    new Uri(builder.HostEnvironment.BaseAddress),
    TimeSpan.FromSeconds(30));

// Source-generated JSON metadata: the app is trimmed when it is published
builder.Services.ConfigureAtcRestClientJsonOptions(o => o.TypeInfoResolverChain.Insert(0, ShowcaseJsonContext.Default));

builder.Services.AddScoped<PriceStreamClient>();

await builder.Build().RunAsync().ConfigureAwait(false);