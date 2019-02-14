using System.Text.RegularExpressions;

namespace OctoGhast.Framework.Data.Loading {
    public class EntityNamespacing {
        public static readonly Regex NamespaceFormat =
            new Regex(@"^(?<type>[a-zA-Z]+)::(?<id>[a-zA-Z]+)", RegexOptions.ECMAScript | RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Attempt to convert a qualified ID into it's constituent parts.
        /// If the name lacks a :: separator, it is assumed to be an unqualified reference with no type.
        /// </summary>
        /// <param name="qualifiedName"></param>
        /// <returns></returns>
        public static (string type, string id) TransformQualifiedId(string qualifiedName)
        {
            if (!(NamespaceFormat.Match(qualifiedName) is Match match) || !match.Success)
                return (null, qualifiedName);

            return (match.Groups["type"].Value, match.Groups["id"].Value);
        }
    }
}