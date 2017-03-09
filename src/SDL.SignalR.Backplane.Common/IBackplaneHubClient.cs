namespace Sdl.SignalR.Backplane.Common
{
    /// <summary>
    /// Interface to be implemented by backplane client in order to
    /// receive notifications from server hub.
    /// </summary>
    public interface IBackplaneHubClient
    {
        /// <summary>
        /// Method is used to push notification messages from server to client.
        /// </summary>
        /// <param name="messageBytes">Message object represented as a byte array.</param>
        void BackplaneReceiveNotification(byte[] messageBytes);
    }
}
