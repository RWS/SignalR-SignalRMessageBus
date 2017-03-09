using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.SqlServer;
using Microsoft.Owin;
using Owin;
using Sdl.SignalR.Backplane.Common;
using Sdl.SignalR.Backplane.Hub;
using System;

[assembly: OwinStartup(typeof(BackendServer.Startup))]

namespace BackendServer
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            string sqlConnectionString = "Data Source=(local);Initial Catalog=signalr;User ID=signalrsa;Password=Pass123";
            Type messageBusType = typeof(SqlMessageBus);
            Type messageBusConfigurationType = typeof(SqlScaleoutConfiguration);
            Type hubType = typeof(SdlBackplaneHub);
            HubLoader.Load(app, messageBusType.Assembly.FullName, messageBusConfigurationType.FullName, messageBusType.FullName, hubType.Assembly.FullName, true, sqlConnectionString);
        }
    }
}
