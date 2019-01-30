using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using OctoGhast.Cataclysm.LegacyLoader;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.RecipeLoader {
    public class RecipeQuality {
        public string QualityName { get; set; }
        public int Level { get; set; }

        public RecipeQuality(JObject jObj) {
            QualityName = jObj["id"].Value<string>();
            Level = jObj["level"].Value<int>();
        }

        public RecipeQuality(string qualityName, int level) {
            QualityName = qualityName;
            Level = level;
        }
    }

    public class RecipeTool {
        public string ToolName { get; set; }
        public int Charges { get; set; }
        public bool UseList { get; set; }

        public RecipeTool(JObject jObj) {
            if (jObj.Type == JTokenType.Array) {
                var jArr = jObj.Children();
                ToolName = jArr[0].Value<string>();
                Charges = jArr[1].Value<int>();

                if (jObj.Count > 2) {
                    UseList = jArr[2].Value<string>().ToUpperInvariant() == "LIST";
                }
            }
        }

        public RecipeTool(string toolName, int charges, bool useList) {
            ToolName = toolName;
            Charges = charges;
            UseList = useList;
        }
    }

    public class RecipeComponent {
        public string ComponentItemType { get; set; }
        public int Quantity { get; set; }

        /// <summary>
        /// Indicates that <see cref="ComponentItemType"/> defines a Material List, rather than a specific item
        /// </summary>
        public bool UseMaterialList { get; set; }

        /// <summary>
        /// Indicates that the component should always be consumed on crafting failure.
        /// </summary>
        public bool ConsumeOnFail { get; set; }

        public RecipeComponent(JToken jObj) {
            // TODO: 

            if (jObj.Type == JTokenType.Array) {
                var jArr = jObj as JArray;

                if (jArr == null)
                    throw new Exception($"???!");

                ComponentItemType = jArr[0].Value<string>();
                Quantity = jArr[1].Value<int>();

                // TODO: Support LIST and failure_consume ? :/
                if (jArr.Count() > 2) {
                    if (jArr[2].Type == JTokenType.String) {
                        UseMaterialList = jArr[2].Value<string>().ToLowerInvariant() == "list";
                    } else if (jArr[2].Type == JTokenType.Boolean) {
                        ConsumeOnFail = jArr[2].Value<bool>();
                    }
                }
            }
        }

        public RecipeComponent(string componentItemType, int quantity, bool useMaterialList) {
            ComponentItemType = componentItemType;
            Quantity = quantity;
            UseMaterialList = useMaterialList;
        }
    }

    public class RecipeType : TemplateType {
        [LoaderInfo("result")]
        public string Result { get; set; }

        [LoaderInfo("id_suffic")]
        public string IdSuffix { get; set; }

        [LoaderInfo("override")]
        public bool Override { get; set; }

        [LoaderInfo("category")]
        public string Category { get; set; }

        [LoaderInfo("subcategory")]
        public string SubCategory { get; set; }

        [LoaderInfo("skill_used")]
        public IEnumerable<string> SkillsUsed { get; set; }

        [LoaderInfo("skills_required")]
        public Dictionary<string, int> SkillsRequired { get; set; }

        [LoaderInfo("reversible")]
        public bool Reversible { get; set; }

        /// <summary>
        /// Automatically learned upon gaining required skills
        /// </summary>
        [LoaderInfo("autolearn")]
        public bool AutoLearn { get; set; }

        /// <summary>
        /// Automatically learned upon gaining listed skills
        /// </summary>
        [LoaderInfo("autolearn")]
        public Dictionary<string,int> AutoLearnSkills { get; set; }

        [LoaderInfo("difficulty")]
        public int Difficulty { get; set; }

        [LoaderInfo("time")]
        public TimeDuration Time { get; set; }

        [LoaderInfo("book_learn")]
        public Dictionary<string, int> BookLearn { get; set; }

        [LoaderInfo("qualities")]
        public IEnumerable<RecipeQuality> Qualities { get; set; }

        [LoaderInfo("tools")]
        public IEnumerable<RecipeTool> Tools { get; set; }

        [LoaderInfo("components", TypeLoader = typeof(RecipeComponentTypeLoader))]
        public IEnumerable<IEnumerable<RecipeComponent>> Components { get; set; }

        /// <inheritdoc />
        public override string GetIdentifier() {
            return $"{Id ?? (Result + IdSuffix)}";
        }
    }

    public class RecipeComponentTypeLoader : TypeLoader, ITypeLoader<IEnumerable<IEnumerable<RecipeComponent>>> {
        /// <inheritdoc />
        public IEnumerable<IEnumerable<RecipeComponent>> Load(JObject jObj, IEnumerable<IEnumerable<RecipeComponent>> existing = default) {
            if (jObj.TryGetValue("components", out var components)) {
                if (components.Children().Any()) {
                    var subArray = components.Children();
                    foreach (var list in subArray) {
                        var emitList = new List<RecipeComponent>();
                        foreach (var item in list) {
                            emitList.Add(new RecipeComponent(item));
                        }

                        yield return emitList;
                    }
                }
            }

            yield break;
        }

        /// <inheritdoc />
        public object Load(JObject data, object existing) {
            return Load(data, existing as IEnumerable<IEnumerable<RecipeComponent>>).ToList();
        }
    }
}