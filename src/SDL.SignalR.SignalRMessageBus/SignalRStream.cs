using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Client;
using System.Linq;
using Sdl.SignalR.Backplane.Common;

namespace Sdl.SignalR.SignalRMessageBus
{
    internal class SignalRStream
    {
        private readonly int _streamIndex;
        private readonly TraceSource _trace;
        private readonly string _tracePrefix;
        private readonly IHubProxy _hubProxy;

        public SignalRStream(int streamIndex, IHubProxy hubProxy, TraceSource traceSource)
        {
            _streamIndex = streamIndex;
            _trace = traceSource;
            _tracePrefix = string.Format(CultureInfo.InvariantCulture, "Stream {0} : ", _streamIndex);
            _hubProxy = hubProxy;

            Received += (_, __) => { };
        }

        public event Action<ulong, ScaleoutMessage> Received;

        public void StartReceiving()
        {
            _hubProxy.On<byte[]>(BackplaneConstants.ReceiveNotification, m =>
            {
                ulong id = BitConverter.ToUInt64(m.Take(8).ToArray(), 0);
                ScaleoutMessage message = ScaleoutMessage.FromBytes(m.Skip(8).ToArray());

                foreach (Message msg in message.Messages)
                {
                    msg.MappingId = id;
                }

                _trace.TraceVerbose("Payload {0} containing {1} message(s) received", id, message.Messages.Count);
                Received(id, message);
            });
        }

        public Task Send(IList<Message> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                var tcs = new TaskCompletionSource<object>();
                tcs.SetResult(null);
                return tcs.Task;
            }

            _trace.TraceVerbose("{0}Sending payload with {1} messages(s)", _tracePrefix, messages.Count, _streamIndex);

            ScaleoutMessage message = new ScaleoutMessage(messages);
            return _hubProxy.Invoke(BackplaneConstants.BroadcastNotification, message.ToBytes());
        }
    }
}
