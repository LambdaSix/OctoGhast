using Capsicum.Interfaces;
using System;
using OctoGhast.Framework.Ecs;

namespace OctoGhast.Components {
    /// <summary>
    /// Durable authoritative identity. Never derive this from Capsicum's
    /// recyclable Entity instance, CreationIndex, or a client object ID.
    /// </summary>
    public sealed class EntityIdentityComponent : IComponent {
        public EntityId Id { get; }

        public EntityIdentityComponent(EntityId id) {
            if (!id.IsValid) throw new ArgumentException("A durable entity identity is required.", nameof(id));
            Id = id;
        }
    }
}
