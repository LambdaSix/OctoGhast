# Spec 12 investigation — local map, coordinates, terrain and spatial simulation

Parent: #65  
Tracking issue: #77  
Reference baseline: `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Status and purpose

This page records the pinned-reference investigation required by #77 and turns the Cataclysm:DDA tactical map behavior into an implementation-facing contract for OctoGhast. It deliberately specifies observable spatial behavior, stable data contracts, mutation/caching obligations, and persistence boundaries rather than requiring CDDA's C++ class layout.

This spec is foundational for construction, combat, AI/pathfinding, vehicles, world generation/mapgen, environment simulation, items, and persistence.

## Authoritative reference anchors

Primary sources at the pinned baseline:

- `src/coords_fwd.h`, `src/coordinates.h`, `src/map_scale_constants.h` — typed coordinate origins/scales, projection rules, map/submap/overmap scale constants and z-level bounds.
- `doc/c++/POINTS_COORDINATES.md` — intended coordinate model and naming conventions.
- `src/map.h`, `src/map.cpp` — loaded reality-bubble map, tile access/mutation, bounds, movement cost, passability, LOS/light/cache interfaces, shifting, loading, saving, bashing and destruction.
- `src/submap.h`, `src/submap.cpp` — persistent 12×12 tile storage and per-submap metadata.
- `src/mapbuffer.h`, `src/mapbuffer.cpp` — world-wide submap ownership/cache and disk lookup.
- `src/mapdata.h`, `src/mapdata.cpp` — terrain/furniture definitions, flags, movement/transparency and bash/deconstruct/open/close metadata.
- `src/field.h`, `src/field_type.h`, `src/trap.h` — tile field/trap state consumed by the map.
- `src/level_cache.h`, `src/lightmap.h`, `src/shadowcasting.h` — derived per-z-level spatial caches and visibility/light calculations.
- `src/line.h`, `src/line.cpp` — deterministic 2D/3D Bresenham line stepping and distance helpers.
- `tests/map_test.cpp` — coordinate round-trip, bounds and map mutation evidence.
- `tests/vision_test.cpp` and related map test helpers — visibility/transparency/light golden-map evidence.

## Fixed spatial constants at the parity baseline

The baseline defines:

| Concept | Contract |
|---|---|
| map square (ms) | smallest tactical tile; scale factor 1 |
| submap (sm) | 12×12 map squares (`SEEX=SEEY=12`) |
| overmap terrain (omt) | 2×2 submaps = 24×24 map squares |
| map/reality bubble width | 11×11 submaps (`MAPSIZE=11`) |
| loaded XY dimensions | 132×132 map squares |
| half-map/view radius constant | 60 map squares |
| overmap width | 180×180 OMTs |
| map segment | 32×32 OMTs |
| z levels | -10 through +10 inclusive |
| total z layers | 21 |

OctoGhast parity code must not scatter these numbers. They should be exposed by one spatial-constants contract so fixtures and conversion tests can pin them.

## Coordinate model

### Two independent dimensions: origin and scale

A coordinate is not merely `(x,y,z)`. It has:

1. **origin** — what zero means;
2. **scale** — what one unit means.

Baseline origins are:

- `relative`: an offset that can be added to compatible coordinates;
- `abs`: global world origin;
- `submap`: corner of a submap;
- `overmap_terrain`: corner of an OMT;
- `overmap`: corner of an overmap;
- `reality_bubble`: corner of the currently loaded map.

Baseline scales are map-square, submap, overmap-terrain, segment, overmap, and vehicle.

OctoGhast should preserve this distinction in its public domain API. Raw integer tuples crossing subsystem boundaries are a parity risk because the reference uses type distinctions to prevent mixing absolute, bubble-local, and relative positions.

### Required coordinate types

At minimum, dependent systems need explicit equivalents of:

- absolute map-square position;
- reality-bubble map-square position;
- submap-local map-square position;
- absolute submap position;
- reality-bubble submap position;
- absolute OMT position;
- relative map-square/submap offsets.

Both 2D and 3D forms are useful, but map simulation should use 3D positions whenever z-level behavior is possible.

### Projection and decomposition

Scale conversion is integer grid projection. One submap is 12 map squares and one OMT is 24 map squares. Conversion from a fine absolute coordinate to a coarser coordinate must also support the remainder/local coordinate needed to identify a tile inside the containing unit.

Negative coordinates are important: conversion must use the same floor-style grid semantics as the reference rather than language-default truncation toward zero. Golden tests must include values immediately around zero and every boundary: -25, -24, -13, -12, -1, 0, 1, 11, 12, 13, 23, 24, 25.

### Bubble/absolute conversion

A loaded map has an absolute submap origin, `abs_sub`. The corresponding absolute map-square origin is its projection to map-square scale.

For a bubble-local map-square position `b`:

`absolute_ms = project_to_ms(abs_sub) + b`

The inverse subtracts that absolute map-square origin.

The baseline map test explicitly requires both directions to round-trip while `abs_sub` is non-zero and at multiple z levels. OctoGhast must make this a core invariant.

### Bounds

For the normal 11-submap map:

- valid bubble X is `0 <= x < 132`;
- valid bubble Y is `0 <= y < 132`;
- valid z is `-10 <= z <= +10` when z-levels are enabled.

Out-of-bounds terrain access behaves as null/nonexistent rather than aliasing a loaded tile. Mutation APIs should reject out-of-bounds coordinates.

## Loaded map and ownership model

### Upstream evidence versus OctoGhast multiplayer adaptation

The pinned CDDA baseline exposes one loaded `map`/reality bubble centered around the local avatar. That 11×11-submap shape, its coordinate conversions, loading order and spatial rules remain evidence for local-map semantics, but **single-avatar ownership is not an OctoGhast server contract**.

OctoGhast adapts this as follows:

- the authoritative server/world owns loaded spatial state;
- each connected player contributes a required active footprint in absolute coordinates; single-player is exactly one such connected player;
- the server derives a normalized **union of active regions** from all required player footprints (plus any explicitly server-required simulation regions);
- separated players may therefore produce disjoint active regions in the same world;
- intersecting/touching player footprints may coalesce operationally, but a world tile/submap belongs to one authoritative loaded instance and is simulated once regardless of how many players require it;
- player-local “reality-bubble” coordinates are projections/views only and never persistence keys, entity identity, or ownership boundaries.

The implementation may retain the pinned 11×11-submap footprint as the default compatibility radius around each player, but it must not allocate independent authoritative bubbles that duplicate overlapping world state.

### Persistent owner: world submap store

The reference `mapbuffer` is the world-wide owner/cache of submaps, keyed by `tripoint_abs_sm`. A lookup:

1. returns an already buffered submap if present;
2. otherwise attempts to deserialize the containing persisted map data;
3. returns null/failure if the submap cannot be obtained.

For OctoGhast, define an interface equivalent to:

`SubmapStore.get_or_load(AbsoluteSubmapPos) -> Submap?`

and separate persistence from the active map view. The loaded reality bubble should reference/lease world submaps rather than becoming their independent authoritative copy.

### Pinned-CDDA active reality bubble

In the pinned baseline, the active map is a rectangular XY window over absolute submaps plus either all supported z levels or a selected z level, depending on world/runtime mode. The following load/shift behavior is upstream evidence to preserve semantically inside each OctoGhast activation transition, not a requirement that the server have one global avatar-owned rectangle.

A full load:

1. clears location-derived indexes (trap/field/active-item/vehicle indexes);
2. sets the absolute submap origin;
3. clears vehicle level caches;
4. loads each XY submap column from the world store;
5. rebuilds vehicle caches;
6. actualizes loaded submaps only after the required set is present, avoiding edge behavior against partially loaded neighbors;
7. reconciles derived indexes/caches.

This ordering is observable for edge-crossing entities and should be preserved semantically.

### OctoGhast server/world-owned active regions

The server maintains an `ActiveRegionSet` in absolute submap/map-square coordinates. It is derived from player interest anchors and explicit simulation leases, normalized so every authoritative submap/tile has one active-state record independent of the number of requesting players.

For every server step:

1. derive each player's required footprint from that player's authoritative position and the configured compatibility/interest radius;
2. compute the absolute union and compare it with the previous active set;
3. load/actualize newly activated submaps before systems may query them;
4. retain already-active submaps and their authoritative entities/caches without duplication;
5. simulate each active world cell/entity exactly once through world-owned scheduler/spatial indexes;
6. after simulation, build each player's visibility/knowledge projection from the same resulting world state;
7. deactivate submaps no longer covered by any player or server lease only after persistence/background-transition obligations are satisfied.

Two disjoint footprints remain independently active and queryable. If they later overlap, no merge copies state: both interests point at the already unique absolute world state. If an overlap separates again, state likewise is not split or cloned.

Reference-counting, interval/chunk sets, merged rectangles, or another representation are implementation choices; externally the invariant is set-union semantics over absolute world coordinates.

### Activation, deactivation and background catch-up

Active simulation and inactive/background world progression are distinct modes over the same persistent world identity.

- **Activate:** acquire/load the absolute submaps, reconstruct authoritative spatial indexes and derived caches, then actualize/catch up elapsed background time before exposing the region to active systems or client projection.
- **Remain active:** advance under canonical authoritative server time exactly once, even if multiple players cover the region.
- **Deactivate:** remove the last active/lease requirement, flush authoritative persistent state as required, record the chronology needed for later catch-up, remove active-only indexes/caches, and release the loaded lease when safe.
- **Reactivate:** compute elapsed authoritative world time since the region's last processed timestamp and apply the owning subsystem's deterministic catch-up/actualization rules before ordinary active ticks resume.
- **Crossing/merging:** a region already active because of another player does not deactivate/reactivate and receives no duplicate catch-up when a second player enters it.

This spec owns the lifecycle and timestamp/index invariants. Environment/monster/vehicle/etc. specs own their detailed catch-up algorithms. Catch-up must be based on authoritative world chronology (#66), never client wall-clock time.

### Pinned-CDDA bubble shifting

Horizontal shifting is in whole submap increments. Existing loaded submaps are reused where possible, outgoing submaps are unloaded/saved as required, incoming submaps are loaded, and bubble-local indexes are translated by the opposite map-square offset:

`bubble_offset = (-shift.x * 12, -shift.y * 12)`

Entries that leave bubble bounds are removed from bubble-local indexes. Incoming submaps are actualized after loading.

This establishes a crucial invariant: **absolute positions remain stable while bubble-local positions change when the reality bubble shifts**.

### Vertical shift

With z-levels enabled, vertical shift changes the active absolute z origin/current level and rejects values outside -10…+10. OctoGhast should expose checked vertical transitions and never silently wrap/clamp invalid z coordinates.

## Submap storage contract

A submap contains 12×12 tile-indexed arrays for:

- terrain ID;
- furniture ID;
- item collection/stack;
- field collection;
- trap ID;
- radiation;
- item-emission luminance count.

It also owns non-tile or sparse metadata such as vehicles, spawns, computers/cosmetics/graffiti, active-item tracking and other persisted submap state.

### Uniform submaps

The reference can represent a submap as a uniform terrain block without allocating full tile arrays. Uniform submaps imply:

- every tile has the same terrain;
- no furniture;
- no items;
- no fields;
- no traps;
- zero radiation/luminance.

The optimization itself is not required for parity, but its semantics are. OctoGhast may store all tiles explicitly if serialized and observable behavior remains identical.

### Tile aggregate

For implementation purposes, model a tile as a logical aggregate:

`MapTile { terrain, furniture?, trap?, fields, items, radiation, derived occupancy }`

Vehicles and creatures are not simply embedded in the persistent tile arrays. They have independent ownership and are projected/indexed onto map positions. Therefore occupancy queries must combine static tile layers with actor/vehicle indexes.

## Terrain, furniture, traps and fields

### Stable IDs

Terrain, furniture, traps and field types are data-defined and must be referenced by stable string IDs at compatibility boundaries. Runtime integer IDs are load-order-dependent and are not stable across content sets.

### Terrain/furniture layering

Every tile has terrain. Furniture is optional and overlays terrain. Movement, transparency, flags, examination, opening/closing, bashing and other behavior are composed from both layers, sometimes with fields and vehicles.

A furniture mutation must:

- validate bounds;
- optionally reject occupied locations when requested;
- replace/clear the furniture ID;
- reset map damage associated with the changed layer;
- mark player-adjusted state outside mapgen;
- invalidate all derived caches affected by the old/new properties;
- preserve explicit behavior for open-air placement constraints.

Terrain mutation has the analogous obligation to invalidate derived spatial state. OctoGhast should centralize mutation rather than permit arbitrary tile-field writes.

### Traps

A tile has zero or one trap ID in the persistent submap layer. The active map also maintains location indexes for efficient trap lookup. Mutation must keep the tile value and any derived index consistent.

### Fields

A tile can contain multiple field entries. Fields contribute to movement cost, transparency and other simulation. A submap tracks whether it has fields, and the active map maintains field-related caches/indexes. Field mutation therefore participates in cache invalidation and later environment processing.

## Items and occupancy

A tile owns a collection of items with the map stack enforcing a maximum item-count rule and volume-related behavior. Detailed item/container semantics belong to #70/#71, but this map spec requires:

- item location is a map-square coordinate plus identity within the tile collection;
- adding/removing emissive items updates tile luminance-derived state;
- active items participate in submap/active-map indexes;
- map shifting must not change an item's absolute world position.

Creature occupancy is resolved through the creature tracker using absolute positions. Vehicle parts are cached/projected per map square and may override movement/transparency. Multiple vehicle parts can relate to one vehicle, but spatial queries need a deterministic part-at-position result.

## Movement and passability

The baseline movement-cost composition is:

1. if terrain movement cost is zero, blocking furniture has negative movement cost, or fields report an impassable total cost, result is impassable (`0`);
2. if a vehicle occupies the tile, the vehicle part movement cost governs;
3. otherwise start with `max(terrain.movecost + field.total_move_cost, 0)`;
4. furniture normally adds non-negative furniture move cost;
5. bridge furniture uses its special baseline rule (`2 + max(furniture.movecost, 0)`).

At the map interface, **zero movement cost means impassable**. Dependent pathfinding should consume the map's passability/movement interface, not reimplement terrain/furniture arithmetic.

Movement cost is a property of spatial contents; actor-specific capabilities (swimming, flying, opening doors, hazards, etc.) belong in pathfinding/actor policy layered over this base map result.

## Transparency, light and visibility

These are derived data, not authoritative tile state.

Per-z-level caches include transparency, seen/visibility, outside/inside, floor support, lightmap, pathfinding and vehicle projections. Mutations that can affect one of these must mark the corresponding cache dirty.

The reference specifically propagates transparency/floor/outside changes into lightmap invalidation below the changed z level because vertical light/visibility depends on upper layers.

OctoGhast should implement explicit invalidation categories rather than a generic undocumented “map dirty” flag:

- transparency;
- visibility/seen;
- outside/inside;
- floor/support;
- lightmap;
- pathfinding/reachability;
- vehicle occupancy.

Correctness takes priority over incremental-cache performance: an initial implementation may rebuild whole per-z caches if it produces the same query results.

### LOS line primitive

The reference uses deterministic Bresenham stepping in 2D and 3D. `line_to` includes the destination and excludes the origin. Tie-break parameters select among valid Bresenham lines.

LOS parity therefore requires deterministic line stepping, not only geometrically similar results. Pin representative horizontal, vertical, diagonal, shallow/steep and cross-z lines as golden fixtures.

### Distance

The reference supports both Euclidean/trigonometric distance and roguelike square/Chebyshev distance. `rl_dist` selects between them using the circular-distance option. Systems consuming “distance” must name which metric they require.

## Doors, gates and transformations

Terrain/furniture definitions carry data and flags used for open/close transformations, bashing, destruction, deconstruction and other transitions. The map is responsible for applying the resulting tile mutation and invalidating derived state.

The parity boundary should expose operations such as:

- `try_open(position, actor/context)`;
- `try_close(position, actor/context)`;
- `bash(position, damage/context)`;
- `destroy(position, context)`;
- `set_terrain`, `set_furniture`, `set_trap`, field add/remove.

Do not encode doors as a special tile class: open/closed states are data-defined terrain/furniture IDs and transitions.

Bashing is stateful enough to track whether something was hit, whether further bashing is possible, whether destruction succeeded, whether a solid layer was bashed, and whether the hit came from above. Exact damage/drop/collapse behavior should be driven by the terrain/furniture data contracts and construction/environment specs.

## Z-level semantics

The supported absolute z range is -10 through +10. A map square is therefore a 3D coordinate even when most interactions are planar.

Cross-z spatial behavior must account for:

- floor/support between levels;
- open air;
- vertical LOS/light propagation;
- stairs/ramps/vertical movement supplied by terrain;
- falling and collapse consumers;
- vehicles spanning/projecting across z;
- cache invalidation between vertically related layers.

A tile at the same x/y but another z is a distinct tile. APIs that intentionally ignore z should use explicit 2D types or names.

## Persistence contract

Submaps are the principal local-map persistence unit and are keyed by absolute submap coordinate. The world map store groups files by larger spatial regions/segments as an implementation detail; OctoGhast does not need identical file packing unless existing-save compatibility is later selected.

Parity requirements are:

- persistent tile layers and submap metadata survive save/load;
- stable string IDs, not runtime integer registry indexes, are the compatibility representation;
- absolute submap identity survives active-bubble shifts;
- derived caches do not need serialization and must be reproducible after load;
- loading a submap reconstructs indexes for traps, fields, active items, vehicles and other derived lookup structures;
- uniform-submap optimization must not lose non-default state;
- save/load round trips must preserve observable map queries.

Reference `map::save` iterates loaded submaps; `saven` performs unload handling before delegating persistent submap state to the map buffer/store.

## Proposed OctoGhast boundaries

Suggested behavioral interfaces:

### SpatialCoordinates

Own constants, typed positions, projections, decomposition/remainder operations, bounds helpers and distance/line primitives. No game-content dependency.

### Submap

Own persistent 12×12 tile layers and submap-local metadata. Expose controlled mutation/query APIs and serialization DTO conversion.

### WorldMapStore

Own/load/save submaps by absolute submap position. Hide disk packing/compression. Permit deterministic in-memory implementation for tests.

### ActiveRegionManager

World/server-owned service that derives and maintains the union of active absolute regions from all player footprints and server simulation leases. Own activation/deactivation transitions, loaded-submap leases, active-only cache lifecycle, and coordination of background catch-up. It must never duplicate authoritative state for overlapping interests.

### PlayerSpatialProjection

Own per-player conversion from absolute authoritative coordinates into player-relative/local view coordinates where useful. Bubble-local coordinates are presentation/query conveniences only; they never own simulation state.

### ActiveMapView

Optional bounded view over a portion of the world-owned active set for algorithms that benefit from CDDA-like local indexing. It references authoritative submaps and cannot create an independent copy of overlapping world state.

### SpatialCaches

Own per-z derived transparency/light/outside/floor/pathfinding/vehicle/visibility data. Caches are disposable and reconstructible.

### Authoritative spatial position, cell membership, index and query

OctoGhast must not make "precise authoritative position", "map cell membership", and "render transform" synonyms.

The generic Core spatial contract therefore separates:

- **WorldPosition** — the authoritative logical position of an entity in simulation space. The exact numeric representation is deliberately not fixed by this spec. It must be deterministic, serialization-stable and independent of Godot types. The initial Cataclysm parity profile constrains actors/items that use tile occupancy to exact grid-aligned positions.
- **SpatialCell** — the discrete typed map-square/submap/chunk bucket used for CDDA tile rules, dense map storage, activation and fast spatial indexing.
- **PresentationTransform** — client-only Godot/render-space state used for interpolation, animation, camera and visual offsets; never authoritative.

For the initial Cataclysm profile, `WorldPosition -> SpatialCell` is trivial because authoritative actors are grid aligned. Core APIs must nevertheless express the mapping explicitly rather than assume all future authoritative positions are integer tuples. A future rules profile may permit sub-cell positions while deriving the containing/overlapping `SpatialCell` set deterministically.

Any future non-grid authoritative position representation must avoid renderer-owned floating-point state as the persistence/network authority. Fixed-point, rational/integer subunits, or another deterministic representation may be selected later.

The server owns spatial indexes keyed/bucketed by authoritative `SpatialCell` values (and, if later required, a secondary precise-position structure). They cover **all currently active regions**, including multiple separated regions, and support entity-at-cell, blockers, region/radius queries, spawn/move/despawn, and player/AI queries without ECS-wide scans.

Position mutation, derived cell-membership mutation and index mutation are one atomic authoritative operation as required by #58:

`WorldPosition change -> derive SpatialCell membership -> validate occupancy/rules -> commit position + index membership atomically`

No system may independently write the position while leaving stale cell membership.

Indexes may be partitioned by submap/chunk/region internally, but a query crossing a partition or overlap boundary observes one coherent world. Overlapping player interests do not create duplicate index entries.

`SpatialQuery` exposes CDDA cell semantics (passability, movement cost, LOS, transparency, occupancy and neighborhood/radius queries) to combat, AI, activities and projection code. Generic Core may additionally expose precise-position/range primitives later without forcing Cataclysm rules to adopt them.

This decomposition preserves behavior while avoiding a direct port of the large CDDA `map` class.

## Per-player FOV, knowledge and remembered-map projection

CDDA's LOS/transparency/light rules remain simulation evidence, but OctoGhast evaluates and projects them per player.

For each connected player, the authoritative server maintains or derives:

- current FOV/visibility from that player's authoritative position, senses, lighting and world state;
- player-specific knowledge/discovery state that is not shared merely because another player can see the same tile/entity;
- remembered-map state representing what that player previously knew, including the last-known representation required by the UI contract;
- an interest/replication set constrained by active-region membership **and** that player's visibility/knowledge permissions.

A player entering an area cannot receive another player's hidden FOV, unexplored map knowledge, unseen creatures/items, or private remembered-map updates unless a separate gameplay rule explicitly shares that information. Remembered map data may be persisted per player; it is not authoritative current world state.

Replication must distinguish at least: currently visible authoritative state, known-but-not-currently-visible remembered state, and unknown state. Server events with spatial content are filtered/projected under the same player-specific interest rules rather than broadcast merely because recipients occupy the same active region.

## Replication and interest-management contract

Active simulation coverage is a server concern; replication coverage is per connection. The server may simulate world state that no particular client is entitled to receive.

Each authoritative tick/snapshot boundary must:

1. accept validated player intents independently of render frame rate;
2. resolve simulation against the world-owned active set;
3. update authoritative spatial indexes;
4. compute each connection's interest set from its player identity, spatial relevance, FOV/knowledge and protocol policy;
5. emit only permitted snapshots/deltas/events with stable entity identifiers and authoritative integer positions.

When two players' interests overlap, shared visible entities refer to the same authoritative entity/state revision. Replication may send separate per-client encodings, but it must not imply two simulation instances. When players are separated, no ECS-wide scan is required to build either interest set.

## Authoritative position versus spatial cell versus Godot 2D presentation

The architecture deliberately has three layers:

1. **Authoritative WorldPosition** — simulation-owned logical position.
2. **Authoritative SpatialCell membership** — discrete map/index membership derived from WorldPosition according to the active rules profile.
3. **PresentationTransform** — Godot/client visual transform.

For the pinned CDDA parity profile, WorldPosition is constrained to grid-aligned tactical positions and therefore maps one-to-one to a single integer map-square cell for ordinary actors/items. Terrain occupancy, collision, LOS, movement costs, traps, fields, melee adjacency, pathfinding, FOV and persistence continue to use the pinned CDDA cell semantics.

Godot 2D transforms remain client presentation state only:

- clients map authoritative projected position/cell state to render-space transforms;
- interpolation may visually move a sprite between previous and latest authoritative states;
- interpolation/animation never creates authoritative occupancy and cannot alter collision, LOS, range or activation;
- client floating-point transforms are never accepted as authoritative simulation positions;
- current Cataclysm intents identify discrete actions/targets using protocol-safe grid coordinates or stable entity IDs;
- correction/reconciliation snaps or re-interpolates presentation toward latest server state without rewriting server history.

The generic Core contracts must not require `WorldPosition == SpatialCell == PresentationTransform`. This preserves an escape hatch for future authoritative sub-tile movement without changing the transport, ECS ownership, active-region or presentation boundaries.

If a future OctoGhast rules mode permits non-grid positions, that mode must explicitly define creature/vehicle geometry, cell overlap/membership, terrain-boundary cost application, collision, melee reach, trap/field triggering, FOV/range semantics and persistence. Those are gameplay-rule changes, not renderer changes, and are outside the pinned-CDDA parity contract.

## State and invariants

The implementation must maintain these invariants:

1. Every loaded bubble tile maps to exactly one absolute map square and one containing absolute submap.
2. Absolute positions do not change when the bubble shifts.
3. Bubble ↔ absolute conversion round-trips for all in-bounds tiles.
4. A submap is 12×12 map squares; an OMT is exactly 2×2 submaps.
5. Active XY map extent is 11×11 submaps for the pinned parity profile.
6. Z coordinates outside -10…+10 are rejected.
7. Every tile always has terrain; optional layers have defined null/empty states.
8. Stable string IDs cross persistence/content boundaries.
9. Derived caches are never authoritative and can be discarded/rebuilt.
10. Any mutation that changes a queryable spatial property invalidates every affected derived cache before the next query.
11. World persistence is keyed in absolute coordinates, never bubble-local coordinates.
12. Out-of-bounds queries cannot alias valid storage.
13. Actor/vehicle occupancy indexes agree with their owners' absolute positions.
14. A save/load round trip cannot change map query results except intentionally time-dependent actualization.
15. Active spatial state is owned by the server/world, not by any player or connection.
16. The active set is the union of all player/server-required regions; separated regions are supported.
17. Every absolute tile/submap/entity is simulated at most once per authoritative step regardless of overlapping player interests.
18. Activation/deactivation is reference/coverage driven; entering an already-active overlap cannot trigger duplicate actualization or catch-up.
19. Background catch-up uses authoritative world chronology and completes before a reactivated region is exposed as current.
20. Authoritative spatial indexes cover all active regions and never require an ECS-wide scan for normal coordinate/region queries.
21. FOV, knowledge and remembered-map state are player-specific projections and cannot leak between players by virtue of shared region activation.
22. Replication interest is per player/connection and is not identical to the global active simulation set.
23. Authoritative simulation position, derived spatial-cell membership and Godot presentation transforms are distinct concepts.
24. For the pinned CDDA parity profile, actor/item WorldPosition is grid aligned and maps deterministically to the same single map-square SpatialCell.
25. Core spatial APIs must not require future authoritative WorldPosition values to be integer/grid tuples, but any future precise representation must be deterministic and serialization-stable.
26. Godot transforms/interpolation are non-authoritative presentation only and can never supply authoritative WorldPosition.

## Failure and edge behavior

OctoGhast conformance should explicitly cover:

- negative absolute coordinates around scale boundaries;
- bubble edges and corners;
- missing/unloadable submaps;
- null/out-of-bounds tile queries;
- invalid z shifts;
- mutation of a tile while caches are warm;
- a vehicle or creature crossing a submap boundary;
- bubble shift while active items/fields/traps exist near outgoing/incoming edges;
- destruction of furniture currently referenced by an actor interaction;
- open-air furniture placement;
- LOS exactly along tile boundaries/tie cases;
- vertical LOS through floors/open air;
- save/load with fields, traps, items, vehicles and modified terrain/furniture.

Failures must be deterministic and diagnosable. Debug logging may differ from CDDA, but invalid operations must not corrupt neighboring tiles or leave caches silently inconsistent.

## Black-box parity/conformance suite

### Coordinates

1. **Scale constants** — assert 12 ms/sm, 24 ms/omt, 11 sm active width, 132×132 active tiles, z -10…+10.
2. **Negative projection table** — pin fine→coarse and remainder results around negative/positive boundaries.
3. **Bubble round trip** — with non-zero absolute submap origin, assert `abs(bub(p)) == p` and `bub(abs(p)) == p` across corners, center and z extremes.
4. **Shift stability** — record absolute positions, shift bubble one submap in each cardinal direction, verify surviving tiles/entities keep absolute positions while bubble coordinates move by 12.

### Tile layers and mutation

5. **Layer round trip** — set terrain, furniture, trap, multiple fields, radiation and items on one tile; save/reload; assert identical public queries.
6. **Mutation invalidation** — warm movement/transparency/light/path caches, change wall↔floor/window/furniture/field, then verify next queries observe new state without manual cache reset.
7. **Uniform semantics** — compare a uniform submap with an explicitly materialized equivalent.

### Movement and occupancy

8. **Movement composition** — fixtures for blocking terrain, blocking furniture, field penalties, bridge furniture and vehicle-part override.
9. **Occupancy** — creature and vehicle at known absolute positions remain discoverable across submap and bubble shifts.
10. **Boundary movement** — passability and neighborhood queries at x/y 0 and 131 never read outside storage.

### LOS/light

11. **Bresenham goldens** — horizontal, vertical, diagonal, shallow, steep and 3D line fixtures; destination included, origin excluded.
12. **Wall/window/open-air visibility** — compact ASCII fixtures adapted from the reference vision-test style.
13. **Cross-z invalidation** — mutate floor/transparency above a viewed tile and verify lower-level light/visibility is recalculated.

### Transformations

14. **Open/close** — data-defined closed/open terrain or furniture transitions preserve unrelated tile layers and invalidate spatial caches.
15. **Bash/destroy** — deterministic seeded fixtures assert resulting terrain/furniture, drops where applicable, passability and visibility.
16. **Grab/reference cleanup** — destroying referenced furniture does not leave a stale interaction reference.

### Persistence/load boundaries

17. **Submap identity** — save multiple adjacent submaps with distinct markers; unload/reload by absolute key; no transposition.
18. **Reality-bubble load** — loading at a non-zero origin yields correct 11×11 submap window.
19. **Outgoing/incoming shift** — edits in an outgoing submap persist and reappear when shifted back.
20. **Derived rebuild** — discard all caches before reload and prove public movement/LOS/occupancy results match pre-save values.

### Multiplayer active-region and projection adaptation

21. **Separated players** — connect players A and B farther apart than two default active footprints. Assert two disjoint regions are active simultaneously; actors/environment in both advance under the same server chronology; authoritative spatial queries find entities in either region without ECS-wide scanning; neither player's movement shifts or unloads the other's region.

22. **Overlapping regions simulated once** — place A and B so their required footprints overlap around a deterministic counter/effect/actor. Advance N authoritative ticks. Assert the overlapped state advances exactly N times, not 2N; both players reference the same entity IDs/state revisions; moving B into/out of overlap neither clones state nor causes activation catch-up while A keeps it active.

23. **Visibility and knowledge isolation** — give A LOS to a creature/tile mutation that B cannot currently see, while both occupy the same active simulation region. Assert A receives current state; B receives no hidden current state/event and does not gain map discovery/knowledge. After B later gains LOS, B receives the then-current authoritative state. After LOS is lost, each player's remembered-map projection reflects only that player's own prior observations unless an explicit sharing rule is invoked.

24. **Activation/background transition** — A is the sole coverage for region R, leaves until R deactivates, authoritative world time advances, then B enters R. Assert R is loaded once, catch-up from its recorded last-processed chronology is applied once before B's first current projection, active indexes are rebuilt consistently, and no stale pre-catch-up snapshot is replicated.

25. **Coverage handoff without deactivation** — while A covers R, B enters R; A then leaves while B remains. Assert R never deactivates, persists one authoritative state/index, receives no background catch-up, and continues exactly-once active simulation.

26. **Presentation-coordinate isolation** — server places an entity at grid-aligned authoritative WorldPosition P, derives SpatialCell P and replicates the permitted state. A Godot client interpolates its visual transform toward P from a previous tile. Assert server occupancy/LOS/range queries use authoritative cell membership throughout and no fractional/client transform can mutate WorldPosition.

27. **Position/cell abstraction** — construct a test spatial topology in Core where a deterministic precise WorldPosition maps to a SpatialCell through the mapping contract. Assert spawn/move/despawn updates WorldPosition and derived cell/index membership atomically. The Cataclysm profile fixture then constrains positions to grid alignment and proves its WorldPosition/SpatialCell mapping is one-to-one.

28. **No integer-position leakage in generic Core contracts** — conformance/API tests ensure generic entity-position mutation and projection code uses the WorldPosition/cell-mapping abstraction rather than requiring Cataclysm map-square tuples everywhere. Cataclysm-specific rules may continue to use typed grid cells explicitly.

## Dependency and sequencing notes

This spec depends on #83 for stable IDs/registries and #66 for global turn semantics. Implementation should precede or provide the substrate for:

- #85 persistence;
- #82 events/EOCs that address map positions;
- #70/#71 item placement and transfer;
- #73 construction;
- #74 combat/projectiles;
- #78 overmap/mapgen;
- #79 environment/fields/fire/scent;
- #80 vehicles;
- #81 AI/pathfinding;
- #86 UI/map presentation.

The environment ticket should own detailed field/fire/scent evolution. The vehicle ticket should own vehicle physics/parts. The AI ticket should own actor-specific path choice. This spec owns the spatial facts and query interfaces those systems consume.

## Compatibility decisions and non-goals

For feature parity, OctoGhast **must** match coordinate meaning, tile-layer behavior, movement/passability results, deterministic line semantics, z bounds, load/shift identity and observable persistence.

OctoGhast **does not need** to copy:

- CDDA's `map`/submap C++ class structure;
- uniform-submap memory optimization;
- exact cache data structures;
- map-file packing/compression layout, unless #85 later selects binary/save-file compatibility;
- runtime integer IDs.

Where exact reference behavior is not yet pinned by a test, add a differential fixture against the pinned CDDA commit before depending on it.

## Implementation-ready acceptance criteria

The #77 spec is satisfied when an OctoGhast implementation can demonstrate:

- typed coordinate origins/scales and boundary-safe conversion;
- the pinned 12×12 submap / 11×11 bubble / 21-z-level geometry;
- persistent submaps keyed by absolute coordinates;
- complete logical tile layers and stable IDs;
- controlled mutation with correct derived-cache invalidation;
- base movement/passability composition;
- deterministic LOS line stepping and transparency/light query boundaries;
- whole-submap bubble shifting without changing absolute identity;
- checked vertical-level behavior;
- save/load round-trip preservation of local-map state;
- the 28 black-box scenarios above as automated tests or equivalent stronger coverage;
- server/world-owned active-region union semantics for separated and overlapping players;
- activation/deactivation with chronology-based background catch-up and no duplicate actualization;
- authoritative spatial indexing across all active regions without ECS-wide scans;
- per-player FOV, knowledge, remembered-map and replication-interest isolation;
- authoritative WorldPosition, derived SpatialCell membership and Godot PresentationTransform kept strictly separate; the pinned Cataclysm profile remains grid-aligned while generic Core does not structurally forbid deterministic sub-cell positioning.

At that point dependent systems can implement against OctoGhast spatial interfaces without rediscovering CDDA's local-map semantics.
