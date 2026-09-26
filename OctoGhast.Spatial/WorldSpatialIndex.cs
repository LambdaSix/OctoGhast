using System;
using System.Collections.Generic;
using System.Linq;

namespace OctoGhast.Spatial
{
    /// <summary>
    /// Authoritative, world-owned index of resident entities in the currently published active
    /// regions. Region activation stages membership externally and publishes it once; multiple player
    /// leases for an overlapping region must not duplicate entries. Durable inactive-entity storage
    /// is outside this index. All mutations are serialized and become visible atomically.
    /// </summary>
    public sealed class WorldSpatialIndex<TEntityId>
    {
        private readonly object gate = new object();
        private readonly ISpatialCellMapper cellMapper;
        private readonly IComparer<TEntityId> idComparer;
        private readonly SortedDictionary<TEntityId, WorldPosition> positions;
        private readonly Dictionary<SpatialCell, SortedSet<TEntityId>> entitiesByCell = new Dictionary<SpatialCell, SortedSet<TEntityId>>();

        /// <param name="cellMapper">Pure, deterministic profile mapping from position to cell.</param>
        /// <param name="idComparer">Stable total order; comparer equality also defines ID uniqueness.</param>
        public WorldSpatialIndex(ISpatialCellMapper cellMapper, IComparer<TEntityId> idComparer)
        {
            if (cellMapper == null) throw new ArgumentNullException("cellMapper");
            if (idComparer == null) throw new ArgumentNullException("idComparer");
            this.cellMapper = cellMapper;
            this.idComparer = idComparer;
            positions = new SortedDictionary<TEntityId, WorldPosition>(idComparer);
        }

        public int Count
        {
            get { lock (gate) return positions.Count; }
        }

        /// <summary>Atomically registers a stable entity id in the active world index.</summary>
        /// <returns>false if that id is already present; the existing record is unchanged.</returns>
        public bool TrySpawn(TEntityId entityId, WorldPosition position)
        {
            ValidateId(entityId);
            lock (gate)
            {
                if (positions.ContainsKey(entityId)) return false;
                var cell = cellMapper.GetCell(position);
                AddToCell(entityId, cell);
                positions.Add(entityId, position);
                return true;
            }
        }

        /// <summary>
        /// Atomically moves an entity only if it is still at expectedPosition. This compare-and-move
        /// contract lets command handlers reject stale requests without overwriting a newer move.
        /// </summary>
        public bool TryMove(TEntityId entityId, WorldPosition expectedPosition, WorldPosition destination)
        {
            ValidateId(entityId);
            lock (gate)
            {
                WorldPosition current;
                if (!positions.TryGetValue(entityId, out current) || current != expectedPosition) return false;
                if (current == destination) return true;

                var oldCell = cellMapper.GetCell(current);
                var newCell = cellMapper.GetCell(destination);
                if (oldCell != newCell)
                {
                    // Allocate/add the destination membership before removing the source. Any
                    // mapper/allocation failure therefore leaves the published source intact.
                    AddToCell(entityId, newCell);
                    RemoveFromCell(entityId, oldCell);
                }
                positions[entityId] = destination;
                return true;
            }
        }

        /// <summary>
        /// Atomically unregisters a resident entity only if its current position matches
        /// expectedPosition. Persistence/world identity deletion is owned by the caller.
        /// </summary>
        public bool TryDespawn(TEntityId entityId, WorldPosition expectedPosition)
        {
            ValidateId(entityId);
            lock (gate)
            {
                WorldPosition current;
                if (!positions.TryGetValue(entityId, out current) || current != expectedPosition) return false;
                RemoveFromCell(entityId, cellMapper.GetCell(current));
                positions.Remove(entityId);
                return true;
            }
        }

        public bool TryGetPosition(TEntityId entityId, out WorldPosition position)
        {
            ValidateId(entityId);
            lock (gate) return positions.TryGetValue(entityId, out position);
        }

        /// <summary>Returns a stable comparer-sorted snapshot of ids in the given cell.</summary>
        public IReadOnlyList<TEntityId> GetEntitiesAt(SpatialCell cell)
        {
            lock (gate)
            {
                SortedSet<TEntityId> entities;
                if (!entitiesByCell.TryGetValue(cell, out entities)) return new TEntityId[0];
                return entities.ToArray();
            }
        }

        /// <summary>
        /// Returns a stable, de-duplicated snapshot across the supplied cells. The caller defines
        /// region/radius geometry by enumerating cells; overlapping queries cannot duplicate entities.
        /// </summary>
        public IReadOnlyList<TEntityId> GetEntitiesInCells(IEnumerable<SpatialCell> cells)
        {
            if (cells == null) throw new ArgumentNullException("cells");
            lock (gate)
            {
                var result = new SortedSet<TEntityId>(idComparer);
                foreach (var cell in cells)
                {
                    SortedSet<TEntityId> entities;
                    if (entitiesByCell.TryGetValue(cell, out entities)) result.UnionWith(entities);
                }
                return result.ToArray();
            }
        }

        /// <summary>Returns a stable snapshot of every currently indexed id and position.</summary>
        public IReadOnlyDictionary<TEntityId, WorldPosition> Snapshot()
        {
            lock (gate)
                return new SortedDictionary<TEntityId, WorldPosition>(positions, idComparer);
        }

        private void AddToCell(TEntityId entityId, SpatialCell cell)
        {
            SortedSet<TEntityId> entities;
            if (!entitiesByCell.TryGetValue(cell, out entities))
            {
                entities = new SortedSet<TEntityId>(idComparer);
                entitiesByCell.Add(cell, entities);
            }
            entities.Add(entityId);
        }

        private void RemoveFromCell(TEntityId entityId, SpatialCell cell)
        {
            SortedSet<TEntityId> entities;
            if (!entitiesByCell.TryGetValue(cell, out entities) || !entities.Remove(entityId))
                throw new InvalidOperationException("Spatial index membership invariant was violated.");
            if (entities.Count == 0) entitiesByCell.Remove(cell);
        }

        private static void ValidateId(TEntityId entityId)
        {
            if (ReferenceEquals(entityId, null)) throw new ArgumentNullException("entityId");
        }
    }
}
