using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using Atc.Test;
using FluentAssertions;
using Xunit;

namespace Atc.Rest.Client.Tests
{
    public class EndpointResponseTests
    {
        [Theory, AutoNSubstituteData]
        public void SuccessContent_Returns_Respose_Upon_Success(
            TestResponse contentObject,
            IReadOnlyDictionary<string, IEnumerable<string>> headers)
        {
            var sut = new EndpointResponse<TestResponse>(
                true,
                HttpStatusCode.OK,
                JsonSerializer.Serialize(contentObject),
                contentObject,
                headers);

            sut
                .SuccessContent
                .Should()
                .Be(contentObject);
        }

        [Fact]
        public void SuccessContent_Returns_Null_Upon_Failure()
        {
            var sut = new EndpointResponse<TestResponse>(
                false,
                HttpStatusCode.BadRequest,
                string.Empty,
                null,
                new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal));

            sut
                .SuccessContent
                .Should()
                .BeNull();
        }

        [Theory, AutoNSubstituteData]
        public void FailureContent_Returns_Null_Upon_Success(
            TestResponse contentObject,
            IReadOnlyDictionary<string, IEnumerable<string>> headers)
        {
            var sut = new EndpointResponse<TestResponse, BadResponse>(
                true,
                HttpStatusCode.OK,
                JsonSerializer.Serialize(contentObject),
                contentObject,
                headers);

            sut
                .FailureContent
                .Should()
                .BeNull();
        }

        [Theory, AutoNSubstituteData]
        public void FailureContent_Returns_Response_Upon_Failure(
            BadResponse contentObject,
            IReadOnlyDictionary<string, IEnumerable<string>> headers)
        {
            var sut = new EndpointResponse<TestResponse, BadResponse>(
                false,
                HttpStatusCode.BadRequest,
                JsonSerializer.Serialize(contentObject),
                contentObject,
                headers);

            sut
                .FailureContent
                .Should()
                .Be(contentObject);
        }

        public class TestResponse
        {
            public string? Name { get; set; }
        }

        public class BadResponse
        {
            public string? Error { get; set; }
        }
    }
}