using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace WebApiServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hub;

        public FileController(IHubContext<NotificationHub> hub)
        {
            _hub = hub;
        }

       
        [HttpPost("add")]
        public async Task<IActionResult> Add(IFormFile file, string clientId)
        {
            //using var ms = new MemoryStream();
            //await file.CopyToAsync(ms);

            //var entity = new FileEntity
            //{
            //    Id = Guid.NewGuid(),
            //    ClientId = clientId,
            //    Content = ms.ToArray(),
            //    Status = "Pending"
            //};

            //_db.Files.Add(entity);
            //await _db.SaveChangesAsync();

            //// notifico client
            //await _hub.Clients
            //    .Group(clientId)
            //    .SendAsync("NotifyFile", entity.Id);

            // notifico client
            await _hub.Clients
                .Group(clientId)
                .SendAsync("ReceiveMessage", new Guid());

            //return Ok(entity.Id);
            return Ok(1);
        }
    }
}
