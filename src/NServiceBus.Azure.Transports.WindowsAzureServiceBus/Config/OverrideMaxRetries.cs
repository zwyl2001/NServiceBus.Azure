﻿namespace NServiceBus.Features
{
    using Config;
    using Config.ConfigurationSource;

    public class OverrideMaxRetries : IProvideConfiguration<TransportConfig>
    {
        public TransportConfig GetConfiguration()
        {
            var source = Configure.ConfigurationSource;
            var c = source.GetConfiguration<AzureServiceBusQueueConfig>();
            var t = source.GetConfiguration<TransportConfig>();

            if (c == null)
            {
                c = new AzureServiceBusQueueConfig();
            }
            if (t == null)
            {
                t = new TransportConfig();
            }

            return new TransportConfig
                        {
                            MaximumConcurrencyLevel = t.MaximumConcurrencyLevel,
                            MaxRetries = t.MaxRetries >= c.MaxDeliveryCount ? c.MaxDeliveryCount - 1 : t.MaxRetries,
                            MaximumMessageThroughputPerSecond = t.MaximumMessageThroughputPerSecond
                        };
        }
    }
}