# Spec 15 — Vehicles and modular mobile structures

Status: investigation complete / implementation-ready specification  
Parent issue: #65  
Child issue: #80  
Reference implementation: `LambdaSix/Cataclysm-DDA`  
Pinned parity baseline: `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Purpose and boundary

This specification defines the vehicle capability required for the OctoGhast Cataclysm reference profile: modular vehicle structure, local part coordinates, spawning, installation/removal/repair, movement, steering, traction, collision/damage, engines/fuels, electrical power networks, cargo, passengers, controls, towing, turrets, appliances, ramps/z movement, splitting and persistence.

The goal is behavioral/data parity with the pinned Cataclysm:DDA baseline, not a C++ class port. Generic Core owns reusable identity, spatial, chronology, scheduling, command/activity, item-location, damage and persistence capabilities. Cataclysm owns the particular grid vehicle rules, 15-degree steering quantization, part flags/locations, velocity/action formulas, collision model, fuel/power rules and JSON schemas described here.

Prerequisite contracts are #66 / `spec-01-game-loop-time-scheduling.md`, #70 / `spec-05-item-model-lifecycle.md`, #71 / `spec-06-inventory-pockets-containment-item-transfer.md`, #74 / `spec-09-combat-core-damage-melee-ranged-projectiles.md`, #77 / `spec-12-local-map-coordinates-spatial-simulation.md`, #83 data/ID loading and #85 / `spec-20-persistence-save-load-migration.md`.

## 2. Authoritative evidence at the pinned baseline

Primary source anchors:

- `src/vehicle.h`, `src/vehicle.cpp`: runtime vehicle/part state, mounting rules, structural splitting, mass/drag, engines/fuels, power consumption, batteries, renewables, towing and lifecycle.
- `src/vehicle_move.cpp`: steering, thrust, traction, movement budget, ramps/falling, collision detection and collision damage.
- `src/veh_type.h`, `src/veh_type.cpp`: immutable vehicle-part definitions, slots, defaults, validation, generated turret parts and migrations.
- `src/veh_interact.cpp`, `src/vehicle_use.cpp`: installation/removal/repair and interaction behavior.
- `src/veh_appliance.cpp`: static appliance/power-grid interaction.
- `src/vehicle_part.cpp`, `src/vehicle_part_location.*`: part runtime capability and location access.
- `src/vehicle_group.*`: data-driven vehicle selection/spawning.
- `src/savegame_json.cpp`: vehicle and vehicle-part persistence and migration.
- `src/power_network.*`: persistent/rebuilt connected electrical networks.
- `doc/JSON/VEHICLES_JSON.md`: vehicle prototype authoring contract.
- `data/json/vehicleparts/`, `data/json/vehicle_part_locations.json`: part definitions and install-location taxonomy.
- `data/json/vehicles/`, `data/json/vehicle_groups.json`, `data/json/road_vehicles.json`: stock prototypes and spawn groups.

Pinned tests that are normative conformance evidence include `vehicle_test.cpp`, `vehicle_part_test.cpp`, `vehicle_power_test.cpp`, `vehicle_ramp_test.cpp`, `vehicle_split_test.cpp`, `vehicle_turrets_test.cpp`, `vehicle_drag_test.cpp`, `vehicle_efficiency_test.cpp`, `vehicle_interact_test.cpp`, `vehicle_export_test.cpp`, `vehicle_fake_part_test.cpp`, mapgen vehicle tests and battery tests.

All references in this page mean the exact pinned commit above.

## 3. Reference/profile/Core separation

### Pinned CDDA reference behavior

CDDA represents one vehicle as a mutable aggregate with an origin, facing/movement direction, velocity and a list of parts. Each part has a vehicle-local mount coordinate; derived rotated coordinates place the part on map squares. Vehicle movement is processed from the active map once per world turn and may spend fractional `of_turn` movement opportunity. Vehicle parts, contents, passengers, power state, tow state and motion state are persisted.

### OctoGhast Cataclysm profile adaptation

OctoGhast preserves the rules but changes authority and scheduling:

- vehicles are authoritative server/world entities, never client/Godot objects;
- a moving vehicle may span several submaps and several players' interest regions but exists and moves exactly once;
- processing is driven by canonical server time, not by a privileged avatar turn;
- one CDDA world turn remains one reference second = 100 move units, while OctoGhast executes 10 canonical ticks per world second per #66;
- vehicle rules that are naturally one-second integrations must use deterministic elapsed/reference-turn integration or equivalent fixed-tick accumulation without changing their one-second result;
- commands from drivers and vehicle interaction UIs are intents; install/remove/repair and similar work are activities; read-only inspection is a query; outcomes are projected events/state;
- clients receive only vehicle state visible/known/authorized for that player.

### Generic Core contract

Core SHALL provide stable entity identity, precise authoritative world position plus derived spatial cells, atomic multi-cell spatial mutation, deterministic canonical scheduling, stable item/entity references, generic damage/event hooks, activities, deterministic RNG streams and persistence ownership. Core SHALL NOT require every future mobile structure to use CDDA's integer mount graph, 15-degree steering, mph-derived internal velocity, part flags or collision equations.

## 4. Definition/template data versus runtime state

### Immutable registered definitions

A `VehiclePartDefinition` is identified by a stable typed part ID and contains definition data such as base item type, install location/category, flags, durability, damage modifier, installation/removal/repair requirements and times, power values, engine/wheel/rotor/workbench/toolkit capability records, fuel type/options, variants and default variant.

Important pinned defaults/validation include:

- flat electrical `epower` defaults to 0 W; positive values generate and negative values consume;
- engine/motor `power` is maximum output; alternator power represents engine load;
- `energy_consumption` represents input energy rate at maximum delivery and is scaled by demand;
- installation time defaults to one hour when not defined; removal defaults to half installation time;
- wheel off-road rating defaults to 0.5;
- unknown fuels, skills or malformed requirement references are diagnosed/finalized according to the pinned loader;
- negative installation times are invalid;
- a part flagged as a turret must have a gun base item and a wheel part must have the corresponding wheel capability;
- APPLIANCE definitions are finalized as structural-location parts;
- mountable gun definitions generate turret part definitions unless suppressed by `NO_TURRET`; liquid-ammunition guns gain tank-fed turret semantics.

A `VehiclePrototypeDefinition` is a spawn template, not runtime vehicle state. Prototype `copy-from` is limited: inherited definition data is applied first, then parts/items/zones append. A spawned vehicle is subsequently persisted in runtime form, not as a prototype.

Prototype part order is semantically significant: parts must appear in an order legal for normal installation (for example frame, wheel mount/hub, then wheel). Repeated prototype entries at one mount append parts.

Prototype optional data includes turret ammunition chance/type/quantity, initial tank fuel, attached vehicle tools, item/item-group spawns with percentage chance, variants, and loot zones. Loot zones are created only for a vehicle with a faction owner.

### Mutable runtime state

A runtime `VehicleState` includes at minimum:

- stable `VehicleId`;
- prototype/type provenance where retained;
- canonical authoritative origin `WorldPosition`;
- facing direction, movement direction, requested/last turn direction and pivot anchor;
- velocity, average velocity, vertical velocity, cruise target, movement-budget carry and falling/skidding/water/flying/ramp state;
- engine/controller/autopilot/follow/patrol/tracking/camera/alarm/lock state;
- ownership/faction state and theft timestamp;
- ordered runtime parts;
- labels, loot zones, vehicle-local variables/effects;
- fuel remainder/last-turn usage;
- last simulation/update time;
- tow linkage by stable vehicle/part reference;
- power-network membership/topology identity where applicable.

A runtime `VehiclePartState` includes definition ID and variant, local mount coordinate, base item instance, HP/damage/degradation through that item, enabled/power-disabled state, open/locked/hidden state, orientation/z offset, passenger/crew IDs, cargo items, attached tools, salvageable install components, target/link information, selected ammo/fuel state, carried/racked-vehicle metadata and timestamps needed by the reference behavior.

Definition records are never mutated to represent damage, fuel or installation state.

## 5. Identity, coordinates and spatial invariants

### Stable identity

Every runtime vehicle SHALL have a stable `VehicleId` independent of memory address, submap cache slot, connection, client node and Godot object. Split-created vehicles receive new stable IDs; the retained component keeps the original identity. Persisted cross-references (tow, power links, activities, item locations, player control) use stable IDs plus a stable part locator where needed.

For part references, OctoGhast SHOULD use a stable `VehiclePartId`/part-instance key rather than durable vector indexes. CDDA frequently uses indexes internally, but indexes can change during cleanup/splitting and are not an acceptable transport/persistence identity.

### Vehicle-local and world coordinates

Pinned mount coordinates are vehicle-local map-square offsets:

- mount X is forward/backward;
- mount Y is left/right;
- prototype positive X points forward/up in its blueprint and positive Y right;
- facing rotation derives each part's occupied absolute map square;
- the pivot anchor is persisted because changing pivot-selection logic must not move old saves.

OctoGhast SHALL model:

`VehicleOrigin WorldPosition + PartMount + VehicleOrientation -> derived occupied SpatialCell(s)`.

For the Cataclysm profile, origin and occupied part positions are grid-authoritative and each material part occupies discrete map cells. Client presentation transforms may interpolate/rotate smoothly but never drive collision or occupancy.

A vehicle footprint is one authoritative multi-cell spatial object. Updating origin/orientation/z placement must atomically update all derived occupied-cell entries, passenger positions and vehicle lookup indexes. No observer may see half the parts at the old transform and half at the new one.

## 6. Part graph, mounting, removal, damage and splitting

### Mount legality

The Cataclysm profile SHALL preserve `vehicle::can_mount` semantics:

1. the first part on an empty mount must be structural;
2. after the vehicle exists, a new mount must be on or cardinal-adjacent to existing structure (subject to the pinned removed-structure repair exception);
3. identical part definitions cannot stack on one mount except power-transfer parts;
4. two parts with the same non-empty part-location slot cannot stack;
5. two cargo parts cannot occupy one mount;
6. mirrors/non-camera vision parts cannot coexist with opaque parts, in either installation order;
7. only one turret mount can occupy a mount;
8. animal-control/protrusion occupancy blocks ordinary additions except tow cable;
9. engine-conflict rules apply;
10. flags with `requires_flag` require a provider on the same mount;
11. all other combinations not explicitly forbidden are permitted.

An invalid install request performs no mutation and returns a structured failure reason suitable for client projection.

### Installation

Successful installation is atomic: validate requirements and current topology; consume/commit activity resources at the defined activity completion point; create the runtime part from its definition/base item; bind local mount; inherit pinned initial enabled-state behavior; rebuild derived part caches/geometry/drag/power indexes; publish the resulting state/event.

A stale multiplayer install request is revalidated at authoritative commit time. If another actor has changed the mount/resources first, the later request fails without partial resource consumption beyond the activity cancellation/refund rules owned by #69/#71.

### Removal and destruction

Removing/destroying a part must perform dependent transitions before final compaction:

- unboard a passenger if their boardable part is removed;
- unrack a carried vehicle if its rack support is removed;
- release/spawn held animals;
- detach/drop attached tools/components according to item-transfer rules;
- remove dependent CURTAIN with WINDOW, SEATBELT with SEAT, and mount-dependent batteries with their mount;
- invalidate visibility/floor/geometry caches where opaque/roof structure changes;
- reconcile engine enable/engine-on state;
- invalidate tow/power links whose endpoint disappears.

Pinned CDDA marks parts removed and defers physical deletion long enough to keep indexes stable during a processing pass. OctoGhast need not copy that representation, but MUST provide equivalent atomic iteration safety: systems executing in one canonical phase cannot observe a dangling part reference.

### Structural splitting

After structural loss, connected components are determined through cardinal adjacency of remaining structural parts. If structure disconnects, components become separate vehicles.

Required split invariants:

- every surviving real part belongs to exactly one resulting vehicle;
- resulting footprints do not overlap merely because of the split;
- the union of resulting part/world positions equals the surviving original footprint;
- owner and applicable motion/control state are transferred as the reference does;
- passengers, cargo, labels/zones, tow endpoints and power links remain attached to the component containing their part;
- new components receive new stable `VehicleId` values;
- power-network identity/topology is deterministically reconciled;
- an appliance component remains appliance-like;
- a still-connected loop remains one vehicle.

The pinned `vehicle_split_section` test is a mandatory golden: destroying the center of its cross fixture yields four vehicles for every 15-degree facing tested; the circle fixture remains one connected vehicle.

## 7. Engines, fuel and electrical power

### Fuel/engine model

An engine selects from its permitted fuel options and contributes power only when enabled, available and sufficiently fueled under the pinned rules. Engine faults/damage, safe-power coefficients, alternator load, muscle engines and perpetual sources modify effective power through their reference formulas.

Liquid/charge storage is part-owned item/container state. Capacity and compatible fuel behavior therefore compose with Spec 05/06 rather than using a separate client-side meter. Vehicle tools/turrets that bind to tanks or batteries must debit the exact authoritative source selected during preparation; sibling tanks cannot be double-counted.

### Ground speed and drag

Pinned maximum ground velocity solves the positive physical root of:

`c_air * v^3 + c_roll * v^2 + c_roll * 33.33 * v - engine_power = 0`

where `v` is converted between m/s and the vehicle internal velocity unit. Safe ground velocity uses the same equation with safe/effective engine power. Rolling resistance follows the reference SAE-style model whose variable term uses the same 33.33 constant and mass/terrain/wheel contribution. Aerodynamic, rolling and water drag coefficients are derived from current vehicle geometry/parts and are invalidated when relevant structure changes.

Do not substitute arbitrary Godot rigid-body physics. These functions are Cataclysm-profile simulation rules.

### Electrical flow

At the pinned reference's one-second vehicle power update:

`net_epower = engine_epower + accessory_epower + alternator_epower`

and power is integrated over one reference turn into battery-energy units. The source comments/tests establish one battery charge unit as 1 kJ for this path.

- surplus charges reachable batteries;
- deficit discharges reachable batteries;
- connected power-transfer links form an electrical graph and may impose transmission loss;
- on unresolved deficit, eligible enabled consumers are disabled and marked `power_disabled`;
- automatic grid recovery may re-enable parts that were disabled specifically by power failure, but must not re-enable a part the user intentionally turned off;
- reactors run on demand only when connected storage can accept their output;
- fuelled reactor production is capped by demand, output and available fuel; fractional fuel consumption uses pinned probabilistic remainder handling;
- solar/wind/water generation is conditioned by environmental state.

Power topology is server/world-owned. It must not end at one player's active-region boundary. A cable can remain valid when its remote endpoint is outside that player's current projection.

### Inactive/background catch-up

Vehicles/appliance grids outside active regions still require chronology-correct renewable progression. The pinned power tests require:

- off-map solar catch-up from absolute position/environment;
- no duplicate generation when the vehicle becomes active after catch-up;
- sub-minute catch-up intervals are skipped by the reference path;
- equivalent interval partitioning (for example two 30-minute updates versus one 60-minute update) produces equivalent energy within the reference's rounding contract;
- remote connected batteries can receive catch-up energy through valid links;
- wind and water sources catch up as well.

OctoGhast SHALL store the authoritative last-processed time needed to make activation idempotent. Catch-up runs before the region is exposed to ordinary active processing/projection.

### Power network identity and persistence

The pinned baseline tests serialize a power-network manager with a stable network ID, next-ID allocation, last-resolved time, root position and rated node values; unchanged topology preserves the network ID across rebuilds, old saves without network data rebuild successfully, topology mutation/splitting reconciles IDs, and independent grids remain separate.

The Cataclysm profile SHALL preserve equivalent continuation behavior. Network IDs are server simulation state, not socket/session IDs. Clients need only projected power summaries and stable vehicle/part identifiers required for interaction.

## 8. Wheels, traction, steering and movement-time coupling

### Steering

Pinned steering quantization is `15 degrees` per steering increment. Facing direction and movement direction are distinct: skids and collisions can make them diverge.

### Wheel/traction gate

Ordinary ground thrust requires a valid wheel configuration and usable traction. Watercraft/flying rotorcraft use their corresponding profile rules instead. A nonfloating vehicle in deep water cannot propel normally and the baseline can sink/destroy it; a floating but grounded craft can become beached.

Ground acceleration is reference acceleration multiplied by traction. Towing can reject acceleration when effective acceleration is too low. Thick ice can probabilistically initiate a skid; lack of a controlling occupant can also eventually cause a moving uncontrolled vehicle to skid.

### Movement opportunity

Pinned `vehicles::vmiph_per_tile` is `400`. For a moving vehicle, one tile of horizontal movement costs:

`turn_cost = 400 / abs(velocity)`

against the vehicle's fractional per-turn movement opportunity (`of_turn`). If the vehicle cannot afford the displacement yet, remaining opportunity is carried rather than moving early. Very low horizontal speed (`abs(velocity) < 20`) is stopped by the reference path when not making a z transition.

OctoGhast SHALL map this to canonical 10 TPS without redefining it as milliseconds. A compatible implementation may accrue 1/10 of the reference one-second movement opportunity each canonical tick and execute a cell displacement only when the accumulated Cataclysm movement opportunity reaches the same threshold. Equivalent tick/input sequences MUST produce the same displacement ordering as the one-second reference integration.

Movement, collision and passenger relocation for one displacement are one authoritative transaction.

## 9. Collision and damage

Collision detection considers projected vehicle parts and classifies at least vehicle, creature/body, bashable terrain/field and non-bashable obstacle collisions. Vehicle-to-vehicle collisions are resolved by the map-level vehicle collision path; part-vs-terrain/body uses part collision rules.

Important pinned rules:

- armor on the impacted mount is targeted before underlying parts;
- impassable fields/terrain are treated as heavy, low-elasticity obstacles;
- tiny terrain normally does not collide unless a wheel occupies the relevant mount;
- protrusions can pass short terrain according to the pinned flags;
- rail-compatible vehicles do not collide with rail terrain as ordinary obstacles;
- creatures riding on the same vehicle's boardable position are not collision targets;
- swimming creatures can be displaced out of the footprint by the reference special case;
- collision uses vehicle/target mass, elasticity, material density and relative velocity;
- for the simple two-body path:
  - `v1' = (m1*v1 + m2*v2 + e*m2*(v2-v1)) / (m1+m2)`
  - `v2' = (m1*v1 + m2*v2 + e*m1*(v1-v2)) / (m1+m2)`
  - deformation energy is pre-collision kinetic energy minus post-collision kinetic energy;
  - total collision damage derives from `deformation_energy / 400`;
- damage share `k` is derived from clamped material-density and logarithmic mass factors and clamped to 10..90 percent for the vehicle part versus target;
- bashable terrain may absorb damage and be repeatedly resolved if bashing exposes another obstacle;
- creature collision can stun, bleed and fling using RNG and the impacted part's SHARP/damage modifiers;
- the vehicle velocity is updated from the collision result and processing stops when velocity direction reverses.

OctoGhast SHALL reuse its authoritative creature/damage/effect contracts for resulting creature injury while retaining these vehicle collision calculations for the Cataclysm profile. Collision RNG belongs to deterministic simulation RNG, never client randomness.

For simultaneous multiplayer interactions, moving-vehicle collision ordering follows the server's stable same-tick ordering from #66. A client prediction cannot commit collision damage.

## 10. Cargo, passengers and control

### Cargo/item location

A cargo part is an authoritative item owner/location covered by Spec 06. Only one cargo part may occupy a mount. A stable item-location path SHALL identify `VehicleId + VehiclePartId + nested item path/ItemUid`, not a client object or vector index.

Vehicle motion does not change logical ownership of cargo; it changes the world position derived from the owning part. Cargo mass participates in vehicle mass/physics where the reference does.

Transfers into/out of cargo are commands/activities with authoritative contention handling. If two players target the same item/slot, deterministic server ordering decides; the loser receives a stale/conflict result.

### Passengers

A boarded character remains a distinct authoritative Character with stable identity. A boardable part stores/references the passenger relationship. Vehicle movement relocates the passenger to the resulting authoritative part world position as part of the same atomic displacement. Removing the occupied boardable part unboards the passenger.

No privileged avatar assumption is allowed: several player-controlled Characters may occupy/control/interact with one vehicle. Projection of occupants follows each receiving player's visibility/knowledge policy.

### Controls

Control is a gameplay relationship between a Character and usable controls, not a socket identity. Driver commands include steering/thrust/brake/cruise/z requests and relevant toggles. They are validated against the Character's authoritative occupancy/control capability at execution time.

Disconnecting the controlling client's transport does not delete the Character or vehicle. The server applies the general disconnected-character policy from #85; absent further authoritative input, the vehicle continues under its current physical state/autopilot/follow rules rather than pausing the world.

## 11. Towing

Towing is a bidirectional invariant between two vehicles with tow-cable endpoints. The pinned baseline rejects/null-checks incomplete pairs and classifies each tow attachment as front/side/back from its local mount. For vehicles of length at least three, the longitudinal extent is trisected; shorter vehicles default to front behavior.

Implementation contract:

- a tow relationship is represented by stable `VehicleId` + stable endpoint part identity on both sides;
- both directions are committed/removed atomically;
- destroying/removing/splitting a tow endpoint transfers the link to the correct split component when possible or invalidates it deterministically;
- invalidation removes tow parts/links according to reference behavior and materializes the cord/item through authoritative item-transfer rules;
- tow-cable length is checked from absolute endpoint positions, not client-local bubble coordinates;
- towing modifies effective movement/acceleration through the pinned rules;
- server active-region ownership must keep the relationship coherent even if only one endpoint is visible to a given client.

## 12. Turrets

Turrets are vehicle-mounted gun items plus vehicle power/ammunition integration; they do not define a separate projectile model.

Pinned conformance requirements:

- mountable guns without `NO_TURRET` obtain turret part definitions during data finalization;
- turret readiness requires all relevant gun/ammunition/power requirements;
- fluid-fed turrets use compatible vehicle tanks;
- energy/vehicle-battery requirements are paid by the vehicle, not silently by the controlling player's personal UPS;
- multimag/pocket preparation binds each requirement to a concrete authoritative source so drain-back debits that exact tank/battery;
- when several compatible tanks exist, selection must not consume an insufficient tank if a sufficient eligible sibling is required/available under the reference algorithm;
- burst modes reserve/debit the per-use requirement multiplied by mode quantity and stop when the next burst cannot be powered;
- temporary power-pocket bindings are cleared after firing;
- firing delegates projectile/damage behavior to Spec 09.

Manual targeting/firing is a command. Automated turret behavior is authoritative scheduled simulation. Target candidates and resulting fire events are projected only where visible/audible/otherwise authorized.

## 13. Appliances and static/mobile power constructs

Pinned appliances are implemented with vehicle-part machinery but behave as static structural constructs and participate in power grids. OctoGhast SHOULD model this as a reusable modular-structure capability rather than forcing all future appliances to be "cars", while the Cataclysm profile may share vehicle part/power implementation.

Placement/removal/repair obey part requirements and activities. Plugging/unplugging changes authoritative power topology. UI inspection is a query/projection and MUST NOT pause canonical time; removal is a long-running activity, not a synchronous client mutation.

## 14. Ramps, z levels, falling and vertical motion

Vehicle parts may occupy different z offsets while crossing ramps. Pinned ramp tests drive both cardinally and diagonally across transitions and require per-part z placement to change as each structural mount crosses the ramp, while the boarded driver's position follows its boardable part.

Implementation contract:

- each part's occupied cell includes z;
- ramp transitions update the multi-cell footprint atomically per displacement;
- requested z change is authoritative vehicle state;
- unsupported vehicles can fall; vertical velocity is advanced with the pinned gravity relation `v2 = sqrt(v1^2 + 2*g*d)` with downward sign and `g=9.8 m/s^2` in the reference path;
- horizontal and vertical collision handling preserve the reference distinction;
- active-region/submap transitions cannot duplicate a vehicle spanning z/submap boundaries.

## 15. Spawning, groups and prototype randomness

Vehicle groups and mapgen select prototype definitions from data; spawning instantiates runtime parts, fuel/ammo, items and zones.

RNG-sensitive prototype choices include at least item chance/group selection, turret ammunition chance/type/quantity and damaged/spawn-state decisions. All such choices MUST consume deterministic authoritative RNG. Given the same pinned data, world seed, spawn context and deterministic RNG state, server/headless runs must produce the same spawn result independent of renderer/network timing.

Prototype validation must fail/diagnose illegal part order or illegal mount combinations instead of creating structurally impossible runtime vehicles.

## 16. Persistence and activation

Spec 20 owns save transactions; this spec defines the vehicle payload and fixups.

Persist at minimum all gameplay-relevant motion/control state, origin/orientation/pivot, runtime parts/base items/contents, damage, fuel/ammo, passenger/crew stable IDs, ownership, labels/zones, effects, tow endpoint reference, last-update chronology and power topology state needed for deterministic continuation. The pinned save explicitly persists face/move/turn directions, velocity/vertical velocity/cruise, falling/water/flying/skid flags, `of_turn_carry`, engine/tracking/camera/autopilot states, `last_update_turn`, pivot, ramp/autodrive/follow/patrol state, fuel remainder, parts and tow restoration location.

Vehicle-part load applies part migrations; invalid part IDs are load errors after migration lookup; unknown variants fall back to the definition default; persisted z offsets outside [-10,+10] are invalid at the pinned baseline.

OctoGhast additionally persists stable `VehicleId` and stable part IDs/references. A vehicle is persisted exactly once by the spatial owner containing its canonical origin per Spec 20; cross-submap footprint caches are derived and never duplicate persistence ownership.

On load/activation:

1. load definition registries/migrations;
2. materialize vehicle IDs and intrinsic state;
3. materialize parts/items;
4. fix up passenger/tow/power/external references by stable IDs;
5. rebuild derived geometry/spatial/power caches;
6. perform elapsed-time catch-up exactly once;
7. expose the vehicle to active simulation and client projection.

Transport connection IDs, Godot transforms/interpolation and per-client interest subscriptions are not save state.

## 17. RNG and determinism contract

Vehicle behavior consumes RNG in observable paths including skid onset/direction, collision stun/bleed/fling, reactor fractional fuel rounding, spawn/prototype randomization, turret/gun behavior and other referenced item/combat mechanics.

Requirements:

- RNG is server-authoritative and part of deterministic simulation ordering;
- clients never choose authoritative random outcomes;
- save/restore preserves the RNG stream/state required by #85 so continuation does not reroll outcomes;
- background catch-up must define deterministic sampling/integration and must not consume a different random sequence merely because no client observed the region;
- equal canonical tick + input + initial world/RNG state produces equal vehicle outcomes independent of render FPS, packet arrival callback thread or player camera state.

## 18. Commands, activities, queries and events

Representative **commands/intents**: steer, throttle/brake, change cruise target, toggle engine/accessory, board/unboard, open/close/lock, connect/disconnect tow or cable, turret target/fire request.

Representative **activities**: install part, remove part, repair part, remove/place appliance and other work whose pinned cost spans action moves/time.

Representative **queries**: inspect vehicle/part, available interaction actions, fuel/power summary, cargo view, install candidate/requirement preview, control/turret status.

Representative **result events/projections**: vehicle moved/turned/stopped, part installed/removed/damaged, split, collision result, occupant boarded/unboarded, power state changed, engine state changed, tow topology changed, turret fired, interaction/activity failed/completed.

The Godot client may display rich vehicle UI, animate interpolation and submit requests, but it never owns `VehicleState`, runs collision authority, mutates cargo, advances an activity or decides a power-network result.

## 19. Multiplayer contention and projection

All vehicle mutations occur in canonical server ordering. Examples requiring revalidation at commit:

- two actors install mutually exclusive parts on the same mount;
- two actors remove/repair the same part;
- one actor transfers cargo while another removes its cargo part;
- one actor starts driving while another alters wheels/engine/controls;
- two actors target the same tow/power endpoint;
- vehicle movement collides with an actor/vehicle whose state changed earlier in the same tick.

State exposed to a client is a player-specific projection. At minimum a visible vehicle projection may contain stable vehicle ID, visible occupied cells/orientation/motion sufficient for interpolation, visible part appearance/damage/open state, visible occupants and interaction summaries the player is authorized to inspect. Hidden cargo contents, arbitrary ECS components, off-screen power topology and other players' private state are not replicated merely because they are server state.

A moving vehicle crossing interest boundaries produces enter/update/leave projections from the same authoritative entity; it is not despawned/recreated as simulation state.

## 20. Failure and validation behavior

Implementation must produce structured failures for at least:

- invalid/missing part definition or migration;
- illegal mount topology/slot/required-flag/engine conflict;
- insufficient skills/tools/components or stale activity target;
- incompatible tank fuel/ammo/capacity;
- no valid wheel/traction/propulsion;
- unavailable/broken control/engine/turret;
- insufficient battery/fuel/ammunition;
- invalid tow/power endpoint or cable length/topology;
- target vehicle/part/item moved or disappeared before authoritative resolution;
- out-of-active-map dependency that cannot be safely activated/resolved;
- corrupt persisted z offset/reference/topology.

Expected gameplay rejection must not be a crash and must not leave partial spatial, inventory, passenger, tow or power state.

## 21. Black-box and parity conformance scenarios

The later automated suite SHALL include at least:

1. **Prototype/legal order** — load representative stock prototypes; illegal wheel-before-support and conflicting same-location parts fail; valid frame/hub/wheel order succeeds.
2. **Prototype copy/variants/items** — inherited parts/items/zones append as pinned; part variant fallback and spawn choices are deterministic for a seeded RNG.
3. **Mount matrix** — cover structural adjacency, duplicate part, duplicate slot, double cargo, opaque/mirror, turret-mount, protrusion/animal and required-flag rules.
4. **Install/remove activity** — installation consumes pinned activity cost/resources; removal returns/salvages expected items and recursively removes dependent curtain/seatbelt/mount dependents.
5. **Passenger removal** — removing an occupied boardable part unboards the correct Character and leaves both identities valid.
6. **Split golden** — port `vehicle_split_section`: cross fixture becomes four disjoint vehicles at each 15-degree facing; circle fixture stays one; cargo/passenger/tow/power references follow their component.
7. **Spatial atomicity** — drive/turn a multi-part vehicle across submap and overlapping-player active-region boundaries; every occupied cell resolves to one vehicle and no duplicate simulation occurs.
8. **Movement budget** — fixed velocity scenarios verify `400 / abs(velocity)` displacement opportunity across 10 TPS, including carry; renderer FPS and packet chunking do not alter displacement.
9. **Traction/drag** — port pinned `vehicle_drag_test` golden coefficients/safe/max speeds and `vehicle_efficiency_test` fuel-distance ranges on pavement/dirt/forward/reverse.
10. **No wheels/beached/deep water** — propulsion rejects invalid wheel state/traction and preserves pinned watercraft/sinking behavior.
11. **Collision terrain** — fixed vehicle/part/mass/velocity against bashable and impassable terrain matches pinned post-collision velocity and damage within exact/reference tolerances.
12. **Collision creature** — deterministic RNG seed matches stun/bleed/fling/damage behavior; same-vehicle passenger is not struck.
13. **Vehicle/vehicle contention** — two moving vehicles reach an intersecting cell in one canonical tick; stable server ordering gives a repeatable collision result.
14. **Battery network loss** — port `power_loss_to_cables` including lossy multi-hop charge/discharge.
15. **Consumer deficit/recovery** — port power-disabled tests: deficit disables eligible consumers; recovered grid re-enables only power-disabled, not user-disabled, loads.
16. **Renewable catch-up** — port summer/winter/no-sun, 2x30 vs 1x60, off-map, no-double-charge, sub-minute skip, wind/water and remote-battery-through-cable cases.
17. **Power network persistence** — serialize/load network ID/last-resolved/nodes, old-save rebuild, stable unchanged ID, topology mutation/split and independent grids.
18. **Fuel/tank binding** — exact source tank/battery is bound/debited; incompatible/insufficient tank behavior matches `vehicle_part_test`.
19. **Turret matrix** — port all-turret install/readiness/fire test plus multimag battery, tank, burst, USE_UPS isolation and insufficient-next-burst cases.
20. **Tow lifecycle** — connect two vehicles, move/tow, exceed cable validity, remove endpoint, split endpoint component, save/load/reconnect; both link directions remain coherent or atomically clear.
21. **Ramp/z** — port ramp positions at transition x 59/60/61, cardinal and angled up/down/no-ramp; passenger follows boardable part; multi-z footprint stays indexed once.
22. **Appliance grid** — place battery/generator/load appliances, plug/unplug, unload/reactivate region, verify topology and energy continuation independent of a UI being open.
23. **Cargo contention** — two players request the same cargo transfer; one deterministic success, one stale rejection; removing cargo part concurrently cannot duplicate/delete item state.
24. **Driver disconnect** — disconnect a controlling client while server runs; vehicle/Character persist, no world pause occurs, reconnect rebinds stable PlayerId/CharacterId and receives authoritative current projection.
25. **Save/load round trip** — a damaged moving/fuelled multi-part vehicle with cargo, passenger, enabled consumers, power link, tow link and partially accumulated movement state round-trips at a save barrier and continues with the same next deterministic result.
26. **Prototype export parity fixture** — use pinned `vehicle_export_test` as a schema/golden fixture where OctoGhast exposes a Cataclysm-prototype export adapter.
27. **Mapgen placement/removal** — port mapgen vehicle placement/removal and split-during-mapgen scenarios without contaminating the server's active world index.

## 22. Ordered implementation slices

A later implementation can be safely decomposed without changing this contract:

1. definitions/prototypes + runtime VehicleId/part graph + persistence skeleton;
2. multi-cell spatial footprint, part transforms and basic spawn;
3. install/remove/repair/split activities and item/cargo ownership;
4. wheels/engine/fuel + fixed-tick movement opportunity and steering;
5. collision/damage + passengers/control;
6. batteries/accessories/renewables + power networks/background catch-up;
7. towing, ramps/z and water/falling;
8. turrets/appliances/advanced vehicle tools;
9. mapgen/groups, interaction projections and complete parity fixtures.

Each slice must remain headless and server-authoritative; Godot integration consumes the explicit query/command/projection contracts rather than introducing another vehicle state model.

## 23. Acceptance-criteria coverage

- Vehicle entity, coordinate transforms and part graph/invariants: §§4–6.
- Installation/removal/damage/splitting: §§6, 9, 20–21.
- Fuel/engines/batteries/generation/electrical load: §7.
- Wheels/traction/steering/movement and time coupling: §8.
- Collision with terrain/creatures/vehicles: §9.
- Cargo/item-location/passenger/control: §10.
- Towing/turrets/appliances/z-level behavior: §§11–14.
- Existing tests catalogued into conformance scenarios including save/load: §§2, 21.

No new cross-cutting architectural decision is required by this investigation. Vehicle-specific authority, scheduling, active/background ownership, identity, persistence and client projection can be expressed using the already-reviewed contracts in #52/#57/#58/#66/#69/#71/#74/#77/#85/#90.
