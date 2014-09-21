using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using zeco.autoapi.Extensions;
using zeco.autoapi.Json;

namespace zeco.autoapi.MVC
{
    public static class EntityInclusion
    {
        public static IHtmlString InlineObject(this HtmlHelper helper, object any, string id, string type = "applicaiton/json")
        {
            var items = any as IEnumerable<IIdentifiable>;
            var json = items != null ? new AutoApiEntityCollection {items}.Serialize() : any.ToSafeJson();
            var html = string.Format("<script type='{0}' id='{1}'>", type, id) + json + "</script>";
            return new HtmlString(html);
        }
    }
}
