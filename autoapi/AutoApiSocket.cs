using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using zeco.autoapi.Extensions;

namespace zeco.autoapi
{
    [Authorize]
    public class AutoApiSocket : PersistentConnection
    {
        #region SocketPacket - Inner Class

        public class SocketPacket
        {
            public dynamic Data { get; set; }
            public string Method { get; set; }
            public int Sequence { get; set; }
            public string Type { get; set; }
        }

        #endregion

        public Dictionary<string, ApiControllerBase> _controllers
            = new Dictionary<string, ApiControllerBase>();
        
        private ApiControllerBase GetController(SocketPacket packet)
        {
            if (!_controllers.ContainsKey(packet.Type))
                Initialize(packet.Type);
            return _controllers[packet.Type];
        }

        private object GetPayload(ApiControllerBase controller, MethodInfo method, SocketPacket packet, bool hasParam)
        {
            var prms = method.GetParameters().SingleOrDefault();
            var data = hasParam ? packet.Data : new object[0];

            if (hasParam && prms != null)
            {
                if (prms.ParameterType == typeof (Guid))
                    data = Guid.Parse(((JValue) packet.Data.id).ToString(CultureInfo.InvariantCulture));

                data = new object[] {data};
            }

            return method.Invoke(controller, data);
        }

        private void Initialize(string typename)
        {
            var kernel = AutoHttpApplication.Instance.Factory.Kernel;
            var type = Util.GetApiControllerFor(typename);
            var controller = (ApiControllerBase) kernel.Resolve(type);
            _controllers[typename] = controller;
        }

        private static MethodInfo GetMethod(ApiControllerBase controller, SocketPacket packet, bool hasParam)
        {
            var name = packet.Method.ToLowerInvariant().Capitalize();

            var method = controller.GetType()
                .GetMethods()
                .Where(m => m.Name == name)
                .SingleOrDefault(m => m.GetParameters().Length == (hasParam ? 1 : 0));

            if (method == null)
                throw new SecurityException(string.Format("Can't call {0}", packet.Method));

            return method;
        }

        protected override Task OnReceived(IRequest request, string connectionId, string data)
        {
            var packet = JsonConvert.DeserializeObject<SocketPacket>(data);

            var hasParam = packet.Data is JArray;
            var jobj = packet.Data as JObject;
            if (jobj != null && jobj.HasValues)
                hasParam = true;

            var controller = GetController(packet);
            var method = GetMethod(controller, packet, hasParam);
            var dataPayload = GetPayload(controller, method, packet, hasParam);

            var json = new
            {
                data = dataPayload,
                sequence = packet.Sequence
            }.ToSafeJson();

            return Connection.Send(connectionId, json);
        }
    }
}