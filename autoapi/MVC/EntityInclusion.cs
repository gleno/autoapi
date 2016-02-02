using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using autoapi.Extensions;
using autoapi.Json;

namespace autoapi.MVC
{
    public static class EntityInclusion
    {
        public static IHtmlString InlineObject(this HtmlHelper helper, object any, string id, string type = "applicaiton/json")
        {
            var items = any as IEnumerable<IIdentifiable>;
            var json = items != null ? new AutoApiEntityCollection {items}.Serialize() : any.ToSafeJson();
            var html = $"<script type='{type}' id='{id}'>" + json + "</script>";
            return new HtmlString(html);
        }
    }
}
