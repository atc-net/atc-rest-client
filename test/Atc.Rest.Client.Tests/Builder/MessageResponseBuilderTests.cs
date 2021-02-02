using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Atc.Rest.Client.Builder;
using Atc.Rest.Client.Serialization;
using Atc.Test;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Atc.Rest.Client.Tests.Builder
{
    public class MessageResponseBuilderTests
    {
        private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

        private MessageResponseBuilder CreateSut(HttpResponseMessage? response)
            => new MessageResponseBuilder(response, serializer);

        [Theory, AutoNSubstituteData]
        public async Task IsSuccess_Should_Respect_Configured_ErrorResponse(
           HttpResponseMessage response,
           CancellationToken cancellationToken)
        {
            var sut = CreateSut(response);
            response.StatusCode = HttpStatusCode.NotFound;

            var result = await sut.AddErrorResponse(response.StatusCode)
                .BuildResponseAsync(res => res, cancellationToken);

            result
                .IsSuccess
                .Should()
                .BeFalse();
        }

        [Theory, AutoNSubstituteData]
        public async Task IsSuccess_Should_Respect_Configured_SuccessResponse(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var sut = CreateSut(response);
            response.StatusCode = HttpStatusCode.NotFound;

            var result = await sut.AddSuccessResponse(response.StatusCode)
                .BuildResponseAsync(res => res, cancellationToken);

            result
                .IsSuccess
                .Should()
                .BeTrue();
        }

        [Theory, AutoNSubstituteData]
        public async Task Should_Deserialize_Configured_SuccessResponseCode(
            HttpResponseMessage response,
            Guid expectedResponse,
            CancellationToken cancellationToken)
        {
            serializer.Deserialize<Guid>(Arg.Any<string>()).Returns(expectedResponse);
            var sut = CreateSut(response);
            response.StatusCode = HttpStatusCode.OK;

            var result = await sut.AddSuccessResponse<Guid>(response.StatusCode)
                .BuildResponseAsync(res => res, cancellationToken);

            result
                .ContentObject
                .Should()
                .BeEquivalentTo(expectedResponse);
        }

        [Theory, AutoNSubstituteData]
        public async Task Should_Deserialize_Configured_ErrorResponseCode(
            HttpResponseMessage response,
            Guid expectedResponse,
            CancellationToken cancellationToken)
        {
            serializer.Deserialize<Guid>(Arg.Any<string>()).Returns(expectedResponse);
            var sut = CreateSut(response);
            response.StatusCode = HttpStatusCode.BadRequest;

            var result = await sut.AddErrorResponse<Guid>(response.StatusCode)
                .BuildResponseAsync(res => res, cancellationToken);

            result
                .ContentObject
                .Should()
                .BeEquivalentTo(expectedResponse);
        }

        [Theory, AutoNSubstituteData]
        public async Task Should_Return_Response_Headers(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            var sut = CreateSut(response);
            var expected = new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
            {
                { "responseHeader", new[] { "value" } },
                { "contentHeader", new[] { "value" } },
            };
            response.Headers.Add("responseHeader", "value");
            response.Content.Headers.Add("contentHeader", "value");
            response.StatusCode = HttpStatusCode.OK;

            var result = await sut.AddSuccessResponse(response.StatusCode)
                .BuildResponseAsync(res => res, cancellationToken);

            result
                .Headers
                .Should()
                .BeEquivalentTo(expected);
        }
    }
}