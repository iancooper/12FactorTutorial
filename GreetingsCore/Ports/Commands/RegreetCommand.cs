using System;
using Paramore.Brighter;

namespace GreetingsCore.Ports.Commands
{
    public class RegreetCommand : Command
    {
        public Guid GreetingId { get; }

        public RegreetCommand() : base(Guid.NewGuid())
        {
            //required for de-serialization
        }
        
        public RegreetCommand(Guid greetingId) : base(Guid.NewGuid())
        {
            GreetingId = greetingId;
        }
    }
}