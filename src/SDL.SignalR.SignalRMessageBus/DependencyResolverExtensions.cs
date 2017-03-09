using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;

namespace Sdl.SignalR.SignalRMessageBus
{
    public static class DependencyResolverExtensions
    {
        public static IDependencyResolver UseSdlBackplane(this IDependencyResolver resolver, string connectionString)
        {
            SignalRScaleoutConfiguration config = new SignalRScaleoutConfiguration(connectionString);
            return UseSdlBackplane(resolver, config);
        }

        public static IDependencyResolver UseSdlBackplane(this IDependencyResolver resolver, SignalRScaleoutConfiguration configuration)
        {
            SignalRMessageBus bus = new SignalRMessageBus(resolver, configuration);
            resolver.Register(typeof(IMessageBus), () => bus);
            return resolver;
        }
    }
}
