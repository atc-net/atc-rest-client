[![NuGet Version](https://img.shields.io/nuget/v/Atc.Rest.Client.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/atc.rest.client)

# 🌐 ATC.Net REST Client

A lightweight and flexible REST client library for .NET, providing a clean abstraction over HttpClient with built-in support for request building, response handling, and dependency injection.

## 📚 Table of Contents

- [🌐 ATC.Net REST Client](#-atcnet-rest-client)
  - [📚 Table of Contents](#-table-of-contents)
  - [✨ Features](#-features)
  - [🚀 Getting Started](#-getting-started)
    - [📦 Installation](#-installation)
    - [⚙️ Service Registration](#️-service-registration)
      - [Approach 1: Core Services Only (No HttpClient Configuration)](#approach-1-core-services-only-no-httpclient-configuration)
      - [Approach 2: Direct Configuration](#approach-2-direct-configuration)
      - [Approach 3: Custom Options Type](#approach-3-custom-options-type)
    - [🔌 Creating an Endpoint](#-creating-an-endpoint)
  - [💡 Usage Examples](#-usage-examples)
    - [📥 Simple GET Request](#-simple-get-request)
    - [📤 POST Request with Body](#-post-request-with-body)
    - [🔗 Using Path and Query Parameters](#-using-path-and-query-parameters)
    - [📎 File Upload (Multipart Form Data)](#-file-upload-multipart-form-data)
    - [📁 File Upload with IFileContent](#-file-upload-with-ifilecontent)
    - [📤 Binary Upload (Raw Stream)](#-binary-upload-raw-stream)
    - [💾 File Download (Binary Response)](#-file-download-binary-response)
    - [🌊 Streaming Responses (IAsyncEnumerable)](#-streaming-responses-iasyncenumerable)
      - [Option 1: Simple Streaming (Basic Usage)](#option-1-simple-streaming-basic-usage)
      - [Option 2: Managed Lifecycle Streaming (Recommended) ✨](#option-2-managed-lifecycle-streaming-recommended-)
    - [📋 Handling Responses](#-handling-responses)
      - [Success and Error Response Handling](#success-and-error-response-handling)
      - [Custom Response Processing](#custom-response-processing)
      - [Plain Text Responses](#plain-text-responses)
      - [Error Response Deserialization Resilience](#error-response-deserialization-resilience)
  - [💎 Best Practices](#-best-practices)
    - [Choosing Between Overloads](#choosing-between-overloads)
    - [Multiple Client Registration](#multiple-client-registration)
  - [📖 API Reference](#-api-reference)
    - [Core Types](#core-types)
      - [`AddAtcRestClientCore` Extension Method](#addatcrestclientcore-extension-method)
      - [`AddAtcRestClient` Extension Methods (Internal)](#addatcrestclient-extension-methods-internal)
      - [`AtcRestClientOptions`](#atcrestclientoptions)
      - [`IHttpMessageFactory`](#ihttpmessagefactory)
      - [`IMessageRequestBuilder`](#imessagerequestbuilder)
      - [`IMessageResponseBuilder`](#imessageresponsebuilder)
    - [Response Status Properties](#response-status-properties)
    - [Response Types](#response-types)
      - [`EndpointResponse`](#endpointresponse)
      - [`BinaryEndpointResponse`](#binaryendpointresponse)
      - [`StreamBinaryEndpointResponse`](#streambinaryendpointresponse)
      - [`StreamingEndpointResponse<T>`](#streamingendpointresponset)
  - [🤝 How to Contribute](#-how-to-contribute)

## ✨ Features

- 🔗 **Fluent HTTP Request Building**: Build complex HTTP requests with a clean, chainable API
- 📦 **Typed Response Handling**: Strongly-typed success and error responses
- ⚙️ **Flexible Configuration**: Multiple ways to configure HTTP clients
- 💉 **Dependency Injection Ready**: Seamless integration with Microsoft.Extensions.DependencyInjection
- 🏷️ **Path Templates**: Support for URI templates with parameter replacement
- 🔍 **Query & Header Parameters**: Easy addition of query strings and headers
- 🔄 **Custom Serialization**: Pluggable contract serialization (defaults to JSON)
- ✅ **Response Processing**: Built-in support for success/error response handling
- 📎 **Multipart Form Data**: File upload support with Stream-based API
- 📤 **Binary Uploads**: Raw binary stream uploads (application/octet-stream)
- 💾 **Binary Responses**: Handle file downloads with byte[] or Stream responses
- 📝 **Plain Text Responses**: First-class `text/plain` handling that bypasses JSON deserialization
- 🌊 **Streaming Support**: IAsyncEnumerable streaming for large datasets with proper lifecycle management
- ⏱️ **HTTP Completion Options**: Control response buffering for streaming scenarios

## 🚀 Getting Started

### 📦 Installation

Install the package via NuGet:

```bash
dotnet add package Atc.Rest.Client
```

### ⚙️ Service Registration

There are multiple ways to register services with dependency injection:

#### Approach 1: Core Services Only (No HttpClient Configuration)

Use this approach when you configure HttpClient separately or use source-generated endpoints:

```csharp
using Atc.Rest.Client.Options;

// Registers IHttpMessageFactory and IContractSerializer (default JSON) only
services.AddAtcRestClientCore();

// Or with a custom serializer
services.AddAtcRestClientCore(myCustomSerializer);
```

#### Approach 2: Direct Configuration

Use this approach when you have straightforward configuration needs:

```csharp
using Atc.Rest.Client.Options;

services.AddAtcRestClient(
    clientName: "MyApiClient",
    baseAddress: new Uri("https://api.example.com"),
    timeout: TimeSpan.FromSeconds(30));
```

#### Approach 3: Custom Options Type

Use this approach when you need to register the options as a singleton for later retrieval:

```csharp
// Define a custom options class
public sealed class MyApiClientOptions : AtcRestClientOptions
{
    public string ApiKey { get; set; } = string.Empty;
}

// Register with custom options
var options = new MyApiClientOptions
{
    BaseAddress = new Uri("https://api.example.com"),
    Timeout = TimeSpan.FromSeconds(30),
    ApiKey = "your-api-key"
};

services.AddAtcRestClient(
    clientName: "MyApiClient",
    options: options);
```

### 🔌 Creating an Endpoint

Create an endpoint class that uses `IHttpMessageFactory` to build and send requests:

```csharp
public interface IUsersEndpoint
{
    Task<EndpointResponse<User>> GetUserAsync(int userId, CancellationToken cancellationToken = default);
}

public class UsersEndpoint : IUsersEndpoint
{
    private readonly IHttpClientFactory clientFactory;
    private readonly IHttpMessageFactory messageFactory;

    public UsersEndpoint(
        IHttpClientFactory clientFactory,
        IHttpMessageFactory messageFactory)
    {
        this.clientFactory = clientFactory;
        this.messageFactory = messageFactory;
    }

    public async Task<EndpointResponse<User>> GetUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateClient("MyApiClient");

        var requestBuilder = messageFactory.FromTemplate("/api/users/{userId}");
        requestBuilder.WithPathParameter("userId", userId);

        using var request = requestBuilder.Build(HttpMethod.Get);
        using var response = await client.SendAsync(request, cancellationToken);

        var responseBuilder = messageFactory.FromResponse(response);
        responseBuilder.AddSuccessResponse<User>(HttpStatusCode.OK);
        responseBuilder.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound);

        return await responseBuilder.BuildResponseAsync<User>(cancellationToken);
    }
}
```

Register the endpoint:

```csharp
services.AddSingleton<IUsersEndpoint, UsersEndpoint>();
```

## 💡 Usage Examples

### 📥 Simple GET Request

```csharp
var requestBuilder = messageFactory.FromTemplate("/api/products");

using var request = requestBuilder.Build(HttpMethod.Get);
using var response = await client.SendAsync(request, cancellationToken);

var responseBuilder = messageFactory.FromResponse(response);
responseBuilder.AddSuccessResponse<List<Product>>(HttpStatusCode.OK);

var result = await responseBuilder.BuildResponseAsync<List<Product>>(cancellationToken);

if (result.IsSuccess)
{
    var products = result.SuccessContent;
    // Process products 🎉
}
```

### 📤 POST Request with Body

```csharp
var newUser = new CreateUserRequest
{
    Name = "John Doe",
    Email = "john@example.com"
};

var requestBuilder = messageFactory.FromTemplate("/api/users");
requestBuilder.WithBody(newUser);

using var request = requestBuilder.Build(HttpMethod.Post);
using var response = await client.SendAsync(request, cancellationToken);

var responseBuilder = messageFactory.FromResponse(response);
responseBuilder.AddSuccessResponse<User>(HttpStatusCode.Created);
responseBuilder.AddErrorResponse<ValidationProblemDetails>(HttpStatusCode.BadRequest);

var result = await responseBuilder.BuildResponseAsync<User>(cancellationToken);
```

### 🔗 Using Path and Query Parameters

```csharp
var requestBuilder = messageFactory.FromTemplate("/api/users/{userId}/posts");
requestBuilder.WithPathParameter("userId", 123);
requestBuilder.WithQueryParameter("pageSize", 10);
requestBuilder.WithQueryParameter("page", 1);
requestBuilder.WithQueryParameter("orderBy", "createdDate");

using var request = requestBuilder.Build(HttpMethod.Get);
// Results in: GET /api/users/123/posts?pageSize=10&page=1&orderBy=createdDate
```

### 📎 File Upload (Multipart Form Data)

Upload files using the Stream-based multipart form data API:

```csharp
// Single file upload
await using var fileStream = File.OpenRead("document.pdf");

var requestBuilder = messageFactory.FromTemplate("/api/files/upload");
requestBuilder.WithFile(fileStream, "file", "document.pdf", "application/pdf");
requestBuilder.WithFormField("description", "My document");

using var request = requestBuilder.Build(HttpMethod.Post);
using var response = await client.SendAsync(request, cancellationToken);
```

Upload multiple files:

```csharp
await using var file1 = File.OpenRead("image1.png");
await using var file2 = File.OpenRead("image2.png");

var files = new List<(Stream, string, string, string?)>
{
    (file1, "images", "image1.png", "image/png"),
    (file2, "images", "image2.png", "image/png")
};

var requestBuilder = messageFactory.FromTemplate("/api/files/upload-multiple");
requestBuilder.WithFiles(files);

using var request = requestBuilder.Build(HttpMethod.Post);
```

### 📁 File Upload with IFileContent

For file uploads via `WithBody()`, implement the `IFileContent` interface:

```csharp
using Atc.Rest.Client;

public class MyFile : IFileContent
{
    public string FileName { get; init; }
    public string? ContentType { get; init; }
    public Stream OpenReadStream() => File.OpenRead(FileName);
}

var requestBuilder = messageFactory.FromTemplate("/api/files/upload");
requestBuilder.WithBody(new MyFile { FileName = "report.pdf", ContentType = "application/pdf" });

using var request = requestBuilder.Build(HttpMethod.Post);
using var response = await client.SendAsync(request, cancellationToken);
```

`WithBody()` also accepts `List<IFileContent>` for multi-file uploads.

> **Platform compatibility:** `WithBody()` automatically detects file-like objects that have
> a `FileName` (or `Name`) property and an `OpenReadStream()` method. This means ASP.NET Core
> `IFormFile` and Blazor `IBrowserFile` objects work without any additional packages or adapters:
>
> ```csharp
> // ASP.NET Core controller - works automatically
> public async Task<IActionResult> Upload(IFormFile file)
> {
>     requestBuilder.WithBody(file);
> }
>
> // Blazor WASM component - works automatically
> private async Task OnFileSelected(InputFileChangeEventArgs e)
> {
>     requestBuilder.WithBody(e.File);
> }
> ```
>
> For compile-time type safety, implement `IFileContent` explicitly.

### 📤 Binary Upload (Raw Stream)

Upload a raw binary stream directly with `application/octet-stream` content type:

```csharp
await using var fileStream = File.OpenRead("document.bin");

var requestBuilder = messageFactory.FromTemplate("/api/files/upload");
requestBuilder.WithBinaryBody(fileStream);

using var request = requestBuilder.Build(HttpMethod.Post);
using var response = await client.SendAsync(request, cancellationToken);
```

Use a custom content type:

```csharp
await using var imageStream = File.OpenRead("photo.png");

var requestBuilder = messageFactory.FromTemplate("/api/images/upload");
requestBuilder.WithBinaryBody(imageStream, "image/png");

using var request = requestBuilder.Build(HttpMethod.Post);
```

> 💡 **When to use `WithBinaryBody` vs `WithFile`:**
>
> - Use `WithBinaryBody` when the API expects raw binary data with `application/octet-stream` or similar content type
> - Use `WithFile` when the API expects `multipart/form-data` format (typical file upload forms)

### 💾 File Download (Binary Response)

Download files as byte arrays or streams:

```csharp
var requestBuilder = messageFactory.FromTemplate("/api/files/{fileId}");
requestBuilder.WithPathParameter("fileId", "123");

using var request = requestBuilder.Build(HttpMethod.Get);
using var response = await client.SendAsync(request, cancellationToken);

var responseBuilder = messageFactory.FromResponse(response);

// Option 1: Get as byte array 📦
var binaryResponse = await responseBuilder.BuildBinaryResponseAsync(cancellationToken);
if (binaryResponse.IsSuccess)
{
    var content = binaryResponse.Content;
    var fileName = binaryResponse.FileName;
    var contentType = binaryResponse.ContentType;
    // Save or process the file...
}

// Option 2: Get as stream (for large files) 🌊
var streamResponse = await responseBuilder.BuildStreamBinaryResponseAsync(cancellationToken);
if (streamResponse.IsSuccess)
{
    await using var content = streamResponse.Content;
    await using var fileStream = File.Create(streamResponse.FileName ?? "download.bin");
    await content!.CopyToAsync(fileStream, cancellationToken);
}
```

### 🌊 Streaming Responses (IAsyncEnumerable)

Stream large datasets efficiently using IAsyncEnumerable. There are two approaches:

#### Option 1: Simple Streaming (Basic Usage)

Use `BuildStreamingResponseAsync<T>` for simple streaming scenarios:

```csharp
var requestBuilder = messageFactory.FromTemplate("/api/data/stream");
requestBuilder.WithHttpCompletionOption(HttpCompletionOption.ResponseHeadersRead);

using var request = requestBuilder.Build(HttpMethod.Get);
using var response = await client.SendAsync(
    request,
    requestBuilder.HttpCompletionOption,
    cancellationToken);

var responseBuilder = messageFactory.FromResponse(response);

// ⚠️ Note: The response must stay alive during enumeration
await foreach (var item in responseBuilder.BuildStreamingResponseAsync<DataItem>(cancellationToken))
{
    if (item is not null)
    {
        Console.WriteLine($"Received: {item.Name}");
    }
}
```

#### Option 2: Managed Lifecycle Streaming (Recommended) ✨

Use `BuildStreamingEndpointResponseAsync<T>` for proper lifecycle management. This approach wraps the streaming content in a disposable response that manages the `HttpResponseMessage` lifecycle:

```csharp
var requestBuilder = messageFactory.FromTemplate("/api/data/stream");
requestBuilder.WithHttpCompletionOption(HttpCompletionOption.ResponseHeadersRead);

using var request = requestBuilder.Build(HttpMethod.Get);

// Don't use 'using' here - the StreamingEndpointResponse will manage the lifecycle
var response = await client.SendAsync(
    request,
    requestBuilder.HttpCompletionOption,
    cancellationToken);

var responseBuilder = messageFactory.FromResponse(response);

// 🎯 The response manages HttpResponseMessage disposal
using var streamingResponse = await responseBuilder.BuildStreamingEndpointResponseAsync<DataItem>(cancellationToken);

if (streamingResponse.IsSuccess && streamingResponse.Content is not null)
{
    await foreach (var item in streamingResponse.Content.WithCancellation(cancellationToken))
    {
        if (item is not null)
        {
            Console.WriteLine($"✅ Received: {item.Name}");
        }
    }
}
else
{
    // Handle error - error content is available
    Console.WriteLine($"❌ Error: {streamingResponse.ErrorContent}");
}
// HttpResponseMessage is automatically disposed here 🧹
```

> 💡 **Why use `BuildStreamingEndpointResponseAsync`?**
>
> - ✅ Proper lifecycle management - the `HttpResponseMessage` is disposed when you dispose the response
> - ✅ Error handling - access to `ErrorContent` when the request fails
> - ✅ Status code information - check `IsSuccess` and `StatusCode`
> - ✅ Avoids premature disposal - no risk of disposing the response before enumeration completes

### 📋 Handling Responses

#### Success and Error Response Handling

```csharp
var responseBuilder = messageFactory.FromResponse(response);
responseBuilder.AddSuccessResponse<User>(HttpStatusCode.OK);
responseBuilder.AddErrorResponse<ProblemDetails>(HttpStatusCode.BadRequest);
responseBuilder.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound);

var result = await responseBuilder.BuildResponseAsync<User, ProblemDetails>(cancellationToken);

if (result.IsSuccess)
{
    var user = result.SuccessContent;
    Console.WriteLine($"✅ Success: {user!.Name}");
}
else
{
    var problem = result.ErrorContent;
    Console.WriteLine($"❌ Error ({result.StatusCode}): {problem?.Detail}");
}
```

#### Custom Response Processing

```csharp
var responseBuilder = messageFactory.FromResponse(response);
responseBuilder.AddSuccessResponse<User>(HttpStatusCode.OK);

var result = await responseBuilder.BuildResponseAsync(
    response => new CustomResult
    {
        Success = response.IsSuccess,
        StatusCode = response.StatusCode,
        User = response.ContentObject as User
    },
    cancellationToken);
```

#### Plain Text Responses

For endpoints that return a `text/plain` (or other `text/*`) body with a `type: string` schema, use `AddSuccessTextResponse` / `AddErrorTextResponse`. These bypass the contract serializer entirely and return the body verbatim — no JSON parsing, no exceptions on non-JSON content:

```csharp
var requestBuilder = messageFactory.FromTemplate("/api/reports/{id}/text");
requestBuilder.WithPathParameter("id", "123");

using var request = requestBuilder.Build(HttpMethod.Get);
using var response = await client.SendAsync(request, cancellationToken);

var responseBuilder = messageFactory.FromResponse(response);
responseBuilder.AddSuccessTextResponse(HttpStatusCode.OK);
responseBuilder.AddErrorTextResponse(HttpStatusCode.BadRequest);

var result = await responseBuilder.BuildResponseAsync<string, string>(cancellationToken);

if (result.IsSuccess)
{
    // SuccessContent is the raw body, e.g. "file contents"
    Console.WriteLine(result.SuccessContent);
}
else
{
    // ErrorContent is the raw error body, e.g. "validation failed"
    Console.WriteLine($"❌ {result.StatusCode}: {result.ErrorContent}");
}
```

> 💡 **When to use `AddSuccessTextResponse` vs `AddSuccessResponse<string>`:**
>
> - Use `AddSuccessTextResponse` when the server returns a raw text body (e.g. `file contents`). The default JSON serializer would reject this since it's not a valid JSON literal.
> - Use `AddSuccessResponse<string>` only when the server returns a JSON-encoded string (e.g. `"file contents"` with quotes).
>
> An empty or whitespace-only body yields a `null` `ContentObject`, matching the behavior of `AddSuccessResponse<T>`. Character encoding follows the `Content-Type` charset.

#### Error Response Deserialization Resilience

When an error response (4xx/5xx) cannot be deserialized to the registered type (e.g., a server returns plain text instead of `ProblemDetails` JSON), the builder falls back to the raw string content instead of throwing. This allows generated result classes to handle the conversion gracefully:

```csharp
responseBuilder.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound);

// If the server returns plain text "Not Found" instead of JSON ProblemDetails,
// ContentObject will be the raw string "Not Found" (not a ProblemDetails instance).
// Generated result classes handle this via ProblemDetailsFactory.
var result = await responseBuilder.BuildResponseAsync(x => x, cancellationToken);
```

> **Note:** For success responses (2xx), deserialization failures still throw `RestClientDeserializationException` since the response body is the primary payload.

## 💎 Best Practices

### Choosing Between Overloads

| Scenario                                          | Recommended Approach                                                 |
|---------------------------------------------------|----------------------------------------------------------------------|
| Simple HTTP client with just base URL and timeout | **Non-generic overload** (`AddAtcRestClient(string, Uri, TimeSpan)`) |
| Additional configuration properties needed        | **Generic overload with custom options type**                        |

### Multiple Client Registration

When registering multiple HTTP clients, consider using a consistent naming convention:

```csharp
// ✅ Good: Clear, distinct names
services.AddAtcRestClient("Users-API", new Uri("https://users.api.com"), TimeSpan.FromSeconds(30));
services.AddAtcRestClient("Orders-API", new Uri("https://orders.api.com"), TimeSpan.FromSeconds(60));
services.AddAtcRestClient("Payments-API", new Uri("https://payments.api.com"), TimeSpan.FromSeconds(45));
```

## 📖 API Reference

### Core Types

#### `AddAtcRestClientCore` Extension Method

Registers core services (`IHttpMessageFactory` and `IContractSerializer`) without HttpClient configuration:

```csharp
IServiceCollection AddAtcRestClientCore(
    this IServiceCollection services,
    IContractSerializer? contractSerializer = null)
```

#### `AddAtcRestClient` Extension Methods (Internal)

These methods are used by source-generated code and are hidden from IntelliSense:

```csharp
// With HttpClient configuration
IServiceCollection AddAtcRestClient(
    this IServiceCollection services,
    string clientName,
    Uri baseAddress,
    TimeSpan timeout,
    Action<IHttpClientBuilder>? httpClientBuilder = null,
    IContractSerializer? contractSerializer = null)

// Generic overload for typed options
IServiceCollection AddAtcRestClient<TOptions>(
    this IServiceCollection services,
    string clientName,
    TOptions options,
    Action<IHttpClientBuilder>? httpClientBuilder = null,
    IContractSerializer? contractSerializer = null)
    where TOptions : AtcRestClientOptions, new()
```

#### `AtcRestClientOptions`

```csharp
public class AtcRestClientOptions
{
    public virtual Uri? BaseAddress { get; set; }
    public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

#### `IHttpMessageFactory`

```csharp
public interface IHttpMessageFactory
{
    IMessageRequestBuilder FromTemplate(string pathTemplate);
    IMessageResponseBuilder FromResponse(HttpResponseMessage? response);
}
```

#### `IMessageRequestBuilder`

```csharp
public interface IMessageRequestBuilder
{
    IMessageRequestBuilder WithPathParameter(string name, object? value);
    IMessageRequestBuilder WithQueryParameter(string name, object? value);
    IMessageRequestBuilder WithHeaderParameter(string name, object? value);
    IMessageRequestBuilder WithBody<TBody>(TBody body);
    HttpRequestMessage Build(HttpMethod method);

    // HTTP completion option for streaming
    IMessageRequestBuilder WithHttpCompletionOption(HttpCompletionOption completionOption);
    HttpCompletionOption HttpCompletionOption { get; }

    // Binary upload support (raw stream)
    IMessageRequestBuilder WithBinaryBody(Stream stream, string? contentType = null);

    // Multipart form data support
    IMessageRequestBuilder WithFile(Stream stream, string name, string fileName, string? contentType = null);
    IMessageRequestBuilder WithFiles(IEnumerable<(Stream Stream, string Name, string FileName, string? ContentType)> files);
    IMessageRequestBuilder WithFormField(string name, string value);
}
```

> **`IFileContent` interface:**
>
> ```csharp
> public interface IFileContent
> {
>     string FileName { get; }
>     string? ContentType { get; }
>     Stream OpenReadStream();
> }
> ```
>
> Used with `WithBody<TBody>()` for file uploads. Objects passed to `WithBody()` that have
> a `FileName`/`Name` property and an `OpenReadStream()` method are automatically detected
> and uploaded as multipart form data — no explicit `IFileContent` implementation required.

#### `IMessageResponseBuilder`

```csharp
public interface IMessageResponseBuilder
{
    IMessageResponseBuilder AddSuccessResponse(HttpStatusCode statusCode);
    IMessageResponseBuilder AddSuccessResponse<TResponseContent>(HttpStatusCode statusCode);
    IMessageResponseBuilder AddErrorResponse(HttpStatusCode statusCode);
    IMessageResponseBuilder AddErrorResponse<TResponseContent>(HttpStatusCode statusCode);

    // 📝 Plain text response support (text/*, bypasses serializer)
    IMessageResponseBuilder AddSuccessTextResponse(HttpStatusCode statusCode);
    IMessageResponseBuilder AddErrorTextResponse(HttpStatusCode statusCode);

    Task<TResult> BuildResponseAsync<TResult>(
        Func<EndpointResponse, TResult> factory,
        CancellationToken cancellationToken);

    Task<EndpointResponse<TSuccessContent>> BuildResponseAsync<TSuccessContent>(
        CancellationToken cancellationToken);

    Task<EndpointResponse<TSuccessContent, TErrorContent>> BuildResponseAsync<TSuccessContent, TErrorContent>(
        CancellationToken cancellationToken);

    // 💾 Binary response support
    Task<BinaryEndpointResponse> BuildBinaryResponseAsync(CancellationToken cancellationToken);
    Task<StreamBinaryEndpointResponse> BuildStreamBinaryResponseAsync(CancellationToken cancellationToken);

    // 🌊 Streaming support
    IAsyncEnumerable<T?> BuildStreamingResponseAsync<T>(CancellationToken cancellationToken = default);
    Task<StreamingEndpointResponse<T>> BuildStreamingEndpointResponseAsync<T>(CancellationToken cancellationToken = default);
}
```

### Response Status Properties

All response types provide two status properties for checking request outcomes:

| Property    | Meaning                        | Determination                                       |
|-------------|--------------------------------|-----------------------------------------------------|
| `IsSuccess` | Request completed successfully | Based on HTTP 2xx status or configured status codes |

**Examples:**

| HTTP Status    | `IsSuccess` |
|----------------|-------------|
| 200 OK         | ✅ `true`   |
| 201 Created    | ✅ `true`   |
| 204 NoContent  | ✅ `true`   |
| 400 BadRequest | ❌ `false`  |
| 404 NotFound   | ❌ `false`  |

**When to use each:**

- **`IsSuccess`**: General success check — "Did the request succeed?"

All response types support both properties:

| Type                                 | `IsSuccess` |
|--------------------------------------|-------------|
| `EndpointResponse`                   | ✅          |
| `EndpointResponse<TSuccess>`         | ✅          |
| `EndpointResponse<TSuccess, TError>` | ✅          |
| `BinaryEndpointResponse`             | ✅          |
| `StreamBinaryEndpointResponse`       | ✅          |
| `StreamingEndpointResponse<T>`       | ✅          |

### Response Types

#### `EndpointResponse`

```csharp
public class EndpointResponse : IEndpointResponse
{
    public bool IsSuccess { get; }

    public HttpStatusCode StatusCode { get; }

    public string Content { get; }

    public object? ContentObject { get; }

    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }

    protected InvalidOperationException InvalidContentAccessException<TExpected>(
        HttpStatusCode expectedStatusCode,
        string propertyName);
}

// Generic variants available:
// - EndpointResponse<TSuccess>
// - EndpointResponse<TSuccess, TError>
```

#### `BinaryEndpointResponse`

```csharp
public class BinaryEndpointResponse : IBinaryEndpointResponse
{
    public bool IsSuccess { get; }

    public HttpStatusCode StatusCode { get; }

    public byte[]? Content { get; }

    public string? ContentType { get; }

    public string? FileName { get; }

    public long? ContentLength { get; }

    public string? ErrorContent { get; }  // Error message if request failed

    protected InvalidOperationException InvalidContentAccessException(
        HttpStatusCode expectedStatusCode,
        string propertyName);
}
```

#### `StreamBinaryEndpointResponse`

```csharp
public class StreamBinaryEndpointResponse : IStreamBinaryEndpointResponse, IDisposable
{
    public bool IsSuccess { get; }

    public HttpStatusCode StatusCode { get; }

    public Stream? Content { get; }

    public string? ContentType { get; }

    public string? FileName { get; }

    public long? ContentLength { get; }

    public string? ErrorContent { get; }  // Error message if request failed

    public void Dispose();

    protected InvalidOperationException InvalidContentAccessException(
        HttpStatusCode expectedStatusCode,
        string propertyName);
}
```

#### `StreamingEndpointResponse<T>`

A disposable response type for streaming `IAsyncEnumerable<T>` content with proper lifecycle management:

```csharp
public class StreamingEndpointResponse<T> : IStreamingEndpointResponse<T>, IDisposable
{
    public bool IsSuccess { get; }

    public HttpStatusCode StatusCode { get; }

    public IAsyncEnumerable<T?>? Content { get; }  // 🌊 The streaming content

    public string? ErrorContent { get; }  // ❌ Error message if request failed

    public void Dispose();  // 🧹 Disposes the underlying HttpResponseMessage

    protected InvalidOperationException InvalidContentAccessException(
        HttpStatusCode expectedStatusCode,
        string propertyName);
}
```

> 💡 **Key Benefits:**
>
> - Manages `HttpResponseMessage` lifecycle automatically
> - Provides `ErrorContent` when the request fails
> - Prevents premature disposal during enumeration
> - Inheritable for custom response types

## 🤝 How to Contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
