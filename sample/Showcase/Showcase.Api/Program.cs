var builder = WebApplication.CreateBuilder(args);

// Traces of the requests - a streamed one stays open for as long as the client reads - go to the Aspire dashboard
// when the API runs under Showcase.Aspire, which sets the OTLP endpoint. Without it, nothing is exported.
var telemetry = builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(x => x.AddService(builder.Environment.ApplicationName))
    .WithTracing(x => x.AddAspNetCoreInstrumentation());

if (!string.IsNullOrEmpty(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
{
    telemetry.UseOtlpExporter();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Lets Visual Studio debug the Blazor WebAssembly app that this API serves
    app.UseWebAssemblyDebugging();
}

// The Blazor WebAssembly app is served from here, so the API and the app share an origin.
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.MapGet("/health", () => "Healthy");
app.MapGet("/api/prices", PriceEndpoints.StreamAsync);
app.MapGet("/api/prices/unavailable", PriceEndpoints.UnavailableAsync);
app.MapGet("/api/prices/huge", PriceEndpoints.HugeAsync);

app.MapFallbackToFile("index.html");

await app.RunAsync().ConfigureAwait(false);