using OctoGhast.DataStructures.Map;
using OctoGhast.Spatial;

namespace OctoGhast.DataStructures.Lighting
{
    public interface ILightSource
    {
        /// <summary>
        /// Source of the light.
        /// </summary>
        Vec Origin { get; set; }

        /// <summary>
        /// Directional angle for the light
        /// </summary>
        float Angle { get; set; }

        /// <summary>
        /// How many cells of light this emits.
        /// </summary>
        int Intensity { get; set; }

        /// <summary>
        /// Type of the light source. 
        /// </summary>
        LightSourceType Type { get; set; }
    }

    public class LightSource : ILightSource
    {
        public Vec Origin { get; set; }
        public float Angle { get; set; }
        public int Intensity { get; set; }
        public LightSourceType Type { get; set; }
    }
}