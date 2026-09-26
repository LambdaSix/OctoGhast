namespace OctoGhast.Framework.Data {
    /// <summary>
    /// Read-only identity contract for a compiled immutable definition.
    /// Implementations should expose definition data through getters only.
    /// </summary>
    public interface IContentDefinition {
        DefinitionId Id { get; }
    }
}
