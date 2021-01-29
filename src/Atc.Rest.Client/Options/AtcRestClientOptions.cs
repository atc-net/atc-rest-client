using System;

namespace Atc.Rest.Client.Options
{
    public abstract class AtcRestClientOptions
    {        
        public virtual Uri? BaseAddress { get; set; }

        public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
