using System;
using GreetingsCore.Ports.Commands;
using Paramore.Brighter;

namespace GreetingsCore.Ports.Handlers
{
    public class RegreetCommandHandler : RequestHandler<RegreetCommand>
    {
        public override RegreetCommand Handle(RegreetCommand command)
        {
            Console.WriteLine("Received Greeting. Message Follows");
            Console.WriteLine("----------------------------------");
            Console.WriteLine(command.Message);
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