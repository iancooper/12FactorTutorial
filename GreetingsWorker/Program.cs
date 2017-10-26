using System;
using GreetingsCore.Adapters.Factories;
using GreetingsCore.Ports.Events;
using GreetingsCore.Ports.Handlers;
using GreetingsCore.Ports.Mappers;
using Microsoft.Extensions.Configuration;
using Paramore.Brighter;
using Paramore.Brighter.MessagingGateway.RMQ;
using Paramore.Brighter.MessagingGateway.RMQ.MessagingGatewayConfiguration;
using Paramore.Brighter.ServiceActivator;
using Polly;
using Serilog;
using SimpleInjector;

namespace GreetingsReceiver
{
 public class Program
    {
        public static void Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();

            var configuration = builder.Build();
            
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .CreateLogger();

            var container = new Container();

            var handlerFactory = new HandlerFactory(container);
            var messageMapperFactory = new MessageMapperFactory(container);
            container.Register<IHandleRequests<GreetingEvent>, GreetingEventHandler>();

            var subscriberRegistry = new SubscriberRegistry();
            subscriberRegistry.Register<GreetingEvent, GreetingEventHandler>();

            //create policies
            var retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromMilliseconds(50),
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(150)
                });

            var circuitBreakerPolicy = Policy
                .Handle<Exception>()
                .CircuitBreaker(1, TimeSpan.FromMilliseconds(500));

            var policyRegistry = new PolicyRegistry
            {
                { CommandProcessor.RETRYPOLICY, retryPolicy },
                { CommandProcessor.CIRCUITBREAKER, circuitBreakerPolicy }
            };

            //create message mappers
            var messageMapperRegistry = new MessageMapperRegistry(messageMapperFactory)
            {
                { typeof(GreetingEvent), typeof(GreetingEventMessageMapper) }
            };

            var amqpUri = configuration["BABEL_BROKER"];
            //create the gateway
            var rmqConnnection = new RmqMessagingGatewayConnection 
            {
                AmpqUri  = new AmqpUriSpecification(new Uri(amqpUri)),
                //AmpqUri  = new AmqpUriSpecification(new Uri("amqp://guest:guest@localhost:5672/%2f")),
                Exchange = new Exchange("paramore.brighter.exchange"),
            };

            var rmqMessageConsumerFactory = new RmqMessageConsumerFactory(rmqConnnection);

            var dispatcher = DispatchBuilder.With()
                .CommandProcessor(CommandProcessorBuilder.With()
                    .Handlers(new HandlerConfiguration(subscriberRegistry, handlerFactory))
                    .Policies(policyRegistry)
                    .NoTaskQueues()
                    .RequestContextFactory(new InMemoryRequestContextFactory())
                    .Build())
                .MessageMappers(messageMapperRegistry)
                .DefaultChannelFactory(new InputChannelFactory(rmqMessageConsumerFactory, null))
                .Connections(new Connection[]
                {
                    new Connection<GreetingEvent>(
                        new ConnectionName("paramore.example.greeting"),
                        new ChannelName("greeting.event"),
                        new RoutingKey("greeting.event"),
                        timeoutInMilliseconds: 200)
                }).Build();

            dispatcher.Receive();

            Console.WriteLine("Press Enter to stop ...");
            Console.ReadLine();

            dispatcher.End().Wait();
        }
    }
}