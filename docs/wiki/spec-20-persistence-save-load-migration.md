# Spec 20 — Persistence, save/load, world state and migration

Status: implementation-ready specification  
Parent issue: #65  
Spec issue: #85  
Reference implementation: `LambdaSix/Cataclysm-DDA`  
Pinned baseline: `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Purpose and compatibility decision

This document specifies the persistence contract OctoGhast needs for behavioral feature parity with the pinned Cataclysm:DDA baseline. It defines state ownership, save-set composition, stable identities, transaction behavior, load ordering, migrations, corruption handling, and conformance tests.

**Compatibility decision:** byte-for-byte or direct read/write compatibility with arbitrary CDDA save directories is **not required** for the first OctoGhast implementation. Behavioral and state-model parity is required. The architecture MUST, however, keep import/export adapters possible: persisted domain state must use stable IDs and explicit versions rather than CLR type names, object addresses, or implementation-specific reflection metadata. A later compatibility slice may add a pinned-baseline CDDA importer without redesigning the domain model.

This choice separates two concerns:
1. faithfully preserving the same observable game state and lifecycle semantics; and
2. reproducing CDDA's historical on-disk representation, including legacy formats.

The first is mandatory. The second is an explicit future compatibility feature.

## 2. Reference evidence

At the pinned baseline the authoritative persistence behavior is distributed across several source areas rather than one serializer.

| Reference area | Evidence / responsibility |
|---|---|
| `src/savegame.cpp` | Main game snapshot serialization/deserialization, global `savegame_version`, `savegame_loading_version`, overmap loading/migrations, global trackers, avatar, active monsters, EOC queues, variables/messages. |
| `src/game.cpp` / `src/game.h` | Save/load orchestration, game lifecycle hooks, reality-bubble/map transitions and save invocation. |
| `src/mapbuffer.cpp` / `src/mapbuffer.h` | Persistent submap storage; mapbuffer explicitly represents storage/buffering/saving of the world map and can save then evict submaps. |
| `src/overmap.cpp`, `src/overmapbuffer.cpp` | Overmap serialization, regional state, NPC/world actor storage, mapgen decisions and migration/fixup behavior. |
| `src/worldfactory.cpp` / `src/worldfactory.h` | World directory metadata, world options/mod selection and world lifecycle. |
| `src/cata_io.h` | Shared JSON archive conventions used by serializable domain objects. |
| `src/path_info.cpp` | Save/config path layout; world options and timestamp filenames. |
| Domain serializers such as `avatar`, `character`, `item`, `npc`, `monster`, `vehicle`, `mission`, `faction` | Entity-owned state and nested object graphs. |

Observed baseline facts that constrain OctoGhast:

- `savegame_version == 40`.
- The main save JSON writes `savegame_loading_version`, current turn/calendar anchors, current map position, view state, scent, active monsters, trackers, player/avatar state, queued/inactive EOCs, global variables, messages and unique NPC bookkeeping.
- Loading establishes calendar and dimension context before loading the active map, then restores active/local runtime state and the avatar.
- The overmap loader contains migrations and explicitly order-dependent reads. Persisted data therefore cannot be treated as independent DTO fields with arbitrary load order.
- Map persistence is a separate mapbuffer concern; loaded tactical state is only a window over persistent submaps.
- CDDA keeps backwards-compatibility logic in readers and uses a detected save version globally while nested entities deserialize.
- Invalid or obsolete IDs can be migrated, substituted, diagnosed, or otherwise repaired during loading depending on the domain.

These are behavioral constraints, not a requirement to reproduce the C++ class layout.

## 3. Persistence model

### 3.1 Save set, not save file

A world save is a **versioned save set** containing independently addressable persistence units. OctoGhast MUST model these logical units:

- world manifest and world options;
- content/mod manifest sufficient to identify the data environment used by the save;
- global game/session state;
- one player/avatar state record per playable character/save slot as applicable;
- tactical submap records;
- overmap/region records;
- off-bubble NPC and monster/world-actor records where ownership requires it;
- vehicle state under its owning spatial record;
- mission/faction/global narrative state;
- queued events/EOCs and script variables;
- auxiliary player-facing state that affects observable continuation, such as messages where retained by parity requirements.

The physical implementation MAY combine logical units into fewer files or a database, but APIs and transactions MUST preserve these ownership boundaries.

### 3.2 Snapshot identity

Every completed save operation produces a monotonically increasing `SnapshotId` within a world. All persistence units committed by that operation MUST identify the same snapshot or a compatible previous snapshot according to the transaction rules below.

A world manifest MUST include at least:

- `formatVersion`;
- `worldId` (stable UUID);
- `snapshotId`;
- game/content compatibility version;
- selected mod/content IDs and ordering;
- current character/save-slot ID;
- current game time;
- last successful save timestamp;
- schema/features flags needed to select readers/migrations.

Do not serialize assembly-qualified .NET type names as durable contracts.

## 4. Ownership boundaries and invariants

### World manifest / options
Owns world identity, selected content set, world-level options, save format metadata and current committed snapshot pointer. It does not own tactical entities.

### Global game state
Owns calendar anchors/current turn, dimension/current active-map origin, global variables, global EOC/event queues, global trackers/achievements required for gameplay, and other singleton simulation state.

### Character/avatar
Owns intrinsic character state: stable character ID, stats, anatomy/body state, needs/effects, skills/progression, inventory/worn/wielded item roots, activities, character-local variables and references to external entities by stable ID only.

### Items
Items are persisted by their current owner: character inventory, map tile/submap, vehicle cargo/part, NPC inventory, etc. Nested pocket/container contents are serialized recursively as part of the owning root. An item MUST have one persistence owner at commit time.

### Tactical map/submaps
A submap is the durable unit for local terrain/furniture/traps/fields/items and other tile state established by Spec 12. Submap coordinates are stable keys. Loading the reality bubble MUST NOT change persistence ownership merely because a submap is cached.

### Vehicles
A vehicle is spatially owned by the persistent map/submap partition that contains its canonical origin/ownership record. Cross-submap footprint data must not create duplicate vehicle identities. References use stable vehicle IDs.

### Monsters
Active monsters in the reality bubble may be represented in the active game snapshot as CDDA does; off-bubble creatures belong to persistent spatial/world records. The save coordinator MUST guarantee that a creature is committed exactly once during active/off-bubble transitions.

### NPCs
NPC identity is global and stable. Their authoritative state may move between active and overmap/world ownership, but a save transaction MUST produce one authoritative record per NPC ID. Relationship/mission/faction references use IDs.

### Missions and factions
Mission/faction state is world-scoped. Missions use stable mission IDs; actor references are stable IDs and resolved after entity materialization.

### Events, EOCs and script state
Persist queued execution time, EOC/content ID, serialized context variables and global script variables. Function pointers/delegates are forbidden in persistence. Content IDs are resolved against the loaded registry.

## 5. Stable identity and references

Durable identity types MUST be explicit value types:

- `WorldId`: UUID.
- `CharacterId` / `NpcId`: stable opaque identifier.
- `CreatureId` where a persistent monster reference is required.
- `VehicleId`: stable opaque identifier.
- `MissionId`: stable opaque identifier.
- `ItemUid`: only required for items that can be externally referenced; ordinary contained items may remain structurally owned.
- typed content IDs (terrain, item type, effect, EOC, faction template, etc.) remain string-backed IDs governed by Spec 18.
- spatial identities use the absolute coordinate types from Spec 12.

Rules:

1. Object references MUST NOT be serialized as memory addresses, runtime hash codes or collection indexes.
2. References to content definitions serialize typed string IDs.
3. References to runtime entities serialize stable runtime IDs.
4. Loaders MUST permit forward references and resolve them during a fixup phase.
5. Missing required references fail the owning record with a diagnostic; missing optional references are cleared with a diagnostic unless a migration supplies a replacement.
6. IDs are never silently regenerated during normal load. Regeneration is a named repair/migration operation.

## 6. Save transaction semantics

### 6.1 Save barrier

Manual save, autosave and quit-save enter a save barrier at a safe game-loop boundary. The coordinator MUST NOT serialize while a turn mutation is partially applied.

A save barrier:
1. finishes the currently atomic action/turn phase;
2. prevents new simulation mutation from beginning;
3. captures the current logical snapshot;
4. flushes dirty persistence units;
5. commits the new manifest last;
6. releases the barrier.

Long-running activities do **not** need to complete. Their resumable state is persisted. A save may therefore occur between activity turns but not halfway through one atomic activity step.

### 6.2 Atomic commit

A failed save MUST leave the previous committed snapshot loadable.

Required implementation behavior:

1. write each changed unit to a temporary/new-generation location;
2. flush/close and validate serialization success;
3. write a new manifest referencing the completed generation;
4. atomically replace/switch the manifest pointer where the platform permits;
5. only after commit, garbage-collect superseded generations.

If the platform cannot provide atomic rename, use a journal/two-manifest protocol with deterministic recovery.

Never overwrite the only known-good unit in place before the new unit is durable.

### 6.3 Dirty tracking

Submaps/overmaps and other partitioned units SHOULD be dirty-tracked. Saving may skip unchanged units, but the manifest/snapshot protocol must make mixed-generation reads explicit and safe.

### 6.4 Autosave

Autosave uses the same transaction path as manual save. It is not a weaker serializer.

Autosave:
- runs only at a safe barrier;
- must not advance game time or consume moves;
- must not change RNG state merely because serialization occurred;
- reports failure non-destructively and leaves play able to continue when safe;
- must not clear dirty state until the corresponding unit is durably committed.

Manual save additionally returns a user-visible success/failure result. Quit-after-save occurs only after successful commit unless the user explicitly chooses to quit without saving.

## 7. Load pipeline and fixups

Load is a staged operation. Required ordering:

### Phase 0 — discovery and integrity
Read the world manifest, choose the latest complete committed snapshot, validate basic syntax/checksums if present, and reject unsupported future format versions before mutating live game state.

### Phase 1 — content environment
Load world options and the recorded mod/content set through the Spec 18 loader. Finalize registries before materializing persisted entities that reference content IDs.

If required content is absent, fail with an actionable diagnostic listing missing IDs/mods. A repair/import mode may substitute content, but normal load must not silently do so.

### Phase 2 — schema migration
Migrate each persistence unit from its stored schema version to the current in-memory schema. Migration is pure with respect to gameplay: it must not consume simulation RNG, moves or time.

### Phase 3 — world/global primitives
Materialize calendar/time anchors, dimension/world identity, global counters and coordinate context needed to interpret spatial records.

### Phase 4 — spatial state
Load the overmap/region containing the active area and the submaps needed for the reality bubble. Reconstruct terrain/furniture/fields/items/vehicles and spatial indexes.

This ordering reflects the pinned baseline, where the main game loader establishes time/dimension and loads the map before restoring later runtime state.

### Phase 5 — entities
Materialize avatar, NPCs, monsters and other actors. Create identity-map entries as each entity is constructed.

### Phase 6 — cross references
Resolve entity-to-entity, mission, faction, vehicle and other forward references. Rebuild derived indexes and ownership maps. References must resolve against the identity map, not by searching display names.

### Phase 7 — queued runtime state
Restore activities, event/EOC queues, script variables, trackers and other resumable runtime state.

### Phase 8 — derived caches
Rebuild non-authoritative caches: visibility/light/transparency, pathing caches, creature occupancy indexes, crafting caches, UI-derived state, etc. Caches SHOULD generally not be serialized unless rebuilding them changes observable behavior or is prohibitively expensive.

### Phase 9 — validation and publish
Run post-load invariants. Only after validation succeeds is the loaded world published as the live game state. A failed load MUST NOT leave a half-loaded world active.

## 8. Versioning and migration

### 8.1 Versions

Use two layers:

- a save-set `formatVersion` for global orchestration/layout;
- per-unit or per-schema versions where independent evolution is useful.

The current version is a positive integer. Readers support a documented range of older versions. Writers emit only the current version.

CDDA's pinned baseline uses global savegame version 40 and exposes the detected loading version to nested loaders. OctoGhast should preserve the capability while avoiding a single global conditional becoming the only migration mechanism.

### 8.2 Migration registry

Migrations are ordered transforms `N -> N+1`. Loading version N applies every transform until current.

Each migration MUST:
- be deterministic;
- be idempotent at the transaction/recovery boundary;
- not use gameplay RNG;
- preserve unknown extension data when the schema permits it;
- emit structured diagnostics for substitutions/dropped data;
- have fixture tests.

Skipping versions by ad-hoc branching is prohibited unless represented as an explicitly tested migration path.

### 8.3 Content-ID migrations

Content definition renames/obsoletions are distinct from save-schema versions. Use Spec 18's migration/obsoletion registry during materialization. Persisted typed IDs may be mapped from obsolete ID to replacement before validation.

If no replacement exists:
- required definition: fail normal load;
- optional definition: clear/substitute only under a documented domain rule and emit a warning.

### 8.4 Post-load fixups

Some migrations require complete graphs or spatial context. These run after materialization but before publish. Examples include rebuilding indexes, converting legacy coordinates, deduplicating ownership, or attaching formerly embedded entities to new aggregate roots.

Fixups MUST be version-gated, deterministic and tested.

## 9. Corruption and failure behavior

Classify load failures:

- **UnsupportedVersion** — save created by a newer unsupported format.
- **MalformedData** — invalid JSON/binary syntax or impossible primitive representation.
- **MissingContent** — required typed content ID cannot be resolved.
- **BrokenReference** — required runtime entity reference cannot be resolved.
- **InvariantViolation** — data parses but violates domain ownership/state rules.
- **IncompleteTransaction** — interrupted save generation exists without committed manifest.
- **IoFailure** — read/write/permission/storage failure.

Normal load behavior:
- recover automatically from an incomplete uncommitted generation by using the previous committed manifest;
- never merge arbitrary pieces from two snapshots unless the manifest explicitly declares unchanged prior-generation units;
- provide path/unit/entity context in diagnostics;
- preserve files for diagnosis;
- never overwrite a corrupt save merely by attempting to load it.

A separate repair tool may offer lossy recovery. Repair must create a new snapshot and retain the source save.

## 10. Serialization contracts

All durable DTOs MUST follow these rules:

- explicit member names and versions;
- culture-invariant numeric representation;
- explicit units for time/distance/energy where ambiguity is possible;
- enum values persisted by stable symbolic name or explicitly versioned numeric mapping;
- typed IDs persisted as strings;
- dictionaries with semantically unordered keys must not influence gameplay determinism;
- deterministic writer ordering SHOULD be used for golden tests and diffs;
- nullable/optional semantics must distinguish absent, null and default where migration behavior differs;
- unknown fields SHOULD be tolerated by readers within the same compatibility family unless strict validation is required;
- duplicate unique IDs are an invariant failure, not last-write-wins.

Persistence DTOs are adapters. Domain entities should not depend on filesystem paths or JSON APIs.

## 11. Proposed OctoGhast boundaries

```text
ISaveCoordinator
  SaveAsync(SaveReason)
  LoadAsync(WorldId, SnapshotSelector)

ISaveStore
  ReadManifest / WriteGeneration / CommitManifest
  EnumerateUnits / OpenRead / OpenWrite

IPersistenceCodec<T>
  Encode(T, PersistenceContext)
  Decode(PersistedNode, PersistenceContext)

IMigrationRegistry
  Migrate(unitType, fromVersion, targetVersion, node)

IIdentityMap
  Register(id, entity)
  Resolve<T>(id)

IPostLoadFixup
  Apply(LoadContext)

IContentIdResolver
  Resolve / MigrateObsolete
```

`PersistenceContext` carries format version, world/snapshot identity, content registry, diagnostics and identity-map access. It must not expose mutable gameplay RNG.

The domain-facing save coordinator depends on abstractions; JSON/file storage is one adapter. This allows tests to use an in-memory store and leaves room for future CDDA import/export adapters.

## 12. Cross-system requirements

### Spec 01 — time/game loop
Save barriers occur at deterministic game-loop boundaries. Save/load itself consumes no turns/moves and does not perturb RNG.

### Spec 12 — local map
Absolute coordinate and submap ownership contracts are authoritative. Loading/unloading the reality bubble is a cache/lifecycle operation, not a change in durable identity.

### Spec 18 — data loading
Persisted content IDs resolve only after registry finalization. Obsolete-ID mapping is shared, not reimplemented by each persistence codec.

### Spec 17 — EOC/events
Queued execution time, EOC ID and serializable context are durable. Runtime callable objects are reconstructed from IDs.

### Character/item/activity specs
Activities and nested item ownership must define their resumable state. Persistence stores that state without invoking gameplay behavior during decode.

### Overmap/NPC/vehicle specs
Each must nominate one persistence owner and stable runtime identity, with fixups for cross-partition references.

## 13. RNG and determinism

Saving MUST be observationally pure with respect to simulation RNG. Serialization order must not call random selection, lazy random initialization, or gameplay code with random side effects.

Loading MUST reconstruct saved RNG-relevant state where such state is part of the simulation. If OctoGhast uses a serializable PRNG stream, its state belongs in global game state. If the architecture instead uses deterministic scoped streams, their seeds/counters must be persisted sufficiently to make save/load continuation equivalent to uninterrupted play.

Required differential invariant:

> Running N turns, saving/loading, then running M turns with the same inputs produces the same observable state as running N+M turns uninterrupted, modulo explicitly non-gameplay metadata such as wall-clock save timestamp.

## 14. Security and resource limits

Treat save data as untrusted input.

Readers MUST impose sane limits on:
- nesting depth;
- collection/string sizes;
- decompressed unit size;
- coordinate ranges;
- entity counts per unit;
- migration expansion.

No persisted value may select an arbitrary CLR type, filesystem path, command, assembly or executable script. EOC/script references resolve only through registered content IDs.

## 15. Acceptance/conformance suite

### P20-01 Basic round trip
Create a world with non-default time/options, avatar state and global variables. Save, destroy all in-memory state, reload. Assert semantic equality and stable IDs.

### P20-02 Nested item graph
Persist inventory containing nested pockets/containers with charges, damage and variables. Reload and assert exact ownership tree with no duplicate item roots.

### P20-03 Spatial round trip
Mutate terrain/furniture/field/item state across multiple submaps including negative coordinates and z-levels. Save, evict all submaps, reload and assert Spec 12 golden state.

### P20-04 Vehicle cross-submap identity
Save a vehicle whose footprint crosses a submap boundary. Reload and assert one vehicle ID/object with all parts and cargo, not duplicate vehicles.

### P20-05 Active/off-bubble actor boundary
Move monster/NPC across the reality-bubble boundary, save at each side, reload, and assert exactly one authoritative entity with same ID/state.

### P20-06 Mission/faction references
Persist missions referencing avatar/NPC/faction entities. Reload and assert all references resolve to the canonical identity-map instances.

### P20-07 Activity resume
Save midway through a multi-turn activity. Reload and continue. Assert remaining work, consumed inputs, moves and final result equal uninterrupted execution.

### P20-08 EOC queue resume
Queue multiple EOCs at distinct times with context variables. Save/reload before execution. Assert order, trigger times and contexts are unchanged and no EOC fires during load.

### P20-09 RNG continuation
Compare uninterrupted N+M execution with N/save/load/M execution using identical inputs and initial seed. Assert gameplay-observable equivalence.

### P20-10 Autosave purity
Trigger autosave at a legal barrier. Assert no move/time/RNG change, same game state, and committed snapshot advances.

### P20-11 Interrupted write
Fault-inject after writing some new units but before manifest commit. Restart and assert previous snapshot loads with no partial new state.

### P20-12 Failed manifest commit
Fault-inject manifest replacement. Assert recovery chooses one complete snapshot deterministically and does not corrupt the previous manifest.

### P20-13 Unsupported future version
Increase fixture format version above supported. Load must fail before publishing state with `UnsupportedVersion` and an actionable diagnostic.

### P20-14 Sequential migration
Load fixtures at every supported old version. Assert N->current migration output equals the current canonical fixture and migration does not alter RNG/time.

### P20-15 Obsolete content ID
Fixture references a mapped obsolete terrain/item/EOC ID. Assert migration resolves the replacement and emits the expected diagnostic.

### P20-16 Missing required content
Remove a required mod/content definition. Normal load must fail with all discoverable missing IDs/mod IDs listed; no half-loaded world becomes active.

### P20-17 Broken optional reference
Fixture contains a missing optional reference covered by a documented domain rule. Assert it is cleared/substituted with warning and remaining state loads.

### P20-18 Duplicate runtime ID
Fixture contains two authoritative NPC/vehicle IDs that collide. Load fails with `InvariantViolation`; it must not silently renumber.

### P20-19 Corrupt unit
Truncate/corrupt one committed unit. Load fails with unit/path context, preserves files, and does not overwrite the save.

### P20-20 Dirty-unit incremental save
Modify one submap only. Save. Assert unchanged units may remain on prior generation, manifest references are coherent, and complete reload equals full-save semantics.

### P20-21 Save while activity exists
Request manual save between activity ticks. Assert it waits for the current atomic tick, commits resumable state, and does not force activity completion/cancellation.

### P20-22 Quit-save failure
Fault-inject storage failure during quit-save. Assert quit is not reported as safely saved and previous snapshot remains loadable.

### P20-23 Deterministic encoding
Serialize identical semantic state twice without intervening simulation. Canonical persisted representation is byte-identical except explicitly excluded metadata.

### P20-24 Load publish isolation
Fault-inject a late fixup failure. Assert the currently running/menu state is not replaced by a partially loaded world.

### P20-25 Pinned-CDDA semantic fixture
Construct a representative scenario in the pinned CDDA baseline containing avatar inventory, map mutation, vehicle, NPC, monster, mission/faction state and queued EOC. Record observable state before/after CDDA save/reload. Recreate the fixture in OctoGhast and assert the same state categories survive with equivalent behavior. This is semantic parity; direct CDDA file ingestion is not required.

## 16. Implementation sequence

1. Define persistence DTO/version conventions and stable runtime IDs.
2. Implement `ISaveStore` generation + manifest atomicity with fault-injection tests.
3. Implement world/global manifest and content-set bootstrap.
4. Implement Spec 12 submap persistence and active-map load integration.
5. Implement avatar/character + nested item graph.
6. Implement identity map and post-load fixup pipeline.
7. Add NPC/monster/vehicle ownership adapters.
8. Add mission/faction/global event/EOC state.
9. Add migration registry and obsolete-content-ID integration.
10. Add autosave/manual/quit-save game-loop hooks.
11. Add corruption diagnostics, repair-copy entry points and resource limits.
12. Add the full conformance suite and pinned-CDDA semantic fixtures.
13. Only after the native format is stable, consider a separate CDDA v40 import adapter.

## 17. Explicit non-goals for the first slice

- byte-identical CDDA save files;
- support for every historical CDDA save version;
- persisting derived rendering/pathfinding/UI caches;
- serializing executable code or CLR runtime type metadata;
- lossy automatic repair during normal load.

## 18. Definition of done

This specification is satisfied when:

- every major state category has one documented persistence owner;
- runtime and content identities are stable and reference-safe;
- save commit is recoverably atomic;
- load/migration/fixup ordering is explicit and implemented;
- autosave/manual save share the same correctness guarantees;
- existing CDDA file compatibility is explicitly scoped as optional import/export rather than a prerequisite for behavioral parity;
- round-trip, migration, corruption, interruption and deterministic-continuation tests P20-01 through P20-25 pass;
- evidence remains pinned to CDDA commit `e262adb299a7613b4aedc5f12c08fe0413c56a84`.
