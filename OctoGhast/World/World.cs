using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using InfiniMap;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ninject.Infrastructure.Language;
using OctoGhast.DataStructures.Lighting;
using OctoGhast.DataStructures.Map;
using OctoGhast.DataStructures.Renderer;
using OctoGhast.Entity;
using OctoGhast.Extensions.FastExpressionCompiler;
using OctoGhast.Renderer.View;
using OctoGhast.Spatial;
using OctoGhast.SystemManager;
using OctoGhast.UserInterface.Core;

namespace OctoGhast {


    /// <summary>
    /// Flags a class to be added to World::DataObjects.
    /// </summary>
    public class ServiceDataAttribute : Attribute {
        public string Name { get; }
        public string Description { get; }

        public ServiceDataAttribute(string name, string description) {
            Name = name;
            Description = description;
        }
    }

    public class WorldConfiguration {
        public Dictionary<string, JObject> Configuration;

        public WorldConfiguration(Dictionary<string, JObject> configuration) {
            Configuration = configuration;
        }

        public JObject this[string key] => Configuration[key];
    }

    /// <summary>
    /// Root container of all data for a game session.
    /// </summary>
    public class World {
        // The world instance
        // Should contain references to all game systems/managers/schedulers
        // World tick isn't related to UI draw tick
        public static World Instance { get; private set; }

        //public Dictionary<SystemPriority, ISystem> Systems { get; } = new Dictionary<SystemPriority, ISystem>();

        public Dictionary<Type, object> DataObjects { get; } = new Dictionary<Type, object>();

        public MessageSystem Messages { get; set; }

        // Default to a 16x16x32 chunk alignment for now
        public Map2D<ITile> Map { get; } = new Map2D<ITile>(16, 16);

        public WorldConfiguration Configuration { get; set; }

        public World(string worldFolder) {
            Configuration = LoadConfiguration(Path.Combine(worldFolder, "configuration.dat"));

            // Initialize all required systems.
            SetupSystems();
            SetupMechanics();
            LoadDataObjects();

            Instance = this;
        }

        public WorldConfiguration LoadConfiguration(string filePath) {
            if (!File.Exists(filePath)) {
                throw new FileNotFoundException($"Unable to locate/open {filePath}");
            }

            var configuration = JsonConvert.DeserializeObject<Dictionary<string, JObject>>(File.ReadAllText(filePath));
            return new WorldConfiguration(configuration);
        }

        public void SetupSystems() {
        }

        private void LoadDataObjects() {
            // TODO: Eventually just use a small DI framework?
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (var type in assembly.GetTypes().Where(s => s.IsClass())) {
                    if (type.GetCustomAttribute<ServiceDataAttribute>() is ServiceDataAttribute attr) {
                        if (Activator.CreateInstance(type) is IDataObject objInstance) {
                            objInstance.Deserialize(Configuration[type.Name]);
                            Insert(objInstance);
                            RegisterObjectInfo(type, attr.Name, attr.Description);
                        }
                        else {
                            throw new Exception($"Unable to create instance of {type.Name} as {nameof(IDataObject)}");
                        }
                    }
                }
            }
        }

        private void RegisterObjectInfo(Type type, string name, string description) {
        }

        public void SetupMechanics() {
        }

        public void Tick(UInt64 gameTime) {
            Calendar.Advance(1);            

            //throw new NotImplementedException();
        }

        /*
         * Functions relating to ServerDataObject, storage area for singleton-esque objects required.
         * Only allows Class objects, not value types.
         * One value per type.
         */

        public void Insert<T>(T obj) where T: class {
            if (!DataObjects.ContainsKey(typeof(T))) {
                DataObjects.Add(typeof(T), obj);
            }
        }

        private void Insert(Type type, object obj) {
            if (!DataObjects.ContainsKey(type)) {
                DataObjects.Add(type, obj);
            }
        }

        public bool Contains<T>() where T: class {
            return DataObjects.ContainsKey(typeof(T));
        }

        public T Retrieve<T>() where T: class {
            if (DataObjects.TryGetValue(typeof(T), out var value))
                return (T) value;

            return default;
        }

        // A helper method for setting the IsWalkable property on a Cell
        public void SetIsWalkable(int x, int y, bool isWalkable)
        {
            ITile cell = Map[x, y];
            cell.IsTransparent = true;
            cell.IsWalkable = true;
            cell.IsExplored = true;
        }

        public bool IsExplored(Vec position)
        {
            return Map[position.X, position.Y].IsExplored;
        }

        public bool IsWalkable(Vec position)
        {
            return Map[position.X, position.Y].IsWalkable;
        }

        public bool IsOpaque(Vec position)
        {
            return !Map[position.X, position.Y].IsTransparent;
        }

        public ITile this[int x, int y]
        {
            get { return Map[x, y]; }
            set { Map[x, y] = value; }
        }

        public ITile this[Vec pos]
        {
            get { return Map[pos.X, pos.Y]; }
            set { Map[pos.X, pos.Y] = value; }
        }

        /*
        // TODO: Move this elsewhere, it's a client side concern.

        // Returns true when able to place the Actor on the cell or false otherwise
        public bool SetActorPosition(Mobile actor, int x, int y)
        {
            // Only allow actor placement if the cell is walkable
            if (Map[x, y].IsWalkable)
            {
                // The cell the actor was previously on is now walkable
                SetIsWalkable(actor.Position.X, actor.Position.Y, true);
                // Update the actor's position
                actor.MoveTo(new Vec(x, y));

                // The new cell the actor is on is now not walkable
                SetIsWalkable(actor.Position.X, actor.Position.Y, false);
                // Don't forget to update the field of view if we just repositioned the player
                if (actor is Player)
                {
                    UpdatePlayerFieldOfView();
                }
                return true;
            }
            return false;
        }

        public void UpdatePlayerFieldOfView() {
            // Compute the field-of-view based on the player's location and awareness
            CalculateFov(Camera.ViewFrustum.Center, 8, (x, y) => new Vec(x, y).ToView(Camera.ViewFrustum));
        }

        public LightMap<TileLightInfo> CalculateFov(Vec viewCenter, int lightRadius, Func<int, int, Vec> translateFunc)
        {
            var lightMap = new LightMap<TileLightInfo>(_screenHeight, _screenWidth);

            // TODO: Loop a list of lights, calculate the FOV for each light then mix it's colour into the tile.

            ShadowCaster.ComputeFieldOfViewWithShadowCasting(viewCenter.X, viewCenter.Y, lightRadius,
                (x, y) => IsOpaque(new Vec(x, y)),
                (x, y) => {
                    var screenPos = translateFunc(x, y);
                    lightMap[screenPos].IsLit = true;
                    lightMap[screenPos].LightColor = new Color(128, 128, 128);
                });
            return lightMap;
        }
        */
    }
}