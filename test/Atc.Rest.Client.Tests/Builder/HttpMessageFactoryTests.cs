using System.Net.Http;
using Atc.Rest.Client.Builder;
using Atc.Rest.Client.Serialization;
using Atc.Test;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Atc.Rest.Client.Tests.Builder
{
    public class HttpMessageFactoryTests
    {
        private readonly IContractSerializer serializer = Substitute.For<IContractSerializer>();

        private HttpMessageFactory CreateSut()
            => new HttpMessageFactory(serializer);

        [Theory, AutoNSubstituteData]
        public void Should_Provide_MessageResponseBuilder_FromResponse(
            HttpResponseMessage response)
            => CreateSut().FromResponse(response)
                .Should()
                .NotBeNull();

        [Fact]
        public void Should_Provide_MessageResponseBuilder_From_Null_Response()
            => CreateSut().FromResponse(null)
                .Should()
                .NotBeNull();

        [Theory, AutoNSubstituteData]
        public void Should_Provide_MessageRequestBuilder_FromTemplate(string template)
            => CreateSut().FromTemplate(template)
                .Should()
                .NotBeNull();
    }
}