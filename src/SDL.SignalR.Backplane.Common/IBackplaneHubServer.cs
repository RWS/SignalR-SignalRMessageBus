namespace Sdl.SignalR.Backplane.Common
{
    /// <summary>
    /// Server hub implements this interface in order
    /// to be used by client to broadcast notifications for other clients
    /// </summary>
    public interface IBackplaneHubServer
    {
        /// <summary>
        /// Puts messages to backplane provider and broadcasts
        /// message to other connected backplane clients.
        /// </summary>
        /// <param name="messageBytes">Message object represented as a byte array.</param>
        void BackplaneBroadcastNotification(byte[] messageBytes);
    }
}
