# Spec 13 — Overmap, world generation and local mapgen

Status: investigation complete / implementation-ready specification  
Parent: [#65](https://github.com/LambdaSix/OctoGhast/issues/65)  
Ticket: [#78](https://github.com/LambdaSix/OctoGhast/issues/78)  
Reference implementation: `LambdaSix/Cataclysm-DDA`  
Pinned parity baseline: `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Purpose and scope

This specification defines the OctoGhast contract for strategic world topology generation and later tactical-map materialization. It covers overmap terrain, regions, cities, roads/connections, specials, mapgen selection, palettes, nested/update mapgen, start locations, world-generation RNG, persistence and multiplayer/server-authority implications.

It consumes rather than redefines:

- Spec 01 / #66: canonical authoritative time and deterministic server ordering.
- Spec 12 / #77: typed absolute coordinates, OMT/submap/map-square scale rules, server/world-owned active regions, visibility/knowledge projection and persistent submap ownership.
- Spec 18 / #83: typed durable string IDs, registries, JSON loading, inheritance, finalization and validation.
- Spec 20 / #85: server-owned save sets, deterministic save barriers, stable world/player identity and persisted RNG/continuation state.
- #90: transport/session/backpressure architecture; world generation is a simulation concern and never a socket callback concern.

The implementation target is observable parity with the pinned CDDA rules/data while retaining a generic Core boundary. CDDA-specific generation algorithms and schemas belong to the Cataclysm profile unless they are genuinely generic capabilities.

## 2. Authoritative pinned evidence

Primary evidence inspected at the pinned commit:

- `src/overmap.cpp`, `src/overmap.h` — overmap generation pipeline, persistent terrain layers, cities, roads/highways/rail, forests/water/ravines, specials, underground/above-ground continuation, monster groups/radios, predecessor tracking and mapgen arguments/decisions.
- `src/overmap_special.cpp`, `src/overmap_special.h` — fixed/mutable specials, placement constraints, occurrences, city constraints, locations, connections, uniqueness flags, validation and parameter finalization.
- `src/overmap_connection.cpp`, `src/overmap_connection.h` — connection definitions and directional/network placement.
- `src/regional_settings.cpp`, `src/regional_settings.h` — region settings, city defaults, forests, trails, water/highway settings, map extras, regional terrain/furniture resolution and weighted building bins.
- `src/mapgen.cpp`, `src/mapgen.h` — weighted mapgen registry, JSON/builtin mapgen, palettes/pieces, phases, nested mapgen, update mapgen, rotations, parameters, predecessor mapgen and consistency checks.
- `src/start_location.cpp`, `src/start_location.h`, `data/json/start_locations.json` — start-location definitions, matching, city/z constraints, parameters and failure behavior.
- `doc/JSON/OVERMAP.md`, `doc/JSON/REGION_SETTINGS.md`, `doc/JSON/REGION_LAYOUT.md`, `doc/JSON/MAPGEN.md` — data contracts and authoring semantics.
- `data/json/overmap/**`, `data/json/region_settings/**`, `data/json/mapgen/**`, `data/json/mapgen_palettes/**` — production fixtures.
- `tests/overmap_test.cpp` — mandatory/optional special placement, mutable-special placement, terrain coverage and deterministic overmap generation.
- `tests/mapgen_function_test.cpp` — directional connectivity behavior.
- `tests/start_location_test.cpp` — start-location generation evidence.

All behavior below is rooted in that pinned commit, not current upstream HEAD.

## 3. Reference model versus OctoGhast model

### 3.1 Pinned CDDA reference behavior

CDDA creates strategic overmap topology as overmaps are needed. Local tactical content is not necessarily materialized at the same time: mapgen is selected/run when the relevant tactical area is first generated. Overmap terrain therefore acts as a durable strategic template/input to local mapgen.

The baseline uses one game process and a global RNG engine. Its deterministic test demonstrates that restoring the same RNG engine state and generating the same 2x2 overmap cluster in the same sequence produces identical z=0 OMT terrain snapshots.

### 3.2 OctoGhast Cataclysm-profile adaptation

OctoGhast retains the same strategic/tactical split and Cataclysm data semantics, but generation is server-authoritative:

- a client may request movement/exploration that causes new world data to become required, but cannot author generated topology or tactical tiles;
- first generation/materialization occurs only in a deterministic authoritative simulation phase;
- two players discovering the same ungenerated location concurrently cause exactly one generation/materialization transition;
- overlapping active regions share one persistent world record and one generated submap set;
- disconnected/reconnecting clients do not own generation progress;
- opening map/overmap UI does not pause generation or canonical time;
- clients receive only player-appropriate projected overmap knowledge and tactical state, not hidden generated world data.

### 3.3 Generic Core contract

Core needs reusable capabilities, not CDDA algorithms:

- stable world identity and absolute spatial keys;
- immutable definition registries;
- deterministic authoritative generation jobs;
- world-seed/RNG stream ownership;
- atomic "unmaterialized -> generated" persistence transitions;
- durable generated records separated from transient active-region residency;
- query interfaces that can distinguish unknown/not-generated/generated without leaking hidden state to clients.

City layout algorithms, OMT types, CDDA special rules, 24x24 OMT mapgen and CDDA JSON schemas are Cataclysm-profile policy.

### 3.4 Future evolution seams

Core MUST NOT require:

- every world to use 180x180 OMT overmaps;
- every tactical generator to emit 24x24 tile blocks;
- city/road/special algorithms identical to CDDA;
- procedural generation to use one specific RNG algorithm forever;
- all future games to expose an overmap UI or tile grid.

Those constraints remain profile/data-level where possible.

## 4. Immutable definitions versus mutable world state

### 4.1 Immutable/finalized definitions

After Spec 18 finalization, the following are immutable reference definitions for a running world/content set:

- overmap terrain/type definitions and flags;
- overmap locations;
- overmap connection definitions;
- overmap special definitions and placement constraints;
- region settings and region-layout definitions;
- city building bins/weights;
- mapgen definitions and weighted variants;
- mapgen palettes;
- nested-mapgen and update-mapgen definitions;
- mapgen parameter definitions;
- start-location definitions.

Durable identity uses typed string IDs. Registry integer indexes are caches only and MUST NOT be persisted.

### 4.2 Mutable authoritative world state

Mutable world state includes, as applicable:

- chosen region for each generated overmap;
- generated OMT terrain layers and predecessor chains;
- city instances (position, size/name and other runtime fields);
- realized roads/connections/highways/railways;
- placed special instances, their chosen orientation/joins and uniqueness accounting;
- persistent overmap notes/reveal/exploration state under the owning player/world knowledge contract;
- mapgen arguments and persistent mapgen parameter decisions that must remain stable on later materialization;
- generated tactical submaps and their entities/items/vehicles/environment state;
- whether a tactical area has been materialized;
- update-mapgen effects already applied where repetition would be observable;
- generation RNG/decision state required for deterministic continuation.

Definition objects are never mutated to represent one world's placement result.

## 5. Strategic coordinate/topology contract

Spec 12 remains authoritative for coordinate math.

For the Cataclysm profile:

- one overmap is 180x180 OMTs;
- one OMT is 2x2 submaps = 24x24 map squares;
- z levels are -10 through +10;
- world-facing keys use absolute typed coordinates;
- overmap-local coordinates are transient decomposition results, not durable global identity.

A generated strategic tile is addressed by absolute OMT coordinate plus dimension/world identity. Neighbor relations use the typed coordinate system and must be valid across overmap-file boundaries.

Generation of adjacent overmaps MUST account for durable edge/network information needed to continue rivers/roads/highways/etc. It must never assume an adjacent client view owns that boundary.

## 6. Region selection and settings

### 6.1 Region layout

Pinned `REGION_LAYOUT.md` defines region assignment at overmap scale. It includes:

- UNIFORM: every overmap uses one configured region;
- RANDOM: weighted region selection;
- ANGLES: region sectors around overmap (0,0);
- static/Voronoi-style layouts with explicit generated bounds and dynamic out-of-bounds fallback.

The pinned documentation warns that cross-region adjacent-overmap feature generation does not fully account for differing neighbor regions. OctoGhast MUST preserve reference behavior for the Cataclysm profile rather than silently "fixing" cross-region seams during parity work. Any later improvement is an intentional post-reference change.

### 6.2 Region settings

Pinned region settings compose references for rivers, lakes, oceans, ravines, forests, forest composition/trails, highways, cities, map extras, regional terrain/furniture, weather, default OMT by z level, groundcover, feature flag filters and named overmap connections.

Defaults visible in `region_settings_city` include:

- `city_size = 8`;
- `city_spacing = 4`;
- `shop_radius = 30`;
- `shop_sigma = 20`;
- `park_radius = shop_radius`;
- `park_sigma = 100 - park_radius`;
- city name snippet `<city_name>`.

Loaded JSON can override these through finalized typed definitions. Region-specific weighted building bins select houses, shops and parks.

## 7. Overmap generation ordering

The pinned `overmap::generate` pipeline has observable ordering dependencies. At the inspected baseline, surface generation proceeds broadly as:

1. initialize/default terrain and regional base features;
2. water/lakes/oceans as enabled;
3. forests, swamps, ravines;
4. polish rivers before highway work where enabled;
5. place highways;
6. place cities;
7. highway interchanges;
8. build cities;
9. forest trails;
10. railroads/roads in region-configured relative order;
11. overmap specials;
12. finalize highways;
13. trailheads;
14. repolish rivers after specials;
15. generate required underground levels downward;
16. generate required above-ground levels upward;
17. place monster groups/radios after terrain topology exists.

This ordering is normative where changing it changes placement validity, predecessors, connectivity, RNG consumption or resulting topology. Implementations may decompose code differently but MUST reproduce observable results for conformance fixtures.

## 8. Cities, roads, connections and specials

### 8.1 Cities

City placement/building is region policy. A city instance has a strategic location and size and is used by special/start-location constraints. Building selection uses weighted region city bins.

The implementation must preserve the distinction between:

- choosing city centers/sizes/topology;
- connecting/building streets;
- selecting city buildings/specials;
- later tactical mapgen for those OMTs.

### 8.2 Connections

Connection definitions describe directional network terrain and are consumed by road/rail/sewer/subway/special placement. Directional terrain variants encode which cardinal edges connect.

The baseline connectivity test asserts the expected north/east/south/west membership for two-, three- and four-way sewer OMT suffixes. OctoGhast conformance must pin equivalent connection truth tables rather than infer them from renderer sprites.

### 8.3 Specials

An overmap special is a strategic multi-OMT placement definition. Important constraints include:

- occurrence interval;
- eligible location/terrain predicates;
- city size and distance constraints;
- rotation/orientation;
- internal OMT layout;
- declared connections/joins;
- fixed versus mutable subtype;
- flags such as `OVERMAP_UNIQUE`, `GLOBALLY_UNIQUE`, `CITY_UNIQUE`;
- mapgen parameters that may be chosen at special/OMT scope.

The three uniqueness flags above are mutually exclusive at validation time.

Baseline consistency checking sums mandatory minimum occurrences and warns when they exceed estimated overmap special capacity. Mandatory specials are expected to meet their minimum placement count when possible; the baseline slow test checks minimum occurrence counts over generated overmaps.

Optional specials remain eligible even when another impossible mandatory special exists; the baseline test explicitly checks that a Cabin can still appear at the origin when a Lab's city requirement is made impossible.

Mutable-special placement must be valid often enough for its data/constraints; the pinned test runs repeated placements for test crater/microlab fixtures and requires >50 successful placements per 100 trials in each sampled overmap.

## 9. Strategic-to-tactical materialization boundary

Strategic generation and tactical materialization are separate lifecycle states.

Conceptually:

```text
Unknown strategic area
  -> generated overmap topology (durable OMT IDs / special decisions)
  -> tactical area requested
  -> select mapgen using OMT + region + neighbors + predecessors + persistent parameters
  -> generate 24x24 OMT tactical result
  -> split/store as persistent submaps
  -> thereafter load/mutate persisted tactical state; do not reroll base mapgen
```

A generated OMT terrain ID is not itself the tactical tile array. It selects/parameterizes mapgen.

First tactical materialization MUST be atomic from the external point of view. Two simultaneous requests cannot each roll and commit different maps.

Once a tactical area has durable mutated state, ordinary activation loads that state. Base mapgen is not rerun merely because all players left and later returned.

## 10. Mapgen selection, weights and validation

### 10.1 Registry and selection

Mapgen definitions are registered by mapgen key, generally corresponding to an OMT mapgen ID. Multiple definitions may contribute weighted variants.

Pinned `load_mapgen_function` defaults `weight` to 1000 and rejects constant weight bounds below 0 or at/above `INT_MAX`. A definition must specify either a valid builtin function or an `object`; otherwise loading fails.

Cataclysm-profile compatibility requires the JSON form and weighted selection behavior. Builtin C++ functions are reference evidence only; OctoGhast may independently implement equivalent generators in C# or declarative form.

### 10.2 Setup/finalization

The baseline performs mapgen setup, then parameter finalization, then consistency checks. Nested and update mapgens participate in the same staged validation.

OctoGhast MUST not run a partially finalized generator. Missing IDs, malformed palette content, illegal coordinates/weights or inconsistent mapgen/OTER relationships must be diagnosed during content validation where possible.

## 11. Mapgen object contract

Pinned documentation states ordinary OMT mapgen operates on a 24x24 tile area. A JSON mapgen can define:

- background/fill terrain;
- ASCII `rows`;
- symbol mappings for terrain/furniture/traps/items/fields/etc.;
- explicit point/line/square placement operations;
- item, monster, NPC, vehicle, computer and other spawn pieces;
- mapgen parameters and parameter-derived values;
- palettes;
- nested mapgen;
- rotations;
- predecessor mapgen;
- map extras and other supported pieces.

The exact complete set is a Cataclysm content-compatibility surface and should be implemented incrementally against production fixtures, but unsupported fields MUST fail/diagnose explicitly rather than be silently ignored when they affect behavior.

## 12. Mapgen phases and ordering

The pinned `mapgen_phase` order is:

1. removal;
2. terrain;
3. furniture;
4. default;
5. nested_mapgen;
6. transform;
7. faction_ownership;
8. zones.

Mapgen pieces are applied according to phase, not arbitrary JSON/object iteration order. Within a phase, deterministic loaded/order semantics must be retained where observable.

This phase ordering is part of the parity contract because later pieces may depend on terrain/furniture or removal performed earlier.

## 13. Palettes

A palette is reusable mapgen symbol/content mapping. Palettes may themselves compose definitions and may contain mapgen values/parameters.

Required behavior:

- palette IDs use stable typed definition identity;
- palette composition resolves during content setup/finalization;
- conflicts/overrides follow pinned palette semantics and Spec 18 source precedence;
- generated output after palette expansion is equivalent to writing the resolved mappings directly;
- palette use does not introduce mutable world identity; only selected persistent parameters/results do.

Representative fixtures should include a common/base palette plus a building-specific palette and parameterized palette.

## 14. Rotation and predecessor semantics

Ordinary JSON mapgen may specify a rotation and OMTs may themselves be rotatable/linear.

The pinned generator deliberately compensates rotations around predecessor mapgen so predecessor layers remain aligned; it samples the mapgen rotation once and reuses that value for inverse/forward rotation.

Required contract:

- rotation is in quarter turns modulo four for the Cataclysm grid profile;
- one random rotation decision is reused consistently throughout that generation;
- OMT orientation and explicit mapgen rotation compose deterministically;
- predecessor terrain/mapgen is generated/applied before the new layer when required;
- predecessor chains persisted on the overmap must survive save/load because later tactical generation can depend on them.

## 15. Nested mapgen

Nested mapgen definitions are weighted registries keyed by `nested_mapgen_id`.

`place_nested` can condition selection on:

- neighboring OMT IDs/match rules;
- mutable-special joins;
- OMT flags;
- predecessors;
- selected z levels.

Nested pieces may target relative z offsets. Pinned documentation allows explicit nested z placement within -20..20 for that field, though actual world coordinate validity remains subject to map/world bounds.

Nested choice belongs to authoritative generation RNG. If its result affects durable tactical state, it is not a client-side decoration.

## 16. Update mapgen

Update mapgen mutates an already-existing tactical map using a named `update_mapgen_id`; it is not equivalent to base first-generation mapgen.

The baseline exposes verification/collision behavior (notably vehicle collision checks) and optional mirroring/rotation.

Implementation contract:

- update mapgen is an explicit authoritative world-mutation operation;
- caller must supply/resolve an absolute target and validated arguments;
- any preflight/verification required by the selected update runs before externally visible commit;
- failed verification/collision leaves authoritative state unchanged;
- successful update mutations are atomic at the simulation boundary;
- repeated invocation is allowed only when the invoking game rule says it is repeatable; the engine must not accidentally replay an already-consumed one-shot update during load/reactivation;
- multiplayer clients submit the underlying gameplay intent; they do not apply update-mapgen locally.

## 17. Mapgen parameters and stable decisions

Pinned mapgen supports parameter scopes including `overmap_special`, `omt`, `nest` and `omt_stack`.

A parameter decision whose scope extends beyond one ephemeral generator call must remain stable whenever later materialization/reload must observe the same value. The baseline overmap stores mapgen arguments/decisions for this purpose.

OctoGhast MUST persist scoped decisions/arguments whenever rerolling would change observable content. A fallback defined by pinned data is used when the baseline specifies one; missing required parameter data without a valid fallback is a deterministic generation error, not permission to choose a new random value silently.

## 18. Start locations

### 18.1 Definition schema

A pinned start-location definition includes:

- stable `id`;
- translated `name`;
- one or more terrain targets;
- target match type/optional mapgen parameters;
- city size interval, default `[0, INT_MAX]`;
- city distance interval, default `[0, INT_MAX]`;
- allowed z levels, default full overmap depth/height;
- flags such as `ALLOW_OUTSIDE` or `BOARDED`.

A string terrain entry defaults to OMT type matching. Object entries can choose explicit match type and parameter assignments.

Validation diagnoses empty target sets, empty target strings, invalid exact/type IDs and pattern matches that match no loaded OMT definition.

### 18.2 Selection behavior

Without a specific city, the baseline:

1. randomly selects one target from the start-location target list;
2. searches overmaps closest-first around the origin with radius 3, generating overmaps as necessary;
3. returns the first overmap containing a matching OMT;
4. returns invalid and displays an error if no location is found.

For a specific city, candidates are searched within the city area and allowed z range, filtered by city size/distance and target match; one valid candidate is chosen randomly. No candidate produces invalid/failure.

Start-location mapgen parameters are bound to the chosen OMT before/for materialization so scenario-specific palettes/choices remain stable.

### 18.3 OctoGhast adaptation

Character/world creation is an authoritative server workflow. A client may select a scenario/start-location definition but cannot choose a hidden coordinate outside server validation.

In co-op, joining an existing world is not automatically a fresh world-generation operation. Spawn/join policy may reuse this start-location machinery only when the higher-level character/session specification requests it. This spec does not invent a multiplayer spawn policy.

## 19. RNG and determinism

### 19.1 Pinned evidence

The baseline overmap deterministic test:

- snapshots the global RNG engine;
- clears overmap state;
- restores the same engine state;
- generates the same 2x2 overmap cluster in the same sequence three times;
- requires every z=0 OMT terrain ID to match.

Therefore hidden nondeterminism is not acceptable for generation.

### 19.2 OctoGhast contract

All generation randomness is server-owned deterministic simulation randomness.

For the Cataclysm profile:

- same pinned definitions/content set, same initial world/generation RNG state, same world options and same ordered generation requests MUST produce the same generated results;
- clients never seed or consume authoritative generation RNG;
- render FPS, connection latency and client reconnect timing MUST NOT change generation output;
- same-boundary competing generation requests use Spec 01's stable deterministic ordering before RNG-consuming generation begins;
- save/load persists enough RNG stream state and durable generation decisions to continue identically;
- save operations themselves consume no generation RNG.

Core may later introduce independently keyed RNG streams, but doing so for the Cataclysm profile must preserve required parity/distribution behavior and is not mandated by this spec.

### 19.3 Statistical conformance

Exact golden generation is required for controlled seeds/ordered requests. In addition, broad-content tests should use statistical/coverage constraints where exact layouts are intentionally random.

Pinned examples include:

- mandatory special minimums across generated overmaps;
- optional specials remaining placeable;
- mutable special placement success fixtures;
- terrain coverage tests that repeatedly generate regions and flag eligible OMT types never observed.

OctoGhast tests should use fixed seeds and declared sample counts/tolerances so failures are reproducible.

## 20. Persistence

Per Spec 20, generated topology and tactical maps are world-owned save state.

Persist at least:

- world/content/options identity needed to interpret generation;
- region assignment where dynamically chosen;
- generated overmap terrain and durable overmap records;
- city/special/connection runtime placement state;
- predecessor chains;
- scoped mapgen arguments/decisions;
- tactical submap state after first materialization;
- durable one-shot/update-mapgen application state where required;
- generation RNG state/stream continuation data.

Do NOT persist:

- connection/socket IDs;
- Godot nodes/transforms;
- client camera/map-panel state as world topology;
- transient active-region membership;
- temporary generation worker/thread objects.

A save barrier cannot observe a half-committed strategic special or half-materialized OMT. Generation commits must be atomic persistence-domain transitions.

## 21. Authoritative-time and activity relationship

Base procedural generation is not a CDDA actor action with a player move cost. It is world realization required to service authoritative simulation.

OctoGhast MUST NOT charge arbitrary wall-clock milliseconds or player moves merely because generation computation takes CPU time.

If gameplay invokes an action/activity that subsequently triggers update mapgen or terrain transformation, the gameplay system retains its pinned move/activity cost; generation/mutation is the resulting authoritative effect.

Canonical simulation time must not advance merely because a generation worker is slow. If generation is implemented asynchronously for performance, its result may only become authoritative at a deterministic simulation boundary and must preserve request/order/RNG semantics.

## 22. Multiplayer concurrency and contention

Required cases:

- **same OMT first discovery:** one generation owner; later contender observes the committed result;
- **same overmap generation:** one strategic generation transition; no duplicate special/uniqueness accounting;
- **adjacent overmaps:** deterministic generation ordering and durable edge data prevent races at shared topology boundaries;
- **update-mapgen conflict:** commands are ordered deterministically; later command revalidates against state left by the earlier command;
- **player disconnect during generation:** connection loss does not roll back or orphan authoritative world generation once admitted; result belongs to world state;
- **reconnect:** client receives current projected result, never a locally replayed generator.

Implementation may use locks/jobs internally, but externally the world behaves as one serialized authoritative history.

## 23. Projection and information boundaries

Overmap topology can contain hidden information. Generation existence is not permission to reveal it.

Per Spec 12:

- each player receives only their known/revealed/visible overmap projection;
- unexplored special/city/map details remain hidden unless another rule reveals them;
- tactical materialization outside a player's interest/visibility is not replicated merely because it exists;
- notes/exploration/remembered-map state follow the existing player-specific knowledge contract;
- Godot receives DTOs/projections and does not load Cataclysm world JSON to independently reconstruct authoritative hidden topology.

Map/overmap UI is a query/presentation surface; opening it does not pause the server.

## 24. Failure and validation behavior

Content-time failures/diagnostics include:

- unknown/invalid typed IDs;
- malformed region or special references;
- invalid mutually exclusive uniqueness flags;
- impossible/malformed mapgen weight;
- mapgen missing both builtin and object;
- invalid palette/nested/update mapgen references;
- start location with no terrain or invalid target;
- inconsistent mapgen versus OMT properties.

Runtime generation failures:

- failure to satisfy a required start-location search returns explicit failure/invalid, never a fabricated coordinate;
- impossible mandatory-special placement is diagnosed and remains observable in conformance tests;
- out-of-bounds coordinate requests are rejected according to Spec 12;
- failed update-mapgen verification/collision is non-mutating;
- generation exceptions must not leave a record marked generated with partial content.

Server/network handling returns structured failure/results to the requesting player when player-visible. Internal hidden reasons must not leak undiscovered world information.

## 25. Implementation interfaces and ownership

A suggested capability decomposition, without prescribing concrete C# classes:

```text
Core
  IWorldGenerationCoordinator
  IGenerationRng / deterministic RNG context
  ISpatialPersistenceStore
  atomic materialization state
  deterministic job admission/commit

Cataclysm profile
  overmap terrain/region/special registries
  CataclysmOvermapGenerator
  CataclysmMapgenRegistry
  palette/nested/update interpreters
  start-location resolver

Server/application
  admits exploration/world-creation intents
  schedules generation at deterministic boundaries
  projects player-specific results

Godot client
  sends intents / queries
  renders projected overmap/tactical state
  never performs authoritative generation
```

Stable references crossing persistence/protocol boundaries use world ID + typed absolute coordinate and stable definition IDs as appropriate. Raw pointers, registry indexes, ECS storage indexes and Godot object IDs are forbidden.

## 26. Implementation sequencing

Implement in this order:

1. finalized overmap/region/connection/special definition model on Spec 18;
2. persistent overmap strategic record keyed by Spec 12 coordinates;
3. deterministic region/base-topology generation;
4. cities/connections/special placement and predecessor/mapgen-decision persistence;
5. base mapgen registry, weighted selection and simple JSON terrain/furniture palettes;
6. full mapgen phase ordering and representative spawn pieces;
7. nested mapgen/parameters/predecessors/rotation;
8. tactical first-materialization persistence;
9. update mapgen;
10. start-location resolver/world-creation integration;
11. broaden production JSON compatibility and statistical parity fixtures.

This separation lets overmap topology tests run without constructing tactical maps, and mapgen tests run against synthetic OMT fixtures without regenerating whole worlds.

## 27. Black-box / conformance scenarios

### WG-01 Coordinate-boundary continuity
Generate adjacent overmaps spanning an absolute overmap boundary. Assert road/river/connection continuations use typed absolute coordinates and no duplicate/disconnected edge caused by local-coordinate aliasing.

### WG-02 Deterministic 2x2 cluster
With fixed content/options and identical saved RNG engine/stream state, generate the same ordered 2x2 overmap cluster three times. Snapshot all z=0 OMT IDs and require exact equality.

### WG-03 Renderer/network independence
Run WG-02 headless, with a 30 FPS Godot client and with a 144 FPS client plus injected network latency. Supply identical admitted exploration intents/order. Generated world state is identical.

### WG-04 Concurrent first discovery
Two player-controlled actors cause the same ungenerated OMT to become required on the same canonical boundary. Assert exactly one generation/materialization commit and both later observe the same world state.

### WG-05 Overlapping region uniqueness
Two player active regions overlap a special-containing overmap. Assert special placement, mapgen parameter decisions and tactical materialization happen once, not once per player.

### WG-06 Separated players
Players discover different overmaps concurrently. Assert both are generated under stable deterministic request ordering and neither client's visibility projection reveals the other's hidden generated topology.

### WG-07 Mandatory special minimum
For a fixed fixture batch/seed set, generate the declared sample area and assert every placeable mandatory special meets its minimum occurrence constraint or emits the expected explicit placement diagnostic.

### WG-08 Optional special survives impossible mandatory
Use the pinned Cabin/Lab-style fixture: make the mandatory special's city requirement impossible while retaining an optional Cabin. Assert the optional special can still be placed at the origin.

### WG-09 Uniqueness flags
Load fixtures with pairs of `GLOBALLY_UNIQUE`, `OVERMAP_UNIQUE`, `CITY_UNIQUE`. Assert multiple flags on one definition diagnose validation failure. For valid definitions, assert placement counts obey their scope.

### WG-10 Mutable special placement
Run fixed-seed repeated `test_crater`/`test_microlab`-equivalent placement trials. Require the pinned test's >50/100 valid-placement threshold for each sampled overmap fixture.

### WG-11 Region override
Load default region plus a copy-from override changing city/forest settings. Generate a fixed-seed overmap and assert the finalized overridden settings, not raw base values, drive generation.

### WG-12 Region-layout weighted determinism
For RANDOM region layout with a fixed weighted list and RNG state, assert exact region choices repeat. Across a declared larger seed set, verify observed frequencies remain within a documented statistical tolerance.

### WG-13 Connection truth table
For two-, three- and four-way sewer/road fixtures, assert directional connectivity exactly matches pinned north/east/south/west suffix behavior.

### WG-14 Strategic before tactical
Generate an overmap containing a house OMT without materializing its tactical submaps. Assert strategic OMT exists while no tactical submap record exists. Enter/activate it, then assert mapgen materializes and persists the tactical record.

### WG-15 No base reroll after unload
Materialize an OMT with random mapgen variation, save, deactivate/unload, reactivate and reload. Assert the exact prior tactical state returns and base mapgen RNG is not consumed again.

### WG-16 Weighted mapgen selection
Create two mapgen variants with known weights and fixed RNG. Assert exact chosen sequence for a golden seed and statistical ratio over a larger deterministic sample.

### WG-17 Invalid weight
Load negative or >=INT_MAX constant mapgen weights. Assert validation rejects/diagnoses them before runtime generation.

### WG-18 Missing mapgen body
Load a mapgen with neither valid builtin nor `object`. Assert deterministic content-load failure.

### WG-19 Phase ordering
Construct a mapgen where removal, terrain, furniture, default, nested, transform, faction ownership and zone operations produce distinguishable output if reordered. Assert pinned phase result.

### WG-20 Palette equivalence
Generate one fixture through a reusable palette and one with the fully expanded equivalent mapping under the same inputs. Assert equal tactical terrain/furniture/spawn state.

### WG-21 Rotation
Generate a non-symmetric fixture at all four quarter-turn orientations. Assert OMT orientation plus explicit mapgen rotation compose correctly and all linked z/predecessor features remain aligned.

### WG-22 Predecessor chain persistence
Strategically replace/refine a requires-predecessor OMT, save before tactical materialization, reload, then materialize. Assert predecessor mapgen result is identical to uninterrupted execution.

### WG-23 Nested neighbor condition
Use a nested fixture conditioned on north/east neighbors and flags. Generate matching and non-matching neighbor arrangements and assert chunks/else_chunks are selected accordingly.

### WG-24 Scoped parameter stability
Choose an `overmap_special` or `omt_stack` parameter, materialize one affected OMT, save/reload and later materialize another affected OMT. Assert the scoped chosen value is reused, not rerolled.

### WG-25 Update-mapgen atomic success
Apply a valid update-mapgen. Assert all mutations become visible together at the authoritative boundary and persist through reload.

### WG-26 Update-mapgen rejection
Create a verification/vehicle-collision failure. Assert no terrain/item/vehicle mutation, no one-shot consumed flag and an explicit failure result.

### WG-27 Same-tick update contention
Two players request conflicting update-producing actions on the same area. Assert Spec 01 ordering; the second revalidates against the first result and cannot overwrite from stale client state.

### WG-28 Start-location terrain matching
Exercise exact/type/prefix/contains start-location target forms against a fixed generated overmap. Assert only matching OMT definitions are candidates.

### WG-29 Start-location city constraints
Use city size, distance and z intervals with both valid and invalid candidate OMTs. Assert only in-range candidates can be selected.

### WG-30 Start-location search failure
Provide a start-location definition with no matching OMT in the radius-3 search fixture. Assert explicit invalid/failure and no fabricated spawn.

### WG-31 Start mapgen parameters
Use an evacuation-shelter-style start definition that supplies a palette parameter. Assert chosen start OMT materializes with that parameter and remains stable after save/load.

### WG-32 Save during generation boundary
Request save while a generation job is computationally in progress. Assert committed snapshot is either wholly before or wholly after the generation commit, never partial; save consumes no RNG/time.

### WG-33 Disconnect during admitted generation
Disconnect the requesting player after authoritative admission but before projection. Assert generation/world commit remains valid, world state persists, and reconnect receives the authoritative projection subject to knowledge rules.

### WG-34 Terrain coverage
Across a fixed declared seed/sample corpus, enumerate spawn-eligible OMT types excluding the same explicit categories/whitelist rules as the parity fixture. Fail if an expected eligible type never appears; diagnostics identify special/source constraints.

## 28. Acceptance checklist mapping

- [x] Overmap coordinate/topology model and persistence are explicit — sections 4–9, 20.
- [x] City/road/connection/special placement constraints and RNG are specified — sections 7–9, 19, scenarios WG-07–WG-13.
- [x] Region/biome configuration and overrides are mapped — section 6 and WG-11/WG-12.
- [x] Mapgen schema, palette composition, rotations, nested/update generation and validation are defined — sections 10–17, 24 and WG-16–WG-27.
- [x] Boundary contract from overmap terrain to generated submaps is explicit — sections 9 and 20 plus WG-14/WG-15.
- [x] Start-location generation/selection behavior is captured — section 18 and WG-28–WG-31.
- [x] Deterministic seeded generation fixtures and statistical constraints are defined — section 19 and WG-02/WG-10/WG-12/WG-16/WG-34.
- [x] World topology generation is separated from local map materialization for implementation sequencing — sections 9 and 26.

## 29. Completion statement

This investigation found no contradiction with the architecture established by #52/#57/#58/#64/#65 or completed prerequisite Specs 01/12/18/20. No new cross-cutting architecture ticket is required.

The pinned Cataclysm generation rules remain the Cataclysm-profile reference. OctoGhast's intentional adaptation is confined to authority, deterministic admission/commit, multiplayer contention and player-specific projection; it does not replace the underlying placement/mapgen/data rules.
