# Spec 20 — Persistence, save/load, world state and migration

Status: implementation-ready specification; authoritative-server/co-op architecture reviewed  
Parent issue: #65  
Spec issue: #85  
Reference implementation: `LambdaSix/Cataclysm-DDA`  
Pinned baseline: `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Purpose and compatibility decision

This document specifies the persistence contract OctoGhast needs for reliable authoritative-world round trips while using the pinned Cataclysm:DDA baseline as the initial reference profile and completeness benchmark. It defines state ownership, save-set composition, stable identities, transaction behavior, load ordering, migrations, corruption handling, and conformance tests without making Cataclysm's on-disk categories or integer/grid spatial representation permanent Core requirements.

**Compatibility decision:** byte-for-byte or direct read/write compatibility with arbitrary CDDA save directories is **not required** for the first OctoGhast implementation. Behavioral and state-model parity is required. The architecture MUST, however, keep import/export adapters possible: persisted domain state must use stable IDs and explicit versions rather than CLR type names, object addresses, or implementation-specific reflection metadata. A later compatibility slice may add a pinned-baseline CDDA importer without redesigning the domain model.

This choice separates two concerns:
1. faithfully preserving the same observable game state and lifecycle semantics; and
2. reproducing CDDA's historical on-disk representation, including legacy formats.

The first is mandatory for the Cataclysm reference profile. The second is an explicit future compatibility feature.

### 1.1 Architectural classification used by this specification

Persistence requirements are classified into four layers and MUST NOT be conflated:

1. **Pinned CDDA persistence behaviour/evidence** — what the pinned baseline demonstrably saves, restores, migrates and orders.
2. **Cataclysm-profile persisted state and compatibility semantics** — the OctoGhast state/categories needed to reproduce that reference behaviour, including grid/map/submap/overmap semantics and Cataclysm content compatibility.
3. **Generic Core persistence infrastructure** — profile-neutral world identity, stable runtime identity/reference restoration, atomic snapshots, versioning/migrations, deterministic continuation, partitioning, schema ownership and profile/content-generation metadata.
4. **Future evolution seams** — profile-owned serializers/state schemas, alternative deterministic `WorldPosition` representations, alternative partition schemes and new state categories that reuse the same Core transaction/identity architecture.

A future rules profile MUST be able to persist authoritative state with a different deterministic spatial/state schema without replacing the save coordinator, snapshot transaction, stable identity map, migration pipeline or deterministic-continuation contracts.

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

A world save is a **versioned save set** containing independently addressable persistence units. Generic Core requires a manifest plus profile-owned persistence units; it does **not** require every profile to expose Cataclysm's map/submap/overmap categories.

Generic Core MUST support logical units for:

- world manifest and world options/configuration;
- rules-profile identity/version and content-generation compatibility metadata;
- authoritative global/world state;
- durable world-local player/control associations;
- profile-defined partition/state units;
- authoritative entity/object-graph state;
- deterministic scheduler/activity/event/RNG continuation state;
- profile-defined auxiliary durable state that affects observable continuation.

For the **Cataclysm profile**, the profile-defined units include the pinned/reference categories already investigated: tactical submaps, overmap/region records, off-bubble/world actors, vehicles under their spatial ownership, missions/factions/narrative state, queued EOCs/script variables, avatar/character state and parity-required messages/auxiliary state.

The physical implementation MAY combine logical units into fewer files or a database, but APIs and transactions MUST preserve declared ownership boundaries and schema/version identity.

### 3.2 Snapshot identity

Every completed save operation produces a monotonically increasing `SnapshotId` within a world. All persistence units committed by that operation MUST identify the same snapshot or a compatible previous snapshot according to the transaction rules below.

A world manifest MUST include at least:

- `formatVersion`;
- `worldId` (stable UUID);
- `snapshotId`;
- rules profile ID and profile schema/version;
- content-generation identity and compatibility metadata sufficient to interpret durable definition IDs;
- selected package/mod/content IDs and ordering where the active profile uses them;
- stable player/character roster and control-binding records required to resume the world (never connection/socket IDs);
- canonical simulation tick/time and world chronology anchors;
- last successful save timestamp;
- schema/features flags needed to select readers/migrations.

Do not serialize assembly-qualified .NET type names as durable contracts.

## 4. Ownership boundaries and invariants

### World manifest / options
Owns world identity, selected content set, world-level options, save format metadata and current committed snapshot pointer. It does not own tactical entities.

### Global game state
Owns the authoritative server's canonical simulation tick/time, world chronology anchors, deterministic scheduler state, global variables, global EOC/event queues, global trackers/achievements required for gameplay, and other singleton simulation state. There is one world clock regardless of connected-player count. Host wall-clock time, render time and client interpolation time are not durable simulation state.

### Player identity and controlled character
A durable `PlayerId` identifies a player/account/slot within the world and is distinct from (a) transient connection/socket/session identity and (b) the stable `CharacterId` of the entity currently controlled. A persisted player record owns only durable gameplay associations and player-specific knowledge/settings that affect continuation. Control is an explicit durable relationship `PlayerId -> CharacterId` when a character is assigned; reconnecting establishes a new connection and rebinds it to the same `PlayerId`, then resumes control of the authoritative character if policy permits.

### Character/avatar
Owns intrinsic character state: stable character ID, stats, anatomy/body state, needs/effects, skills/progression, inventory/worn/wielded item roots, activities, character-local variables and references to external entities by stable ID only. The character remains server/world-owned whether or not its player is connected.

### Items
Items are persisted by their current owner: character inventory, map tile/submap, vehicle cargo/part, NPC inventory, etc. Nested pocket/container contents are serialized recursively as part of the owning root. An item MUST have one persistence owner at commit time.

### Profile-defined spatial/world partitions

Generic Core owns the ability to persist deterministic profile-defined world partitions and their stable keys/metadata; it does not mandate grid cells, submaps, overmaps, integer coordinates or any particular partition size.

For the **Cataclysm profile**, a submap is the durable unit for local terrain/furniture/traps/fields/items and other tile state established by Spec 12, and submap/overmap coordinates remain stable profile-defined keys. Loading an active region/reality-bubble view MUST NOT change persistence ownership merely because a Cataclysm submap is cached.

### Vehicles
A vehicle is spatially owned by the persistent map/submap partition that contains its canonical origin/ownership record. Cross-submap footprint data must not create duplicate vehicle identities. References use stable vehicle IDs.

### Monsters
Pinned CDDA may serialize active monsters with the local active game snapshot, but OctoGhast MUST NOT define persistence ownership by one avatar's reality bubble. Creatures are server/world-owned and persisted exactly once by their canonical entity/spatial ownership record. Server active-region membership is runtime lifecycle state and may change without changing durable identity or creating duplicate records.

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
- `ItemUid`: every runtime Cataclysm item instance has a persistent UID per Spec 05 §3.2, including ordinary/nested contained items. Structural ownership determines where it is serialized, not whether it has identity. Generic Core persists each domain's declared identity requirements and need not assign IDs to every immutable value.
- typed content IDs (terrain, item type, effect, EOC, faction template, etc.) remain string-backed IDs governed by Spec 18.
- spatial identity/state is owned by the active rules profile: generic persistence requires a deterministic, serialization-stable `WorldPosition` representation/codec where position is durable, while derived `SpatialCell` membership may be persisted or rebuilt according to profile schema; Cataclysm uses Spec 12's typed grid coordinates and submap/overmap keys.

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
1. is entered by the authoritative server at a deterministic canonical tick boundary after the current atomic simulation phase;
2. stops admission/application of new world-mutating requests to the snapshot being captured (requests already received are either deterministically included before the barrier or remain queued for a later tick);
3. prevents new simulation mutation from beginning while the logical snapshot is captured;
4. captures one coherent world snapshot spanning every active region and all durable background/world partitions, independent of which clients currently subscribe to them;
5. flushes dirty persistence units;
6. commits the new manifest last;
7. releases the barrier and resumes request/tick processing.

Clients may remain connected during the barrier. They do not serialize their own authoritative world copies, and acknowledgements/replication packets are not part of the transaction.

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
Materialize world identity, canonical time/chronology anchors, global counters and any profile-defined context required to interpret persisted units. Generic Core MUST NOT assume this context includes integer/grid coordinate spaces.

### Phase 4 — profile-defined world/spatial state
Ask the active rules profile to materialize its persistent world partitions/state using the saved profile/schema metadata. Rebuild authoritative spatial indexes from authoritative `WorldPosition` plus the profile's deterministic `WorldPosition -> SpatialCell` mapping where such indexing exists. Initial active regions are then derived from connected/controlled players and server policy; no saved client/reality-bubble view is authoritative.

For the **Cataclysm profile**, this phase loads persistent overmap/submap partitions, reconstructs terrain/furniture/fields/items/vehicles and uses Spec 12's grid-aligned `WorldPosition` / derived `SpatialCell` semantics. CDDA's local active-map ordering remains evidence for state that must survive, but OctoGhast may have zero, one or many separated/overlapping active regions. Overlap is still one authoritative world region, not duplicated per player.

This preserves the pinned baseline's required load ordering without turning its coordinate or partition shape into a Core invariant.

### Phase 5 — entities
Materialize avatar, NPCs, monsters and other actors. Create identity-map entries as each entity is constructed.

### Phase 6 — cross references
Resolve entity-to-entity, mission, faction, vehicle and other forward references. Rebuild derived indexes and ownership maps. References must resolve against the identity map, not by searching display names.

### Phase 7 — queued runtime state
Restore activities, event/EOC queues, script variables, trackers and other resumable runtime state.

### Phase 8 — derived caches and projections
Rebuild non-authoritative caches: active-region membership, client interest subscriptions, per-connection replication baselines, visibility/light/transparency caches, pathing caches, creature occupancy indexes, crafting caches and UI-derived state. Per-player durable knowledge/remembered-map data is restored where gameplay requires it, but current FOV, socket subscriptions and "what this connection was last sent" are recomputed. Caches SHOULD generally not be serialized unless rebuilding them changes observable behavior or is prohibitively expensive.

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
- typed IDs use explicit stable versioned codecs; Cataclysm content definition IDs persist as strings per Spec 18. Runtime opaque IDs and another profile's definition keys need not share the Cataclysm string schema; ephemeral registry indexes remain forbidden.
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
Save barriers occur at deterministic canonical tick boundaries. Persist canonical tick/time, world chronology, actor action budgets, scheduler/deadline state, resumable activities and every deterministic/RNG stream seed/counter/state required for continuation. Save/load itself consumes no ticks/moves and does not perturb RNG. Host elapsed-time accumulators used only to pace catch-up are not persisted; after load the server resumes from the saved canonical time under a fresh host-clock baseline.

### Spec 12 — local map
Consume Spec 12's separation of authoritative `WorldPosition`, derived `SpatialCell` membership and client-only presentation transforms. Generic Core persistence must not require `WorldPosition` to be an integer/grid tuple. The Cataclysm profile persists its grid-aligned logical positions and submap/overmap partition keys for parity. Multi-player active regions remain server/world-owned runtime lifecycle state: separated regions may coexist and overlapping regions simulate once. Activation/deactivation and client interest are derived after load; persist only semantic world/profile state plus durable timestamps/deadlines needed for deterministic background catch-up.

### Specs 18/19 — profile and content-generation identity
Persisted definition IDs resolve only after registry finalization. Every world save identifies the active rules profile plus the immutable content generation/compatibility metadata required by Specs 18/19 to interpret those IDs. Session-local registry indexes are forbidden as durable identity. A materially different generation may load only after an explicit profile compatibility/migration decision; stable IDs must never be silently reinterpreted. Obsolete-ID mapping is shared, not reimplemented by each persistence codec.

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
- profile-defined spatial/state primitive ranges and encoded size;
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


## 15A. Authoritative-server/co-op persistence adaptation

This section is an **intentional OctoGhast adaptation**, not a claim about upstream CDDA multiplayer behavior.

### Authority and world-save ownership

The authoritative server is the sole writer/owner of world state in both deployment modes. "Single-player" means exactly one logical player connected to that server through the in-process transport; multiplayer changes connection count/transport, not persistence authority. Clients may persist local presentation preferences outside the world save, but cannot commit authoritative entities, time, RNG, maps, activities or narrative state.

A committed save represents the world, not a connected-client session. It remains loadable with zero clients connected and may later accept the same or different set of authorized players.

### Connection lifecycle

- **Connect:** authenticate/resolve a durable `PlayerId`; create a transient connection/session; bind it to the permitted `CharacterId`; derive interest/FOV/replication state from authoritative world state.
- **Disconnect:** destroy transport/session state only. Do not delete, clone or serialize a socket identity into the character. The controlled character remains authoritative world state.
- **Reconnect:** a new connection resolves the same durable `PlayerId`, re-establishes the control binding, and receives a fresh projection/snapshot. No old replication sequence/buffer state is resumed from disk.
- **Simultaneous duplicate control:** server policy MUST reject or explicitly transfer an existing control lease; two connections MUST NOT independently command one character.

### Disconnected player characters

Default OctoGhast policy is **world continuity**: disconnect does not pause the global clock and does not remove the character from the world. A disconnected player character remains an authoritative entity and continues to be affected by world simulation. It submits no new player intents while disconnected. An already-started activity may continue according to normal activity/interruption rules; autonomous defensive/AI behavior, if desired later, is a separate explicit gameplay policy and MUST NOT be invented by persistence.

If no active region would otherwise cover the disconnected character, the normal server active/background lifecycle applies. Deactivation must first externalize enough state/timestamps/deadlines for the same background catch-up semantics used by non-player world state. Reconnect activates the required region and catches it up before publishing a playable projection.

### Durable versus transient state

**Persist:** canonical world time/tick; chronology; actor action budgets and scheduling eligibility/deadlines; resumable activities; queued events/EOCs; authoritative entities/components; stable player and character IDs/control association; durable per-player knowledge/remembered-map state; world partitions; RNG state/seeds/counters needed for deterministic continuation; background-simulation timestamps/deadlines.

**Do not persist in the world save:** socket handles; connection IDs; authentication challenge/session tokens; packet sequence numbers; retransmit queues; network buffers; RPC/request objects; pending presentation acknowledgements; replication baselines; current interest subscriptions; current FOV result caches; render transforms; interpolation history; camera/UI state; host wall-clock timestamps used only for pacing.

An accepted gameplay request that has crossed the deterministic server admission boundary before the save barrier must be reflected either in committed authoritative state/command scheduling or in an explicitly durable deterministic command queue. Merely received transport bytes are not durable.

### Multi-region persistence

Persistent spatial state is world-partitioned by canonical coordinates/IDs, never by client interest. Multiple separated active regions and overlapping active regions are transient server lifecycle views over the same durable world. A save captures each authoritative entity/partition exactly once. Overlap MUST NOT duplicate state. Current subscriptions, FOV and interest sets are rebuilt after load.

Activation status itself is persisted only when it has semantic meaning beyond optimization. Normally the durable contract is the partition's authoritative state plus its last-simulated/catch-up anchors and scheduled deadlines, allowing the server to derive activation and perform background catch-up after load.

### Save quiescence with clients and requests

Save is server-coordinated and tick-consistent. At the barrier, simulation mutation is quiescent across all active regions. The server establishes a deterministic cut for command admission: commands applied through tick T are in the snapshot; commands not yet admitted remain outside it and are processed only after the barrier. Transport receive/send loops may continue buffering, but those buffers are not serialized.

If an implementation chooses a durable admitted-command queue, queue entries require stable player/entity IDs, canonical target tick/order keys and idempotency identifiers; socket/session references are forbidden. The first implementation MAY instead require the queue to be drained to the deterministic boundary before snapshot capture.

### One-player and multiplayer equivalence

For identical world seed/state and identical admitted command sequence, a one-player in-process server and a network server with one player MUST round-trip the same authoritative state. Adding another player changes only additional player/entity/world interactions and active-region coverage; it does not select a different save format, clock, ownership model or persistence path.

## 15B. Additional architecture-review conformance scenarios

### P20-26 One-player-server round trip
Run single-player through the in-process transport, save at canonical tick T, destroy server and client, reload server, reconnect the same `PlayerId`, and assert world/character/activity/scheduler/RNG state matches uninterrupted execution. Assert no transport identity is required to load.

### P20-27 Multiplayer separated-region round trip
Two players occupy separated active regions with independently mutated maps/entities. Save once, destroy all processes, reload, reconnect both in reverse order, and assert both regions and players restore from one coherent snapshot with no dependence on which player reconnects first.

### P20-28 Multiplayer overlapping-region deduplication
Two players share/overlap an active region containing the same vehicle, monster and item graph. Save/reload and assert each authoritative runtime ID is materialized exactly once and both players' projections reference the same resulting world state.

### P20-29 Visibility and interest are not authority
Give two players different FOV/knowledge in one area. Save/reload. Assert durable remembered knowledge required by gameplay is restored per player, while current FOV, replication subscriptions/baselines and presentation state are recomputed. No player gains another player's private knowledge because it was present in a server snapshot.

### P20-30 Disconnect/reconnect identity
Player A disconnects; its socket/session is destroyed while its character remains in-world. Advance time, save, reload, reconnect A with a new connection, and assert the same `PlayerId` controls the same `CharacterId` at the authoritative post-advance state. Assert old connection IDs/buffers are absent.

### P20-31 Disconnected activity continuation
Start a resumable activity, disconnect its player, advance according to normal active/background policy, save/reload, reconnect, and compare against uninterrupted server execution. Assert persistence neither cancels nor invents activity progress.

### P20-32 Save with concurrent clients and in-flight requests
With two clients sending commands, request a save. Record the deterministic admission boundary T. Assert all commands admitted through T are represented exactly once, later/unadmitted transport requests are not accidentally serialized as world state, and reload produces the same state as a reference execution cut at T.

### P20-33 Multi-region activation/background transition
With one player in region A and another in region B, disconnect/move so B deactivates, advance world time, save/reload while B is inactive, then reactivate B. Assert catch-up uses persisted semantic timestamps/deadlines and yields the same result as the documented background policy; no saved client interest set is needed.

### P20-34 Transport/presentation exclusion
Populate socket buffers, connection IDs, replication sequence state, interpolation transforms and camera/UI state, then save. Inspect the persistence DTO/save set and reload. Assert none of those values are present or required; authoritative integer/grid positions and simulation state are preserved.

### P20-35 Network/in-process persistence equivalence
Execute the same deterministic one-player command trace once over in-process transport and once over loopback network transport. Save at the same canonical tick. After excluding permitted non-gameplay metadata, assert semantically identical save state and identical continuation after reload.

### P20-36 Cataclysm grid-position round trip
Create Cataclysm-profile entities at representative positive, negative and z-level grid-aligned authoritative `WorldPosition` values spanning submap/overmap boundaries. Save/reload and assert positions, profile-defined partition ownership and derived `SpatialCell` membership round-trip exactly with no Godot/presentation transform involved.

### P20-37 Alternative Core test-profile position schema
Use a deterministic Core test profile whose authoritative `WorldPosition` is not an integer/grid tuple (for example a fixed-point pair plus a profile-defined region key). Save/reload through the same Core coordinator and assert exact semantic position/state equality. Assert no Cataclysm submap/overmap codec or integer-coordinate API is required.

### P20-38 Stable references independent of position representation
Create two runtime entities with stable IDs and a durable reference between them, move one under both the Cataclysm profile and the alternative test profile, then save/reload. Assert identity-map restoration resolves the same IDs/references independent of each profile's position representation and partition mapping.

### P20-39 Profile/content-generation compatibility metadata
Save a world with profile P, profile schema version V and content generation G. Reload with the identical compatible generation and succeed. Attempt load with a different profile, unsupported profile schema, or materially different content generation lacking an explicit compatibility/migration declaration and assert a structured compatibility failure before live-world publication.

### P20-40 World rollback does not roll back server-account state
Persist world snapshot W1, advance Spec 28 / #91 account meta-progression state, then load/rollback the world to W1. Assert the world-local `PlayerId`/world state rolls back while the server-scoped `AccountId` meta state remains at its independently committed revision and is neither overwritten nor duplicated by Spec 20.

### P20-41 One-player transport save equivalence
Run the same one-player authoritative command trace through in-process transport and network transport, save at the same canonical tick, and compare profile-owned world state, stable identities, scheduler/activity/RNG continuation and compatibility metadata. Excluding permitted non-gameplay metadata, saves are semantically equivalent.

### P20-42 Disconnected-player profile-neutral round trip
Disconnect a player, advance/save/reload with no socket/session state, then reconnect the same world-local `PlayerId` to the same `CharacterId`. Run under Cataclysm and the alternative Core test profile. Assert stable identity/control restoration does not depend on position schema or prior connection identity.

### P20-43 Save-barrier cut with in-flight requests
With multiple clients and profile-owned world state changing in different partitions, request a save while requests are in flight. Record the deterministic admission cut. Assert all admitted mutations through the cut are captured exactly once, unadmitted transport bytes are excluded, profile-specific position/state codecs observe one coherent snapshot, and reload matches a reference execution cut at that boundary.

### P20-44 Account-affecting save barrier ordering
Complete a Spec 28 account-affecting achievement, then request a world save while the derived account persistence mutation is still pending. Assert the world snapshot is not published as a successful post-completion snapshot until the already-issued account mutation has durably committed; if the account store fails, the world save fails/defers rather than embedding account state or falsely claiming a coherent durable cut. An account commit that precedes a later failed/rolled-back world save remains valid.

## 15C. Foundational Core/profile persistence re-evaluation

This re-evaluation preserves the completed pinned-CDDA investigation and authoritative-server/co-op review. It changes classification and extension boundaries, not the established save transaction semantics.

### Generic Core persistence responsibilities

Core owns and standardizes:

- stable world/runtime/player identity and reference restoration;
- object-graph materialization, forward-reference fixup and invariant validation;
- deterministic snapshot barriers and recoverably atomic commit;
- save-set and per-unit schema/version metadata;
- deterministic migration orchestration and diagnostics;
- canonical simulation time plus scheduler/activity/event/RNG continuation sufficient for deterministic resume;
- profile-neutral partition/unit storage and transaction participation;
- schema ownership/dispatch so profile codecs can evolve independently;
- active rules-profile identity/version and immutable content-generation compatibility metadata;
- exclusion of transport/session/Godot/presentation state from authoritative world saves.

Core does **not** require integer/grid `WorldPosition`, Cataclysm submaps/overmaps, CDDA save categories, JSON, one fixed partition size or one universal world-state schema.

### Cataclysm-profile persisted state and compatibility semantics

The Cataclysm profile owns the compatibility meaning of grid-aligned positions, map-square/submap/overmap keys, terrain/furniture/trap/field/item partition state, CDDA actor/vehicle/narrative/EOC categories, content/package semantics and any pinned save-order/fixup requirements documented above. Those remain mandatory for the Cataclysm reference-parity milestone but are not platform invariants.

### World-local player identity versus Spec 28 server-account state

`PlayerId` in this specification is a durable identity **within one authoritative world**. Spec 28 resolves one `(AccountId, WorldId)` membership to at most one such `PlayerId`; that world membership may own multiple Characters and may be used by multiple simultaneous sessions controlling distinct Characters. `AccountId` is the separate server-scoped cross-world identity.

Spec 20 MUST NOT store Spec 28 achievement history, cross-world unlocks or other account meta-progression inside a world snapshot. Loading or rolling back an older world snapshot may restore an older world-local `PlayerId -> CharacterId` association/state, but MUST NOT implicitly roll back independently committed account state. Conversely, the account store may keep membership/derived locators but MUST NOT become a second authoritative copy of Character/world state.

### Future evolution seam

A future rules profile may provide different deterministic codecs and schemas for `WorldPosition`, world partitions, entities or other authoritative state. As long as it supplies stable serialization, schema/version ownership, migration/compatibility policy and deterministic restoration hooks, it reuses the same Core save barrier, snapshot generation, manifest, identity map, migration orchestration and publication pipeline.

No additional cross-cutting architecture ticket is required by this review. The formerly unresolved #91 account/meta-progression boundary is now resolved by Spec 28; Spec 20 remains authoritative only for world snapshots and world-local continuation.

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
- round-trip, migration, corruption, interruption, deterministic-continuation, authoritative-server/co-op and Core/profile-boundary tests P20-01 through P20-43 pass;
- evidence remains pinned to CDDA commit `e262adb299a7613b4aedc5f12c08fe0413c56a84`.

## Post-spec identity and command-continuation checks

**P20-AUD-01 — contained item identity:** save/reload an ordinary unreferenced nested Cataclysm item, then expose/transfer/reference it. Its original ItemUid survives; ownership nesting or later projection does not allocate a replacement identity. Split/merge retirement continues to follow Specs 05/06.

[#96](https://github.com/LambdaSix/OctoGhast/issues/96) owns the missing bounded retry/outcome-history contract across reconnect/save/rollback. Existing raw-transport exclusions remain mandatory; if the decision requires durable operation records, those are semantic command state captured atomically with effects, not persisted RPC objects or socket buffers. Do not infer that a fresh projection alone answers whether a particular lost-response command committed.

The former #95 shared order/cut is resolved by the [canonical ordering/admission/activation architecture](./architecture-canonical-ordering-admission-activation.md). Save state required for deterministic continuation includes the phase-plan identity/version, canonical tick/semantic frontier, actor budget/debt/remainders, admitted/deferred semantic work with stable ordering keys, scheduler sequence, per-domain activation/catch-up markers and required RNG continuation. A coherent save may not cut through a suspended synchronous authoritative operation; finish/resume it to the next quiescent semantic boundary rather than serializing arbitrary interpreter/handler frames.


**P20-95-01 — save/load ordered pending work:** save at a quiescent cut with admitted and deferred work, activation markers and phase-plan metadata. Reloading reproduces the same future ordering/outcomes for the same canonical inputs. Raw transport queues and worker objects are absent.
