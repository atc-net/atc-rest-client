namespace Atc.Rest.Client.Tests.TestTypes;

internal sealed class DisposeCountingHttpResponseMessage : HttpResponseMessage
{
    public DisposeCountingHttpResponseMessage(HttpStatusCode statusCode)
        : base(statusCode)
    {
    }

    public int DisposeCount { get; private set; }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeCount++;
        }

        base.Dispose(disposing);
    }
}