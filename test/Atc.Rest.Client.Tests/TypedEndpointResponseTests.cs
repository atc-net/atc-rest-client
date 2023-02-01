using System.Collections.Generic;
using System.Net;
using Atc.Test;
using FluentAssertions;
using Xunit;

namespace Atc.Rest.Client.Tests
{
    public class TypedEndpointResponseTests
    {
        [Theory, AutoNSubstituteData]
        public void SuccessContent_Returns_Respose_Upon_Success(TestResponse data)
        {
            var sut = new TypedEndpointResponse<TestResponse>(
                true,
                HttpStatusCode.OK,
                string.Empty,
                data,
                new Dictionary<string, IEnumerable<string>>());

            sut
                .SuccessContent
                .Should()
                .Be(data);
        }

        [Fact]
        public void SuccessContent_Returns_Null_Upon_Failure()
        {
            var sut = new TypedEndpointResponse<TestResponse>(
                false,
                HttpStatusCode.BadRequest,
                string.Empty,
                null,
                new Dictionary<string, IEnumerable<string>>());

            sut
                .SuccessContent
                .Should()
                .BeNull();
        }

        [Theory, AutoNSubstituteData]
        public void FailureContent_Returns_Null_Upon_Success(TestResponse data)
        {
            var sut = new TypedEndpointResponse<TestResponse, BadResponse>(
                true,
                HttpStatusCode.OK,
                string.Empty,
                data,
                new Dictionary<string, IEnumerable<string>>());

            sut
                .FailureContent
                .Should()
                .BeNull();
        }

        [Theory, AutoNSubstituteData]
        public void FailureContent_Returns_Response_Upon_Failure(BadResponse data)
        {
            var sut = new TypedEndpointResponse<TestResponse, BadResponse>(
                false,
                HttpStatusCode.BadRequest,
                string.Empty,
                data,
                new Dictionary<string, IEnumerable<string>>());

            sut
                .FailureContent
                .Should()
                .Be(data);
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