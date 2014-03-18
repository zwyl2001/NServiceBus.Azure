﻿namespace NServiceBus.AcceptanceTests.WindowsAzureServiceBus
{
    using System;
    using System.Transactions;
    using AcceptanceTesting;
    using Config;
    using Config.ConfigurationSource;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_sending_an_oversized_message_from_a_transaction_scope : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_log_message_too_large_exception()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<MyEndpoint>(b => b.When(bus =>
                {
                    using (var scope = new TransactionScope(TransactionScopeOption.RequiresNew))
                    {
                        bus.Send(Address.Local, new OversizedRequest());
                        scope.Complete();
                    }
                }))
                .Run();

            Assert.IsTrue(context.Exceptions.StartsWith("NServiceBus.Azure.Transports.WindowsAzureServiceBus.MessageTooLargeException"));
        }

        class Context : ScenarioContext{}

        class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class ConfigMaxDeliveryCount : IProvideConfiguration<AzureServiceBusQueueConfig>
            {
                public AzureServiceBusQueueConfig GetConfiguration()
                {
                    return new AzureServiceBusQueueConfig
                    {
                        MaxDeliveryCount = 1
                    };
                }
            }
        }

        [Serializable]
        public class OversizedRequest : IMessage
        {
            public OversizedRequest()
            {
                OversizedProperty = new string('*', 265000);
            }

            public string OversizedProperty { get; set; }
        }
    }
}