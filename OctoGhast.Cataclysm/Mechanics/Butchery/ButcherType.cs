namespace OctoGhast.Cataclysm.Loaders.Terrain {
    /// <summary>
    /// Butchering can yield various types of items based on the action being undertaken.
    /// Some actions remove items from the possible pool of drops,
    /// For example, Quartering if performed before Skinning will remove any chance of retrieving hides/furs.
    /// </summary>
    public enum ButcherType {
        Skin,
        Flesh,
        Offal,
        Bones,

    }
}