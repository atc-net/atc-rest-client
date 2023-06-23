using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Numerics;
using System.Threading.Tasks;
using System.Web;
using Atc.Rest.Client.Builder;
using Atc.Rest.Client.Serialization;
using Atc.Test;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Atc.Rest.Client.Tests.Builder
{
    public class MessageRequestBuilderTests
    {
        private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

        private MessageRequestBuilder CreateSut(string? pathTemplate = null)
            => new MessageRequestBuilder(pathTemplate ?? "api/", serializer);

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
                .BeEquivalentTo(MediaTypeWithQualityHeaderValue.Parse("application/json"));
        }

        [Theory, AutoNSubstituteData]
        public void Should_Set_Content_MediaType_To_ApplicationJson_When_Body_Is_Present(string body)
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
        [InlineData(null, "foo")]
        [InlineData("", "foo")]
        [InlineData(" ", "foo")]
        [InlineData("foo", null)]
        [InlineData("foo", "")]
        [InlineData("foo", " ")]
        public void WithPathParameter_Throws_If_Parameters_Are_Null_Or_WhiteSpace(
            string name,
            string value)
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
        public void Should_Omit_Query_Parameters_With_DateTime_List(
            string template,
            IEnumerable<Vector2> values)
        {
            var sut = CreateSut(template);

            sut.WithQueryParameter("foo", values);
            var message = sut.Build(HttpMethod.Get);

            message!
                .RequestUri!
                .ToString()
                .Should()
                .BeEquivalentTo($"/api?foo={HttpUtility.UrlEncode(values.ToString())}");
        }

        [Theory]
        [InlineAutoNSubstituteData("/api")]
        public void Should_Omit_Query_Parameters_With_Empty_List(
            string template)
        {
            var sut = CreateSut(template);

            var values = new List<string>();

            sut.WithQueryParameter("foo", values);
            var message = sut.Build(HttpMethod.Get);

            message!
                .RequestUri!
                .ToString()
                .Should()
                .Be($"/api");
        }

        [Theory]
        [InlineAutoNSubstituteData("/api")]
        public void Should_Omit_Query_Parameters_With_Empty_Array(
            string template)
        {
            var sut = CreateSut(template);

            var values = Array.Empty<string>();

            sut.WithQueryParameter("foo", values);
            var message = sut.Build(HttpMethod.Get);

            message!
                .RequestUri!
                .ToString()
                .Should()
                .Be($"/api");
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
    }
}