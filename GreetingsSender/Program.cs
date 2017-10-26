using System;
using GreetingsCore.Adapters.Factories;
using GreetingsCore.Ports.Events;
using GreetingsCore.Ports.Mappers;
using Microsoft.Extensions.Configuration;
using Paramore.Brighter;
using Paramore.Brighter.MessagingGateway.RMQ;
using Paramore.Brighter.MessagingGateway.RMQ.MessagingGatewayConfiguration;
using SimpleInjector;

namespace GreetingsSender
{
    class Program
    {
        static void Main(string[] args)
        {

            var container = new Container();

            var configBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            var configuration = configBuilder.Build();
             
            var messageMapperFactory = new MessageMapperFactory(container);
            
            var messageMapperRegistry = new MessageMapperRegistry(messageMapperFactory)
            {
                { typeof(GreetingEvent), typeof(GreetingEventMessageMapper) }
            };
            
            var messageStore = new InMemoryMessageStore();
            var amqpUri = configuration["BABEL_BROKER"];
            var rmqConnnection = new RmqMessagingGatewayConnection 
            {
                AmpqUri  = new AmqpUriSpecification(new Uri(amqpUri)),
                Exchange = new Exchange("paramore.brighter.exchange"),
            };
            var producer = new RmqMessageProducer(rmqConnnection);

            var builder = CommandProcessorBuilder.With()
                .Handlers(new HandlerConfiguration())
                .DefaultPolicy()
                .TaskQueues(new MessagingConfiguration(messageStore, producer, messageMapperRegistry))
                .RequestContextFactory(new InMemoryRequestContextFactory());

            var commandProcessor = builder.Build();

            commandProcessor.Post(new GreetingEvent("Ian"));
        }
    }
}