using System;
using Microsoft.AspNet.SignalR.Messaging;

namespace Sdl.SignalR.SignalRMessageBus
{
    /// <summary>
    /// Settings for the SignalR scale-out message bus implementation.
    /// </summary>
    public class SignalRScaleoutConfiguration : ScaleoutConfiguration
    {
        public SignalRScaleoutConfiguration(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            ConnectionString = connectionString;
        }

        /// <summary>
        /// The SignalR endpoint address string to use.
        /// </summary>
        public string ConnectionString
        {
            get;
            private set;
        }
    }
}
