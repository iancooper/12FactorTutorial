using System;
using System.Linq;
using GreetingsCore.Adapters.Db;
using GreetingsCore.Model;
using GreetingsCore.Ports.Commands;
using Microsoft.EntityFrameworkCore;
using Paramore.Brighter;

namespace GreetingsCore.Ports.Handlers
{
    public class RegreetCommandHandler : RequestHandler<RegreetCommand>
    {
        private readonly DbContextOptions<GreetingContext> _options;

        public RegreetCommandHandler(DbContextOptions<GreetingContext> options)
        {
            _options = options;
        }

        public override RegreetCommand Handle(RegreetCommand command)
        {

            //Note how we share the Db - same microservice, different process, so 
            // we can just ask for the greeting
            Greeting greeting;
            using (var uow = new GreetingContext(_options))
            {
                greeting = uow.Greetings.Single(g => g.Id == command.CorrelationId);
            }

            //we don't want to terminate, so on to the next message
            if (greeting == null)
            {
                return base.Handle(command);
            }
            
            Console.WriteLine("Received Greeting. Message Follows");
            Console.WriteLine("----------------------------------");
            Console.WriteLine(greeting.Message);
            Console.WriteLine("----------------------------------");
            Console.WriteLine("Correlation Id from Originator Follows");
            Console.WriteLine("----------------------------------");
            Console.WriteLine(command.CorrelationId.ToString());
            Console.WriteLine("----------------------------------");
            Console.WriteLine("Message Ends");
  
            return base.Handle(command);
        }
    }
}