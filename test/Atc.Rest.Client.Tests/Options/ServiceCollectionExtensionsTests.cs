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
}