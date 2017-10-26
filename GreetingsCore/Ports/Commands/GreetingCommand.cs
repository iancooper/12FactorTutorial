using System;
using Paramore.Brighter;

namespace GreetingsCore.Ports.Commands
{
    public class GreetingCommand : Command
    {
        public string Message { get; }
        
        public GreetingCommand(Guid id, string message) : base(id) {}

        public GreetingCommand(string message) : this(Guid.NewGuid(), message) {}
    }
}