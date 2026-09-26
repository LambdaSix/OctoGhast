# Spec 23 — Debugging, developer tools and parity test harness

## 1. Purpose and architectural position

This specification defines the implementation contract for OctoGhast developer observability, controlled debug mutation, deterministic test-world construction, and the systematic conformance/parity harness used to measure the Cataclysm reference profile.

The behavioural/reference baseline is pinned to:

`LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`.

This specification is an **engineering-enabler and verification contract**, not a requirement to reproduce Cataclysm:DDA's curses/ImGui debug UI, Catch2 internals, C++ test architecture, file layout, or developer-only presentation exactly.

The governing architectural split is:

1. **Pinned CDDA reference evidence** — the baseline's debug capabilities, test fixtures, deterministic setup practices, test cases and diagnostics are evidence for what must be observable and testable.
2. **OctoGhast Cataclysm profile** — supplies Cataclysm-specific debug catalogues, fixture adapters, expected behaviours and reference-comparison normalizers.
3. **Generic Core/test infrastructure** — supplies authority-aware inspection/mutation commands, deterministic world factories, stable fixture identity, canonical-time/RNG control, capture/trace APIs, scenario execution, snapshot comparison and result reporting without assuming Cataclysm rules.
4. **Future evolution seam** — later OctoGhast rulesets may use different fixture schemas, action economies, spatial models and parity references while reusing the same harness primitives.

The authoritative server remains the sole owner of simulation state in both single-player and co-op. Debugging does not create a privileged client-side mutation path. A local developer UI, headless test runner and remote admin client all use explicit authority-checked debug/test requests that enter deterministic simulation ordering.

## 2. Scope

In scope:

- developer/debug command catalogue and discovery;
- authoritative state inspection and controlled mutation;
- map/overmap/world editing and reset helpers;
- actor/item/vehicle/weather/time/EOC/data inspection and setup;
- structured debug capture, traces, monitors and reports;
- data/content validation;
- deterministic test-world and fixture construction;
- unit, contract, scenario, golden, differential and fuzz/property test layers;
- pinned reference-executable comparison protocol;
- fixture and test-data mod isolation;
- save/load/serialization round-trip testing;
- reference-parity matrix schema and status rules;
- reproducible failure artifacts;
- in-process versus loopback-network equivalence testing;
- multiplayer contention, projection/privacy and reconnect conformance patterns.

Out of scope:

- implementation of gameplay systems being tested;
- production authentication/security design, owned by Spec 29 / #92, and server-account/meta-progression design, owned by Spec 28 / #91;
- reproducing CDDA's exact developer UI;
- shipping unrestricted remote debug mutation to ordinary players;
- making the C++ reference executable a runtime dependency of OctoGhast;
- using wall-clock timing as a substitute for canonical simulation semantics.

## 3. Reference evidence at the pinned baseline

### 3.1 Debug command surface

Primary source evidence:

- [`src/debug_menu_types.h`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/debug_menu_types.h) enumerates the debug action surface.
- [`src/debug_menu.h`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/debug_menu.h) exposes item/monster/mutation/bionic/skill/proficiency wishes, map reveal, debug execution and report/archive entry points.
- [`src/debug_menu.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/debug_menu.cpp) implements the observable actions and categorised action registry.
- [`src/debug_console.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/debug_console.cpp) and [`tests/debug_console_test.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/debug_console_test.cpp) provide a searchable/monitoring developer console and verify action-table/category invariants and persisted per-tab state.
- [`src/debug_capture.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/debug_capture.cpp) and [`tests/debug_capture_test.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/debug_capture_test.cpp) provide bounded log/EOC trace capture, monitors, JSONL output and file rotation.

The pinned `debug_menu_index` includes capabilities covering at least:

- items and item groups;
- monsters, monster groups, NPCs and followers;
- character stats, damage, bleeding, mutations, bionics, skills, theory, proficiencies, spells, martial arts and recipes;
- short/long/overmap teleportation;
- game-state inspection;
- map editor, overmap editor, map reveal, map extras and nested mapgen;
- vehicle spawn/delete/export/battery/effects;
- weather, wind, temperature, snow, sound, scent, lighting, visibility, transparency and radiation;
- chronology/time changes and hour timing;
- horde, faction, NPC path/attack/magic inspection;
- EOC activation, global EOC/global-variable/timed-event inspection;
- reports, save archive, screenshot, benchmark and deliberate crash/end-screen testing;
- quick setup and body normalization.

**Parity interpretation:** OctoGhast does not need one identical menu. It MUST expose equivalent engineering capabilities needed to construct and observe each implemented domain without bypassing authoritative invariants.

### 3.2 Pinned test runner lifecycle and deterministic setup

Authoritative evidence:

- [`tests/test_main.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/test_main.cpp) constructs an isolated test user directory/world, loads core plus selected mods, always adds the `test_data` mod, creates an avatar/map/overmap, seeds the game RNG from the Catch seed at section start, clears messages, and resets common global state after a test case.
- The same runner supports `--rng-seed-fuzz` to rerun with varying seeds while reusing loaded static data.
- [`src/rng.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/rng.cpp) exposes deterministic engine reseeding; distribution objects reset cached state where necessary after reseeding.
- [`tests/rng_test.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/rng_test.cpp) validates probabilistic helpers statistically rather than expecting a single fixed sequence for distributional behaviour.

Reference behaviour that matters:

- a test seed is reportable and reproducible;
- fixture setup is isolated from ordinary user saves/config;
- the same static content can be reused while runtime state is reset;
- random behaviour can be tested either deterministically with a fixed seed or statistically across many samples/seeds;
- test failures retain enough context to reproduce the failing seed/setup.

OctoGhast adaptation: authoritative RNG state may be split into named deterministic streams rather than CDDA's single engine, per the existing system specs. The harness MUST seed, snapshot and report all streams that can affect the scenario.

### 3.3 TEST_DATA as behavioural fixture content

Authoritative evidence:

- [`data/mods/TEST_DATA/modinfo.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/mods/TEST_DATA/modinfo.json) identifies the dedicated test-data mod.
- [`data/mods/TEST_DATA/`](https://github.com/LambdaSix/Cataclysm-DDA/tree/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/mods/TEST_DATA) contains purpose-built fixtures for activities, EOCs, bionics, body parts, construction, damage types, effects, enchantments, factions, fields, furniture, item groups/items, mapgen, materials, missions, monster attacks/factions/groups/monsters, mutations, NPC behaviour, pockets, proficiencies, recipes, requirements, scenarios, terrain, tools, vehicles, weakpoints, weapons, widgets and more.
- [`src/test_data.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/test_data.cpp) loads test-only expected datasets such as known-good/known-bad sets, pulping fixtures, overmap coverage exceptions, drag/efficiency data, expected DPS, bash tests and spawn-test datasets.

Implementation consequence: fixtures are **immutable test definitions/content**, separate from mutable runtime test-world state. OctoGhast MUST support a dedicated test-content generation/version so fixture identifiers and expected values are explicit and reviewable.

### 3.4 Test helpers and controlled world mutation

Representative evidence:

- [`tests/map_helpers_tests.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/map_helpers_tests.cpp) constructs controlled terrain maps, clears furniture/traps/items, spawns monsters, changes time and rebuilds caches.
- [`tests/force_load_game_test.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/force_load_game_test.cpp) documents the distinction between tests requiring initialized game data/world state and `[nogame]` tests.
- [`tests/submap_load_test.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/submap_load_test.cpp), [`tests/safe_reference_test.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/safe_reference_test.cpp) and persistence-adjacent tests demonstrate load/reference round-trip evidence.
- Domain test files in `tests/` provide feature-specific behavioural evidence and are mapped in section 13.

CDDA helpers can mutate globals directly because the reference executable is single-process test code. OctoGhast test helpers MUST instead use server-owned fixture/setup APIs or explicit harness-only world bootstrap hooks so the same invariants apply to one-player and multiplayer simulations.

## 4. Core concepts and ownership

### 4.1 Immutable definitions

The following are immutable for the duration of one test run/scenario unless a test explicitly performs a content-generation reload:

- content definitions and typed IDs;
- test fixture definitions;
- scenario manifests;
- golden expected documents;
- reference baseline identity;
- normalization/tolerance rules;
- parity matrix row identity;
- rules-profile configuration.

Each run records a `ContentGenerationId`/fixture-set identity consistent with Specs 18/19/20.

### 4.2 Mutable runtime state

Mutable test state includes:

- world/ECS state;
- canonical simulation tick and world chronology;
- actor action budgets and activities;
- scheduler/deadline state;
- active-region leases;
- authoritative RNG stream states;
- player identities, controlled entities and session attachment;
- world knowledge/FOV state;
- debug capture buffers and per-run observations;
- in-flight commands and deterministic request order.

Runtime state MUST be reset or recreated according to the selected isolation level before another scenario consumes it.

### 4.3 Ownership

- **Simulation server:** authoritative owner of world state, time, schedules, RNG, activities, spatial state and authoritative debug mutations.
- **Harness coordinator:** owns scenario definition, fixture selection, server lifecycle, synthetic clients, command schedule, observation collection and result comparison.
- **Client probes:** own only client-local presentation/input state and captured projections. They cannot inspect hidden server state unless the scenario explicitly uses an authorized server-side probe.
- **Reference adapter:** owns launching/interacting with the pinned CDDA reference executable or consuming captured reference fixtures. It never mutates OctoGhast state directly.
- **Parity matrix store:** engineering metadata, not world state.

## 5. Debug capability catalogue

Every implemented major domain MUST expose enough **read** capability to diagnose state and enough **controlled write/setup** capability to construct representative scenarios. Capability names below describe contracts, not UI layout.

| Domain | Minimum inspection | Minimum controlled setup/mutation |
| --- | --- | --- |
| Time/scheduler | canonical tick, chronology, eligible actors, deadlines, budgets, activities | set fixture start time before run; advance exact ticks; schedule/cancel harness fixture work |
| Character | stable ID, stats, anatomy, needs, effects, skills/proficiencies, inventory summary | spawn fixture character; set bounded fixture stats/effects/skills through validated debug command |
| Items/inventory | item IDs, definitions, charges, pockets, containment/location refs, active timers | spawn fixture item; transfer through authoritative transaction; set test charges/condition |
| Map/spatial | WorldPosition, SpatialCell membership, terrain/furniture/traps/fields, active regions/index consistency | create/reset fixture region; place terrain/furniture/traps; move/spawn through authoritative spatial API |
| Creatures/NPCs | identity, type, HP/effects, AI goal/path/target, faction | spawn/despawn fixture actor; set relationship/goal fixture inputs |
| Combat | attack inputs, damage instances, armor/weakpoint resolution, projectile trace/result | construct attacker/target/loadout fixtures; execute exact command |
| Crafting/construction | recipe/project IDs, requirements, providers, progress, reserved/consumed resources | create exact resource/site fixture; start/cancel/advance activity |
| Environment | weather, temperature, fields/fire/scent, rot/decay state | set initial fixture weather/field/temperature where profile permits; advance canonical time |
| Worldgen/mapgen | seed, overmap/mapgen IDs, generated region identity, special placement trace | create world/region from explicit seed and content generation |
| Vehicles | stable vehicle/part IDs, position/orientation, energy/fuel, cargo | spawn fixture vehicle; set bounded energy/fuel; issue movement/interaction commands |
| Events/EOC | queued/scheduled EOCs, talkers/context, variables, trace, emitted events | activate named test EOC with explicit context; set test-scoped variable |
| Data/registries | loaded definitions, provenance, inheritance resolution, diagnostics | load isolated test content generation; validate/reload only at test boundary |
| Persistence | save barrier/tick, object IDs, fixup results, migration diagnostics | request save/load/rollback through authoritative persistence service |
| Networking | connection/session/player/control identity, queue/backpressure metrics, request/result trace | synthetic connect/disconnect/reconnect; inject bounded malformed/fragmented test frames in transport tests |
| Projection/UI | per-player projected entities/facts/events, semantic UI state | synthetic player input only; no hidden-state mutation |
| Presentation | resolved semantic visual/audio/localization keys and local fallback diagnostics | client-local pack/language/resource fixture selection |

Read-only developer inspection SHOULD be separable from mutation authority. Production dedicated-server builds MAY expose only safe diagnostics by default.

## 6. Debug/admin command contract

### 6.1 No privileged-avatar path

A debug command is an explicit request:

`DebugRequest { RequestId, Principal, Capability, TargetRef?, Preconditions?, Payload }`

It is admitted at a deterministic simulation boundary and produces:

`DebugResult { RequestId, Accepted/Rejected, Reason, MutatedRefs, ObservationRefs }`

plus normal authoritative events/projections caused by the mutation.

Requirements:

- no Godot node or UI object is authoritative;
- no network callback mutates ECS state;
- a command targeting an entity uses stable server identity/reference;
- commands may target any authorized actor/entity, not only one special avatar;
- commands touching world state participate in the same deterministic ordering as gameplay/admin commands;
- debug mutation MUST maintain spatial, containment, scheduler and persistence invariants;
- failures are explicit and leave authoritative state unchanged.

### 6.2 Authorization

The harness has a dedicated trusted test principal. Interactive developer/admin use MUST distinguish at minimum:

- observe-only diagnostics;
- world/actor mutation;
- destructive lifecycle operations;
- transport fault injection.

Spec 25 / #90 owns session mechanics; Spec 29 / #92 owns production authentication/security policy. This spec owns capability requirements, not an account system. Ordinary players MUST NOT gain debug mutation simply by knowing a message type.

### 6.3 Continuous-time rule

Opening a debug console/UI never pauses the world by itself. A harness requiring a frozen setup MUST create the world in a controlled non-running/pre-start phase or use the server-level pause policy defined by Spec 01. Once the scenario run begins, canonical time progresses only according to the scenario's explicit tick plan/server policy.

A debug time change is not a local clock edit. It is an authoritative chronology/scheduling operation with defined validation. Tests SHOULD prefer creating a world at the required start time or advancing canonical ticks rather than teleporting chronology through states that require intermediate simulation.

## 7. Structured observability and capture

The harness MUST support structured observations with stable schemas rather than scraping rendered UI text.

Minimum channels:

- simulation events/results;
- authoritative state probes selected by scenario;
- scheduler/activity transitions;
- RNG stream metadata/counters where needed for diagnosis;
- EOC execution trace;
- projection messages per synthetic client;
- connection/session lifecycle and queue metrics for #90 tests;
- validation diagnostics;
- save/load/fixup/migration diagnostics;
- optional performance counters.

Capture buffers MUST be bounded. A test that exceeds a configured capture budget fails with a clear `CaptureBudgetExceeded` result rather than consuming unbounded memory.

Trace entries use canonical tick/order identifiers, not host wall-clock timestamps, for behavioural ordering. Host durations may be recorded separately for performance tests and MUST NOT influence parity results.

Failure artifacts SHOULD include:

- scenario ID/version;
- OctoGhast commit/build identity;
- pinned reference identity;
- rules profile/content generation;
- canonical start/end tick;
- all RNG seeds/stream states needed for replay;
- ordered input schedule;
- normalized actual/expected observations;
- first divergence;
- relevant bounded trace window;
- save/snapshot artifact where permitted.

## 8. Test-layer taxonomy

### 8.1 Unit tests

Purpose: pure/local invariants, formulas, parsers, deterministic helpers and value objects.

Properties:

- no server/world unless required;
- no transport;
- fast and highly isolated;
- exact assertions preferred.

Examples: typed ID parsing, move-cost formula, damage arithmetic, JSON field default, reference normalization.

### 8.2 Contract tests

Purpose: reusable interface/boundary guarantees.

Required contract suites include:

- Core rules-profile boundary;
- command admission/result semantics;
- persistence serializer/fixup contract;
- transport semantic equivalence;
- projection privacy;
- registry/definition generation;
- spatial index invariants.

The same contract suite SHOULD run against alternative implementations/backends where possible.

### 8.3 Scenario/conformance tests

Purpose: black-box multi-system behaviour over canonical time.

A scenario defines:

- fixture/content generation;
- initial world seed/time;
- player/actor identities;
- initial authoritative setup;
- scheduled commands/disconnects/reconnects;
- number of ticks or completion condition;
- permitted observations;
- expected authoritative and per-player results.

Scenario tests are the primary implementation-ticket acceptance pattern.

### 8.4 Golden tests

Purpose: stable structured output where the exact normalized result is meaningful and reviewable.

Suitable for:

- resolved definition snapshots;
- generated map fragments after normalization;
- projection DTO traces;
- save-schema snapshots;
- event sequences.

Goldens MUST be deterministic, text/structured-data diffable, versioned with the scenario and regenerated only by an explicit review action.

Do not use goldens for inherently noisy values where semantic assertions are clearer.

### 8.5 Differential/reference tests

Purpose: execute equivalent inputs against pinned CDDA and OctoGhast Cataclysm profile, normalize both outputs and compare the declared parity surface.

Differential tests are strongest for deterministic, externally observable rules. Where the reference cannot be controlled sufficiently, use evidence-derived golden/scenario expectations instead.

### 8.6 Statistical/property/fuzz tests

Purpose: probabilistic rules, broad seed-space coverage and invariant discovery.

Requirements:

- failing seed is always emitted;
- seeded rerun reproduces the failure;
- statistical tests define sample size/confidence/tolerance;
- fuzz tests assert invariants rather than arbitrary exact sequences unless sequence identity is itself the contract.

### 8.7 Persistence round-trip tests

Purpose: save/load deterministic continuation and stable identity/reference restoration.

Pattern:

1. construct scenario;
2. advance to deterministic barrier;
3. capture expected continuation;
4. rerun to barrier, save and reload;
5. execute identical continuation inputs;
6. compare authoritative state/events/projections and RNG outcomes.

### 8.8 Network equivalence tests

Purpose: prove one-player in-process transport and loopback socket transport carry the same logical protocol.

The exact serialized bytes need not match typed in-process transport. The decoded request/result/projection semantics and authoritative outcome MUST match.

## 9. Deterministic setup strategy

### 9.1 Run identity

Each test run has a `TestRunId`; each scenario has a stable `ScenarioId` plus schema version.

A reproducibility tuple is:

`{ OctoGhastBuild, RulesProfileVersion, ContentGenerationId, ScenarioId@Version, WorldSeed, RngSeedSet, InputSchedule }`.

### 9.2 Canonical time

Scenarios specify start canonical tick/world chronology explicitly. Host elapsed time and renderer frames are excluded.

Advancement APIs:

- `AdvanceTicks(n)`;
- `AdvanceUntil(predicate, maxTicks)`;
- `RunUntilActivityComplete(activityRef, maxTicks)`.

The harness MUST fail on exhausted bounds; it must not wait indefinitely.

### 9.3 RNG

The harness MUST be able to:

- seed every authoritative deterministic RNG stream;
- snapshot/restore stream state where persistence requires it;
- report stream seeds/counters/state identifiers on failure;
- separate cosmetic client RNG from authoritative RNG;
- rerun a scenario over a seed set/range.

Tests MUST NOT rely on global host randomness for authoritative setup.

For Cataclysm-reference comparisons, exact random-sequence identity is required only when the spec for that feature declares it. Otherwise compare rule-level distributions/invariants or use controlled outcomes/fixtures.

### 9.4 Stable same-tick ordering

Simultaneous test inputs include explicit source/player identity and sequence metadata and are admitted according to the canonical ordering contract from Specs 01/04/#90. Reordering host threads, renderer frames or socket callbacks MUST not alter the result.

## 10. Fixture/data isolation

### 10.1 Test content pack

OctoGhast SHALL maintain a dedicated test-only content pack/profile equivalent in purpose to pinned CDDA `TEST_DATA`.

Requirements:

- never enabled in ordinary production worlds by default;
- immutable for a run;
- explicit IDs prefixed/namespaced to avoid collision with production content;
- may contain deliberately invalid definitions for negative validation tests;
- fixture provenance includes source spec/scenario;
- loaded through the same normal definition/registry pipeline unless the test is specifically for lower-level parsing.

### 10.2 Fixture classes

Minimum reusable fixture builders:

- empty deterministic world;
- local map region with explicit terrain/furniture/fields/traps/items;
- player identity + controlled character;
- NPC/monster actor;
- inventory/container tree;
- weapon/armor/combat pair;
- recipe/resources/workstation;
- construction site;
- vehicle;
- weather/environment state;
- EOC/talker/variable context;
- multi-player separated regions;
- multi-player overlapping region/contention setup.

Fixture builders return stable references, not direct mutable ECS objects.

### 10.3 Isolation levels

Tests declare one isolation level:

- `NoWorld` — pure/unit/static-data;
- `FreshServer` — new process/logical host and content load;
- `FreshWorld` — reused immutable content, new authoritative world;
- `FreshRegion` — new/reset isolated region in a test world;
- `Continuation` — deliberate save/load/reconnect continuation.

State leakage across declared isolation boundaries is a harness defect.

## 11. Test-world lifecycle

State machine:

`Uninitialized -> ContentLoaded -> WorldCreated -> Setup -> Running -> QuiescentBarrier -> Completed/Failed -> Disposed`

Optional persistence path:

`Running -> QuiescentBarrier -> Saved -> Disposed -> Reloaded -> Running`.

Rules:

- setup mutations occur before `Running` or as explicit ordered debug commands;
- background simulation is disabled before `Running` unless the test explicitly covers startup progression;
- dispose releases server/session/client resources;
- temporary saves/logs/traces use per-run directories;
- failed runs MAY retain artifacts according to test-run policy;
- successful runs delete transient world state unless explicitly requested;
- no production user config/save directory is read or written.

## 12. Map/world reset semantics

A reset helper is not a raw memory clear. It MUST restore subsystem invariants.

For a local-region reset:

1. stop scenario advancement at a deterministic boundary;
2. detach/despawn fixture entities through authoritative lifecycle APIs;
3. clear/reset terrain/furniture/traps/fields/items via map-domain mutation API;
4. rebuild/invalidate derived map/spatial/FOV caches as required;
5. clear relevant scheduled/active work owned by removed fixtures;
6. verify spatial index consistency;
7. optionally reapply a named fixture definition;
8. resume only after invariant checks pass.

World reset creates a fresh world identity unless the test explicitly targets rollback semantics.

## 13. Mapping pinned CDDA tests to behavioural evidence

The 255 pinned C++ tests are evidence, not code to port one-for-one. OctoGhast groups them by the feature specs they substantiate.

Representative mapping:

| Feature/spec area | Pinned evidence examples |
| --- | --- |
| Spec 01 time/scheduling | `calendar_test.cpp`, `start_date_test.cpp`, `activity_tracker_test.cpp`, `player_activities_test.cpp`, `weary_test.cpp` |
| Specs 02–03 character | `char_*_test.cpp`, `limb_test.cpp`, `encumbrance_test.cpp`, `health_test.cpp`, `new_character_test.cpp`, `skill_test.cpp`, `mutation_test.cpp` |
| Spec 04 action/activity | `activity_tracker_test.cpp`, `player_activities_test.cpp`, `craft_attention_test.cpp` |
| Specs 05–06 items/inventory | `item_*_test.cpp`, `item_pocket_test.cpp`, `item_location_test.cpp`, `inventory_test.cpp`, `reload_*_test.cpp` |
| Spec 07 crafting | `crafting_test.cpp`, `recipe_*_test.cpp`, `requirements_test.cpp`, `uncraft_test.cpp` |
| Spec 08 construction | `act_build_test.cpp`, `map_bash_test.cpp`, `ground_destroy_test.cpp` |
| Spec 09 combat | `melee_test.cpp`, `melee_dodge_hit_test.cpp`, `projectile_test.cpp`, `ranged_balance_test.cpp`, `weakpoint_test.cpp`, `archery_damage_test.cpp` |
| Spec 10 creatures | `creature_test.cpp`, `monster_test.cpp`, `monster_attack_test.cpp`, `monster_stair_test.cpp`, `flying_creature_test.cpp` |
| Spec 11 NPC/dialogue | `npc_*_test.cpp`, `mission_test.cpp`, `faction_*_test.cpp` |
| Spec 12 map/spatial | `coordinate_test.cpp`, `map_*_test.cpp`, `move_cost_test.cpp`, `shadowcasting_test.cpp`, `vision_test.cpp` |
| Spec 13 world/mapgen | `overmap_*_test.cpp`, `mapgen_*_test.cpp`, `start_location_test.cpp`, `nest_conditional_placement_test.cpp` |
| Spec 14 environment | `field_test.cpp`, `weather_test.cpp`, `temperature_test.cpp`, `snow_depth_test.cpp`, `rot_test.cpp` |
| Spec 15 vehicles | `vehicle_*_test.cpp`, `vehicle_drag_test.cpp`, `vehicle_efficiency_test.cpp`, `vehicle_power_test.cpp` |
| Spec 16 AI/pathfinding | `behavior_test.cpp`, `pathfinding_test.cpp`, `simple_pathfinding_test.cpp`, `npc_behavior_rules_test.cpp` |
| Spec 17 event/EOC | `event_test.cpp`, `eoc_test.cpp`, `stats_tracker_test.cpp` |
| Spec 18 data loading | `json_load_test.cpp`, `json_test.cpp`, `generic_factory_test.cpp`, `string_ids_test.cpp` |
| Spec 19 mods/content | loader/content validation tests plus `TEST_DATA` and selected-mod test-run support |
| Spec 20 persistence | `submap_load_test.cpp`, `safe_reference_test.cpp`, `memorial_test.cpp`, serialization/load-focused cases |
| Specs 21–22 UI/presentation | `advanced_inventory_test.cpp`, `crafting_gui_test.cpp`, `widget_test.cpp`, `translation_system_test.cpp`, `pixel_minimap_test.cpp`, renderer/audio tests |
| Spec 23 harness itself | `test_main.cpp`, `rng_test.cpp`, `debug_console_test.cpp`, `debug_capture_test.cpp`, map/test helpers |

Each OctoGhast feature spec SHOULD maintain a small evidence table linking its scenarios to exact pinned files/tests. Spec 23 owns the cross-spec matrix schema, not all feature-specific expected values.

## 14. Differential-test protocol

### 14.1 Reference identity

Every differential result records:

- repository: `LambdaSix/Cataclysm-DDA`;
- exact commit: `e262adb299a7613b4aedc5f12c08fe0413c56a84`;
- reference build/toolchain identity where it can affect serialization/floating output;
- active mods/content fixture set;
- reference RNG seed/options;
- scenario adapter version.

No test may silently use upstream HEAD.

### 14.2 Input model

A differential scenario defines a **semantic input script**, not a byte-for-byte UI recording:

- world/content seed;
- initial entities/items/map state expressible in both systems;
- chronology;
- ordered player/system actions;
- relevant RNG seed/control;
- observation checkpoints.

Adapters translate semantic inputs to each executable.

### 14.3 Output model

Comparison uses a normalized structured observation schema. Examples:

- outcome/rejection code;
- actor/item state deltas;
- move/action costs;
- emitted gameplay events/messages;
- map mutations;
- damage instances;
- activity progress/completion;
- generated IDs/content keys where stable;
- save-relevant semantic state.

Exclude:

- pointer/object addresses;
- Godot node/resource IDs;
- socket IDs;
- unordered container iteration;
- absolute temp paths;
- host timestamps;
- localization/pixel layout unless presentation is the subject;
- diagnostic wording unless wording itself is a compatibility target.

### 14.4 Tolerances

Default comparison is exact after normalization.

Non-zero tolerance requires an explicit field rule in the scenario/profile adapter:

`{ fieldPath, comparator, tolerance, rationale, authoritativeEvidence }`.

Permitted examples:

- floating value absolute/relative tolerance where upstream floating computation is observable but exact representation is not contractually meaningful;
- statistical distribution acceptance for probabilistic rules;
- set/multiset comparison where ordering is documented as non-semantic.

A blanket "close enough" tolerance is forbidden.

### 14.5 Differential classification

Result states:

- `Match`;
- `IntentionalDeviation` — linked to documented OctoGhast adaptation;
- `KnownGap` — implementation incomplete;
- `ReferenceAmbiguity` — baseline evidence is internally ambiguous and requires investigation;
- `HarnessIncomparable` — equivalent semantic setup/observation cannot yet be produced;
- `Regression`.

`IntentionalDeviation` must name the owning architecture/spec decision. It is not a way to hide unexplained mismatches.

## 15. Parity matrix schema and status rules

The programme parity matrix is version-controlled structured data plus a generated human-readable view.

Minimum row schema:

| Field | Meaning |
| --- | --- |
| `AreaId` | stable feature/subfeature identifier |
| `Spec` | owning OctoGhast spec/issue |
| `ReferenceEvidence` | pinned source/JSON/docs/tests |
| `ProfileRule` | Cataclysm-profile behaviour being checked |
| `CoreCapability` | reusable engine capability involved |
| `ImplementationStatus` | implementation state |
| `ConformanceTests` | OctoGhast test/scenario IDs |
| `DifferentialStatus` | latest differential classification where applicable |
| `PersistenceCoverage` | none/round-trip/continuation/migration |
| `MultiplayerCoverage` | none/single-player-equivalence/contention/privacy/reconnect |
| `IntentionalDeviation` | linked decision or empty |
| `LastVerifiedBuild` | OctoGhast commit/build |
| `BaselineCommit` | must equal current programme pin |

Implementation status vocabulary:

- `Unspecified` — no implementation-ready contract;
- `Specified` — spec exists, implementation absent;
- `Partial` — some required behaviour implemented;
- `Implemented` — declared behaviour implemented but conformance not complete;
- `Conformant` — required automated conformance scenarios pass;
- `ParityVerified` — relevant pinned differential/golden evidence also passes;
- `IntentionalDeviation` — implementation satisfies OctoGhast contract while differing from reference by an approved documented decision;
- `OutOfScope` — explicitly excluded by programme decision.

Rules:

- status never advances solely because source code exists;
- `Conformant` requires all mandatory black-box scenarios for the row;
- `ParityVerified` requires baseline identity to match #64/#65 pin;
- any baseline repin invalidates `ParityVerified` until delta assessment/reverification;
- `KnownGap` differential results prevent `ParityVerified`;
- one failing required conformance scenario downgrades the affected row from `Conformant`/`ParityVerified`;
- intentional deviations stay visible in generated reports.

## 16. Standard conformance-test patterns for later implementation tickets

Every later implementation ticket can reference one or more of these patterns.

### P23-A Pure rule

Given immutable fixture inputs, call the rule service twice with identical inputs/seed and assert exact normalized output plus invariants. No world/client required.

### P23-B Authoritative command

Create fresh server/world/actor fixtures, submit one command at tick T, advance deterministic ticks, assert result code, cost, authoritative state delta and emitted event sequence.

### P23-C Long-running activity

Start activity, assert durable activity identity/progress; advance exact ticks; optionally interrupt; assert move/budget consumption, state transitions, completion/cancellation effects and persistence continuation.

### P23-D Multiplayer contention

Create two stable players targeting one authoritative resource/site. Submit same-tick requests in declared ordering. Assert exactly one deterministic winner (or documented merge semantics), explicit loser result, no duplication and correct per-player projections.

### P23-E Visibility/privacy

Run two clients with different FOV/knowledge/ownership. Mutate authoritative state once. Assert each receives only its permitted projection and that hidden state does not leak through DTOs/events/debug-facing client APIs.

### P23-F Disconnect/reconnect

Start state/activity, disconnect one transport, continue world, reconnect same stable player, assert correct entity/session rebind and current-state projection without socket identity entering world state.

### P23-G Persistence continuation

Run to barrier, save/reload, continue with same inputs and compare against uninterrupted run including RNG, schedules, references and projections.

### P23-H Differential

Run semantic script against pinned reference and OctoGhast, normalize observations, compare exact/toleranced fields, classify divergence.

### P23-I Content validation

Load a dedicated fixture content generation containing valid, invalid, inherited and overridden definitions. Assert diagnostics, registry result and no partial live-generation contamination.

### P23-J Transport equivalence

Run identical semantic script once over in-process transport and once over loopback network transport; compare authoritative outcomes and decoded projections.

### P23-K Active-region overlap/separation

Run two players first in disjoint active regions then overlapping coverage. Assert regions simulate as specified, overlap is simulated once, projection remains player-specific and deactivation/background catch-up is deterministic.

### P23-L Statistical rule

Run declared seed/sample plan, evaluate confidence/tolerance, emit failing distribution/seed evidence and keep authoritative RNG isolated from client cosmetic RNG.

## 17. Persistence and stable identity

The harness consumes Spec 20 rather than inventing a test-only save model.

Persistent scenario-relevant identities include, where applicable:

- `PlayerId`;
- entity/item/container/project/activity IDs;
- world/content generation identity;
- canonical time and schedule/deadline identity;
- stable spatial/world references;
- RNG continuation state.

Excluded from world saves:

- test-run ID;
- socket/connection ID;
- Godot object/node IDs;
- capture ring buffers;
- host temporary paths;
- matrix/report state.

A saved test fixture may include test-content IDs only when the same test content generation is loaded for restoration.

## 18. Multiplayer/co-op adaptations

Pinned CDDA debug/test code often assumes one process, one avatar and directly accessible global state. OctoGhast MUST adapt this explicitly.

### Pinned reference behaviour

- the debug menu commonly operates on the current avatar/game/global map;
- test setup constructs one global game/avatar/map;
- direct test helpers can mutate global map/time state.

### OctoGhast adaptation

- the server/world, not one player, owns active state;
- debug targets are explicit stable references;
- multiple player-controlled actors can be inspected/set up independently;
- observer/client projections remain privacy-filtered;
- a developer/server probe may inspect authoritative state only under explicit test/admin authority;
- overlapping active regions never duplicate simulation;
- disconnecting a client does not destroy harness-owned player/world state unless the scenario requests it;
- the world continues while a debug UI is open.

### Implementation contract

All non-presentation black-box scenarios MUST run without a graphical/window/audio surface. Core unit/contract fixtures remain runnable without Godot; production scenarios whose gameplay depends on a Spec 24 server adapter MUST also exercise the real headless libgodot implementation. UI/presentation scenarios use their required client surface. Deterministic fakes provide isolation, not proof of production adapter equivalence. UI debug tooling is an optional front end over the same debug/test services.

## 19. Networking and security boundaries

This spec inherits #90:

- asynchronous I/O never mutates simulation directly;
- parser/buffer/rate-limit tests use bounded fault-injection fixtures;
- connection identity is distinct from stable player/control identity;
- outbound debug/diagnostic streams have bounded budgets/backpressure;
- ordinary gameplay protocol exposure does not imply debug capability.

Remote debug/admin transport SHOULD be disabled or capability-gated in production configurations. Test-only fault injection MUST not be reachable from an unauthenticated production client.

## 20. Validation and failure behaviour

Harness/setup operations fail atomically.

Required failure cases:

- unknown fixture/content ID -> `FixtureNotFound`;
- stale stable reference -> `StaleReference`;
- invalid target type -> `InvalidTarget`;
- capability denied -> `UnauthorizedDebugCapability`;
- mutation violates invariant -> rejection with no partial state;
- setup cannot reach requested deterministic barrier -> bounded timeout/failure;
- world/content-generation mismatch on fixture restore -> explicit incompatibility;
- reference executable/baseline mismatch -> differential run refused;
- unknown/unapproved tolerance -> differential run refused;
- capture budget exceeded -> fail with bounded retained context;
- golden schema/version mismatch -> explicit update/migration required;
- test leaked state into next isolated run -> harness invariant failure.

## 21. Required black-box/conformance scenarios

### HAR23-01 Fixed-seed replay
Run one stochastic combat/environment scenario twice with identical seed set and input schedule. Normalized authoritative event/state traces are identical.

### HAR23-02 Different seed controlled divergence
Run the same stochastic scenario with a different authoritative seed. At least one declared stochastic observation may differ, while deterministic invariants and command ordering remain valid.

### HAR23-03 Renderer independence
Run a scenario headlessly, with a slow synthetic render cadence, and with a fast synthetic render cadence. Authoritative ticks/results are identical.

### HAR23-04 Fresh-world isolation
Mutate world A heavily, dispose it, create fresh world B from the same fixture/seed. B equals a clean baseline and contains no A identities/state.

### HAR23-05 TEST_DATA isolation
A test-only item/monster/recipe definition exists in the harness content generation and is absent from an ordinary Cataclysm production content generation.

### HAR23-06 Debug spawn preserves invariants
Spawn an item/creature via debug request. Stable identity, spatial/container index and projections are consistent; direct client-side object creation cannot affect server state.

### HAR23-07 Debug invalid mutation atomicity
Request an impossible container/spatial/state mutation. Request is rejected and all affected authoritative state remains unchanged.

### HAR23-08 Explicit non-avatar target
Two player characters exist. A debug operation targets B by stable entity reference while A is the requesting admin identity. Only B changes.

### HAR23-09 Debug UI does not pause
Open/leave idle an interactive debug client while another player acts. Canonical server time and the other player's activity continue.

### HAR23-10 Same-tick contention
Two synthetic players attempt the same pickup/construction/resource action on the same tick. Result follows canonical deterministic ordering, with no duplication.

### HAR23-11 Projection privacy
Authoritative probe sees hidden creature X. Player B lacks visibility/knowledge. B's captured protocol/projection contains no X stable reference or hidden location.

### HAR23-12 Reconnect stability
Disconnect and reconnect a player during a long activity. Connection ID changes; stable PlayerId/entity/activity references and continuation semantics follow owning specs.

### HAR23-13 Save/load continuation
Compare uninterrupted run with save/reload continuation from the same deterministic barrier. Post-barrier authoritative events/state/RNG outcomes match.

### HAR23-14 In-process/loopback equivalence
Execute the same scenario over in-process and loopback network transports. Decoded semantics and authoritative results match.

### HAR23-15 Active-region separation
Two players occupy distant regions. Both regions advance under one canonical clock without assuming one player's bubble owns the world.

### HAR23-16 Active-region overlap
Players converge so active coverage overlaps. Shared entities/world tiles update exactly once and are projected independently.

### HAR23-17 Structured capture bounds
Generate more trace/log entries than configured capacity. Old entries are deterministically trimmed or the configured overflow policy applies; memory remains bounded.

### HAR23-18 EOC trace
Activate a test EOC with explicit talkers/context. Capture shows deterministic activation/order/side-effect trace without leaking another player's private variables to an unauthorized client.

### HAR23-19 Data validation isolation
Load a content generation containing one invalid test definition. Validation produces expected diagnostic and the currently active good generation remains usable.

### HAR23-20 Golden review discipline
A normalized golden differs. Test fails with structured diff; normal test execution never rewrites the golden automatically.

### HAR23-21 Differential exact match
A deterministic semantic scenario supported by both systems executes against the pinned reference and OctoGhast and yields `Match` after normalization.

### HAR23-22 Differential intentional deviation
A turn-gated interaction differs only because OctoGhast's documented continuous-time/server-authority adaptation applies. Result is classified `IntentionalDeviation` and links the owning spec decision.

### HAR23-23 Differential baseline mismatch
Configure reference executable at any commit other than the #64/#65 pin. Harness refuses parity-verification status.

### HAR23-24 Explicit tolerance
A floating field with approved tolerance passes inside the bound and fails outside it; an undeclared tolerance cannot be supplied ad hoc.

### HAR23-25 RNG fuzz reproducibility
Run a scenario over N generated seeds, inject/encounter failure at seed S, and rerun exactly S to reproduce the failure.

### HAR23-26 Statistical rule
A probabilistic rule is sampled according to a declared confidence/tolerance plan. The report includes sample count, seed plan and observed statistic.

### HAR23-27 Map reset
Dirty a fixture region with terrain/furniture/items/fields/actors, run authoritative region reset, reapply fixture and verify spatial/index/cache invariants plus expected baseline state.

### HAR23-28 Stable reference stale rejection
Capture entity/item reference, despawn/delete it, then issue a debug/game command with the old reference. It is rejected as stale rather than retargeting a reused runtime object.

### HAR23-29 Client cosmetic RNG isolation
Change Godot sprite/audio cosmetic RNG while running identical authoritative scenario. World state/save/event ordering remains identical.

### HAR23-30 Capture artifact completeness
Force a deterministic test failure. Failure artifact contains scenario/build/profile/content/baseline identity, seed set and first divergence sufficient for headless replay.

### HAR23-31 Malformed network test boundedness
Synthetic transport sends fragmented/oversized/malformed frames according to #90. Parser rejects within configured limits without ECS mutation or unbounded allocation.

### HAR23-32 Slow-client backpressure
One synthetic client stops consuming replaceable projections while another remains healthy. Server memory/queues stay bounded and healthy client's simulation/projection continues.

### HAR23-33 Definition/runtime separation
Mutating runtime item/entity state never modifies the immutable fixture/content definition observed by a second fresh instance.

### HAR23-34 Alternative rules profile
A non-Cataclysm profile uses different chronology/action/spatial fixture semantics while reusing generic scenario, command, capture, persistence and result-report infrastructure.

### HAR23-35 Matrix status gating
A row with passing unit tests but missing required scenario remains below `Conformant`; a conformant row with missing/failed reference evidence remains below `ParityVerified`.

### HAR23-36 Baseline repin invalidation
Change programme baseline metadata in a test copy of the matrix. Previous `ParityVerified` rows become pending reverification until delta assessment completes.

## 22. Concrete feature-ticket acceptance template

A gameplay implementation ticket should add:

1. **Evidence:** exact pinned source/JSON/docs/test references.
2. **Fixture:** named Spec-23-compatible setup fixture(s).
3. **Pattern(s):** P23-A through P23-L.
4. **Inputs:** commands/ticks/seeds/content generation.
5. **Authoritative assertions:** state transitions, costs, events and invariants.
6. **Projection assertions:** what each player may observe.
7. **Persistence assertion:** if state survives save/load.
8. **Differential/golden assertion:** where feasible.
9. **Matrix rows:** affected `AreaId` entries.
10. **Failure artifact:** seed/scenario identity sufficient for replay.

Example:

`Spec09.Melee.HitResolution -> P23-B + P23-H + P23-L; fixture combat/basic_melee_v1; exact move cost/damage-type sequence; statistical hit distribution where RNG applies; player-specific combat message projection; row CAT-COMBAT-MELEE-001.`

## 23. Implementation sequence

1. Define test-run/scenario manifest schemas and reproducibility tuple.
2. Implement headless fresh-server/fresh-world lifecycle and isolated temporary directories.
3. Implement rules-profile-aware deterministic time/RNG seeding and failure reporting.
4. Implement test-only content-pack loading and fixture builders through normal registries.
5. Implement stable authoritative probe/debug request APIs with capability gating.
6. Implement structured bounded capture for commands/results/events/scheduler/EOC/projections.
7. Implement scenario executor with synthetic player clients and exact tick scheduling.
8. Add in-process and loopback transport adapters conforming to #90.
9. Implement normalized structured snapshot/golden comparison.
10. Implement persistence continuation runner.
11. Implement optional pinned-CDDA reference adapter and semantic differential protocol.
12. Add parity matrix structured store/generator and CI status gates.
13. Migrate/add feature scenarios incrementally as implementation tickets land.

## 24. CI and execution policy

Recommended lanes:

- **PR-fast:** unit + contract + selected deterministic scenarios.
- **PR-feature:** affected feature conformance scenarios and matrix rows.
- **Network:** parser/backpressure/disconnect/reconnect/in-process-loopback tests.
- **Persistence:** round-trip/continuation suites.
- **Reference-differential:** pinned reference comparisons where the executable/tooling is available.
- **Seed-fuzz:** bounded rotating seed corpus plus replay of previously failing seeds.
- **Nightly/long:** larger statistical, worldgen and multi-region scenarios.

A feature merge gate SHOULD be based on declared affected rows/patterns, not a brittle expectation that every extremely expensive differential/statistical test runs on every edit.

## 25. Core versus Cataclysm profile boundary

Generic Core MUST NOT hard-code:

- CDDA's Catch2 tags/test runner;
- CDDA's single RNG engine;
- a one-avatar global test context;
- 10 TPS/100 moves as universal test semantics;
- integer-grid positions as the only future fixture representation;
- CDDA JSON TEST_DATA schemas;
- CDDA debug action names;
- curses/ImGui developer UI;
- exact C++ save/reference formats.

Generic Core/test infrastructure SHOULD provide:

- deterministic host/world lifecycle;
- canonical-time stepping;
- named RNG control;
- authoritative command/probe/debug capability contracts;
- stable references;
- fixture and scenario lifecycle;
- structured bounded observations;
- snapshot/golden/differential comparators;
- persistence/reconnect/network adapters;
- matrix/reporting primitives.

The Cataclysm profile provides the pinned rules/data adapters and expected behavioural evidence.

## 26. Dependencies and contracts

- **#52/#57 / Spec 01:** canonical time, deterministic scheduling, profile rate mapping.
- **#58 / Spec 12:** authoritative WorldPosition/SpatialCell and active-region spatial invariants.
- **Spec 04:** command/activity lifecycle and result ordering.
- **Specs 18/19:** registries, definitions, content generations and mod loading.
- **Spec 20:** save barriers, stable identity/reference and deterministic continuation.
- **Specs 21/22:** client input/projection/presentation boundaries.
- **Spec 17:** EOC/talker context and trace semantics.
- **#90:** transport/session lifecycle, parser limits, bounded queues/backpressure and deterministic intake.
- **Spec 28 / #91:** server-scoped account/meta-progression state; test worlds must use isolated account fixtures and must not accidentally rewrite real user/server account state.

## 27. Unresolved architecture

No new cross-cutting architecture decision is required by this investigation.

The spec consumes existing decisions for canonical time, server authority, active regions, stable identity, persistence, projection and networking. Concrete authentication/role management for production remote administrators is owned by Spec 29 / #92; Spec 23 requires capability separation but does not invent an account system.

## 28. Definition of done

Spec 23 is implementation-ready when:

- debug capabilities required to inspect/manipulate all major implemented domains are catalogued;
- test-layer taxonomy is explicit;
- deterministic RNG/time/world setup and replay are explicit;
- test fixture/data isolation and world lifecycle are explicit;
- pinned CDDA tests are mapped as reusable behavioural evidence rather than one-for-one port targets;
- differential protocol pins reference version, semantic inputs, normalized outputs, explicit tolerances and divergence classification;
- parity matrix schema/status rules are explicit;
- later implementation tickets have reusable concrete conformance patterns;
- server authority, canonical continuous time, multiplayer contention/privacy, active regions, disconnect/reconnect and transport equivalence are covered;
- immutable fixture definitions are separated from mutable runtime state;
- stable identity/reference, persistence and RNG requirements are explicit;
- no developer UI is allowed to become a hidden privileged-avatar or client-side mutation path.

## Post-spec cross-contract scenarios

The [post-spec audit](./post-spec-architecture-audit.md) identified integration gates in #95 (order/activation) and [#96](https://github.com/LambdaSix/OctoGhast/issues/96) (bounded retries/outcomes). #95 is now resolved by the [canonical ordering/admission/activation architecture](./architecture-canonical-ordering-admission-activation.md); its matrix rows may be implemented/tested against that contract. #96 rows remain non-Conformant until its separate decision is resolved.

**HAR23-AUD-01 — production adapter proof:** where authoritative results depend on a Godot server adapter, run real-adapter headless save/restart and deterministic continuation tests as well as isolated Core fixtures. No render/window/audio dependency is introduced; tolerance in a CDDA differential fixture does not automatically permit divergence in OctoGhast replay.


### #95 deterministic integration scenarios

- **HAR23-95-01:** reversed PlayerId/ActorId contention across inventory/combat/construction records bounded admission and profile execution as separate trace fields.
- **HAR23-95-02:** callback/thread permutations with identical frozen candidates and cursor reproduce the same selected/deferred admission trace.
- **HAR23-95-03:** a dynamic EOC activation suspension records prefix/frontier/activation/resume exactly once under varied worker completion timing.
- **HAR23-95-04:** profile construction fails when potentially conflicting authoritative writers have no explicit order or declared commutativity/independence.
- **HAR23-95-05:** independent-region worker scheduling permutations yield identical canonical mutation/RNG/publication traces.

## #96 command-outcome fault-injection coverage — 2026-09-26

The harness MUST implement the conformance surface defined by [Architecture — bounded command idempotency and outcome recovery](./architecture-bounded-command-idempotency-outcome-recovery.md), including OP96-01 through OP96-15.

Required fault/cut points include:

- duplicate before admission;
- duplicate while queued;
- duplicate while executing;
- disconnect after authoritative commit but before result delivery;
- save before/after effect and operation-record creation;
- failure before manifest commit;
- clean committed save/restart;
- crash after in-memory commit but before a save containing it;
- generation rollover/retention expiry;
- deliberate older-snapshot restore/history-epoch change;
- same-account concurrent-session duplicate submission;
- changed-payload key reuse;
- changed controlled Character;
- current-authorization privacy filtering of historical outcomes;
- hostile duplicate/result-query load.

Tests MUST assert effect count, move/action cost count, resource consumption count and gameplay RNG draw trace—not merely final visible state—so a duplicated hidden attempt cannot pass.

Every applicable scenario MUST run through both the in-process transport boundary and the loopback/network logical protocol path. Serializer/framing differences must not change canonical semantic fingerprint or outcome behaviour.

