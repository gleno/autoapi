using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace zeco.autoapi.MVC
{
    public static class RazorUtilities
    {

        public class Tag : IDisposable
        {
            private readonly TextWriter _writer;
            private readonly Action _fin;
            bool _disposed;

            public Tag(TextWriter writer, Action fin)
            {
                _writer = writer;
                _fin = fin;
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
                    _fin();
                }
            }
        }

        public static string Iff(this HtmlHelper htmlHelper, bool condition, string output)
        {
            return condition ? output : String.Empty;
        }

        public static Tag InlineTemplate(this HtmlHelper htmlHelper, string name, string prefix = "/ng/")
        {
            var writer = htmlHelper.ViewContext.Writer;
            writer.Write("<script type=\"text/ng-template\" id=\"{1}{0}\">", name, prefix);
            return new Tag(writer, () => writer.Write("</script>"));
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