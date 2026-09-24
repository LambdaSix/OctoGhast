# Spec 26 — Multiplayer active regions, visibility and interest management

Status: implementation-ready specification  
Parent issue: #65  
Spec issue: #93  
Reference implementation: `LambdaSix/Cataclysm-DDA`  
Pinned baseline: `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Purpose

This specification defines OctoGhast's authoritative multiplayer world-activation, interest-management, visibility/knowledge and projection lifecycle.

It consumes the completed architectural decisions in #52, #57, #58, #64 and #65 plus:

- Spec 01 / #66 — canonical authoritative time and deterministic tick ordering;
- Spec 12 / #77 — coordinates, local-map ownership, active-region union and spatial-index lifecycle;
- Spec 20 / #85 — authoritative world persistence, stable world-local player identity and disconnect/reconnect;
- Spec 25 / #90 — transport/session/projection boundary and bounded networking.

It does **not** redesign those decisions and does not implement runtime code.

The central distinction is:

1. **Pinned CDDA reference behaviour** — a single local avatar drives one loaded reality bubble and its visibility/memory.
2. **OctoGhast Cataclysm profile** — preserves the relevant Cataclysm spatial, visibility, loading and catch-up semantics.
3. **Generic Core/server contract** — owns world-region activation leases, unique authoritative state, stable identity, deterministic lifecycle transitions and player-specific projections.
4. **Future evolution seam** — region shape/radius, world-position representation, visibility model and replication strategy may evolve without changing authority or identity.

## 2. Authoritative pinned-CDDA evidence

### 2.1 Reality-bubble dimensions and ownership

Pinned `src/map.h` documents the ordinary loaded `map` as an 11×11-submap window. With 12×12 map-square submaps this is 132×132 tactical tiles. The window is centered such that five submaps (60 tiles) extend around the current submap, and it shifts when the player crosses a submap boundary.

Pinned `src/game.cpp` new-game setup projects the start location to submap coordinates, subtracts `HALF_MAPSIZE`, loads that window, places the avatar, rebuilds map/visibility caches and then loads nearby NPCs and monsters.

This is authoritative evidence for Cataclysm's local active-space semantics, but **not** evidence for a multi-player ownership model.

### 2.2 Whole-submap shift behaviour

Pinned `game::update_map` recenters the local avatar in whole `SEEX`/`SEEY` increments. For each required submap shift it calls `map::shift`; after coordinate shifting it:

1. shifts active monsters;
2. despawns monsters that leave the reality bubble;
3. shifts active NPC bubble positions and removes NPCs that leave loaded bounds from the active list;
4. shifts scent;
5. sets the avatar's new bubble-local position;
6. loads newly nearby NPCs;
7. invalidates/rebuilds map caches;
8. spawns/loads appropriate monsters;
9. updates overmap visibility.

Pinned `game::shift_monsters` explicitly states that a monster outside the reality bubble is saved/despawned.

The observable invariant is that absolute world identity/position persists while the current loaded view changes.

### 2.3 Loading and actualization

Pinned `map::load`:

- clears location-derived trap/field/active-item indexes;
- sets the absolute submap origin;
- clears vehicle caches;
- loads every required XY submap;
- rebuilds vehicle caches;
- actualizes loaded submaps only after loading the full required set, to avoid edge errors;
- reconciles loaded item/crafting indexes.

Pinned `map::process_items` only processes loaded/in-bounds active-item submaps and loaded vehicles in the current map. This is evidence that active processing is tied to loaded tactical state in the baseline.

The implementation detail of a single rectangular map is not required in OctoGhast, but activation must preserve the semantic ordering: required world state is loaded and caught up before active simulation or projection exposes it.

### 2.4 NPC and monster active/inactive transitions

Pinned `game::load_npcs` finds nearby overmap NPCs, rejects those already active or on companion missions, verifies they lie within current map bounds, places them on the loaded map, and adds them to the active NPC tracker.

When the map shifts, NPCs that leave map bounds are removed from the active list after `on_unload`; their durable world identity remains in overmap/world state.

Pinned monster shifting similarly removes out-of-bubble monsters from active tactical processing and serializes/despawns them to overmap storage.

OctoGhast must preserve the distinction between a durable world actor and its active-simulation residency.

### 2.5 Visibility is observer-relative

Pinned `map::update_visibility_cache` reads `get_player_character()`, the player's current position and the player's vision threshold, clairvoyance and sight impairment. It calculates per-tile apparent light/visibility and marks sufficiently visible overmap submaps as seen.

Pinned `tests/vision_test.cpp` contains black-box visibility cases demonstrating that visibility depends on observer state and line/light conditions; tests include reciprocal/non-reciprocal cases and remote-camera/moncam vision.

Therefore visibility is not a property that may be copied wholesale from authoritative world state to every client.

### 2.6 Map memory is durable player knowledge

Pinned `map_memory` manages remembered terrain/decoration/symbol state in absolute coordinates. `prepare_region` caches the required region around requested world coordinates; `load` and `save` operate around a world position.

Pinned `tests/map_memory_test.cpp` verifies:

- region preparation/caching;
- default unknown memory;
- remembering and overwriting terrain/decoration/symbol data;
- selective forgetting/clearing;
- cache shifting.

This is evidence for a durable knowledge layer distinct from current authoritative tile state.

### 2.7 Data/JSON scope

No JSON definition controls the core reality-bubble ownership, region union, visibility cache ownership or interest-management lifecycle at this pinned baseline. Those semantics are primarily C++ runtime behaviour and fixed spatial constants.

Data-defined terrain/furniture/field/creature properties consumed by LOS, transparency, movement and rendering remain owned by Specs 12/14/10 and the Cataclysm content loader; this specification references their results but does not duplicate their schemas.

## 3. Immutable definitions versus mutable runtime state

### 3.1 Immutable/profile definition data

The Cataclysm profile may expose immutable configuration such as:

- compatibility active-footprint shape/radius;
- submap/tile scale constants inherited from Spec 12;
- visibility/FOV profile parameters;
- rules controlling which background systems require active simulation versus elapsed-time catch-up;
- projection field schemas and visibility classifications.

These are definitions/configuration, not world instances.

### 3.2 Mutable authoritative world state

Mutable authoritative state includes:

- world/submap/entity state;
- canonical tick and chronology;
- stable entity/world/player identities;
- active/inactive residency state where semantically required;
- last-processed/catch-up chronology markers;
- per-player durable knowledge/map memory where required by the profile;
- authoritative schedules/activities/events/RNG state.

### 3.3 Ephemeral derived server state

Derived, rebuildable server state includes:

- normalized active-region lease sets;
- active spatial-index partitions;
- visibility/light/pathfinding caches;
- per-session interest sets;
- per-session projection baseline/snapshot caches;
- transport coalescing/backpressure bookkeeping.

These are not durable world truth unless another spec explicitly promotes a datum for deterministic continuation.

## 4. Core domain model

Conceptual contracts:

```text
ActiveRegionRequirement
  source: PlayerId | ServerLeaseId
  footprint: absolute spatial region
  reason: PlayerPresence | Activity | System | Admin/Test

ActiveRegion
  absolute region/chunk set
  reference/lease count
  activation state
  lastProcessedTick / chronology marker

InterestView
  PlayerId
  controlled CharacterId?
  authorized spatial interest
  current visibility
  durable knowledge reference
  projection revision

ProjectionObject
  stable authoritative identity/reference
  viewer-safe fields only
  projection revision/version
```

These names are conceptual. Implementations may use chunk sets, interval sets, reference-counted submaps or another equivalent representation.

## 5. Ownership and invariants

### 5.1 World ownership

The authoritative server/world owns:

- loaded/active world partitions;
- the authoritative spatial index;
- all entity/world mutation;
- activation/deactivation transitions;
- deterministic catch-up;
- visibility/knowledge queries;
- projection authorization.

No player, connection, Godot scene, camera or UI owns authoritative active state.

### 5.2 Unique-state invariant

For any stable world partition/entity identity:

> There is at most one authoritative live instance at a time.

Two overlapping player footprints must not create duplicate map tiles, creatures, items, vehicles, activities, fields or RNG processing.

### 5.3 Set-union invariant

At each deterministic activation phase:

`ActiveWorld = union(PlayerRequiredRegions, ServerRequiredRegions)`

A region remains active while at least one valid requirement covers it.

Connected-player count is not equivalent to active-region count: footprints may overlap, coalesce or be disjoint.

### 5.4 Absolute-coordinate invariant

Activation/interest is expressed in authoritative world coordinates, consuming Spec 12's `WorldPosition` and derived `SpatialCell` contracts.

Bubble-local coordinates and Godot transforms are projections only.

### 5.5 Observer-isolation invariant

A player's projection is:

`Projection(Player) ⊆ AuthorizedInterest(Player) ∩ AllowedKnowledge(Player)`

Active server state is **not** sufficient authorization to replicate it.

## 6. Player-required region policy

### 6.1 Cataclysm-profile footprint

For parity, the default tactical active footprint around a controlled Cataclysm Character SHOULD reproduce the pinned local-map extent: an 11×11-submap XY footprint (132×132 tiles) aligned using the same submap/grid semantics as Spec 12.

This value is profile policy, not a generic Core invariant.

A future profile may use a different radius/shape or continuous-position-derived partition policy.

### 6.2 Server-required leases

Systems may hold active leases independently of connected players when active simulation is semantically required, for example:

- an explicitly server-hosted scripted encounter;
- a long-running activity that another system has specified must remain actively simulated;
- a deterministic test/admin lease.

Such leases require a stable reason/source identity and deterministic acquisition/release ordering.

They must not become an ad hoc method for keeping the entire world active.

### 6.3 Disconnected players

A network connection itself holds no durable world authority.

On disconnect:

- release connection/session projection resources immediately;
- release any active-region requirement that exists **only** for that live client's observation;
- retain `PlayerId`, `CharacterId`, world state and activities per Specs 20/02/04;
- if the disconnected character or its current activity independently requires an active server lease, that lease remains for the owning gameplay reason, not because a socket once existed.

Reconnect recreates session/projection state from current authoritative world state and durable player knowledge; stale client projection state is never trusted as authority.

## 7. Activation lifecycle

All changes occur at deterministic simulation boundaries.

### 7.1 Phase order

Normative phase order:

1. admit/resolve movement and other commands according to Spec 01/04 ordering;
2. compute resulting authoritative player/entity positions;
3. derive next required active-region set;
4. identify regions entering, remaining in and leaving the active set;
5. load/activate/catch up newly entering regions;
6. update authoritative spatial indexes and derived caches;
7. run active-world simulation for the canonical tick exactly once;
8. compute observer-specific visibility/knowledge;
9. build/diff projections;
10. release/deactivate regions whose final lease has expired after all required mutation/persistence handoff is safe.

An implementation may pipeline work internally, but externally no observer may see a half-activated or duplicate-simulated region.

### 7.2 Activate

Activation must:

- acquire the persistent partition/submap state;
- restore stable entity references;
- run profile/subsystem actualization and elapsed-time catch-up to the current canonical chronology;
- reconstruct active-only indexes/caches;
- enroll active entities/systems in the scheduler exactly once;
- make the region queryable only when the activation transaction is complete.

If loading/catch-up fails, the region must remain non-active and the owning request/session receives a deterministic failure/degraded-join result. Partial state must not be published.

### 7.3 Remain active

A region covered by one or more leases:

- advances once per canonical simulation step;
- participates once in spatial queries/schedulers;
- does not repeat catch-up merely because another player enters;
- may serve several independent player projections.

### 7.4 Deactivate

When the final lease ends:

- finish the current deterministic tick boundary;
- record any chronology markers required for later catch-up;
- detach active-only scheduler/index/cache registrations;
- flush authoritative persistent state according to Spec 20/world-store policy;
- release loaded residency when safe.

Projection removal is a separate observer operation and may happen before physical memory eviction.

### 7.5 Reactivate

Reactivation computes elapsed authoritative world time since last processing and invokes each owning subsystem's specified catch-up/actualization contract.

This spec does not invent domain catch-up formulas. It delegates:

- items/rot/temperature to Spec 05/14;
- monsters/NPCs to Specs 10/11/16;
- fields/weather/environment to Spec 14;
- vehicles to Spec 15;
- activities/EOCs/schedulers to Specs 04/17/01.

If a subsystem has no valid background approximation and correctness requires intermediate active steps, its owning spec must request/retain an active server lease or define bounded deterministic catch-up.

## 8. Overlap, separation and contention

### 8.1 Overlap

If player A and player B's footprints overlap:

- the overlap has one world partition and one spatial-index representation;
- entities are simulated once;
- commands from both players may target the same state;
- command contention is resolved by Spec 01/25 deterministic admission plus owning-domain validation;
- each player receives only their individually authorized projection.

### 8.2 Separation

Separated players may occupy disjoint active regions simultaneously.

No assumption may require:

- a single map origin;
- a single player-centered `get_map()`;
- one global viewer;
- one player-owned visibility cache.

Gameplay systems that still depend on a privileged avatar/reality bubble must be refactored behind explicit world/actor context before they are conformant.

### 8.3 Crossing region boundaries

Moving across a submap boundary must not change authoritative entity identity.

Activation of incoming partitions and retention/release of outgoing partitions are determined from absolute footprints after the authoritative movement result.

If another lease already covers an outgoing partition, it remains active with no unload/reload transition.

## 9. Visibility, knowledge and remembered map

### 9.1 Three separate concepts

Implementations must distinguish:

1. **Authoritative world state** — what actually exists.
2. **Current perception** — what this viewer can currently perceive.
3. **Durable knowledge/memory** — what this player has previously learned/remembers.

A tile/entity may exist authoritatively while absent from both current perception and durable knowledge.

A remembered tile may be projected even when its current authoritative contents have changed, according to Cataclysm memory semantics.

### 9.2 Viewer context

Visibility queries require explicit viewer/actor context:

```text
ComputeVisibility(
    viewerCharacterId,
    authoritativeWorld,
    currentTick,
    profileRules
) -> VisibilitySet
```

There is no generic global `get_player_character()` visibility cache in multiplayer server code.

### 9.3 FOV and current perception

For the Cataclysm profile, reproduce pinned LOS/light/vision behaviour from Specs 12/02/14:

- terrain/furniture/field transparency;
- light and sight thresholds;
- actor effects/traits such as impaired sight/clairvoyance;
- deterministic line stepping;
- remote/moncam-derived perception where supported.

Current visibility is recalculated/incrementally invalidated from authoritative state, not accepted from a client.

### 9.4 Durable player knowledge

Durable map/overmap knowledge belongs to stable world-local `PlayerId` (or the profile's explicitly defined player-knowledge owner), not to `ConnectionId`.

At minimum this includes Cataclysm-equivalent remembered terrain/decorations/symbols and overmap seen state where parity requires it.

Reconnect restores this knowledge through Spec 20 persistence/state ownership.

Other players' knowledge is not automatically merged. Party/shared-map mechanics, if later desired, require explicit rules/events and must not happen merely because players overlap spatially.

### 9.5 Entity knowledge

For moving/runtime entities:

- currently visible entities may be projected with stable references and allowed fields;
- entities leaving current visibility generate a projection removal/hidden transition;
- server authority retains the entity;
- the client must not continue receiving hidden live transforms unless an explicit gameplay rule authorizes tracking;
- remembered last-known entity state, if a gameplay feature requires it, must be a deliberate player-knowledge datum rather than accidental client cache persistence.

## 10. Interest management and projection contract

### 10.1 Interest is not visibility

Interest is the server's candidate scope for producing a player's projection. Visibility/knowledge then filters that candidate set.

The implementation may use a slightly larger technical interest envelope than visual FOV for continuity/prefetch, but invisible authoritative detail within that envelope must still be filtered.

### 10.2 Projection lifecycle

Each live session maintains a viewer-specific projection revision.

Conceptual events/messages:

```text
RegionProjectionSnapshot
MapCellEnteredKnowledge
MapCellUpdated
EntityEnteredView
EntityStateUpdate
EntityLeftView
KnowledgeUpdated
ProjectionReset
```

Exact DTO names are Spec 25 implementation detail, but semantics are required.

### 10.3 Enter

When an object/cell becomes newly projectable:

- send enough viewer-authorized state to construct its client representation;
- use stable authoritative IDs/references, never ECS object pointers/Godot node IDs;
- establish a projection revision/version suitable for rejecting stale local UI references.

### 10.4 Update

Updates include only viewer-authorized fields.

Spec 25 egress classes apply:

- durable gameplay facts/results are reliable ordered;
- current replaceable projected state may be coalesced;
- cosmetic hints may be disposable.

### 10.5 Leave/hide

When an object leaves projectable current visibility/interest:

- send an explicit removal/hide transition or a replacement snapshot that makes removal unambiguous;
- stop streaming live hidden state;
- do not delete authoritative state;
- retain only profile-defined player knowledge.

### 10.6 Snapshot/recovery

A reconnect or detected projection desynchronization may request/receive a fresh viewer-specific snapshot.

A snapshot contains only state authorized for that PlayerId at the snapshot revision. It is not an ECS/world dump.

## 11. Stable identity and reference requirements

### 11.1 Authoritative identity

World entities that can survive activation transitions use stable identities owned by their domain specs.

A stable ID does not change when:

- a region deactivates/reactivates;
- two player footprints begin/end overlap;
- the player disconnects/reconnects;
- a client rebuilds Godot presentation nodes.

### 11.2 Spatial references

Long-lived references use absolute authoritative coordinates plus stable object identity where applicable.

Never persist or send as durable identity:

- bubble-local array index;
- active-region lease slot;
- spatial-index bucket ordinal;
- network connection ID;
- Godot node instance ID.

### 11.3 Projection references

Client DTO references are opaque stable server references with revision/precondition information sufficient for stale-command detection.

If a referenced entity leaves interest before a request resolves, the server revalidates against authoritative identity/state and either resolves legally or returns a stable stale/not-visible/not-authorized result.

## 12. Determinism and RNG

### 12.1 Deterministic transition ordering

Given identical:

- world snapshot;
- canonical tick sequence;
- player/server lease inputs;
- admitted command order;
- RNG state;

activation/deactivation, catch-up and resulting authoritative simulation must be deterministic.

Stable ordering must not depend on:

- hash-map iteration;
- socket callback timing;
- client render frame;
- filesystem enumeration;
- order in which overlapping clients happened to connect.

### 12.2 Region ordering

When several partitions activate/deactivate on the same tick, process them in a stable absolute spatial key order unless an owning subsystem requires a stronger dependency order.

Within a region, use owning-domain deterministic ordering.

### 12.3 RNG isolation

Visibility and projection filtering must not consume authoritative gameplay RNG merely because a viewer is connected.

Adding a second observer who performs no gameplay action must not alter simulation RNG outcomes.

Background catch-up uses the owning subsystem's authoritative RNG stream/continuation rules. It must not reseed from wall-clock time or connection identity.

Presentation-only randomness remains client-local per Specs 22/25.

## 13. Persistence

Spec 20 remains authoritative.

Persist:

- authoritative world/entity state;
- stable PlayerId/CharacterId associations;
- durable player knowledge/map memory;
- chronology and per-domain markers needed for deterministic background catch-up;
- deterministic scheduler/RNG continuation where required.

Do **not** persist merely because it exists at runtime:

- active-region lease counts derived from current connections;
- transport interest subscriptions;
- per-session projection baselines;
- visibility/light/pathfinding caches;
- connection/session IDs;
- Godot presentation state.

On load, active regions are reconstructed from current server/player requirements. Persistent state is then activated/caught up as required.

A save barrier taken while multiple clients overlap a region records the region once.

## 14. Commands, queries, activities and events

### 14.1 Commands/intents

Movement, interaction and gameplay mutation remain authoritative commands through Specs 04/25.

Crossing an active-region boundary is not a separate client mutation command; it is a server consequence of authoritative movement.

### 14.2 Synchronous queries

Client queries such as inspecting a visible tile/entity are permitted only against that player's current authorized projection/knowledge and must be revalidated server-side.

A query must not become an oracle for hidden active-region state.

### 14.3 Activities

Activities remain actor-owned and advance under canonical time. Their region residency requirements are defined by the owning activity/domain rule, not by an open UI.

### 14.4 Events/messages

Gameplay events are produced once from authoritative simulation, then audience-filtered using Spec 17/25 policy.

Overlapping players may both receive the same world event when both are authorized observers, but this does not mean the event executed twice.

## 15. Failure and edge cases

### Activation load failure

No partial authoritative activation may leak to simulation/projection. Return a deterministic server/world error and preserve prior stable state.

### Catch-up failure

Treat as activation failure; do not silently skip elapsed-time processing.

### Last lease removed during same tick another is added

Normalize requirements before deactivation. If net coverage remains, do not unload/reactivate or run duplicate catch-up.

### Two players enter the same inactive region simultaneously

Activate once, in stable region ordering, then serve both projections from the same state.

### Player disconnects at a boundary

Connection-owned interest/projection is removed. Character/world progression follows durable gameplay policy. Region coverage is recomputed from remaining player/server leases.

### Player reconnects far from stale client camera

Ignore client cache authority; derive interest from authoritative controlled-character state and issue a fresh projection.

### Hidden entity targeted from stale UI

Revalidate visibility/authorization and authoritative preconditions. Reject with stable stale/not-visible/not-authorized semantics; do not reveal hidden state in the error payload.

### Entity moves between two simultaneously active disjoint/connecting regions

Identity and state remain unique; spatial-index membership changes atomically per Spec 12/#58.

### Save during overlap

Save one authoritative state and player-specific durable knowledge separately; never serialize duplicate copies per observer.

## 16. Dependency and contract matrix

| Dependency | This spec consumes |
|---|---|
| #52 | authoritative server/ECS/client separation |
| #57 / Spec 01 | canonical tick, action economy hosting, deterministic phase ordering |
| #58 / #77 / Spec 12 | WorldPosition/SpatialCell, submap store, active-region union, spatial index |
| Spec 02 | viewer Character state affecting vision |
| Spec 04 | commands/activities/queries |
| Specs 05/10/11/14/15/17 | active/background/catch-up semantics for owned domains |
| Spec 20 | world save, PlayerId/CharacterId, disconnect/reconnect, snapshot barriers |
| Spec 21/22 | Godot/UI/rendering consume projections only |
| Spec 23 | deterministic black-box/parity harness |
| Spec 25 | session/transport, bounded queues, projection delivery, stale commands |

## 17. Implementation slices

Recommended order:

1. **Region requirement model** — absolute partition keys, normalized union, stable lease sources.
2. **Single-player parity region** — one Cataclysm footprint reproducing Spec 12 behaviour through the same generic manager.
3. **Activation transaction** — load/catch-up/index enrollment/deactivation.
4. **Multiple disjoint regions** — no privileged global map origin.
5. **Overlap/reference counting** — exactly-once simulation.
6. **Viewer-specific visibility/knowledge service** — remove global player cache assumptions.
7. **Interest/projection diffing** — enter/update/leave and snapshots.
8. **Reconnect/save conformance**.
9. **Performance optimization** — chunk representations, incremental cache rebuilds, projection coalescing, without changing semantics.

## 18. Black-box and conformance scenarios

### AR26-01 — pinned single-player footprint
Given one Cataclysm-profile player at a stable position, the required active tactical footprint is 11×11 submaps and behaves equivalently to the pinned local reality-bubble extent.

### AR26-02 — submap-boundary move
When the controlled Character crosses the compatibility recenter threshold, incoming submaps activate and outgoing-only submaps release while the Character's stable identity and absolute WorldPosition remain unchanged except for the actual movement result.

### AR26-03 — separated players
Place players A and B farther apart than twice the active-footprint extent. Both regions remain active and advance under the same canonical tick. Actions near A do not require moving/deactivating B's region.

### AR26-04 — overlapping players
Place A and B so footprints overlap. A creature in the overlap executes one AI turn per canonical opportunity, not one per covering player.

### AR26-05 — overlap acquisition
Start with A covering a region; move B into it. No reload, reactivation, duplicate catch-up or identity replacement occurs.

### AR26-06 — overlap release
Move A away while B still covers the shared region. The shared region remains active and receives no deactivation/reactivation cycle.

### AR26-07 — final lease release
When the last covering player/server lease leaves, the region transitions inactive only after the deterministic boundary and persistence/background markers are valid.

### AR26-08 — reactivation catch-up
Deactivate a region at tick T1, advance world chronology, reactivate at T2. Domain state equals the owning subsystem's defined deterministic elapsed-time catch-up result before the first active projection is emitted.

### AR26-09 — activation ordering
Activate several partitions simultaneously from the same saved state. Repeating with different client packet arrival/thread schedules yields identical authoritative results.

### AR26-10 — FOV isolation
A and B stand in overlapping active space separated by an opaque wall. An entity visible only to A appears only in A's projection.

### AR26-11 — knowledge isolation
A explores a tile; B does not. After both leave current LOS, A may receive Cataclysm-profile remembered map data while B remains unknown.

### AR26-12 — observer noninterference
Add a second connected observer who issues no gameplay commands. Authoritative RNG state and simulation outcome remain identical to a one-player observation run.

### AR26-13 — entity leaves view
An entity moves from visible to hidden while remaining active due to another player's footprint. The first client receives a leave/hide transition and no subsequent live hidden transforms.

### AR26-14 — entity enters view
A previously hidden active entity becomes visible. The client receives a stable identity plus sufficient authorized state to create presentation exactly once.

### AR26-15 — stale hidden target
A client submits an interaction against an entity that left authorized visibility before resolution. The server rejects without mutation and without leaking current hidden state.

### AR26-16 — reconnect projection reset
Disconnect A, mutate the world legitimately while A is absent, reconnect. A receives projection derived from current authoritative state plus durable A-specific knowledge; stale client cache is discarded.

### AR26-17 — disconnected Character continuation
Disconnect A during an activity. World/Character/activity continuation follows Specs 02/04/20 independent of connection existence; region residency follows gameplay/server lease policy, not socket lifetime.

### AR26-18 — one-player transport equivalence
Run the same semantic input sequence through in-process single-player and loopback network transport. Authoritative active-region transitions and projections are semantically equivalent.

### AR26-19 — save during overlap
Save with A/B overlapping. Reload and reconnect both. The authoritative overlap state exists once; each PlayerId's durable knowledge is restored separately; ephemeral interest/visibility caches are rebuilt.

### AR26-20 — save while separated
Save with two disjoint active regions. Reload with only A connected. Only requirements needed by the resumed server/player state become active; B's prior connection interest is not persisted.

### AR26-21 — simultaneous region acquisition
A and B enter the same previously inactive partition on the same tick. Exactly one activation/catch-up transaction occurs, then both receive their own filtered projection.

### AR26-22 — simultaneous last-release/new-acquire
A leaves a partition on the same tick B enters it. Normalized net requirements keep it active without unload/reload.

### AR26-23 — active item once under overlap
An active item in an overlapped submap processes once per profile schedule, not once per player's view.

### AR26-24 — event audience filtering
An authoritative event occurs in overlap. A can perceive it and B cannot. Event side effects occur once; only A receives the viewer-visible message/event.

### AR26-25 — remote/alternate perception
Use a Cataclysm-supported remote perception source such as moncam. Viewer projection follows the profile's visibility result without changing region/world ownership.

### AR26-26 — memory persistence
Remembered map data survives disconnect/save/load according to PlayerId ownership while current visibility caches do not.

### AR26-27 — activation failure atomicity
Inject a partition load/catch-up failure. No half-loaded entities appear in the spatial index or any projection, and retry from the same pre-state is deterministic.

### AR26-28 — absolute identity across region cycles
An entity deactivates to durable world state and later reactivates. Stable identity/reference remains the same and client references can detect revision/staleness without depending on former active-memory address.

### AR26-29 — Godot independence
Different render FPS/interpolation/camera positions do not change active-region membership, visibility authorization or authoritative simulation.

### AR26-30 — future-profile seam
A test profile uses a different active-footprint shape and non-Cataclysm WorldPosition representation while reusing the same lease/identity/projection architecture. No generic Core API requires an 11×11 grid bubble.

## 19. Acceptance criteria mapping

- Pinned single-avatar bubble/loading evidence: §§2.1–2.6, AR26-01/02.
- Reference/adaptation/implementation separation: §§1–2 and throughout.
- Separated/overlapping regions, exactly-once simulation: §§5–8, AR26-03–07/21–23.
- Activation/deactivation/background catch-up/spatial index: §§7, 11, AR26-07–09/27/28.
- FOV/knowledge/map memory/projection: §§9–10, AR26-10–16/24–26.
- Interest versus authoritative persistence: §§3, 10, 13, AR26-19/20.
- Disconnect/reconnect: §§6.3, 10.6, 13, AR26-16/17.
- Identity/determinism/RNG: §§11–12, AR26-09/12/28.
- Transport equivalence: §10 + Spec 25, AR26-18.
- Dependencies: §16.
- Cross-cutting decision handling: no new unresolved decision identified by this investigation.

## 20. Architectural conclusion

The pinned Cataclysm implementation is explicitly built around one player-centered tactical reality bubble and player-relative visibility/memory. Those behaviours provide the reference geometry, load/shift/actualization ordering and perception semantics needed for parity.

OctoGhast intentionally adapts ownership:

- active spatial state belongs to the authoritative world;
- several disjoint or overlapping player-required regions may coexist;
- overlapping state is simulated exactly once;
- current perception and durable player knowledge are viewer-specific;
- interest/projection caches are ephemeral and never authority;
- canonical time, stable identities and persistence continue independently of presentation and connection lifetime.

No genuinely unresolved cross-cutting architecture decision was exposed. This specification makes #77's foundational active-region decision implementation-ready at the Product Surface boundary and inherits networking/persistence/time policy from their authoritative specs rather than redefining them.
