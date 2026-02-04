namespace Atc.Rest.Client.Tests.Builder;

public sealed class MessageRequestBuilderTests
{
    private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

    private MessageRequestBuilder CreateSut(
        string? pathTemplate = null)
        => new(pathTemplate ?? "api/", serializer);

    [Fact]
    public void Null_Path_Throws()
    {
        Action ctor = () => new MessageRequestBuilder(null!, this.serializer);

        ctor.Should()
            .Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("pathTemplate");
    }

    [Fact]
    public void Null_ContractSerializer_Throws()
    {
        Action ctor = () => new MessageRequestBuilder(string.Empty, null!);

        ctor.Should()
            .Throw<ArgumentNullException>()
            .Which.ParamName.Should().Be("serializer");
    }

    [Theory, AutoNSubstituteData]
    public void Should_Use_HttpMethod(HttpMethod method)
        => CreateSut().Build(method)
            .Method
            .Should()
            .Be(method);

    [Fact]
    public void Should_Add_ApplicationJson_To_Accept_Header_By_Default()
    {
        var message = CreateSut().Build(HttpMethod.Post);

        message
            .Headers
            .Accept
            .Should()
            .BeEquivalentTo(new[] { MediaTypeWithQualityHeaderValue.Parse("application/json") });
    }

    [Theory, AutoNSubstituteData]
    public void Should_Set_Content_MediaType_To_ApplicationJson_When_Body_Is_Present(
        string body)
    {
        var sut = CreateSut();

        sut.WithBody(body);

        var message = sut.Build(HttpMethod.Post);

        message!
            .Content!
            .Headers
            .ContentType
            .Should()
            .Be(MediaTypeHeaderValue.Parse("application/json"));
    }

    [Theory]
    [InlineData("", "foo")]
    [InlineData(" ", "foo")]
    [InlineData("foo", null)]
    [InlineData("foo", "")]
    [InlineData("foo", " ")]
    public void WithPathParameter_Throws_If_Parameters_Are_Null_Or_WhiteSpace(
        string name,
        string? value)
    {
        var sut = CreateSut();

        sut.Invoking(x => x.WithPathParameter(name, value))
            .Should()
            .Throw<ArgumentException>();
    }

    [Theory]
    [InlineAutoNSubstituteData("/api/{foo}/bar/{baz}/biz")]
    public void Should_Replace_Path_Parameters(
        string template,
        string fooValue,
        int bazValue)
    {
        var sut = CreateSut(template);

        sut.WithPathParameter("foo", fooValue);
        sut.WithPathParameter("baz", bazValue);
        var message = sut.Build(HttpMethod.Post);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be($"/api/{fooValue}/bar/{bazValue}/biz");
    }

    [Fact]
    public void WithHeaderParameter_Throws_If_Parameters_Are_Null_Or_WhiteSpace()
    {
        var sut = CreateSut();

        sut.Invoking(x => x.WithHeaderParameter(null, "foo"))
            .Should()
            .Throw<ArgumentException>();
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters(
        string template,
        string fooValue,
        int barValue)
    {
        var sut = CreateSut(template);

        sut.WithQueryParameter("foo", fooValue);
        sut.WithQueryParameter("bar", barValue);
        var message = sut.Build(HttpMethod.Post);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be($"/api?foo={fooValue}&bar={barValue}");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters_With_Enum_Member_Value(string template)
    {
        const OperatorRole operatorRole = OperatorRole.Owner;
        var sut = CreateSut(template);

        sut.WithQueryParameter("operatorRole", operatorRole);
        var message = sut.Build(HttpMethod.Post);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api?operatorRole=owner");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters_With_Enum_Without_EnumMember_Attribute(string template)
    {
        // OperatorRole.None does not have an EnumMember attribute, so it should fall back to ToString()
        const OperatorRole operatorRole = OperatorRole.None;
        var sut = CreateSut(template);

        sut.WithQueryParameter("operatorRole", operatorRole);
        var message = sut.Build(HttpMethod.Post);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api?operatorRole=None");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Multiple_Enum_Parameters(string template)
    {
        var sut = CreateSut(template);

        sut.WithQueryParameter("role1", OperatorRole.Owner);
        sut.WithQueryParameter("role2", OperatorRole.Admin);
        sut.WithQueryParameter("role3", OperatorRole.None);
        var message = sut.Build(HttpMethod.Post);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api?role1=owner&role2=admin&role3=None");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters_With_DateTime(string template)
    {
        var from = DateTime.UtcNow;

        var sut = CreateSut(template);

        sut.WithQueryParameter("from", from);
        var message = sut.Build(HttpMethod.Post);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be($"/api?from={Uri.EscapeDataString(from.ToString("o"))}");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters_With_DateTimeOffset(string template)
    {
        var from = DateTimeOffset.UtcNow;

        var sut = CreateSut(template);

        sut.WithQueryParameter("from", from);
        var message = sut.Build(HttpMethod.Post);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be($"/api?from={Uri.EscapeDataString(from.ToString("o"))}");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters_WithNull(
        string template,
        string? fooValue,
        int? barValue)
    {
        var sut = CreateSut(template);

        sut.WithQueryParameter("foo", fooValue);
        sut.WithQueryParameter("bar", barValue);
        var message = sut.Build(HttpMethod.Post);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be($"/api?foo={fooValue}&bar={barValue}");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters_With_ArrayOfItems1(
        string template)
    {
        var sut = CreateSut(template);

        var values = new[]
        {
            1,
        };

        sut.WithQueryParameter("foo", values);
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be($"/api?foo={values[0]}");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters_With_ArrayOfItems3(
        string template)
    {
        var sut = CreateSut(template);

        var values = new[]
        {
            1,
            2,
            3,
        };

        sut.WithQueryParameter("foo", values);
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be($"/api?foo={values[0]}&foo={values[1]}&foo={values[2]}");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters_With_ListOfItems3(
        string template)
    {
        var sut = CreateSut(template);

        var values = new List<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
        };

        sut.WithQueryParameter("foo", values);
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be($"/api?foo={values[0]}&foo={values[1]}&foo={values[2]}");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Omit_Query_Parameters_With_Empty_Array(
        string template)
    {
        var sut = CreateSut(template);

        sut.WithQueryParameter("foo", Array.Empty<string>());
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Omit_Query_Parameters_With_Empty_List(
        string template)
    {
        var sut = CreateSut(template);

        sut.WithQueryParameter("foo", new List<int>());
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Omit_Query_Parameters_With_Null_Collection(
        string template)
    {
        var sut = CreateSut(template);

        sut.WithQueryParameter("foo", (System.Collections.IEnumerable?)null);
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Include_Other_Parameters_When_Empty_Collection_Is_Omitted(
        string template)
    {
        var sut = CreateSut(template);

        sut.WithQueryParameter("foo", Array.Empty<string>());
        sut.WithQueryParameter("bar", "value");
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api?bar=value");
    }

    [Fact]
    public void WithBody_Throws_If_Parameter_Is_Null()
    {
        var sut = CreateSut();

        sut.Invoking(x => x.WithBody<string>(null!))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Theory, AutoNSubstituteData]
    public async Task Should_Include_Body(string content)
    {
        serializer.Serialize(default!).ReturnsForAnyArgs(x => x[0]);
        var sut = CreateSut();

        sut.WithBody(content);

        var message = sut.Build(HttpMethod.Post);
        var result = await message!.Content!.ReadAsStringAsync();

        result
            .Should()
            .Be(content);
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters_With_Array_Containing_Special_Characters(string template)
    {
        var sut = CreateSut(template);

        var values = new[] { "hello world", "foo&bar", "test=value" };

        sut.WithQueryParameter("items", values);
        var message = sut.Build(HttpMethod.Get);

        var uri = message!.RequestUri!.ToString();
        uri.Should().Contain("items=hello%20world");
        uri.Should().Contain("items=foo%26bar");
        uri.Should().Contain("items=test%3Dvalue");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Replace_Query_Parameters_With_Single_Item_Array(string template)
    {
        var sut = CreateSut(template);

        var values = new[] { "single" };

        sut.WithQueryParameter("item", values);
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api?item=single");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Handle_Query_Parameters_With_Nullable_Enum(string template)
    {
        OperatorRole? nullableRole = OperatorRole.Admin;
        var sut = CreateSut(template);

        sut.WithQueryParameter("role", nullableRole);
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api?role=admin");
    }

    [Theory]
    [InlineAutoNSubstituteData("/api")]
    public void Should_Omit_Query_Parameters_With_Null_Nullable_Enum(string template)
    {
        OperatorRole? nullableRole = null;
        var sut = CreateSut(template);

        sut.WithQueryParameter("role", nullableRole);
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api");
    }

    [Fact]
    public void Should_Use_Cached_EnumMember_Value_On_Subsequent_Calls()
    {
        var sut1 = CreateSut("/api");
        var sut2 = CreateSut("/api");

        // First call populates cache
        sut1.WithQueryParameter("role", OperatorRole.Owner);
        var message1 = sut1.Build(HttpMethod.Get);

        // Second call should use cached value
        sut2.WithQueryParameter("role", OperatorRole.Owner);
        var message2 = sut2.Build(HttpMethod.Get);

        message1!.RequestUri!.ToString().Should().Be("/api?role=owner");
        message2!.RequestUri!.ToString().Should().Be("/api?role=owner");
    }

    [Fact]
    public void WithPathParameter_WithZeroValue_IncludesInUri()
    {
        var sut = CreateSut("/api/items/{id}");

        sut.WithPathParameter("id", 0);
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api/items/0");
    }

    [Fact]
    public void WithPathParameter_WithNegativeValue_IncludesInUri()
    {
        var sut = CreateSut("/api/items/{id}");

        sut.WithPathParameter("id", -1);
        var message = sut.Build(HttpMethod.Get);

        message!
            .RequestUri!
            .ToString()
            .Should()
            .Be("/api/items/-1");
    }
}