using System;
using System.Web.Mvc;

namespace autoapi.MVC
{
    public static class RazorUtilities
    {

        public class Tag : IDisposable
        {
            private readonly Action _action;
            private bool _disposed;

            public Tag(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                Dispose(true);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_disposed) return;
                _disposed = true;
                _action();
            }
        }

        public static string Iff(this HtmlHelper htmlHelper, bool condition, string output)
        {
            return condition ? output : string.Empty;
        }

        public static Tag InlineTemplate(this HtmlHelper htmlHelper, string name, string prefix = "/ng/")
        {
            var writer = htmlHelper.ViewContext.Writer;
            writer.Write("<script type=\"text/ng-template\" id=\"{1}{0}\">", name, prefix);
            return new Tag(() => writer.Write("</script>"));
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