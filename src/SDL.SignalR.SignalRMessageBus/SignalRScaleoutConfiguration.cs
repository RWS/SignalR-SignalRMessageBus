using System;
using Microsoft.AspNet.SignalR.Messaging;

namespace SDL.SignalR.SignalRMessageBus
{
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

        public string ConnectionString
        {
            get;
            private set;
        }
    }
}
