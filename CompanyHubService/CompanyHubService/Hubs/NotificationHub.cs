using Microsoft.AspNetCore.SignalR;

namespace CompanyHubService.Hubs
{
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            Console.WriteLine($"🔗 SignalR connected: {userId}");
            return base.OnConnectedAsync();
        }
    }
}
