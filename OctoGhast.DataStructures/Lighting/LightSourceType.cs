namespace OctoGhast.DataStructures.Lighting
{
    /// <summary>
    /// The kind of light source.
    /// - Point emits a omni-directional light outwards from Origin
    /// - Spot emits a directed light from Origin in the specified direction
    /// </summary>
    public enum LightSourceType {
        Point,
        Spot
    }
}