using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Atc.Rest.Client
{
    public class EndpointResult<TSuccessContent, TErrorContent> : EndpointResponse
    {
        public EndpointResult(EndpointResponse response)
            : base(response)
        {
        }

        [return: MaybeNull]
        public TResponseContent SuccessContent
        {
            get
            {
                if (IsSuccess && ContentObject is TResponseContent content)
                {
                    return content;
                }
                
                return null;
            }
        }

        [return: MaybeNull]
        public TErrorContent ErrorContent
        {
            get
            {
                if (!IsSuccess && ContentObject is TErrorContent errorContent)
                {
                    return errorContent;
                }
                
                return null;
            }
        }
    }
}
