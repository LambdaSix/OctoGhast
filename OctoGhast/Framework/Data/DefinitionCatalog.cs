using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OctoGhast.Framework.Data {
    /// <summary>
    /// Mutable build-time registry. Conflict policy is explicit: Add rejects
    /// duplicates and Replace is the opt-in hook for profile-owned overrides.
    /// </summary>
    public sealed class DefinitionRegistryBuilder<TDefinition> where TDefinition : class, IContentDefinition {
        private readonly Dictionary<DefinitionId, TDefinition> _definitions =
            new Dictionary<DefinitionId, TDefinition>();
        private bool _frozen;

        public int Count => _definitions.Count;

        public void Add(TDefinition definition) {
            EnsureMutable();
            Validate(definition);
            if (_definitions.ContainsKey(definition.Id)) {
                throw new ArgumentException("A definition with ID '" + definition.Id + "' is already registered.", nameof(definition));
            }
            _definitions.Add(definition.Id, definition);
        }

        /// <summary>
        /// Replace an existing definition only when the selected content
        /// profile has already resolved an authorized override.
        /// </summary>
        public void Replace(TDefinition definition) {
            EnsureMutable();
            Validate(definition);
            if (!_definitions.ContainsKey(definition.Id)) {
                throw new KeyNotFoundException("Cannot replace missing definition '" + definition.Id + "'.");
            }
            _definitions[definition.Id] = definition;
        }

        /// <summary>
        /// Freeze registry membership for a world/content generation. The ID
        /// is a caller-supplied stable compatibility fingerprint, not an index.
        /// </summary>
        public DefinitionCatalog<TDefinition> Freeze(string contentGenerationId) {
            EnsureMutable();
            if (string.IsNullOrWhiteSpace(contentGenerationId)) {
                throw new ArgumentException("A stable content generation ID is required.", nameof(contentGenerationId));
            }

            _frozen = true;
            return new DefinitionCatalog<TDefinition>(contentGenerationId,
                new ReadOnlyDictionary<DefinitionId, TDefinition>(
                    new Dictionary<DefinitionId, TDefinition>(_definitions)));
        }

        private void EnsureMutable() {
            if (_frozen) throw new InvalidOperationException("The definition registry has already been frozen.");
        }

        private static void Validate(TDefinition definition) {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!definition.Id.IsValid) throw new ArgumentException("Definitions must have a valid stable ID.", nameof(definition));
        }
    }

    /// <summary>
    /// Read-only definition lookup for one immutable content generation.
    /// Definition implementations are expected to be immutable value graphs.
    /// </summary>
    public sealed class DefinitionCatalog<TDefinition> where TDefinition : class, IContentDefinition {
        private readonly IReadOnlyDictionary<DefinitionId, TDefinition> _definitions;

        public string ContentGenerationId { get; }
        public int Count => _definitions.Count;
        public IEnumerable<TDefinition> Definitions => _definitions.Values;

        internal DefinitionCatalog(string contentGenerationId,
            IReadOnlyDictionary<DefinitionId, TDefinition> definitions) {
            ContentGenerationId = contentGenerationId;
            _definitions = definitions;
        }

        public bool TryGet(DefinitionId id, out TDefinition definition) => _definitions.TryGetValue(id, out definition);

        public TDefinition GetRequired(DefinitionId id) {
            if (_definitions.TryGetValue(id, out var definition)) return definition;
            throw new KeyNotFoundException("Definition '" + id + "' is not present in content generation '" + ContentGenerationId + "'.");
        }
    }
}
