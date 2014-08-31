using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;

namespace zeco.autoapi.MVC
{
    public static class FileInclusion
    {
        private enum SourceType
        {
            Javascript, CSS, Template
        }

        private class SourceFile
        {
            public SourceType Type { get; set; }
            public string Source { get; set; }
        }

        private static readonly ConcurrentDictionary<string, string> _cache
            = new ConcurrentDictionary<string, string>();

        public static IHtmlString InlineFile(this HtmlHelper helper, string filename)
        {

            if (helper.IsDebug())
            {
                var tags = "";
                foreach (var name in GetNames(filename))
                    tags += GetLinkTags(name);
                return new HtmlString(tags);
            }

            if (!_cache.ContainsKey(filename))
            {
                var names = GetNames(filename);
                var tags = names.GroupBy(Extension).ToDictionary(g => GetFileType(g.Key), g =>
                {
                    var files = g.ToArray();

                    var source = new StringBuilder();
                    for (var idx = 0; idx < files.Length; idx++)
                    {
                        var file = LoadFile(files[idx]);
                        source.Append(Minify(file));
                        if (idx < files.Length - 1) source.Append(Pad(file.Type));
                    }

                    return source.ToString();
                });

                var output = "";
                foreach (var tag in tags)
                    output += WrapSource(tag.Key, tag.Value);

                _cache[filename] = output;
            }

            return new HtmlString(_cache[filename]);

        }

        private static string[] GetNames(string filename)
        {
            var names = (filename.EndsWith("*") ? ListDirectory(filename) : new[] {filename});
            return names;
        }

        private static string Extension(string filename)
        {
            return filename
                .Substring(filename.LastIndexOf(".", StringComparison.Ordinal) + 1)
                .ToLowerInvariant();
        }

        private static string Pad(SourceType type)
        {
            switch (type)
            {
                case SourceType.Javascript:
                    return ";";

                case SourceType.CSS:
                    return " ";
            }

            return "\n";
        }

        public static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            var md5 = MD5.Create();
            var inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private static string GetLinkTags(string filename)
        {
            var signature = CreateMD5(LoadFile(filename).Source);

            switch (GetFileType(Extension(filename)))
            {
                case SourceType.Javascript:
                    return string.Format("<script src='/{0}?rnd={1}'></script>", filename, signature);

                case SourceType.CSS:
                    return string.Format("<link rel='stylesheet' href='/{0}?rnd={1}'/>", filename, signature);

                default:
                    throw new NotImplementedException();
            }
        }

        private static string WrapSource(SourceType type, string data)
        {
            switch (type)
            {
                case SourceType.Javascript:
                    return "<script>//<![CDATA[\n" + data + "\n//]]></script>";

                case SourceType.CSS:
                    return "<style>" + data + "</style>";

                default: throw new NotImplementedException();
            }
        }

        private static SourceType GetFileType(string extension)
        {
            if (extension == "js") return SourceType.Javascript;
            if (extension == "css") return SourceType.CSS;

            throw new NotImplementedException(string.Format("Unknown file {0}", extension));
        }

        private static SourceFile LoadFile(string filename)
        {
            var minfname = filename
                .Replace(".js", ".min.js")
                .Replace(".css", ".min.css");

            var source =  File.ReadAllText(GetFilePath(minfname) ?? GetFilePath(filename));

            return new SourceFile
            {
                Source = source,
                Type = GetFileType( Extension(filename) )
            };

        }

        private static string Minify(SourceFile source)
        {
            var minifier = new Minifier();

            switch (source.Type)
            {
                case SourceType.Javascript:
                    return minifier.MinifyJavaScript(source.Source, new CodeSettings
                    {
                        PreserveImportantComments = false
                    });

                case SourceType.CSS:
                    return minifier.MinifyStyleSheet(source.Source, new CssSettings
                    {
                        CommentMode = CssComment.None
                    });
            }

            throw new NotImplementedException();
        }

        private static string[] ListDirectory(string dirspec)
        {
            var path = Path.Combine(HttpRuntime.AppDomainAppPath, dirspec.Substring(0, dirspec.Length - 1));

            return Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                .Select(f => new Uri(HttpRuntime.AppDomainAppPath, UriKind.Absolute).MakeRelativeUri(new Uri(f, UriKind.Absolute)).ToString())
                .Where(f => f.EndsWith(".js") || f.EndsWith(".css"))
                .Where(f => !f.EndsWith(".min.js"))
                .Where(f => !f.EndsWith(".min.css"))
                .OrderBy(fn => fn.Length)
                .ToArray();
        }

        private static string GetFilePath(string filename)
        {
            var path =  Path.Combine(HttpRuntime.AppDomainAppPath, filename);
            return !File.Exists(path) ? null : path;
        }
    }
}