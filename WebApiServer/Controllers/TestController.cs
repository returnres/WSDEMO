using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace WebApiServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hub;

        public TestController(IHubContext<NotificationHub> hub)
        {
            _hub = hub;
        }

        [HttpGet]
        public async Task Test()
        {
            await _hub.Clients.All.SendAsync("TestMessage", "Ciao dal server, mi hai chiamato!");
        }
    }
}
