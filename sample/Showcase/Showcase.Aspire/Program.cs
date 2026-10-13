var builder = DistributedApplication.CreateBuilder(args);

// The API streams the prices and serves the Blazor app that reads them, so it is the one resource.
// Open its endpoint from the dashboard to see the showcase.
builder
    .AddProject<Projects.Showcase_Api>("showcase")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

await builder
    .Build()
    .RunAsync()
    .ConfigureAwait(false);