using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Activation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ninject.Planning.Bindings.Resolvers;
using OctoGhast.Entity;
using OctoGhast.Extensions;

namespace OctoGhast {
    public class TemplateLoader {
        public Dictionary<string, List<JObject>> Templates { get; set; } = new Dictionary<string, List<JObject>>();
        public Dictionary<string, ITemplateLoader> Loaders { get; set; } = new Dictionary<string, ITemplateLoader>();

        public void LoadFromPath(string path) {
            var typeDefs = new List<(string type, JObject itemDef)>();

            foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories)) {
                var fileHnd = new JsonTextReader(new StreamReader(File.OpenRead(file)));
                JToken container;
                try {
                    container = JToken.Load(fileHnd);
                }
                catch (JsonReaderException ex) {
                    // TODO: Replace this with a better exception type for pulling apart later
                    throw new Exception($"In file: {file} // {ex.Message}", ex);
                }

                typeDefs.AddRange(LoadObject(container, typeDefs));
            }

            var byType = typeDefs.GroupBy(s => s.type);
            if (!Templates.Any()) {
                Templates = byType.ToDictionary(grp => grp.Key, grp => grp.Select(s => s.itemDef).ToList());
            }
            else {
                // Probably loading a mod?
                // Throw the templates in, the loader handling the types can work out if there are duplicates we care about.
                foreach (var item in byType) {
                    Templates.Add(item.Key, item.Select(s => s.itemDef).ToList());
                }
            }
        }

        private IEnumerable<(string,JObject)> LoadObject(JToken container, List<(string type, JObject itemDef)> typeDefs) {
            foreach (var item in container.Children<JObject>()) {
                if (!item.ContainsKey("type")) // TODO: Capture the line numbers?
                    throw new Exception("Entity with missing 'type' information");

                var typeName = item.GetValue("type").Value<string>();
                yield return (typeName, item);
            }
        }

        private void SetupCoreLoadHandlers() {
            var assembly = Assembly.GetExecutingAssembly();
            var loaders = assembly.GetTypes().Where(s => s.IsAnsiClass)
                .Where(s => s.GetInterfaces().Contains(typeof(ITemplateLoader)));

            foreach (var loader in loaders) {
                var attribute = loader.GetCustomAttribute<TemplateLoaderAttribute>();
                if (attribute is null)
                    throw new Exception("Class inheriting from ITemplateLoader lacks TemplateLoaderAttribute");

                var instance = Activator.CreateInstance(loader) as ITemplateLoader;
                foreach (var type in attribute.LoadableTypes) {
                    RegisterHandler(type, instance);
                }
            }
        }

        public void RegisterHandler(string type, ITemplateLoader loader) {
            if (Loaders.ContainsKey(type))
                throw new Exception($"Attempted to register duplicate type loader for {type}");

            Loaders.Add(type, loader);
        }
    }

    public class TemplateLoaderAttribute : Attribute {
        public IEnumerable<string> LoadableTypes { get; set; }
        public TemplateLoaderAttribute(string type) {
            LoadableTypes = new[] {type};
        }

        public TemplateLoaderAttribute(params string[] types) {
            LoadableTypes = types.AsEnumerable();
        }
    }

    public interface ITemplateLoader {

        /// <summary>
        /// Load a single Type into a template
        /// </summary>
        void LoadTemplate(string type, JObject data);

        /// <summary>
        /// Check the consistency of loaded data
        /// </summary>
        bool CheckData();

        /// <summary>
        /// Finalize any data
        /// </summary>
        /// <returns></returns>
        bool FinalizeData();
    }

    // Base loader for just about all kinds of items
        [TemplateLoader("AMMO", "GUN", "ARMOR", "TOOL", "TOOLMOD", "TOOL_ARMOR", "BOOK", "CONTAINER", "ENGINE", "WHEEL",
            "FUEL", "GUNMOD", "MAGAZINE", "GENERIC", "BIONIC_ITEM")]
    public class ItemFactory : ITemplateLoader {
        private Dictionary<string, BaseItemTemplate> _items { get; set; } = new Dictionary<string, BaseItemTemplate>();
        private Dictionary<string, TypeInfo> Loaders { get; set; } = new Dictionary<string, TypeInfo>();

        /// <summary>
        /// Deferred items we need to try load again because they have a unsatisfied dependency (copy-from or looks-like)
        /// </summary>
        private List<(string, JObject)> _deferred = new List<(string, JObject)>();

        public ItemFactory() {
            RegisterLoader<BaseItemTemplate>("item_info");
            RegisterLoader<RangedDataTemplate>("ranged_info");
        }

        public void LoadTemplate(string type, JObject data) {
            /*
             * 1. Check for 'copy-from', if present:
             *  i. Search in the existing _items for the target object
             *  ii. If found, copy the existing one and start modifying
             *  iii. If not found, push it into _deferred and try again after loading everything else
             *
             * 2. Like TemplateLoader, we have a list of loaders, so use 'type' and pass 'data' off
             * 2b. If we did find a copy-from template, we pass that into the template loader and it will perform the copy
             * 3. Stash the returned BaseItemTemplate in _items
             *
             * 4. Process any templates in the deferred list as in 2 & 3
             */
            
            /*
             * Item("bow_self")
             * -> HasComponents("item", "melee", "armor", "ranged");
             */
        }

        public void RegisterLoader<T>(string type) {
            Loaders.Add(type, typeof(T).GetTypeInfo());
        }

        public bool LoadDefinition(string type, JObject data, out BaseItemTemplate template) {
            template = null;

            // If we're copying from an existing definition, go find it and return a copy
            if (data.TryGetValue("copy-from", out var tokenValue)) {
                if (_items.TryGetValue(tokenValue.Value<string>(), out var existing)) {
                    template = existing;
                    return true;
                }

                // Couldn't find an existing item, push it back to deferred.
                _deferred.Add((type, data));
                return false;
            }

            return true;
        }

        public bool FinalizeData() {
            throw new NotImplementedException();
        }

        public bool CheckData() {
            throw new NotImplementedException();
        }

        public BaseTemplate FindTypeId(string id) {
            return _items.TryGetValue(id, out var val)
                ? val
                : null;
        }
    }

    /// <summary>
    /// Defines the base template for all loadable data
    /// </summary>
    public class BaseTemplate {
        public string Type { get; set; }

        public BaseTemplate(JObject data) {
            Type = data.TryGetValue("type", out var value)
                ? value.Value<string>()
                : throw new Exception("Error loading type");
        }
    }

    public class BaseItemInfo {
        public string Id { get; set; }
        public IEnumerable<string> Flags { get; set; }
        public IEnumerable<(string Name,int Level)> Qualities { get; set; }
        public IEnumerable<string> Techniques { get; set; }

        public BaseItemInfo(JObject data) {
            Id = data.GetValueOr("id", default(string));
            Flags = data.GetArray<string>("flags").ToList();
            Qualities = data.GetArrayOfPairs("qualities",
                (name, level) => (name.Value<string>(), level.Value<int>())).ToList();
            Techniques = data.GetArray<string>("techniques").ToList();
        }

        public BaseItemInfo() { }

        public bool HasFlag(string flagName) {
            return Flags.Contains(flagName);
        }

        public (bool, int Level) HasQuality(string qualityName) {
            var qualitity = Qualities.Where(s => s.Name == qualityName).ToList();
            if (qualitity.Any()) {
                return (true, qualitity.Single().Level);
            }
            return (false,-1);
        }

        public bool HasTechnique(string techniqueName) => Techniques.Contains(techniqueName);
    }

    public class ItemInfo : BaseItemInfo {
        public int DamageMin = -1000;
        public int DamageMax = 4000;

        public string Name { get; set; }
        public string Description { get; set; }
        public string Symbol { get; set; }
        public string Color { get; set; }
        public string Category { get; set; }
        public int Weight { get; set; }
        public int Volume { get; set; }
        public IEnumerable<string> Materials { get; set; }

        /// <inheritdoc />
        public ItemInfo(JObject data) {
            Name = data.GetValueOr("name", default(string));
            Description = data.GetValueOr("description", default(string));
            Symbol = data.GetValueOr("symbol", default(string));
            Color = data.GetValueOr("color", default(string));
            Category = data.GetValueOr("category", default(string));
            Weight = data.GetValueOr("weight", 0);
            Volume = data.GetValueOr("volume", 0);
            // TODO: This needs to be processed & linked to the Materials loader
            Materials = data.GetArray<string>("material");
        }
    }

    public class MeleeInfo : BaseItemInfo {
        public int Cutting { get; set; }
        public int Bashing { get; set; }
        public IEnumerable<string> Techniques { get; set; }
        public int ToHit { get; set; }

        public MeleeInfo(JObject data) {
            Cutting = data.GetValueOr("cutting", 0);
            Bashing = data.GetValueOr("bashing", 0);
            Techniques = data.GetArray<string>("techniques");
            ToHit = data.GetValueOr("to_hit", 0);
        }
    }

    public class ArmorInfo : BaseItemInfo {
        public IEnumerable<string> Covers { get; set; }
        public int Coverage { get; set; }
        public int MaterialThickness { get; set; }
        public int Encumbrance { get; set; }
        public int Warmth { get; set; }
        public int EnvironmentalProtection { get; set; }

        public ArmorInfo(JObject data) {
            Covers = data.GetArray<string>("covers");
            Coverage = data.GetValueOr("coverage", 0);
            MaterialThickness = data.GetValueOr("material_thickness", 0);
            Encumbrance = data.GetValueOr("encumbrance", 0);
        }
    }

    public class RangedInfo : BaseItemInfo {
        public int ReloadNoiseVolume { get;set; }
        public string Skill { get; set; }
        public string Ammo { get; set; }
        public int RangedDamage { get; set; }
        public int Range { get; set; }
        public int Dispersion { get; set; }
        public int MagazineSize { get; set; }
        public int ReloadTime { get; set; }
        public int Durability { get; set; }

        public RangedInfo(JObject data) {
            ReloadNoiseVolume = data.GetValueOr("reload_noise_volume", 0);
            Skill = data.GetValueOr("skill", default(string));
            Ammo = data.GetValueOr("ammo", default(string));
            RangedDamage = data.GetValueOr("ranged_damage", 0);
            Range = data.GetValueOr("range", 0);
            Dispersion = data.GetValueOr("dispersion", 0);
            MagazineSize = data.GetValueOr("magazine_size", 0);
            ReloadTime = data.GetValueOr("reload_time", 0);
            Durability = data.GetValueOr("durability", 0);
        }
    }

    public class BaseItemTemplate : BaseTemplate {
        public Dictionary<Type,BaseItemInfo> Info { get; set; }

        public BaseItemTemplate(JObject data, BaseItemTemplate src) : base(data) { }

        public bool HasComponent<T>() where T: BaseItemInfo {
            return Info.ContainsKey(typeof(T));
        }

        public T GetComponent<T>() where T: BaseItemInfo {
            return Info.TryGetValue(typeof(T), out var value)
                ? value as T
                : default;
        }

        public void AddInfo<T>(T info) where T : BaseItemInfo {
            if (Info.ContainsKey(typeof(T))) {
                // TODO: Overwrite for now, maybe exception throw?
                Info[typeof(T)] = info;
            }
            else {
                Info.Add(typeof(T), info);
            }
        }
    }

    public class Item {
        public BaseItemTemplate Template { get; }
        public Item(BaseItemTemplate template) {
            Template = template;
        }

        public Item() {
            
        }

        public int Damage { get; set; }

        private Dictionary<string, Dictionary<string, string>> Tags { get; } =
            new Dictionary<string, Dictionary<string, string>>();

        /// <summary>
        /// Add a tag with no associated data
        /// </summary>
        /// <param name="tagName"></param>
        public void AddTag(string tagName) {
            if (Tags.ContainsKey(tagName)) {
                throw new Exception("Duplicate tag added");
            }

            Tags.Add(tagName, default);
        }

        /// <summary>
        /// Add data to a tag, fi the tag does not exist, it will be created and set
        /// with the initial data.
        /// </summary>
        public void AddTagData(string tagName, string dataKey, string dataValue) {
            if (Tags.ContainsKey(tagName)) {
                Tags[tagName].Add(dataKey, dataValue);
            }
            else {
                Tags.Add(tagName, new Dictionary<string, string>
                {
                    [dataKey] = dataValue
                });
            }
        }

        /// <summary>
        /// Checks if this Item has the requested tag, regardless of any data within
        /// </summary>
        public bool HasTag(string tagName) {
            return Tags.ContainsKey(tagName);
        }

        /// <summary>
        /// Get the data associated with a tag, or null
        /// </summary>
        public Dictionary<string, string> GetTagData(string tagName) {
            return Tags.TryGetValue(tagName, out var value) ? value : null;
        }

        public bool HasFlag(string flag) {
            return Template.GetComponent<BaseItemInfo>()?.HasFlag(flag) ?? default;
        }

        public int DamageLevel(int max) {
            if (Damage == 0 || max <= 0) {
                return 0;
            }
            if (ItemData.DamageMax <= 1) {
                return Damage > 0 ? max : Damage;
            }
            if (Damage < 0) {
                return -((max - 1) * (-Damage - 1) / (ItemData.DamageMax - 1) + 1);
            }

            return (max - 1) * (Damage - 1) / (ItemData.DamageMax - 1) + 1;
        }

        public ItemInfo ItemData => Template.Info.TryGetValue(typeof(ItemInfo), out var value)
            ? value as ItemInfo
            : null;

        // Covers GUN
        public bool IsRanged => Template.Info.ContainsKey(typeof(RangedInfo));
        public ItemRanged AsRanged() => this as ItemRanged;

        // Covers ARMOR
        public bool IsArmor => Template.HasComponent<ArmorInfo>();
        public ItemArmor AsArmor() => this as ItemArmor;

        // Covers GENERIC + item-inbuilt melee data
        public bool IsMelee => Template.HasComponent<MeleeInfo>();

        // IsTool, IsBook, IsModification, IsComestible, IsContainer, IsEngine, IsWheel, IsFuel, IsBionic
    }

    public class ItemRanged : Item {
        public RangedInfo Info => base.Template.Info[typeof(RangedInfo)] as RangedInfo;
        private ItemRanged() { }
    }

    public enum ArmorLayer {
        Underwear,
        Regular,
        Waist,
        Outer,
        Belted
    }

    public class ItemArmor : Item {
        public int Thickness => Info.MaterialThickness;
        public int Warmth => Info.Warmth;
        public int Coverage => Info.Coverage;

        public ArmorInfo Info => base.Template.Info[typeof(ArmorInfo)] as ArmorInfo;
        private ItemArmor() { }

        private int ResistType(DamageType type, bool toSelf) {
            // HACK: This entire type is a little messy because of Materials + paddin
            if (toSelf && (type == DamageType.Acid || type == DamageType.Heat))
                return Int32.MaxValue;

            if (type == DamageType.Acid || type == DamageType.Heat) {
                float resistSpecial = 0;
                foreach (var material in ItemData.Materials) {
                    // TODO: Material[] not String[]
                    resistSpecial += material.GetResistance(type);
                }
                resistSpecial /= ItemData.Materials.Count();

                var env = Info.EnvironmentalProtection;
                resistSpecial += env / 10.0f;
                return (int) Math.Round(resistSpecial);
            }

            int baseThickness = Thickness;
            float resist = 0;
            float padding = 0;
            int effectiveThickness = 1;

            // This should probably be replaced with looking up the Leather & Kevlar Materials
            // and apply their thickness
            int leatherBonus = HasTag("leather_padded") ? baseThickness : 0;

            // Replace with the Material system. But basically Kevlar guards more against CUT (bullets deal CUT)
            var kevlarMult = HasTag("kevlar_padding")
                ? (type == DamageType.Cut)
                    ? 2
                    : 1
                : 0;
           
            int kevlarBonus = baseThickness * kevlarMult;
            padding += leatherBonus + kevlarBonus;

            // Bash
            int dmg = DamageLevel(4);
            int effectiveDamage = toSelf ? Math.Min(dmg, 0) : Math.Max(dmg, 0);
            effectiveThickness = Math.Max(1, Thickness - effectiveDamage);

            // Apply material bonuses:
            foreach (var material in ItemData.Materials) {
                // TODO: Process ItemData.Materials as a Material[] rather than String[]
                resist += material.GetResistance(type);
            }
            resist /= ItemData.Materials.Count();

            var result = (int) Math.Round(resist * effectiveThickness + padding);
            if (type == DamageType.Stab) {
                return (int) (0.8f * result);
            }

            return result;
        }

        public int DamageResist(DamageType type, bool toSelf) {
            switch (type) {
                case DamageType.Debug:
                case DamageType.Biological:
                case DamageType.Electric:
                case DamageType.Cold:
                    return toSelf ? Int32.MaxValue : 0; // ???
                case DamageType.Bash:
                case DamageType.Cut:
                case DamageType.Acid:
                case DamageType.Stab:
                case DamageType.Heat:
                    return ResistType(type, toSelf);
            }
        }

        public virtual int GetWarmth() {
            int furBonus = 0;
            int woolBonus = 0;

            int baseWarmth = Warmth;
            // TODO: var bonus = GetTagData("warmthBonus"); (item.AddTagData("warmthBonus", "furred", "35");)
            // Mod thing?
            if (HasTag("furred")) {
                furBonus = (35 * Coverage) / 100;
            }
            if (HasTag("wooled")) {
                woolBonus = (20 * Coverage) / 100;
            }

            return baseWarmth + furBonus + woolBonus;
        }

        public virtual ArmorLayer GetLayer() {
            // Should this be a mod thing?
            if (HasFlag("SKINTIGHT")) {
                return ArmorLayer.Underwear;
            }
            if (HasFlag("WAIST")) {
                return ArmorLayer.Waist;
            }
            if (HasFlag("OUTER")) {
                return ArmorLayer.Outer;
            }
            if (HasFlag("BELTED")) {
                return ArmorLayer.Belted;
            }

            return ArmorLayer.Regular;
        }
    }

    public class RangedDataTemplate : BaseItemTemplate {
        public RangedDataTemplate(JObject data, BaseItemTemplate src) : base(data,src) {

        }
    }

    public class ArmorTemplate : BaseItemTemplate {
        public ArmorTemplate(JObject data, BaseItemTemplate src) : base(data,src) {

        }
    }

    public class ToolTemplate : BaseTemplate {
        public ToolTemplate(JObject src) : base(src) {

        }
    }

    public class ToolModTemplate : BaseTemplate
    {
        public ToolModTemplate(JObject src) : base(src)
        {

        }
    }

    [Obsolete("Migrate IUSE handling into ArmorTemplate")]
    public class ToolArmorTemplate : BaseTemplate {
        public ToolArmorTemplate(JObject src) : base(src) {

        }
    }

    public class BookTemplate : BaseTemplate {
        public BookTemplate(JObject src) : base(src) {

        }
    }
}