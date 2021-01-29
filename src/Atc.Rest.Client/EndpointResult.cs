using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Atc.Rest.Client
{
    public class EndpointResult<TResponseContent> : EndpointResponse
    {
        public EndpointResult(EndpointResponse response)
            : base(response)
        {
        }

        [SuppressMessage("Major Code Smell", "S2372:Exceptions should not be thrown from property getters", Justification = "OK.")]
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "OK.")]
        public TResponseContent SuccessContent
        {
            get
            {
                if (IsSuccess)
                {
                    return ContentObject switch
                    {
                        TResponseContent content => content,
                        null => throw new InvalidDataException("Underlying ContentObject is null"),
                        _ => throw new InvalidDataException($"Type is {ContentObject.GetType()}, but expected {typeof(TResponseContent)}")
                    };
                }

                throw new InvalidDataException("The state is not success");
            }
        }

        public ProblemDetails? FailedContent
        {
            get
            {
                if (IsSuccess)
                {
                    return null;
                }

                if (ContentObject is ProblemDetails problemDetails)
                {
                    return problemDetails;
                }

                return new ProblemDetails
                {
                    Status = (int)StatusCode,
                    Detail = Content,
                };
            }
        }
    }
}