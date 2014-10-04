using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using zeco.autoapi.Extensions;

namespace zeco.autoapi
{
    [Authorize]
    public class AutoApiSocket : PersistentConnection
    {
        public Dictionary<string, ApiControllerBase> _controllers
            = new Dictionary<string, ApiControllerBase>();

        public class SocketPacket
        {
            public string Type { get; set; }
            public dynamic Payload { get; set; }
            public string Method { get; set; }
            public int Sequence { get; set; }
        }

        public class SocketSendPacket
        {
            public object Payload { get; set; }

            public int Sequence { get; set; }
        }

        private void Initialize(string typename)
        {
            var type = Util.GetApiControllerFor(typename);
            var controller = (ApiControllerBase) AutoHttpApplication.Instance.Factory.Kernel.Resolve(type);
            _controllers[typename] = controller;
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            var packet = JsonConvert.DeserializeObject<SocketPacket>(data);
            var controller = GetController(packet);
            var method = GetMethod(controller, packet);
            var payload = GetPayload(packet, method, controller);

            var json = new SocketSendPacket
            {
                Payload = payload,
                Sequence = packet.Sequence
            }.ToSafeJson();

            return Connection.Send(connectionId, json);
        }

        private static MethodInfo GetMethod(ApiControllerBase controller, SocketPacket packet)
        {
            var method = controller.GetType().GetMethods()
                .SingleOrDefault(m => m.Name == packet.Method && m.GetParameters().Length == (packet.Payload != null ? 1 : 0));

            if (method == null)
                throw new SecurityException(string.Format("Can't call {0}", packet.Method));
            return method;
        }

        private ApiControllerBase GetController(SocketPacket packet)
        {
            if (!_controllers.ContainsKey(packet.Type))
                Initialize(packet.Type);

            var controller = _controllers[packet.Type];
            return controller;
        }

        private object GetPayload(SocketPacket packet, MethodInfo method, ApiControllerBase controller)
        {
            if (packet.Payload != null)
            {
                var type = method.GetParameters()[0].ParameterType;
                if (type == typeof (Guid))
                    packet.Payload = Guid.Parse(packet.Payload);

                packet.Payload = new object[] {packet.Payload};
            }

            return method.Invoke(controller, packet.Payload);
        }
    }
}
