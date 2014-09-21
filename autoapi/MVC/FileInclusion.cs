﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
            public string FullName { get; set; }
            public string Checksum { get; set; }
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
                    return string.Format("<script type=\"text/ng-template\" id=\"{0}\">", file.FullName);

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

        public static string Checksum(string input)
        {
            return string.Join("", MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(input)).Select(hb => hb.ToString("X2")));
        }

        private static string GetLinkTags(string filename)
        {
            var checksum = Checksum(LoadFile(filename).Source);

            switch (GetFileType(Extension(filename)))
            {

                case SourceType.Template:
                    return "";

                case SourceType.Javascript:
                    return string.Format("<script src='/{0}?rnd={1}'></script>", filename, checksum);

                case SourceType.CSS:
                    return string.Format("<link rel='stylesheet' href='/{0}?rnd={1}'/>", filename, checksum);

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

            throw new NotImplementedException(string.Format("Unknown file {0}", extension));
        }

        private static SourceFile LoadFile(string filename)
        {
            var minfname = filename
                .Replace(".js", ".min.js")
                .Replace(".css", ".min.css");

            var source =  File.ReadAllText(GetFilePath(minfname) ?? GetFilePath(filename));

            

            var checksum = Checksum(source);

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
                        PreserveImportantComments = false
                    });

                case SourceType.CSS:
                    return minifier.MinifyStyleSheet(file.Source, new CssSettings
                    {
                        CommentMode = CssComment.None
                    });

                case SourceType.Template:
                    return MinifyHtml(file);


                default:throw new NotImplementedException();
            }
        }

        private static string MinifyHtml(SourceFile file)
        {
            var s = file.Source;

            s = Regex.Replace(s, @"(?s)\s+(?!(?:(?!</?pre\b).)*</pre>)", " ");
            s = Regex.Replace(s, @"(?s)\s*\n\s*(?!(?:(?!</?pre\b).)*</pre>)", "\n");
            s = Regex.Replace(s, @"(?s)\s*\>\s*\<\s*(?!(?:(?!</?pre\b).)*</pre>)", "><");
            s = Regex.Replace(s, @"(?s)<!--((?:(?!</?pre\b).)*?)-->(?!(?:(?!</?pre\b).)*</pre>)", "");

            return s;
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