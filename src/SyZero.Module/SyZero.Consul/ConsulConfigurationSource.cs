using Microsoft.Extensions.Configuration;
using NConsul;
using System;
using System.Net.Http;
using System.Threading;

namespace SyZero.Consul
{
    internal sealed class ConsulConfigurationSource : IConsulConfigurationSource
    {
        public ConsulConfigurationSource(string serviceKey, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(serviceKey))
            {
                throw new ArgumentNullException(nameof(serviceKey));
            }

            this.ServiceKey = serviceKey;
            this.CancellationToken = cancellationToken;
        }

        #region IConsulConfigurationSource Members

        public CancellationToken CancellationToken { get; }

        public Action<ConsulClientConfiguration> ConsulClientConfiguration { get; set; }

        public Action<HttpClient> ConsulHttpClient { get; set; }

        public Action<HttpClientHandler> ConsulHttpClientHandler { get; set; }

        public string ServiceKey { get; }

        public bool Optional { get; set; } = false;

        public QueryOptions QueryOptions { get; set; }

        public int ReloadDelay { get; set; } = 250;

        public bool ReloadOnChange { get; set; } = false;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            ConsulConfigurationParser consulParser = new ConsulConfigurationParser(this);

            return new ConsulConfigurationProvider(this, consulParser);
        }

        #endregion IConsulConfigurationSource Members
    }
}