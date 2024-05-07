namespace Atc.Rest.Client.Builder;

public interface IMessageResponseBuilder
{
    IMessageResponseBuilder AddSuccessResponse(
        HttpStatusCode statusCode);

    IMessageResponseBuilder AddSuccessResponse<TResponseContent>(
        HttpStatusCode statusCode);

    IMessageResponseBuilder AddErrorResponse(
        HttpStatusCode statusCode);

    IMessageResponseBuilder AddErrorResponse<TResponseContent>(
        HttpStatusCode statusCode);

    Task<TResult> BuildResponseAsync<TResult>(
        Func<EndpointResponse, TResult> factory,
        CancellationToken cancellationToken);

    Task<EndpointResponse<TSuccessContent>>
        BuildResponseAsync<TSuccessContent>(
            CancellationToken cancellationToken)
        where TSuccessContent : class;

    Task<EndpointResponse<TSuccessContent, TErrorContent>>
        BuildResponseAsync<TSuccessContent, TErrorContent>(
            CancellationToken cancellationToken)
        where TSuccessContent : class
        where TErrorContent : class;
}