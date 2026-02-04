namespace Atc.Rest.Client.Tests.Options;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAtcRestClientCore_RegistersHttpMessageFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAtcRestClientCore();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IHttpMessageFactory>();

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void AddAtcRestClientCore_RegistersDefaultContractSerializer()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAtcRestClientCore();
        var provider = services.BuildServiceProvider();
        var serializer = provider.GetService<IContractSerializer>();

        // Assert
        serializer.Should().NotBeNull();
        serializer.Should().BeOfType<DefaultJsonContractSerializer>();
    }

    [Fact]
    public void AddAtcRestClientCore_WithCustomSerializer_UsesProvidedSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        var customSerializer = Substitute.For<IContractSerializer>();

        // Act
        services.AddAtcRestClientCore(customSerializer);
        var provider = services.BuildServiceProvider();
        var resolvedSerializer = provider.GetService<IContractSerializer>();

        // Assert
        resolvedSerializer.Should().BeSameAs(customSerializer);
    }

    [Fact]
    public void AddAtcRestClientCore_DoesNotOverwriteExistingRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingSerializer = Substitute.For<IContractSerializer>();
        services.AddSingleton(existingSerializer);

        // Act
        services.AddAtcRestClientCore();
        var provider = services.BuildServiceProvider();
        var resolvedSerializer = provider.GetService<IContractSerializer>();

        // Assert
        resolvedSerializer.Should().BeSameAs(existingSerializer);
    }

    [Fact]
    public void AddAtcRestClientCore_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddAtcRestClientCore();

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddAtcRestClient_WithOptions_RegistersHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new TestOptions
        {
            BaseAddress = new Uri("https://api.example.com"),
            Timeout = TimeSpan.FromSeconds(60),
        };

        // Act
        services.AddAtcRestClient("TestClient", options);
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("TestClient");

        // Assert
        client.BaseAddress.Should().Be(options.BaseAddress);
        client.Timeout.Should().Be(options.Timeout);
    }

    [Fact]
    public void AddAtcRestClient_WithOptions_RegistersCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new TestOptions
        {
            BaseAddress = new Uri("https://api.example.com"),
        };

        // Act
        services.AddAtcRestClient("TestClient", options);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IHttpMessageFactory>().Should().NotBeNull();
        provider.GetService<IContractSerializer>().Should().NotBeNull();
    }

    [Fact]
    public void AddAtcRestClient_WithOptions_UsesCustomSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new TestOptions
        {
            BaseAddress = new Uri("https://api.example.com"),
        };
        var customSerializer = Substitute.For<IContractSerializer>();

        // Act
        services.AddAtcRestClient("TestClient", options, contractSerializer: customSerializer);
        var provider = services.BuildServiceProvider();
        var resolvedSerializer = provider.GetService<IContractSerializer>();

        // Assert
        resolvedSerializer.Should().BeSameAs(customSerializer);
    }

    [Fact]
    public void AddAtcRestClient_WithOptions_InvokesHttpClientBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new TestOptions
        {
            BaseAddress = new Uri("https://api.example.com"),
        };
        var builderInvoked = false;

        // Act
        services.AddAtcRestClient("TestClient", options, _ =>
        {
            builderInvoked = true;
        });
        _ = services.BuildServiceProvider();

        // Assert
        builderInvoked.Should().BeTrue();
    }

    [Fact]
    public void AddAtcRestClient_WithUriAndTimeout_RegistersHttpClient()
    {
        // Arrange
        var services = new ServiceCollection();
        var baseAddress = new Uri("https://api.test.com");
        var timeout = TimeSpan.FromMinutes(2);

        // Act
        services.AddAtcRestClient("TestClient", baseAddress, timeout);
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("TestClient");

        // Assert
        client.BaseAddress.Should().Be(baseAddress);
        client.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void AddAtcRestClient_WithUriAndTimeout_RegistersCoreServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var baseAddress = new Uri("https://api.test.com");
        var timeout = TimeSpan.FromSeconds(45);

        // Act
        services.AddAtcRestClient("TestClient", baseAddress, timeout);
        var provider = services.BuildServiceProvider();

        // Assert
        provider.GetService<IHttpMessageFactory>().Should().NotBeNull();
        provider.GetService<IContractSerializer>().Should().NotBeNull();
    }

    [Fact]
    public void AddAtcRestClient_WithUriAndTimeout_UsesCustomSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        var customSerializer = Substitute.For<IContractSerializer>();

        // Act
        services.AddAtcRestClient(
            "TestClient",
            new Uri("https://api.test.com"),
            TimeSpan.FromSeconds(30),
            contractSerializer: customSerializer);
        var provider = services.BuildServiceProvider();
        var resolvedSerializer = provider.GetService<IContractSerializer>();

        // Assert
        resolvedSerializer.Should().BeSameAs(customSerializer);
    }

    [Fact]
    public void AddAtcRestClient_WithUriAndTimeout_InvokesHttpClientBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        var builderInvoked = false;

        // Act
        services.AddAtcRestClient(
            "TestClient",
            new Uri("https://api.test.com"),
            TimeSpan.FromSeconds(30),
            _ =>
            {
                builderInvoked = true;
            });
        _ = services.BuildServiceProvider();

        // Assert
        builderInvoked.Should().BeTrue();
    }

    [Fact]
    public void AddAtcRestClient_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddAtcRestClient(
            "TestClient",
            new Uri("https://api.test.com"),
            TimeSpan.FromSeconds(30));

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddAtcRestClient_WithOptions_ReturnsServiceCollectionForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var options = new TestOptions
        {
            BaseAddress = new Uri("https://api.test.com"),
        };

        // Act
        var result = services.AddAtcRestClient("TestClient", options);

        // Assert
        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddAtcRestClientCore_WhenCalledMultipleTimes_UsesFirstRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var firstSerializer = Substitute.For<IContractSerializer>();
        var secondSerializer = Substitute.For<IContractSerializer>();

        // Act - register with first serializer, then call again with second
        services.AddAtcRestClientCore(firstSerializer);
        services.AddAtcRestClientCore(secondSerializer);
        var provider = services.BuildServiceProvider();
        var resolvedSerializer = provider.GetService<IContractSerializer>();

        // Assert - first registration wins due to TryAddSingleton
        resolvedSerializer.Should().BeSameAs(firstSerializer);
    }

    [Fact]
    public void AddAtcRestClient_WithSmallTimeout_ConfiguresSmallTimeout()
    {
        // Arrange
        var services = new ServiceCollection();
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act
        services.AddAtcRestClient("TestClient", new Uri("https://api.test.com"), timeout);
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("TestClient");

        // Assert
        client.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void AddAtcRestClient_WithLargeTimeout_ConfiguresLargeTimeout()
    {
        // Arrange
        var services = new ServiceCollection();
        var timeout = TimeSpan.FromHours(24);

        // Act
        services.AddAtcRestClient("TestClient", new Uri("https://api.test.com"), timeout);
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("TestClient");

        // Assert
        client.Timeout.Should().Be(timeout);
    }

    [Fact]
    public void AddAtcRestClient_WithDifferentClientNames_RegistersMultipleClients()
    {
        // Arrange
        var services = new ServiceCollection();
        var baseAddress1 = new Uri("https://api1.test.com");
        var baseAddress2 = new Uri("https://api2.test.com");

        // Act
        services.AddAtcRestClient("Client1", baseAddress1, TimeSpan.FromSeconds(30));
        services.AddAtcRestClient("Client2", baseAddress2, TimeSpan.FromSeconds(60));
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client1 = factory.CreateClient("Client1");
        var client2 = factory.CreateClient("Client2");

        // Assert
        client1.BaseAddress.Should().Be(baseAddress1);
        client2.BaseAddress.Should().Be(baseAddress2);
        client1.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        client2.Timeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void AddAtcRestClient_WithHttpsAndHttpBaseAddresses_BothWork()
    {
        // Arrange
        var services = new ServiceCollection();
        var httpsAddress = new Uri("https://secure.test.com");
        var httpAddress = new Uri("http://insecure.test.com");

        // Act
        services.AddAtcRestClient("SecureClient", httpsAddress, TimeSpan.FromSeconds(30));
        services.AddAtcRestClient("InsecureClient", httpAddress, TimeSpan.FromSeconds(30));
        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();

        // Assert
        factory.CreateClient("SecureClient").BaseAddress.Should().Be(httpsAddress);
        factory.CreateClient("InsecureClient").BaseAddress.Should().Be(httpAddress);
    }

    [SuppressMessage("Major Code Smell", "S2094:Classes should not be empty", Justification = "Test helper type")]
    private sealed class TestOptions : AtcRestClientOptions
    {
    }
}