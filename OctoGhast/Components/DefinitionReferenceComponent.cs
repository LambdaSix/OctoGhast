using Capsicum.Interfaces;
using System;
using OctoGhast.Framework.Data;

namespace OctoGhast.Components {
    /// <summary>
    /// Stable reference to immutable type/template data. Mutable instance
    /// state belongs in separate components and is never written into a definition.
    /// </summary>
    public sealed class DefinitionReferenceComponent : IComponent {
        public DefinitionId DefinitionId { get; }

        public DefinitionReferenceComponent(DefinitionId definitionId) {
            if (!definitionId.IsValid) throw new ArgumentException("A stable definition reference is required.", nameof(definitionId));
            DefinitionId = definitionId;
        }
    }
}
