using Microsoft.AspNetCore.SignalR;

namespace WebApiServer
{
    public class NotificationHub : Hub
    {
        public async Task Register(string clientId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, clientId);
        }
    }
}


