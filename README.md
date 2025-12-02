[![NuGet Version](https://img.shields.io/nuget/v/Atc.Rest.Client.svg?logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/atc.rest.client)

# ATC.Net REST Client

A lightweight and flexible REST client library for .NET, providing a clean abstraction over HttpClient with built-in support for request building, response handling, and dependency injection.

## Table of Contents

- [ATC.Net REST Client](#atcnet-rest-client)
  - [Table of Contents](#table-of-contents)
  - [Features](#features)
  - [Getting Started](#getting-started)
    - [Installation](#installation)
    - [Service Registration](#service-registration)
      - [Approach 1: Simple Registration (No HttpClient Configuration)](#approach-1-simple-registration-no-httpclient-configuration)
      - [Approach 2: Direct Configuration](#approach-2-direct-configuration)
      - [Approach 3: Custom Options Type](#approach-3-custom-options-type)
    - [Creating an Endpoint](#creating-an-endpoint)
  - [Usage Examples](#usage-examples)
    - [Simple GET Request](#simple-get-request)
    - [POST Request with Body](#post-request-with-body)
    - [Using Path and Query Parameters](#using-path-and-query-parameters)
    - [File Upload (Multipart Form Data)](#file-upload-multipart-form-data)
    - [File Download (Binary Response)](#file-download-binary-response)
    - [Streaming Responses (IAsyncEnumerable)](#streaming-responses-iasyncenumerable)
    - [Handling Responses](#handling-responses)
      - [Success and Error Response Handling](#success-and-error-response-handling)
      - [Custom Response Processing](#custom-response-processing)
  - [Best Practices](#best-practices)
    - [Choosing Between Overloads](#choosing-between-overloads)
    - [Multiple Client Registration](#multiple-client-registration)
  - [API Reference](#api-reference)
    - [Core Types](#core-types)
      - [`AddAtcRestClient` Extension Methods](#addatcrestclient-extension-methods)
      - [`AtcRestClientOptions`](#atcrestclientoptions)
      - [`IHttpMessageFactory`](#ihttpmessagefactory)
      - [`IMessageRequestBuilder`](#imessagerequestbuilder)
      - [`IMessageResponseBuilder`](#imessageresponsebuilder)
      - [`EndpointResponse`](#endpointresponse)
      - [`BinaryEndpointResponse`](#binaryendpointresponse)
      - [`StreamBinaryEndpointResponse`](#streambinaryendpointresponse)
  - [How to Contribute](#how-to-contribute)

## Features

- **Fluent HTTP Request Building**: Build complex HTTP requests with a clean, chainable API
- **Typed Response Handling**: Strongly-typed success and error responses
- **Flexible Configuration**: Multiple ways to configure HTTP clients
- **Dependency Injection Ready**: Seamless integration with Microsoft.Extensions.DependencyInjection
- **Path Templates**: Support for URI templates with parameter replacement
- **Query & Header Parameters**: Easy addition of query strings and headers
- **Custom Serialization**: Pluggable contract serialization (defaults to JSON)
- **Response Processing**: Built-in support for success/error response handling
- **Multipart Form Data**: File upload support with Stream-based API
- **Binary Responses**: Handle file downloads with byte[] or Stream responses
- **Streaming Support**: IAsyncEnumerable streaming for large datasets
- **HTTP Completion Options**: Control response buffering for streaming scenarios

## Getting Started

### Installation

Install the package via NuGet:

```bash
dotnet add package Atc.Rest.Client
```

### Service Registration

There are multiple ways to register services with dependency injection:

#### Approach 1: Simple Registration (No HttpClient Configuration)

Use this approach when you configure HttpClient separately or use source-generated endpoints:

```csharp
using Atc.Rest.Client.Options;

// Registers IHttpMessageFactory and IContractSerializer only
services.AddAtcRestClient();

// Or with a custom serializer
services.AddAtcRestClient(myCustomSerializer);
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

### Creating an Endpoint

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

## Usage Examples

### Simple GET Request

```csharp
var requestBuilder = messageFactory.FromTemplate("/api/products");

using var request = requestBuilder.Build(HttpMethod.Get);
using var response = await client.SendAsync(request, cancellationToken);

var responseBuilder = messageFactory.FromResponse(response);
responseBuilder.AddSuccessResponse<List<Product>>(HttpStatusCode.OK);

var result = await responseBuilder.BuildResponseAsync<List<Product>>(cancellationToken);

if (result.IsSuccess)
{
    var products = result.OkContent;
    // Process products
}
```

### POST Request with Body

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

### Using Path and Query Parameters

```csharp
var requestBuilder = messageFactory.FromTemplate("/api/users/{userId}/posts");
requestBuilder.WithPathParameter("userId", 123);
requestBuilder.WithQueryParameter("pageSize", 10);
requestBuilder.WithQueryParameter("page", 1);
requestBuilder.WithQueryParameter("orderBy", "createdDate");

using var request = requestBuilder.Build(HttpMethod.Get);
// Results in: GET /api/users/123/posts?pageSize=10&page=1&orderBy=createdDate
```

### File Upload (Multipart Form Data)

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

### File Download (Binary Response)

Download files as byte arrays or streams:

```csharp
var requestBuilder = messageFactory.FromTemplate("/api/files/{fileId}");
requestBuilder.WithPathParameter("fileId", "123");

using var request = requestBuilder.Build(HttpMethod.Get);
using var response = await client.SendAsync(request, cancellationToken);

var responseBuilder = messageFactory.FromResponse(response);

// Option 1: Get as byte array
var binaryResponse = await responseBuilder.BuildBinaryResponseAsync(cancellationToken);
if (binaryResponse.IsSuccess)
{
    var content = binaryResponse.Content;
    var fileName = binaryResponse.FileName;
    var contentType = binaryResponse.ContentType;
    // Save or process the file...
}

// Option 2: Get as stream (for large files)
var streamResponse = await responseBuilder.BuildStreamBinaryResponseAsync(cancellationToken);
if (streamResponse.IsSuccess)
{
    await using var contentStream = streamResponse.ContentStream;
    await using var fileStream = File.Create(streamResponse.FileName ?? "download.bin");
    await contentStream!.CopyToAsync(fileStream, cancellationToken);
}
```

### Streaming Responses (IAsyncEnumerable)

Stream large datasets efficiently using IAsyncEnumerable:

```csharp
var requestBuilder = messageFactory.FromTemplate("/api/data/stream");

// Set HttpCompletionOption for streaming (don't buffer the entire response)
requestBuilder.WithHttpCompletionOption(HttpCompletionOption.ResponseHeadersRead);

using var request = requestBuilder.Build(HttpMethod.Get);
using var response = await client.SendAsync(
    request,
    requestBuilder.HttpCompletionOption,  // Use the configured option
    cancellationToken);

var responseBuilder = messageFactory.FromResponse(response);

// Stream items as they arrive
await foreach (var item in responseBuilder.BuildStreamingResponseAsync<DataItem>(cancellationToken))
{
    if (item is not null)
    {
        Console.WriteLine($"Received: {item.Name}");
    }
}
```

### Handling Responses

#### Success and Error Response Handling

```csharp
var responseBuilder = messageFactory.FromResponse(response);
responseBuilder.AddSuccessResponse<User>(HttpStatusCode.OK);
responseBuilder.AddErrorResponse<ProblemDetails>(HttpStatusCode.BadRequest);
responseBuilder.AddErrorResponse<ProblemDetails>(HttpStatusCode.NotFound);

var result = await responseBuilder.BuildResponseAsync<User, ProblemDetails>(cancellationToken);

if (result.IsOk)
{
    var user = result.OkContent;
    Console.WriteLine($"Success: {user.Name}");
}
else if (result.IsBadRequest)
{
    var problem = result.BadRequestContent;
    Console.WriteLine($"Validation Error: {problem.Detail}");
}
else if (result.IsNotFound)
{
    Console.WriteLine("User not found");
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

## Best Practices

### Choosing Between Overloads

| Scenario | Recommended Approach |
|----------|---------------------|
| Simple HTTP client with just base URL and timeout | **Non-generic overload** (`AddAtcRestClient(string, Uri, TimeSpan)`) |
| Additional configuration properties needed | **Generic overload with custom options type** |

### Multiple Client Registration

When registering multiple HTTP clients, consider using a consistent naming convention:

```csharp
// Good: Clear, distinct names
services.AddAtcRestClient("Users-API", new Uri("https://users.api.com"), TimeSpan.FromSeconds(30));
services.AddAtcRestClient("Orders-API", new Uri("https://orders.api.com"), TimeSpan.FromSeconds(60));
services.AddAtcRestClient("Payments-API", new Uri("https://payments.api.com"), TimeSpan.FromSeconds(45));
```

## API Reference

### Core Types

#### `AddAtcRestClient` Extension Methods

```csharp
// Simple registration (no HttpClient configuration)
IServiceCollection AddAtcRestClient(this IServiceCollection services)

// With custom serializer
IServiceCollection AddAtcRestClient(
    this IServiceCollection services,
    IContractSerializer contractSerializer)

// With configuration action
IServiceCollection AddAtcRestClient(
    this IServiceCollection services,
    Action<AtcRestClientOptions> configure)

// With HttpClient configuration
IServiceCollection AddAtcRestClient(
    string clientName,
    Uri baseAddress,
    TimeSpan timeout,
    Action<IHttpClientBuilder>? httpClientBuilder = null,
    IContractSerializer? contractSerializer = null)

// Generic overload for typed options
IServiceCollection AddAtcRestClient<TOptions>(
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

    // Multipart form data support
    IMessageRequestBuilder WithFile(Stream stream, string name, string fileName, string? contentType = null);
    IMessageRequestBuilder WithFiles(IEnumerable<(Stream Stream, string Name, string FileName, string? ContentType)> files);
    IMessageRequestBuilder WithFormField(string name, string value);
}
```

#### `IMessageResponseBuilder`

```csharp
public interface IMessageResponseBuilder
{
    IMessageResponseBuilder AddSuccessResponse(HttpStatusCode statusCode);
    IMessageResponseBuilder AddSuccessResponse<TResponseContent>(HttpStatusCode statusCode);
    IMessageResponseBuilder AddErrorResponse(HttpStatusCode statusCode);
    IMessageResponseBuilder AddErrorResponse<TResponseContent>(HttpStatusCode statusCode);

    Task<TResult> BuildResponseAsync<TResult>(
        Func<EndpointResponse, TResult> factory,
        CancellationToken cancellationToken);

    Task<EndpointResponse<TSuccessContent>> BuildResponseAsync<TSuccessContent>(
        CancellationToken cancellationToken)
        where TSuccessContent : class;

    Task<EndpointResponse<TSuccessContent, TErrorContent>> BuildResponseAsync<TSuccessContent, TErrorContent>(
        CancellationToken cancellationToken)
        where TSuccessContent : class
        where TErrorContent : class;

    // Binary response support
    Task<BinaryEndpointResponse> BuildBinaryResponseAsync(CancellationToken cancellationToken);
    Task<StreamBinaryEndpointResponse> BuildStreamBinaryResponseAsync(CancellationToken cancellationToken);

    // Streaming support
    IAsyncEnumerable<T?> BuildStreamingResponseAsync<T>(CancellationToken cancellationToken = default);
}
```

#### `EndpointResponse`

```csharp
public class EndpointResponse : IEndpointResponse
{
    public bool IsSuccess { get; }
    public HttpStatusCode StatusCode { get; }
    public string Content { get; }
    public object? ContentObject { get; }
    public IReadOnlyDictionary<string, IEnumerable<string>> Headers { get; }
}

// Generic variants available:
// - EndpointResponse<TSuccess>
// - EndpointResponse<TSuccess, TError>
```

#### `BinaryEndpointResponse`

```csharp
public class BinaryEndpointResponse
{
    public bool IsSuccess { get; }
    public bool IsOk { get; }  // True if StatusCode == 200
    public HttpStatusCode StatusCode { get; }
    public byte[]? Content { get; }
    public string? ContentType { get; }
    public string? FileName { get; }
    public long? ContentLength { get; }
}
```

#### `StreamBinaryEndpointResponse`

```csharp
public class StreamBinaryEndpointResponse : IDisposable
{
    public bool IsSuccess { get; }
    public bool IsOk { get; }  // True if StatusCode == 200
    public HttpStatusCode StatusCode { get; }
    public Stream? ContentStream { get; }
    public string? ContentType { get; }
    public string? FileName { get; }
    public long? ContentLength { get; }

    public void Dispose();
}
```

## How to Contribute

[Contribution Guidelines](https://atc-net.github.io/introduction/about-atc#how-to-contribute)

[Coding Guidelines](https://atc-net.github.io/introduction/about-atc#coding-guidelines)
