namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageRequestBuilderWireFormatTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageRequestBuilder CreateSut(string template = "/api")
        => new(template, serializer);

    [Fact]
    public void WithPathParameter_DateTimeOffset_IsIso8601_WhateverTheCulture()
        => InCulture("da-DK", () =>
        {
            var sut = CreateSut("/events/{day}");

            sut.WithPathParameter("day", new DateTimeOffset(1990, 2, 28, 12, 30, 0, TimeSpan.FromHours(2)));

            BuildUri(sut).Should().Be("/events/1990-02-28T12%3A30%3A00.0000000%2B02%3A00");
        });

    [Fact]
    public void WithPathParameter_DateOnly_IsYearMonthDay_WhateverTheCulture()
        => InCulture("da-DK", () =>
        {
            var sut = CreateSut("/events/{day}");

            sut.WithPathParameter("day", new DateOnly(1990, 2, 28));

            BuildUri(sut).Should().Be("/events/1990-02-28");
        });

    [Fact]
    public void WithPathParameter_Decimal_UsesInvariantCulture()
        => InCulture("da-DK", () =>
        {
            var sut = CreateSut("/amounts/{amount}");

            sut.WithPathParameter("amount", 1.5m);

            BuildUri(sut).Should().Be("/amounts/1.5");
        });

    [Fact]
    public void WithPathParameter_Enum_UsesEnumMemberValue()
    {
        var sut = CreateSut("/roles/{role}");

        sut.WithPathParameter("role", OperatorRole.Admin);

        BuildUri(sut).Should().Be("/roles/admin");
    }

    [Fact]
    public void WithHeaderParameter_DateOnly_IsYearMonthDay_WhateverTheCulture()
        => InCulture("da-DK", () =>
        {
            var sut = CreateSut();

            sut.WithHeaderParameter("valid-on", new DateOnly(1990, 3, 2));

            sut.Build(HttpMethod.Get).Headers.GetValues("valid-on").Should().Equal("1990-03-02");
        });

    [Fact]
    public void WithHeaderParameter_Enum_UsesEnumMemberValue()
    {
        var sut = CreateSut();

        sut.WithHeaderParameter("role", OperatorRole.Owner);

        sut.Build(HttpMethod.Get).Headers.GetValues("role").Should().Equal("owner");
    }

    [Fact]
    public void WithQueryParameter_Double_UsesInvariantCulture()
        => InCulture("da-DK", () =>
        {
            var sut = CreateSut();

            sut.WithQueryParameter("ratio", 1.5);

            BuildUri(sut).Should().Be("/api?ratio=1.5");
        });

    [Fact]
    public void WithQueryParameter_TimeOnly_IsHoursMinutesSeconds_WhateverTheCulture()
        => InCulture("da-DK", () =>
        {
            var sut = CreateSut();

            sut.WithQueryParameter("at", new TimeOnly(12, 30, 15));

            BuildUri(sut).Should().Be("/api?at=12%3A30%3A15");
        });

    [Fact]
    public void WithQueryParameter_NumberEnum_IsItsNumber()
    {
        var sut = CreateSut();

        sut.WithQueryParameter("priority", Priority.Medium);

        BuildUri(sut).Should().Be("/api?priority=2");
    }

    [Fact]
    public void WithQueryParameter_JsonStringEnumMemberName_IsUsed()
    {
        var sut = CreateSut();

        sut.WithQueryParameter("channel", ContactChannel.Email);
        sut.WithQueryParameter("fallback", ContactChannel.Sms);

        BuildUri(sut).Should().Be("/api?channel=e-mail&fallback=Sms");
    }

    [Fact]
    public void WithQueryParameter_Bool_IsLowercase()
    {
        var sut = CreateSut();

        sut.WithQueryParameter("active", true);

        BuildUri(sut).Should().Be("/api?active=true");
    }

    [Fact]
    public void WithQueryParameter_ArrayOfDateOnly_FormatsEachItem_WhateverTheCulture()
        => InCulture("da-DK", () =>
        {
            var sut = CreateSut();

            sut.WithQueryParameter("days", new[] { new DateOnly(1990, 2, 28), new DateOnly(1990, 3, 1) });

            BuildUri(sut).Should().Be("/api?days=1990-02-28&days=1990-03-01");
        });

    [Fact]
    public void WithQueryParameter_ArrayOfNumberEnum_WritesNumbers()
    {
        var sut = CreateSut();

        sut.WithQueryParameter("levels", new[] { Priority.Medium, Priority.High });

        BuildUri(sut).Should().Be("/api?levels=2&levels=3");
    }

    [Fact]
    public void WithQueryParameter_ArrayOfEnum_UsesEnumMemberValues()
    {
        var sut = CreateSut();

        sut.WithQueryParameter("roles", new[] { OperatorRole.Owner, OperatorRole.Admin });

        BuildUri(sut).Should().Be("/api?roles=owner&roles=admin");
    }

    [Fact]
    public void WithQueryParameter_ArrayOfDecimal_UsesInvariantCulture()
        => InCulture("da-DK", () =>
        {
            var sut = CreateSut();

            sut.WithQueryParameter("amounts", new[] { 1.5m, 2.25m });

            BuildUri(sut).Should().Be("/api?amounts=1.5&amounts=2.25");
        });

    [Fact]
    public void Bool_IsLowercase_InPathQueryListAndHeader()
    {
        var sut = CreateSut("/flags/{flag}");

        sut.WithPathParameter("flag", true);
        sut.WithQueryParameter("q", false);
        sut.WithQueryParameter("list", new[] { true, false });
        sut.WithHeaderParameter("x-flag", true);
        var message = sut.Build(HttpMethod.Get);

        message.RequestUri!.OriginalString.Should().Be("/flags/true?q=false&list=true&list=false");
        message.Headers.GetValues("x-flag").Should().Equal("true");
    }

    [Fact]
    public void NullableBool_IsLowercase_AndNullIsOmitted()
    {
        bool? set = true;
        bool? unset = null;
        var sut = CreateSut();

        sut.WithQueryParameter("set", set);
        sut.WithQueryParameter("unset", unset);

        BuildUri(sut).Should().Be("/api?set=true");
    }

    private static string BuildUri(MessageRequestBuilder sut)
        => sut.Build(HttpMethod.Get).RequestUri!.OriginalString;

    private static void InCulture(
        string culture,
        Action action)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}