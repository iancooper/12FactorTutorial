using System;
using Paramore.Brighter;

namespace GreetingsCore.Ports.Commands
{
    public class RegreetCommand : Command
    {
        public Guid CorrelationId { get; }
        public string Message { get; }
        public RegreetCommand(Guid correlationId, string message) : this(Guid.NewGuid(), correlationId, message) {}
        
        public RegreetCommand(Guid id, Guid correlationId, string message) : base(id)
        {
            CorrelationId = correlationId;
            Message = message;
        }
    }
}