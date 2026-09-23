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

### Persistent owner: world submap store

The reference `mapbuffer` is the world-wide owner/cache of submaps, keyed by `tripoint_abs_sm`. A lookup:

1. returns an already buffered submap if present;
2. otherwise attempts to deserialize the containing persisted map data;
3. returns null/failure if the submap cannot be obtained.

For OctoGhast, define an interface equivalent to:

`SubmapStore.get_or_load(AbsoluteSubmapPos) -> Submap?`

and separate persistence from the active map view. The loaded reality bubble should reference/lease world submaps rather than becoming their independent authoritative copy.

### Active reality bubble

The active map is a rectangular XY window over absolute submaps plus either all supported z levels or a selected z level, depending on world/runtime mode.

A full load:

1. clears location-derived indexes (trap/field/active-item/vehicle indexes);
2. sets the absolute submap origin;
3. clears vehicle level caches;
4. loads each XY submap column from the world store;
5. rebuilds vehicle caches;
6. actualizes loaded submaps only after the required set is present, avoiding edge behavior against partially loaded neighbors;
7. reconciles derived indexes/caches.

This ordering is observable for edge-crossing entities and should be preserved semantically.

### Bubble shifting

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

### ActiveMap

Own the reality-bubble window, absolute origin, loaded-submap references, bubble/absolute conversion, shifting, tile queries/mutations and cache invalidation.

### SpatialCaches

Own per-z derived transparency/light/outside/floor/pathfinding/vehicle/visibility data. Caches are disposable and reconstructible.

### SpatialQuery

Expose passability, movement cost, LOS, transparency, occupancy and neighborhood/radius queries to combat, AI, activities and UI.

This decomposition preserves behavior while avoiding a direct port of the large CDDA `map` class.

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
- the 20 black-box scenarios above as automated tests or equivalent stronger coverage.

At that point dependent systems can implement against OctoGhast spatial interfaces without rediscovering CDDA's local-map semantics.
