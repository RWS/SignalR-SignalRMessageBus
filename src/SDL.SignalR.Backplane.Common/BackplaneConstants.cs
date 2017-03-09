namespace Sdl.SignalR.Backplane.Common
{
    /// <summary>
    /// Constants for backplane hub.
    /// </summary>
    public static class BackplaneConstants
    {
        /// <summary>
        /// Backplane hub name.
        /// </summary>
        public const string SdlBackplaneHub = "SdlBackplane";

        /// <summary>
        /// Backplane hub proxy method for notification broadcast.
        /// </summary>
        public const string BroadcastNotification = "BackplaneBroadcastNotification";

        /// <summary>
        /// The name of the callback method to be registered on the client side in order to
        /// receive push notifications from Backplane hub.
        /// </summary>
        public const string ReceiveNotification = "BackplaneReceiveNotification";
    }
}
