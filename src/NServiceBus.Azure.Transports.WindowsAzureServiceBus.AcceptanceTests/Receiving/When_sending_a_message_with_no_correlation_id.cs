﻿namespace NServiceBus.AcceptanceTests.WindowsAzureServiceBus
{
    using System;
    using Config;
    using Config.ConfigurationSource;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;

    public class When_a_handler_fails_to_process_a_message : NServiceBusAcceptanceTest
    {  
        [Test]
        public void Should_abandon_the_message()
        {
            var context = new Context();

            Scenario.Define(context)
                    .WithEndpoint<MyEndpoint>(b => b.When(bus => bus.Send(Address.Local, new MyRequest())))
                    .Done(c => c.ReceivedAgain)
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool Thrown { get; set; }
            public bool ReceivedAgain { get; set; }
        }

        public class MyEndpoint : EndpointConfigurationBuilder
        {
            public MyEndpoint()
            {
                EndpointSetup<DefaultServer>()
                    .AllowExceptions()
                    ;
            }

            public class SettingLockDurationLargerThanTestTimeout : IProvideConfiguration<AzureServiceBusQueueConfig>
            {
                public AzureServiceBusQueueConfig GetConfiguration()
                {
                    return new AzureServiceBusQueueConfig
                    {
                        LockDuration = 300000 // 5 mins, think test timeout is 2 minutes
                    };
                }
            }

            public class MyResponseHandler : IHandleMessages<MyRequest>
            {
                public Context Context { get; set; }

                public IBus Bus { get; set; }

                public void Handle(MyRequest response)
                {
                    if (!Context.Thrown)
                    {
                        Context.Thrown = true;
                        throw new Exception();
                    }

                    Context.ReceivedAgain = true;
                }
            }
        }


        [Serializable]
        public class MyRequest : IMessage
        {
        }
    }
}
