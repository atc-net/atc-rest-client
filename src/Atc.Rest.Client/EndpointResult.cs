namespace Atc.Rest.Client
{
    public class EndpointResult<TSuccessContent, TErrorContent> : EndpointResponse
    {
        public EndpointResult(EndpointResponse response)
            : base(response)
        {
        }

        public TSuccessContent? SuccessContent
        {
            get
            {
                if (IsSuccess && ContentObject is TSuccessContent content)
                {
                    return content;
                }

                return default;
            }
        }

        public TErrorContent? ErrorContent
        {
            get
            {
                if (!IsSuccess && ContentObject is TErrorContent errorContent)
                {
                    return errorContent;
                }

                return default;
            }
        }
    }
}