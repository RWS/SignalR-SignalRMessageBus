using Microsoft.AspNet.SignalR.Hubs;
using SDL.SignalR.Backplane.Common;

namespace SDL.SignalR.Backplane.Hub
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
