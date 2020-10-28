using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OctoGhast.Framework;

namespace OctoGhast.Cataclysm.Loaders.Item {

    /*
      {
       "type" : "terrain",
       "id" : "t_grass",
       "name": "grass",
       "description": "A beautiful, if unkempt, section of hardy Kentucky bluegrass.  Cutting the grass short enough would destroy the root system, causing this area to turn into a patch of dirt.",
       "symbol": ".",
       "color": "green",
       "move_cost": 2,
       "flags": ["TRANSPARENT", "DIGGABLE", "FLAT"],
       "bash": {
        "sound": "thump",
        "ter_set": "t_null",
        "str_min": 40,
        "str_max": 100,
        "str_min_supported": 100,
        "bash_below": true
        }
       }
     */

    public class TerrainType : TemplateType {
        public override string NamespaceName { get; } = "terrain";
        
        [LoaderInfo("name")]
        public string Name { get; set; }

        [LoaderInfo("description")]
        public string Description { get; set; }

        [LoaderInfo("symbol")]
        public string Symbol { get; set; }

        public int MoveCost { get; set; }

        public IEnumerable<string> Flags { get; set; }

        [LoaderInfo("bash", TypeLoader = typeof(TerrainBashDataLoader))]
        public TerrainBashData BashData { get; set; }
    }

    public class TerrainBashData {
        /// <summary>
        /// Sound to emit when bashing this terrain.
        /// </summary>
        [LoaderInfo("sound")]
        public string Sound { get; set; }

        /// <summary>
        /// What this terrain will become after it is destroyed through bashing.
        /// </summary>
        [LoaderInfo("ter_set")]
        public string TerrainResult { get; set; }

        /// <summary>
        /// Minimum strength required to do any damage
        /// </summary>
        [LoaderInfo("str_min")]
        public float MinimumStrength { get; set; }

        /// <summary>
        /// Maximum strength required to do any damage
        /// </summary>
        [LoaderInfo("str_max")]
        public float MaximumStrength { get; set; }

        /// <summary>
        /// Minimum strength required to damage this when supported from beneath.
        /// </summary>
        [LoaderInfo("str_min_supported")]
        public float MinimumStrengthSupported { get; set; }

        /// <summary>
        /// Allow to bash this terrain from beneath it.
        /// </summary>
        [LoaderInfo("bash_below")]
        public bool CanBashFromBelow { get; set; }
    }

    public class TerrainBashDataLoader : TypeLoader, ITypeLoader<TerrainBashData> {
        public TerrainBashData Load(JObject jObj, TerrainBashData existing = default(TerrainBashData)) {
            throw new System.NotImplementedException();
        }

        public object Load(JObject data, object existing, LoaderInfoAttribute info = null) {
            return Load(data, existing as TerrainBashData);
        }
    }
}