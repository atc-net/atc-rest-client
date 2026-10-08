namespace Atc.Rest.Client.AotSample;

/// <summary>
/// Exercises the Atc.Rest.Client features that depend on reflection or JSON metadata,
/// so a Native AOT build of this app shows whether they survive trimming and AOT compilation.
/// </summary>
public sealed class AotChecks
{
    private const string OrderJson = """{"id":7,"title":"Desk","status":"shipped","priority":3,"optionalPriority":null}""";

    private static readonly Order SampleOrder = new(7, "Desk", OrderStatus.Open, Priority.Medium, Priority.High);

    private readonly IContractSerializer serializer;
    private readonly IHttpMessageFactory messageFactory;

    public AotChecks(
        IContractSerializer serializer,
        IHttpMessageFactory messageFactory)
    {
        this.serializer = serializer;
        this.messageFactory = messageFactory;
    }

    public async Task<int> RunAsync()
    {
        var checks = new (string Name, Func<Task> Check)[]
        {
            ("JSON body is serialized through the JsonSerializerContext", CheckJsonBodyAsync),
            ("Deserialize<T>(string)", () => Run(CheckDeserializeGeneric)),
            ("Deserialize<T>(byte[])", () => Run(CheckDeserializeBytes)),
            ("Deserialize(string, Type)", () => Run(CheckDeserializeByType)),
            ("DeserializeAsyncEnumerable<T>", CheckDeserializeAsyncEnumerableAsync),
            ("Response builder deserializes the success content", CheckResponseBuilderAsync),
            ("A type outside the context throws NotSupportedException", () => Run(CheckUnregisteredTypeThrows)),
            ("Path parameter enum uses [EnumMember]", () => Run(CheckPathEnum)),
            ("Header parameter enum uses [EnumMember]", () => Run(CheckHeaderEnum)),
            ("Query enum uses [JsonStringEnumMemberName]", () => Run(CheckQueryJsonStringEnumMemberName)),
            ("Query array of a JsonNumberEnumConverter enum writes numbers", () => Run(CheckQueryNumberEnumArray)),
            ("Query DateOnly and decimal are culture-independent", () => Run(CheckQueryInvariantValues)),
            ("bool path, query and header values are lowercase", () => Run(CheckBoolValues)),
            ("IFileContent body is sent as multipart", () => Run(CheckFileContentBody)),
            ("Repeated multipart form fields are all sent", CheckRepeatedFormFieldsAsync),
            ("URL-encoded form body", CheckUrlEncodedFormAsync),
        };

        var failures = 0;
        foreach (var (name, check) in checks)
        {
            try
            {
                await check().ConfigureAwait(false);
                Console.WriteLine($"PASS  {name}");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                failures++;
                Console.WriteLine($"FAIL  {name}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Console.WriteLine();
        Console.WriteLine(failures == 0
            ? $"All {checks.Length} checks passed."
            : $"{failures} of {checks.Length} checks failed.");

        return failures == 0 ? 0 : 1;
    }

    private static Task Run(Action check)
    {
        check();
        return Task.CompletedTask;
    }

    private static void Expect<T>(
        T actual,
        T expected)
    {
        if (!EqualityComparer<T>.Default.Equals(actual, expected))
        {
            throw new InvalidOperationException($"expected '{expected}', got '{actual}'");
        }
    }

    private static void ExpectContains(
        string actual,
        string expected)
    {
        if (!actual.Contains(expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"expected '{actual}' to contain '{expected}'");
        }
    }

    private async Task CheckJsonBodyAsync()
    {
        using var request = messageFactory
            .FromTemplate("/orders")
            .WithBody(SampleOrder)
            .Build(HttpMethod.Post);

        var body = await request.Content!.ReadAsStringAsync().ConfigureAwait(false);

        ExpectContains(body, "\"title\": \"Desk\"");
        ExpectContains(body, "\"status\": \"open\"");
        ExpectContains(body, "\"priority\": 2");
        ExpectContains(body, "\"optionalPriority\": 3");
    }

    private void CheckDeserializeGeneric()
        => Expect(
            serializer.Deserialize<Order>(OrderJson),
            new Order(7, "Desk", OrderStatus.Shipped, Priority.High, OptionalPriority: null));

    private void CheckDeserializeBytes()
        => Expect(
            serializer.Deserialize<Order>(Encoding.UTF8.GetBytes(OrderJson)),
            new Order(7, "Desk", OrderStatus.Shipped, Priority.High, OptionalPriority: null));

    private void CheckDeserializeByType()
        => Expect(
            serializer.Deserialize(OrderJson, typeof(Order)),
            (object)new Order(7, "Desk", OrderStatus.Shipped, Priority.High, OptionalPriority: null));

    private async Task CheckDeserializeAsyncEnumerableAsync()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"[{OrderJson},{OrderJson}]"));

        var count = 0;
        await foreach (var order in serializer.DeserializeAsyncEnumerable<Order>(stream).ConfigureAwait(false))
        {
            Expect(order?.Title, "Desk");
            count++;
        }

        Expect(count, 2);
    }

    private async Task CheckResponseBuilderAsync()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(OrderJson, Encoding.UTF8, "application/json"),
        };

        var result = await messageFactory
            .FromResponse(response)
            .AddSuccessResponse<Order>(HttpStatusCode.OK)
            .BuildResponseAsync<Order>(CancellationToken.None)
            .ConfigureAwait(false);

        Expect(result.SuccessContent?.Status, OrderStatus.Shipped);
    }

    private void CheckUnregisteredTypeThrows()
    {
        try
        {
            serializer.Deserialize<UnregisteredModel>("""{"value":"x"}""");
        }
        catch (NotSupportedException)
        {
            return;
        }

        throw new InvalidOperationException("expected NotSupportedException, but deserialization succeeded");
    }

    private void CheckPathEnum()
    {
        using var request = messageFactory
            .FromTemplate("/orders/{status}")
            .WithPathParameter("status", OrderStatus.Shipped)
            .Build(HttpMethod.Get);

        Expect(request.RequestUri!.OriginalString, "/orders/shipped");
    }

    private void CheckHeaderEnum()
    {
        using var request = messageFactory
            .FromTemplate("/orders")
            .WithHeaderParameter("x-status", OrderStatus.Open)
            .Build(HttpMethod.Get);

        Expect(string.Join(",", request.Headers.GetValues("x-status")), "open");
    }

    private void CheckQueryJsonStringEnumMemberName()
    {
        using var request = messageFactory
            .FromTemplate("/contacts")
            .WithQueryParameter("channel", ContactChannel.Email)
            .Build(HttpMethod.Get);

        Expect(request.RequestUri!.OriginalString, "/contacts?channel=e-mail");
    }

    private void CheckQueryNumberEnumArray()
    {
        using var request = messageFactory
            .FromTemplate("/orders")
            .WithQueryParameter("priorities", new[] { Priority.Medium, Priority.High })
            .Build(HttpMethod.Get);

        Expect(request.RequestUri!.OriginalString, "/orders?priorities=2&priorities=3");
    }

    private void CheckQueryInvariantValues()
    {
        using var request = messageFactory
            .FromTemplate("/orders")
            .WithQueryParameter("day", new DateOnly(1990, 2, 28))
            .WithQueryParameter("amount", 1.5m)
            .Build(HttpMethod.Get);

        Expect(request.RequestUri!.OriginalString, "/orders?day=1990-02-28&amount=1.5");
    }

    private void CheckBoolValues()
    {
        using var request = messageFactory
            .FromTemplate("/flags/{flag}")
            .WithPathParameter("flag", true)
            .WithQueryParameter("list", new[] { true, false })
            .WithHeaderParameter("x-flag", false)
            .Build(HttpMethod.Get);

        Expect(request.RequestUri!.OriginalString, "/flags/true?list=true&list=false");
        Expect(string.Join(",", request.Headers.GetValues("x-flag")), "false");
    }

    private void CheckFileContentBody()
    {
        using var request = messageFactory
            .FromTemplate("/files")
            .WithBody(new SampleFile("report.pdf", "application/pdf", [1, 2, 3]))
            .Build(HttpMethod.Post);

        Expect(request.Content is MultipartFormDataContent, expected: true);
    }

    private async Task CheckRepeatedFormFieldsAsync()
    {
        using var request = messageFactory
            .FromTemplate("/forms")
            .WithFormField("items", "finance")
            .WithFormField("items", "q3")
            .Build(HttpMethod.Post);

        var body = await request.Content!.ReadAsStringAsync().ConfigureAwait(false);

        ExpectContains(body, "finance");
        ExpectContains(body, "q3");
    }

    private async Task CheckUrlEncodedFormAsync()
    {
        using var request = messageFactory
            .FromTemplate("/connect/token")
            .WithUrlEncodedFormField("grant_type", "client_credentials")
            .WithUrlEncodedFormField("scope", "orders.read")
            .WithUrlEncodedFormField("scope", "orders.write")
            .Build(HttpMethod.Post);

        var body = await request.Content!.ReadAsStringAsync().ConfigureAwait(false);

        Expect(body, "grant_type=client_credentials&scope=orders.read&scope=orders.write");
    }
}