using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.SignalR.Hubs;

namespace SDL.SignalR.Backplane.Common
{
    /// <summary>
    /// Extends hosted hub pipeline to faciliate tracing of errors that occur during message broadcasting.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ErrorHandlingPipelineModule : HubPipelineModule
    {
        protected override void OnIncomingError(ExceptionContext exceptionContext, IHubIncomingInvokerContext invokerContext)
        {
            Trace.WriteLine(exceptionContext.Error);
            
            base.OnIncomingError(exceptionContext, invokerContext);
        }
    }
}
