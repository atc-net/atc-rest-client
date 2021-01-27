using System;

namespace Atc.Rest.Client.Options
{
    public abstract class AtcRestClientOptions
    {
        public virtual string? EnvironmentName { get; set; }

        public virtual EnvironmentType EnvironmentType { get; set; }

        public virtual Uri? BaseAddress { get; set; }

        public virtual TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }
}
