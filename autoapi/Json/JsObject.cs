using System.Dynamic;
using zeco.autoapi.Extensions;

namespace zeco.autoapi.Json
{
    public class JsObject : JsObjectBase
    {
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            Properties.TryGetValue(binder.Name, out result);
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Properties[binder.Name] = value;
            return true;
        }

        public string ToJson()
        {
            return Properties.ToJson();
        }
    }
}