using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;

namespace zeco.autoapi.MVC
{
    public static class RazorExtensions
    {
        private enum Filetype
        {
            Javascript, CSS
        }

        private static readonly ConcurrentDictionary<string, string> _filecache
            = new ConcurrentDictionary<string, string>();

        public class TagCloser : IDisposable
        {
            readonly ViewContext _viewContext;
            readonly string _html;
            bool _disposed;

            public TagCloser(ViewContext viewContext, string html)
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

        public static TagCloser InlineTemplate(this HtmlHelper htmlHelper, string name, string prefix = "/ng/")
        {
            htmlHelper.ViewContext.Writer.Write("<script type=\"text/ng-template\" id=\"{1}{0}\">", name, prefix);
            return new TagCloser(htmlHelper.ViewContext, "</script>");
        }

        public static IHtmlString InlineFile(this HtmlHelper htmlHelper, string filename, bool minify = true)
        {
            if (filename.EndsWith("*"))
            {
                var files = ListDirectory(filename.Substring(0, filename.Length - 1)).OrderBy(fn => fn);
                var builder = new StringBuilder();

                foreach(var file in files)
                {
                    var str = InlineFile(htmlHelper, file, minify);
                    builder.Append(str);
                }

                return new HtmlString(builder.ToString());
            }

            var type = GetFileType(filename);

            if (IsDebug()) return GetLinkTags(filename, type);

            var data = LoadFile(filename, minify, type);
            return GetInlineCode(type, data);
        }

        private static IHtmlString GetLinkTags(string filename, Filetype type)
        {
            switch (type)
            {
                case Filetype.Javascript:
                    return new HtmlString(string.Format("<script src='/{0}'></script>", filename));
                case Filetype.CSS:
                    return new HtmlString(string.Format("<link rel='stylesheet' href='/{0}'/>", filename));
                default:
                    throw new NotImplementedException();
            }
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

        private static IHtmlString GetInlineCode(Filetype type, string data)
        {
            switch (type)
            {
                case Filetype.Javascript:
                    return new HtmlString("<script>//<![CDATA[\n" + data + "\n//]]></script>");

                case Filetype.CSS:
                    return new HtmlString("<style>" + data + "</style>");

                default: throw new NotImplementedException();
            }
        }

        private static Filetype GetFileType(string filename)
        {
            if (filename.EndsWith(".js")) return Filetype.Javascript;
            if (filename.EndsWith(".css")) return Filetype.CSS;

            throw new NotImplementedException(string.Format("Can't include {0}", filename));
        }

        private static string LoadFile(string filename, bool minify, Filetype type)
        {
            minify = minify && !filename.Contains(".min.");

            if (!_filecache.ContainsKey(filename))
            {
                var minfname = filename
                    .Replace(".js", ".min.js")
                    .Replace(".css", ".min.css");

                var path = GetFilePath(filename);
                if (File.Exists(GetFilePath(minfname)) && minify)
                {
                    path = GetFilePath(minfname);
                    minify = false;
                }

                var data = File.ReadAllText(path);

                if (type == Filetype.Javascript)
                    data = data.Replace("</script>", @"<\/script>");

                if (minify)
                {
                    if (type == Filetype.Javascript)
                        data = new Minifier().MinifyJavaScript(data);

                    else if (type == Filetype.CSS)
                        data = new Minifier().MinifyStyleSheet(data);
                }
                _filecache[filename] = data;
            }

            return _filecache[filename];
        }

        private static IEnumerable<string> ListDirectory(string sub)
        {
            var path = Path.Combine(HttpRuntime.AppDomainAppPath, sub);

            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

            return files
                .Select(f => Relative(f, HttpRuntime.AppDomainAppPath))
                .Where(f => f.EndsWith(".js") || f.EndsWith(".css"))
                .Where(f => !f.EndsWith(".min.js"))
                .Where(f => !f.EndsWith(".min.js"));
        }

        private static string Relative(string path, string root)
        {
            return new Uri(root, UriKind.Absolute).MakeRelativeUri(new Uri(path, UriKind.Absolute)).ToString();
        }

        private static string GetFilePath(string filename)
        {
            return Path.Combine(HttpRuntime.AppDomainAppPath, filename);
        }
    }
}