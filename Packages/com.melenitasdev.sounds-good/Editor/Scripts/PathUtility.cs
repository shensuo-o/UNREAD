using System;
using System.IO;

using System.Text.RegularExpressions;

namespace MelenitasDev.SoundsGood.Editor
{
    internal static class PathUtility
    {
        private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();
        private static readonly Regex MultiSlash = new Regex("/+", RegexOptions.Compiled);

        public static bool TrySanitizeDataRootPath (string input, out string sanitized, out string error)
        {
            sanitized = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(input))
            {
                error = "Path is empty.";
                return false;
            }

            string path = input.Trim().Replace("\\", "/");
            path = MultiSlash.Replace(path, "/");
            
            if (path.Contains(":/") || path.StartsWith("/") || path.StartsWith("~"))
            {
                error = "Path must be project-relative under Assets/.";
                return false;
            }

            if (!path.StartsWith("Assets/") && !path.StartsWith("Assets"))
                path = "Assets/" + path.TrimStart('/');

            if (!path.EndsWith("/"))
                path += "/";

            string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0 || segments[0] != "Assets")
            {
                error = "Path must start with Assets/.";
                return false;
            }
            
            for (int i = 1; i < segments.Length; i++)
            {
                string seg = segments[i];

                if (seg == "." || seg == "..")
                {
                    error = "Path cannot contain '.' or '..' segments.";
                    return false;
                }

                if (seg.IndexOfAny(InvalidPathChars) >= 0 ||
                    seg.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    error = $"Invalid characters in folder name: '{seg}'.";
                    return false;
                }
                
                if (!Regex.IsMatch(seg, @"^[a-zA-Z0-9 _\-]+$"))
                {
                    error = $"Folder '{seg}' contains unsupported characters.";
                    return false;
                }

                if (seg.Length > 60)
                {
                    error = $"Folder '{seg}' is too long.";
                    return false;
                }
            }

            if (path.Length > 200)
            {
                error = "Path is too long.";
                return false;
            }

            sanitized = string.Join("/", segments) + "/";
            return true;
        }
    }
}