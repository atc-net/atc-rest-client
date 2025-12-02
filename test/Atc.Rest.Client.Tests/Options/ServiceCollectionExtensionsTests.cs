namespace Atc.Rest.Client.Tests.Options;

using Atc.Rest.Client.Options;
using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAtcRestClient_RegistersHttpMessageFactory()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAtcRestClient();
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IHttpMessageFactory>();

        // Assert
        factory.Should().NotBeNull();
    }

    [Fact]
    public void AddAtcRestClient_RegistersDefaultContractSerializer()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAtcRestClient();
        var provider = services.BuildServiceProvider();
        var serializer = provider.GetService<IContractSerializer>();

        // Assert
        serializer.Should().NotBeNull();
        serializer.Should().BeOfType<DefaultJsonContractSerializer>();
    }

    [Fact]
    public void AddAtcRestClient_WithCustomSerializer_UsesProvidedSerializer()
    {
        // Arrange
        var services = new ServiceCollection();
        var customSerializer = Substitute.For<IContractSerializer>();

        // Act
        services.AddAtcRestClient(customSerializer);
        var provider = services.BuildServiceProvider();
        var resolvedSerializer = provider.GetService<IContractSerializer>();

        // Assert
        resolvedSerializer.Should().BeSameAs(customSerializer);
    }

    [Fact]
    public void AddAtcRestClient_DoesNotOverwriteExistingRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var existingSerializer = Substitute.For<IContractSerializer>();
        services.AddSingleton(existingSerializer);

        // Act
        services.AddAtcRestClient();
        var provider = services.BuildServiceProvider();
        var resolvedSerializer = provider.GetService<IContractSerializer>();

        // Assert
        resolvedSerializer.Should().BeSameAs(existingSerializer);
    }

    [Fact]
    public void AddAtcRestClient_WithNullSerializer_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddAtcRestClient((IContractSerializer)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("contractSerializer");
    }

    [Fact]
    public void AddAtcRestClient_WithConfigure_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var configureWasCalled = false;

        // Act
        services.AddAtcRestClient(options =>
        {
            configureWasCalled = true;
            options.Timeout = TimeSpan.FromSeconds(60);
        });
        var provider = services.BuildServiceProvider();
        var factory = provider.GetService<IHttpMessageFactory>();
        var serializer = provider.GetService<IContractSerializer>();

        // Assert
        configureWasCalled.Should().BeTrue();
        factory.Should().NotBeNull();
        serializer.Should().NotBeNull();
    }

    [Fact]
    public void AddAtcRestClient_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddAtcRestClient((Action<AtcRestClientOptions>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configure");
    }
}