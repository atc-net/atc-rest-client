namespace Atc.Rest.Client.Tests.Options;

public sealed class MaxStreamRecordLengthTests
{
    private const string LineOfTwentyTwoBytes = "\"aaaaaaaaaaaaaaaaaaaa\"\n";

    [Fact]
    public void MaxStreamRecordLength_DefaultsToSixteenMebibytes()
    {
        // Act
        var options = new AtcRestClientOptions();

        // Assert
        options.MaxStreamRecordLength.Should().Be(16 * 1024 * 1024);
        AtcRestClientOptions.DefaultMaxStreamRecordLength.Should().Be(16 * 1024 * 1024);
    }

    [Fact]
    public async Task AddAtcRestClientCore_WithoutConfiguration_UsesTheDefaultLimit()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAtcRestClientCore();

        // Act
        var items = await ReadAsync(services);

        // Assert
        items.Should().ContainSingle();
    }

    [Fact]
    public async Task AddAtcRestClientCore_WithACustomSerializer_UsesTheDefaultLimit()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAtcRestClientCore(new DefaultJsonContractSerializer());

        // Act
        var items = await ReadAsync(services);

        // Assert
        items.Should().ContainSingle();
    }

    [Fact]
    public async Task Configure_SetsTheLimitOfTheStreamReaders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAtcRestClientCore();
        services.Configure<AtcRestClientOptions>(o => o.MaxStreamRecordLength = 16);

        // Act
        var act = () => ReadAsync(services);

        // Assert
        var exception = await act.Should().ThrowAsync<InvalidDataException>();
        exception.WithMessage("*16 bytes*");
    }

    [Fact]
    public Task AddAtcRestClient_WithOptions_SetsTheLimitOfTheStreamReaders()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAtcRestClient(
            "limited",
            new AtcRestClientOptions { BaseAddress = new Uri("https://limited.example"), MaxStreamRecordLength = 16 });

        // Act
        var act = () => ReadAsync(services);

        // Assert
        return act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public Task AddAtcRestClient_AnotherClientWithTheDefault_KeepsTheLimitThatWasSet()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAtcRestClient(
            "limited",
            new AtcRestClientOptions { BaseAddress = new Uri("https://limited.example"), MaxStreamRecordLength = 16 });
        services.AddAtcRestClient(
            "other",
            new AtcRestClientOptions { BaseAddress = new Uri("https://other.example") });

        // Act
        var act = () => ReadAsync(services);

        // Assert
        return act.Should().ThrowAsync<InvalidDataException>();
    }

    [Fact]
    public async Task AddAtcRestClient_SeveralClientsWithALimit_TheLastOneRegisteredApplies()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAtcRestClient(
            "limited",
            new AtcRestClientOptions { BaseAddress = new Uri("https://limited.example"), MaxStreamRecordLength = 16 });
        services.AddAtcRestClient(
            "other",
            new AtcRestClientOptions { BaseAddress = new Uri("https://other.example"), MaxStreamRecordLength = 1024 });

        // Act
        var items = await ReadAsync(services);

        // Assert
        items.Should().ContainSingle();
    }

    [Fact]
    public async Task Configure_TheLargestLimit_SwitchesTheLimitOff()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAtcRestClientCore();
        services.Configure<AtcRestClientOptions>(o => o.MaxStreamRecordLength = int.MaxValue);

        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpMessageFactory>();
        using var response = StreamWire.Response(StreamFraming.ServerSentEvents, StreamWire.Document(StreamFraming.ServerSentEvents, "1", "2"));

        // Act
        using var result = await factory
            .FromResponse(response)
            .BuildStreamingEndpointResponseAsync<int>(StreamFraming.ServerSentEvents);
        var items = await result.Content!.ToListAsync();

        // Assert
        items.Should().Equal(1, 2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Configure_ALimitThatIsNotPositive_Throws(
        int maxStreamRecordLength)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAtcRestClientCore();
        services.Configure<AtcRestClientOptions>(o => o.MaxStreamRecordLength = maxStreamRecordLength);

        // Act
        var act = () => ReadAsync(services);

        // Assert
        var exception = await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        exception.WithMessage("*MaxStreamRecordLength*");
    }

    private static async Task<List<string?>> ReadAsync(
        ServiceCollection services)
    {
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpMessageFactory>();
        using var response = StreamWire.Response(StreamFraming.JsonLines, LineOfTwentyTwoBytes);

        using var result = await factory
            .FromResponse(response)
            .BuildStreamingEndpointResponseAsync<string>(StreamFraming.JsonLines);
        return await result.Content!.ToListAsync();
    }
}