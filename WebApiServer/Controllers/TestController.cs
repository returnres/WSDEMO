using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace WebApiServer.Controllers
{
    [ApiController]
    [Route("test")]
    public class TestController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hub;

        public TestController(IHubContext<NotificationHub> hub)
        {
            _hub = hub;
        }

        [HttpGet]
        public async Task Send()
        {
            await _hub.Clients.All.SendAsync("ReceiveMessage", "Ciao dal server");
        }
    }
}
