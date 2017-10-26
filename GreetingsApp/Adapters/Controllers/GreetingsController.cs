using System;
using System.Threading.Tasks;
using GreetingsCore.Adapters.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Paramore.Brighter;
using Paramore.Darker;

namespace GreetingsApp.Adapters.Controllers
{
    [Route("api/[controller]")]
    public class GreetingsController : Controller
    {
        private readonly IAmACommandProcessor _commandProcessor;
        private readonly IQueryProcessor _queryProcessor;
        
        
        // GET
        public GreetingsController(IAmACommandProcessor commandProcessor, IQueryProcessor queryProcessor)
        {
            _commandProcessor = commandProcessor;
            _queryProcessor = queryProcessor;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok();
        }

        [HttpGet("{id}", Name = "GetGreeting")]
        public async Task<IActionResult> GetById(Guid id)
        {
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AddGreetingRequest request)
        {
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {

            return Ok();
        }
    }
}