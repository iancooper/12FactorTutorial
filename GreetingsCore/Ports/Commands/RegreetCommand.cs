using System;
using Paramore.Brighter;

namespace GreetingsCore.Ports.Commands
{
    public class RegreetCommand : Command
    {
        public Guid CorrelationId { get; }
        public RegreetCommand(Guid correlationId) : this(Guid.NewGuid(), correlationId) {}
        
        public RegreetCommand(Guid id, Guid correlationId) : base(id)
        {
            CorrelationId = correlationId;
        }
    }
}