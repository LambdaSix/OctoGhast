using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OctoGhast.Extensions;

namespace OctoGhast {
    /// <summary>
    /// Generic info that belongs to all items.
    /// </summary>
    public class GenericItemComponent : ItemComponent {
        public string Id { get; set; }
        public IEnumerable<string> Flags { get; set; }
        public IEnumerable<(string Name,int Level)> Qualities { get; set; }
        public IEnumerable<string> Techniques { get; set; }
        public IEnumerable<string> Materials { get; set; }

        public bool HasFlag(string flagName) {
            return Flags.Contains(flagName);
        }

        public (bool, int Level) HasQuality(string qualityName) {
            var quality = Qualities.Where(s => s.Name == qualityName).ToList();
            if (quality.Any()) {
                return (true, quality.Single().Level);
            }
            return (false,-1);
        }

        public bool HasTechnique(string techniqueName) => Techniques.Contains(techniqueName);

        public static GenericItemComponent Deserialize(JObject data) {
            return new GenericItemComponent()
            {
                Id = data.GetValueOr("id", default(string)),
                Flags = data.GetArray<string>("flags").ToList(),
                Qualities = data.GetArrayOfPairs("qualities",
                    (name, level) => (name.Value<string>(), level.Value<int>())).ToList(),
                Techniques = data.GetArray<string>("techniques").ToList(),
                // TODO: This needs to be processed & linked to the Materials loader
                Materials = data.GetArray<string>("material"),
            };
        }
    }
}