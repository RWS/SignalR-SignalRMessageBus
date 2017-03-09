using Microsoft.AspNet.SignalR.Hubs;
using Sdl.SignalR.Backplane.Common;

namespace Sdl.SignalR.Backplane.Hub
{
    [HubName(BackplaneConstants.SdlBackplaneHub)]
    public class SdlBackplaneHub : Microsoft.AspNet.SignalR.Hub<IBackplaneHubClient>, IBackplaneHubServer
    {
        public void BackplaneBroadcastNotification(byte[] messageBytes)
        {
            Clients.All.BackplaneReceiveNotification(messageBytes);
        }
    }
}
