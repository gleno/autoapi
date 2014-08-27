using System;
using System.Web.Mvc;

namespace zeco.autoapi.MVC
{
    public static class RazorUtilities
    {
        public class Tag : IDisposable
        {
            readonly ViewContext _viewContext;
            readonly string _html;
            bool _disposed;

            public Tag(ViewContext viewContext, string html)
            {
                _viewContext = viewContext;
                _html = html;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _viewContext.Writer.Write(_html);
                }
            }
        }

        public static string Iff(this HtmlHelper htmlHelper, bool condition, string output)
        {
            return condition ? output : String.Empty;
        }

        public static Tag InlineTemplate(this HtmlHelper htmlHelper, string name, string prefix = "/ng/")
        {
            htmlHelper.ViewContext.Writer.Write("<script type=\"text/ng-template\" id=\"{1}{0}\">", name, prefix);
            return new Tag(htmlHelper.ViewContext, "</script>");
        }

        public static bool IsDebug(this HtmlHelper helper)
        {
            return IsDebug();
        }

        private static bool IsDebug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
    }
}