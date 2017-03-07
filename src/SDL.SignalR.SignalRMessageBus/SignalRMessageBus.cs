using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Microsoft.AspNet.SignalR.Client;
using System.Net;
using SDL.SignalR.Backplane.Common;

namespace SDL.SignalR.SignalRMessageBus
{
    public class SignalRMessageBus : ScaleoutMessageBus
    {
        private readonly TraceSource _trace;
        private readonly HubConnection _hubConnection;
        private readonly IHubProxy _hubProxy;
        private SignalRStream _stream;

        public SignalRMessageBus(IDependencyResolver resolver, SignalRScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager[GetType().Name];

            _hubConnection = new HubConnection(configuration.ConnectionString);
            _hubConnection.Credentials = CredentialCache.DefaultNetworkCredentials;
            _hubProxy = _hubConnection.CreateHubProxy(BackplaneConstants.SdlBackplaneHub);

            Initialize();
        }

        protected override int StreamCount
        {
            get
            {
                return 1;
            }
        }

        private void Initialize()
        {
            _trace.TraceInformation("SignalR message bus initializing");

            _hubConnection.Closed += () =>
            {
                try
                {
                    _hubConnection.Start();
                }
                catch (Exception ex)
                {
                    _trace.TraceError("Error occurred while establishing SignalR message bus connection: {0}", ex);
                }
            };

            _stream = new SignalRStream(0, _hubProxy, _trace);
            _stream.Received += (id, messages) => OnReceived(0, id, messages);
            _stream.StartReceiving();

            Open(0);
            _hubConnection.Start().Wait();
        }

        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return Send(messages);
        }

        protected override Task Send(IList<Message> messages)
        {
            if (_hubConnection.State != ConnectionState.Connected)
            {
                return Task.FromResult(false);
            }

            return _stream.Send(messages);
        }

        protected override void Dispose(bool disposing)
        {
            _trace.TraceInformation("SignalR message bus disposing");
            base.Dispose(disposing);
        }
    }
}
