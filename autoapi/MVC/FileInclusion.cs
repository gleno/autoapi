using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using zeco.autoapi.Extensions;

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
            public string FullName { get; set; }
            public string Checksum { get; set; }
        }

        private static readonly ConcurrentDictionary<string, string> _cache
            = new ConcurrentDictionary<string, string>();

        public static IHtmlString InlineFile(this HtmlHelper helper, string filename)
        {
            var names = GetNames(filename);

            if (helper.IsDebug())
            {
                var tags = "";
                foreach (var name in names)
                    tags += GetLinkTags(name);
                return new HtmlString(tags);
            }

            if (!_cache.ContainsKey(filename))
            {
                var tags = names.GroupBy(Extension).ToDictionary(g => GetFileType(g.Key), g =>
                {
                    var files = g.ToArray();

                    var source = new StringBuilder();

                    for (var idx = 0; idx < files.Length; idx++)
                    {
                        var file = LoadFile(files[idx]);
                        source.Append(Prefix(file));
                        source.Append(Minify(file));
                        source.Append(Suffix(file));
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
            return filename.Contains("*") ? ListDirectory(filename) : new[] {filename};
        }

        private static string Extension(string filename)
        {
            return filename
                .Substring(filename.LastIndexOf(".", StringComparison.Ordinal) + 1)
                .ToLowerInvariant();
        }

        private static string Prefix(SourceFile file)
        {
            switch (file.Type)
            {
                case SourceType.Template:
                    return $"<script type=\"text/ng-template\" id=\"/{file.FullName}\">";

                default:
                    return "";
            }
        }       
        
        private static string Suffix(SourceFile file)
        {
            switch (file.Type)
            {
                case SourceType.Javascript:
                    return ";";

                case SourceType.CSS:
                    return " ";

                case SourceType.Template:
                    return "</script>";
            }

            return "\n";
        }

        private static string GetLinkTags(string filename)
        {
            var file = LoadFile(filename);

            switch (GetFileType(Extension(filename)))
            {

                case SourceType.Template:
                    return "";

                case SourceType.Javascript:
                    return $"<script src='/{filename}?rnd={file.Checksum}'></script>";

                case SourceType.CSS:
                    return $"<link rel='stylesheet' href='/{filename}'/>";

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

                case SourceType.Template:
                    return data;

                default: throw new NotImplementedException();
            }
        }

        private static SourceType GetFileType(string extension)
        {
            if (extension == "js") return SourceType.Javascript;
            if (extension == "css") return SourceType.CSS;
            if (extension == "html") return SourceType.Template;

            throw new NotImplementedException($"Unknown file {extension}");
        }

        private static SourceFile LoadFile(string filename)
        {
            var minfname = filename
                .Replace(".js", ".min.js")
                .Replace(".css", ".min.css");

            string source;
            try
            {
                source = File.ReadAllText(GetFilePath(minfname) ?? GetFilePath(filename));
            }
            catch
            {
                throw new Exception($"Unable to load file : {filename}");
            }

            var checksum = source.MD5();

            return new SourceFile
            {
                Checksum = checksum,
                FullName = filename,
                Source = source,
                Type = GetFileType( Extension(filename) )
            };

        }

        private static string Minify(SourceFile file)
        {
            var minifier = new Minifier();

            switch (file.Type)
            {
                case SourceType.Javascript:
                    return minifier.MinifyJavaScript(file.Source, new CodeSettings
                    {
                        PreserveImportantComments = false,
                    });

                case SourceType.CSS:
                    return minifier.MinifyStyleSheet(file.Source, new CssSettings
                    {
                        CommentMode = CssComment.None
                    });

                case SourceType.Template:
                    return MinifyHtml(file.Source);


                default:throw new NotImplementedException();
            }
        }

        private static string MinifyHtml(string html)
        {
            html = Regex.Replace(html, @"(?s)\s+(?!(?:(?!</?pre\b).)*</pre>)", " ");
            html = Regex.Replace(html, @"(?s)\s*\n\s*(?!(?:(?!</?pre\b).)*</pre>)", "\n");
            html = Regex.Replace(html, @"(?s)\s*\>\s*\<\s*(?!(?:(?!</?pre\b).)*</pre>)", "><");
            html = Regex.Replace(html, @"(?s)<!--((?:(?!</?pre\b).)*?)-->(?!(?:(?!</?pre\b).)*</pre>)", "");
            return html;
        }

        private static string[] ListDirectory(string dirspec)
        {

            var rule = Regex.Match(dirspec, @"\*(.*)");

            var path = Path.Combine(HttpRuntime.AppDomainAppPath, dirspec.Substring(0, Array.IndexOf(dirspec.ToArray(), '*')));

            var files =  Directory.GetFiles(path, "*." + (rule.Value.Length > 0 ? rule.Value : "*"), SearchOption.AllDirectories)
                .Select(f => new Uri(HttpRuntime.AppDomainAppPath, UriKind.Absolute).MakeRelativeUri(new Uri(f, UriKind.Absolute)).ToString())
                .Where(f => f.EndsWith(".js") || f.EndsWith(".css") || f.EndsWith(".html"))
                .Where(f => !f.EndsWith(".min.js"))
                .Where(f => !f.EndsWith(".min.css"))
                .OrderBy(fn => !Regex.IsMatch(fn, @"_.*\..{2,4}$"))
                .ThenBy(fn => Regex.IsMatch(fn, @"~.*\..{2,4}$"))
                .ThenBy(fn => fn.Length)
                .ToArray();

            return files;
        }

        private static string GetFilePath(string filename)
        {
            var path =  Path.Combine(HttpRuntime.AppDomainAppPath, filename);
            return !File.Exists(path) ? null : path;
        }
    }
}