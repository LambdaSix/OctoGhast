using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OctoGhast.Entity;

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
        private Dictionary<string, BaseItemTemplate> _items { get; set; } = new Dictionary<string, BaseTemplate>();
        private Dictionary<string, TypeInfo> Loaders { get; set; } = new Dictionary<string, TypeInfo>();

        /// <summary>
        /// Deferred items we need to try load again because they have a unsatisfied dependency (copy-from or looks-like)
        /// </summary>
        private List<(string, JObject)> _deferred = new List<(string, JObject)>();

        public ItemFactory() {
            RegisterLoader<AmmoItemTemplate>("AMMO");
            RegisterLoader<GunItemTemplate>("GUN");
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
            if (LoadDefinition(type, data, out var itemDef)) {
                if (Loaders.TryGetValue(type, out var value)) {
                    Activator.CreateInstance(value.AsType(), BindingFlags.CreateInstance, data, itemDef);
                }
            }
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

    public class Quality {
        public string Id { get; }
        public string Name { get; }

        // TODO: Usages implied from qualities

        public Quality(string id, string name) {
            Id = id;
            Name = name;
        }
    }

    public class Material {
        /*
         {
           "type": "material",
           "ident": "aluminum",
           "name": "Aluminum",
           "density": 10,
           "bash_resist": 0,
           "cut_resist": 4,
           "acid_resist": 4,
           "fire_resist": 2,
           "elec_resist": 0,
           "chip_resist": 10,
           "repaired_with": "material_aluminium_ingot",
           "dmg_adj": [ "dented", "bent", "smashed", "shattered" ],
           "bash_dmg_verb": "dented",
           "cut_dmg_verb": "scratched"
         },
         */
        public string Identity { get; set; }
        public string Name { get; set; }
    }

    public class BaseItemTemplate : BaseTemplate {
        // All the freaking properties
        public string ID { get; set; } = null;

        public string Name { get; set; }
        public string NamePlural { get; set; }

        public string LooksLike { get; set; }
        public string SnippetCategory { get; set; }
        public string Description { get; set; }
        public string DefaultContainer { get; set; }

        public Dictionary<Quality, int> ToolQualities { get; set; }
        public Dictionary<string,string> Properties { get; set; }

        public List<Material> Materials { get; set; }

        public Dictionary<string, Action> UseMethods { get; set; }

        public BaseItemTemplate(JObject data, BaseItemTemplate src) : base(data) {
            
        }
    }

    public class FakeMechanic {
        public void Process(IEnumerable<Item> items) {
            foreach (var item in items) {
                switch (item) {
                    case AmmoItem ammo:
                        ammo.GetAmmoDescription();
                        break;
                }

                if (item is GunItem gun) {
                    gun.Fire();
                }
            }
        }
    }

    public class Item {
        public BaseItemTemplate Template { get; }
        public Item(BaseItemTemplate template) {
            Template = template;
        }

        public bool IsActive { get; set; }

        public Material GetPrimaryMaterial() => Template.Materials.FirstOrDefault();
        public string GetDescription() => Template.Description;

        public int DamageLevel(int max) {
            throw new NotImplementedException();
        }

        public virtual void Deactivate(IMobile mob, bool alert) {
            if (!IsActive) {
                return;
            }
        }

        public virtual void Activate() {
            if (IsActive) {
                return;
            }
        }

        public virtual void Reload() {

        }

        public virtual void Unload() {

        }

        public virtual void Damage() {

        }

        public virtual IEnumerable<ItemInfo> GetItemInfo(dynamic ItemInfoQuery, int itemCount) {
            // TODO: This should handle all the 'base' properties on Item
            throw new NotImplementedException();
        }
    }

    public class ItemInfo {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Separator { get; set; }
        public string Value { get; set; }
        public bool ValueIsNumber { get; set; }
        public decimal ValueDecimal { get; set; }
        public bool NewLine { get; set; }
        public bool LowerIsBetter { get; set; }
        public bool DrawName { get; set; }

        /// <inheritdoc />
        public ItemInfo(string type, string name, string separator = "", string value = "", bool newLine = true, bool lowerIsBetter = false, bool drawName = true) {
            Type = type;
            Name = name;
            Separator = separator;
            Value = value;
            NewLine = newLine;
            LowerIsBetter = lowerIsBetter;
            DrawName = drawName;

            if (Decimal.TryParse(value, out var res)) {
                ValueIsNumber = true;
                ValueDecimal = res;
            }
        }
    }

    public class AmmoItem : Item {
        public new AmmoItemTemplate Template { get; }
        public AmmoItem(AmmoItemTemplate template) : base(template) {
            Template = template;
        }

        public AmmoType GetAmmoType() => Template.AmmoType;
        public string GetAmmoDescription() => base.Template.Description + GetAmmoType().ToString();

        /// <inheritdoc />
        public override IEnumerable<ItemInfo> GetItemInfo(dynamic itemInfoQuery, int itemCount) {
            foreach (var item in base.GetItemInfo(null, itemCount)) {
                yield return item;
            }

            // Start yielding out our items
        }
    }

    public class ArmorItem : Item {
        /// <inheritdoc />
        public ArmorItem(ArmorTemplate template) : base(template) { }
    }

    public class AmmoType {
        public string Type { get; }

        public AmmoType(string type) {
            Type = type;
        }

        /// <inheritdoc />
        public override string ToString() {
            return Type;
        }
    }
    public class AmmoItemTemplate : BaseItemTemplate {
        public AmmoType AmmoType { get; set; }

        public AmmoItemTemplate(JObject data, BaseItemTemplate src) : base(data, src) {
            // Load specific Ammo properties
        }
    }

    public class GunItemTemplate : BaseItemTemplate {
        public GunItemTemplate(JObject data, BaseItemTemplate src) : base(data,src) {

        }
    }

    public class GunItem : Item {
        /// <inheritdoc />
        public GunItem(GunItemTemplate template) : base(template) { }

        public void Fire() {
            throw new NotImplementedException();
        }

        public void Reload(AmmoType type, long amount) {
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