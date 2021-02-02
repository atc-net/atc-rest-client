using System;

namespace Atc.Rest.Client.Options
{
    public class AtcRestClientOptions
    {
        public virtual Uri? BaseAddress { get; set; }

        public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
