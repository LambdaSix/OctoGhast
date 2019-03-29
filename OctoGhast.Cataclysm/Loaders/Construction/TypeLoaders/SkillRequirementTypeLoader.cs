using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.Loaders.Item;

namespace OctoGhast.Cataclysm.Loaders.Construction.TypeLoaders {
    public class SkillRequirementTypeLoader : TypeLoader, ITypeLoader<IEnumerable<SkillRequirement>> {
        public IEnumerable<SkillRequirement> Load(JObject jObj, IEnumerable<SkillRequirement> existing = default) {
            if (jObj.TryGetValue("required_skills", out var skills)) {
                foreach (var skill in skills) {
                    if (skill.Type == JTokenType.Array) {
                        yield return new SkillRequirement(skill as JArray);
                    }
                }
            }
        }

        public object Load(JObject data, object existing) {
            return Load(data, existing as IEnumerable<SkillRequirement>);
        }
    }
}