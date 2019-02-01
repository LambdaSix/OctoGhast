using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.LegacyLoader {
    public class SnippetCategory {
        // If there is only an ID, then it's a reference, not a full snippet.
        public bool IsReference => string.IsNullOrWhiteSpace(Text);

        [LoaderInfo("id")]
        public string Id { get; set; }

        [LoaderInfo("text")]
        public string Text { get; set; }
    }
}