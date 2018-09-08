using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using InfiniMap;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;

namespace OctoGhast {

    /// <summary>
    /// Responsible for re-hydrating a world from a save directory.
    /// </summary>
    public class WorldFactory {
        private bool _newWorld;

        public WorldFactory(string worldName = null) {
            if (worldName is null)
                _newWorld = true;
        }
    }

    public class WorldChunk {
        public int Version { get; set; }
        /// <summary>
        /// World co-ordinates.
        /// </summary>
        public Vec Coordinates { get; set; }

        // Median temperature for the chunk
        public int Temperature { get; set; }

        // The last turn index this chunk was touched, used for fast-forwarding chunks to the present
        public int TurnLastUpdated { get; set; }

        public IEnumerable<FurnitureReference> Furniture { get; set; }
        public IEnumerable<FieldReference> Fields { get; set; }
        public IEnumerable<ItemReference> Items { get; set; }
        public IEnumerable<RadiationReference> Radiation { get; set; }
        public IEnumerable<SpawnReference> Spawns { get; set; }
        public IEnumerable<TerrainReference> Terrain { get; set; }
        public IEnumerable<TrapReference> Traps { get; set; }
        public IEnumerable<VehicleReference> Vehicles { get; set; }

        public void Deserialize(JTokenReader reader) {
        }

        public void Serialize(JTokenWriter writer) {
            writer.WriteStartObject();
            writer.WritePropertyName("version");
            writer.WriteValue(25);

            writer.WritePropertyName("coordinates");
            writer.WriteStartArray();
            writer.WriteValue(1); // x
            writer.WriteValue(1); // y
            writer.WriteValue(0); // z
            writer.WriteEndArray();

            writer.WritePropertyName("turn_last_touched");
            writer.WriteValue(1922);

            writer.WritePropertyName("temperature");
            writer.WriteValue(11);
            writer.WriteEndObject();
        }
    }

    public class TypeId {
        public string TypeName { get; }

        public TypeId(string typeName) {
            TypeName = typeName;
        }

        // Method to turn a stringified typename into an instance of that type?
        // Need to consult the ItemFactory to see if it knows the string typename and it's mapped base-type, and any specific
        // implementation that's available.
        // "f_kiln" -> FurnitureType -> FurnitureKiln
        public TypeInfo CreateNew() => throw new NotImplementedException();
    }

    public class MapObjectReference {
        public Vec PositionVector { get; set; }
        public TypeId TypeId { get; set; }

        public virtual void Deserialize(JObject data) {}
    }

    /// <summary>
    /// Save game file format reference to a piece of furniture
    /// Simplified JSON format:
    /// [ 1,1,"f_kiln_full", { /* Serialized dictionary/property-bag */ } ]
    /// </summary>
    public class FurnitureReference : MapObjectReference {
        public Dictionary<string,object> Properties { get; set; }

        public FurnitureReference() : base() {
            
        }
    }

    /// <summary>
    /// Save game file format reference to a Field
    /// </summary>
    public class FieldReference : MapObjectReference {
        public Dictionary<string,object> Properties { get; set; }

        public FieldReference() : base() {
            
        }
    }

    public class ItemReference : MapObjectReference {
        public Dictionary<string,object> Properties { get; set; }

        public ItemReference() : base() {
            
        }
    }

    public class RadiationReference : MapObjectReference {

        /// <summary>
        /// Intensity of radiation flux in CPM (https://en.wikipedia.org/wiki/Counts_per_minute)
        /// </summary>
        public int Intensity { get; set; }

        public RadiationReference() {
            TypeId = new TypeId("Radiation");
        }
    }

    public class SpawnReference : MapObjectReference {
        public SpawnReference() : base() {
            
        }
    }

    public class TerrainReference : MapObjectReference {
        public TerrainReference() : base() {
            
        }
    }

    public class TrapReference : MapObjectReference {
        public TrapReference() : base() {
            
        }
    }

    public class VehicleReference : MapObjectReference {
        public VehicleReference() : base() {
            
        }
    }
}