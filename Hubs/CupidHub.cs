using Microsoft.AspNetCore.SignalR;
using PubSub.models;

namespace PubSub.Hubs
{
    public class CupidHub : Hub
    {
        private static readonly Dictionary<string, string> UserConnections = new();

        public async Task JoinAsync(string username)
        {
            UserConnections[username] = Context.ConnectionId;
            await Clients.Caller.SendAsync("Connected", $"Povezan si kao {username}");
        }

        public static async Task SendLetterToUserAsync(IHubContext<CupidHub> hubContext, string username, LoveLetter letter)
        {
            if (UserConnections.TryGetValue(username, out var connectionId))
            {
                await hubContext.Clients.Client(connectionId).SendAsync("ReceiveLetter", new
                {
                    senderUsername = letter.Sender.Username,
                    senderCity = letter.Sender.City,
                    senderAge = letter.Sender.Age,
                    senderPhone = letter.Sender.Phone,
                    message = letter.Message
                });
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userToRemove = UserConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (userToRemove != null)
            {
                UserConnections.Remove(userToRemove);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
