namespace Atc.Rest.Client.Tests.Options;

public sealed class AtcRestClientOptionsTests
{
    [Fact]
    public void DefaultTimeout_IsThirtySeconds()
    {
        // Arrange & Act
        var sut = new AtcRestClientOptions();

        // Assert
        sut.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void BaseAddress_DefaultsToNull()
    {
        // Arrange & Act
        var sut = new AtcRestClientOptions();

        // Assert
        sut.BaseAddress.Should().BeNull();
    }

    [Fact]
    public void BaseAddress_CanBeSet()
    {
        // Arrange
        var sut = new AtcRestClientOptions();
        var expected = new Uri("https://api.example.com");

        // Act
        sut.BaseAddress = expected;

        // Assert
        sut.BaseAddress.Should().Be(expected);
    }

    [Fact]
    public void Timeout_CanBeSet()
    {
        // Arrange
        var sut = new AtcRestClientOptions();
        var expected = TimeSpan.FromMinutes(5);

        // Act
        sut.Timeout = expected;

        // Assert
        sut.Timeout.Should().Be(expected);
    }

    [Fact]
    public void DerivedClass_CanOverrideProperties()
    {
        // Arrange
        var sut = new DerivedOptions();

        // Assert
        sut.BaseAddress.Should().Be(new Uri("https://override.example.com"));
        sut.Timeout.Should().Be(TimeSpan.FromMinutes(2));
    }

    private sealed class DerivedOptions : AtcRestClientOptions
    {
        public override Uri? BaseAddress { get; set; } = new Uri("https://override.example.com");

        public override TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);
    }
}