using System;
using System.Collections.Generic;

namespace OctoGhast.Units {
    /// <summary>
    /// Core materials required by the system, can be replaced at runtime with alternate definitions.
    /// </summary>
    public static class CoreMaterials {
        private static Dictionary<string, Material> materialMap { get; } = new Dictionary<string, Material>()
        {
            ["air"] = new Material()
            {
                Name = "Air",
                Id = "core_air"
            },
            ["water"] = new Material()
            {
                Name = "Water",
                Id = "core_water",
                Density = 1.0f,
            }
        };

        public static Material Air { get; } = materialMap["air"];

        public static Material Water { get; } = materialMap["water"];

        public static void Update(params Material[] newDefinitions) {
            foreach (var material in newDefinitions) {
                if (material.Name is null)
                    throw new ArgumentNullException($"Supplied material ({material.Id ?? "unknown"}) has no name");

                if (materialMap.ContainsKey(material.Name)) {
                    materialMap[material.Name] = material;
                }
            }
        }
    }
}