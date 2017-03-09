using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.AspNet.SignalR.Messaging;
using FakeItEasy;
using Microsoft.AspNet.SignalR.Client;
using System.Threading.Tasks;

namespace Sdl.SignalR.SignalRMessageBus.Tests
{
    [TestClass]
    public class StreamTest
    {
        [TestMethod]
        public void Send_OneMessage_Success()
        {
            var fakeHubProxy = A.Fake<IHubProxy>();
            var fakeInvoke = A.CallTo(() => fakeHubProxy.Invoke(null, null));
            fakeInvoke.WithAnyArguments().Returns(Task.Delay(1));

            SignalRStream stream = new SignalRStream(0, fakeHubProxy, new TraceSource("ts"));

            var messages = new List<Message>();
            messages.Add(new Message("src", "key", "val"));
            stream.Send(messages).Wait();

            fakeInvoke.MustHaveHappened();
        }

        [TestMethod]
        public void Send_MultipleMessage_Success()
        {
            var fakeHubProxy = A.Fake<IHubProxy>();
            var fakeInvoke = A.CallTo(() => fakeHubProxy.Invoke(null, null));
            fakeInvoke.WithAnyArguments().Returns(Task.Delay(1));

            SignalRStream stream = new SignalRStream(0, fakeHubProxy, new TraceSource("ts"));

            var messages = new List<Message>();
            messages.Add(new Message("src1", "key1", "val1"));
            messages.Add(new Message("src2", "key2", "val2"));
            messages.Add(new Message("src3", "key3", "val3"));
            stream.Send(messages).Wait();

            fakeInvoke.MustHaveHappened();
        }

        [TestMethod]
        public void Send_EmptyList_Success()
        {
            var fakeHubProxy = A.Fake<IHubProxy>();
            var fakeInvoke = A.CallTo(() => fakeHubProxy.Invoke(null, null));
            fakeInvoke.WithAnyArguments().Returns(Task.Delay(1));

            SignalRStream stream = new SignalRStream(0, fakeHubProxy, new TraceSource("ts"));

            var messages = new List<Message>();
            stream.Send(messages).Wait();

            fakeInvoke.MustNotHaveHappened();
        }
    }
}
