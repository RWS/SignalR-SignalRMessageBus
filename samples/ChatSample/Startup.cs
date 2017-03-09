using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
using Sdl.SignalR.SignalRMessageBus;

[assembly: OwinStartup(typeof(ChatSample.Startup))]

namespace ChatSample
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            GlobalHost.DependencyResolver.UseSdlBackplane("http://localhost:61457/");
            app.MapSignalR();
        }
    }
}
