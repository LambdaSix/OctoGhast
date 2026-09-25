# Spec 14 — Environment simulation: weather, fields, fire, scent and decay

Status: **Specification complete**  
Programme: #64, #65  
Feature ticket: #79  
Prerequisites: #66 / Spec 01, #77 / Spec 12, #83 / data registries, #70 / Spec 05, #67 / Spec 02, #85 / Spec 20, #82 / Spec 17  
Pinned reference: `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Scope and architecture boundary

This specification defines the autonomous environmental simulation required for the Cataclysm reference profile: calendar/season-coupled weather, ambient temperature and daylight, weather effects, fields, fire, smoke/gases, emissions, scent, plant growth/harvest timing, rot/spoilage environmental inputs, and environmental exposure/hazard interfaces.

It is a behavioral specification, not a port of CDDA's class layout. The implementation must preserve pinned-CDDA externally observable rules and data semantics while following OctoGhast's existing authoritative real-time/co-op architecture.

Four layers are deliberately distinguished:

1. **Pinned CDDA reference behaviour** — the exact baseline used as evidence.
2. **OctoGhast Cataclysm profile** — the compatibility implementation of those rules.
3. **Generic Core contract** — reusable deterministic environmental state, scheduling, spatial queries and mutation boundaries.
4. **Future evolution seams** — CDDA-specific constants, grid propagation rules, weather taxonomy and plant/spoilage policy stay profile-level unless independently useful as Core invariants.

CDDA parity is a completeness benchmark, not a permanent limit on future OctoGhast environmental mechanics.

## 2. Authoritative evidence

All references below are to the pinned commit.

### Weather, calendar and light
- `src/weather_gen.cpp/.h` — spatial/time weather generation; temperature, humidity, pressure and wind; generator JSON; weather classification.
- `src/weather.cpp/.h` — current-weather lifecycle, durations, local humidity/wind, precipitation effects, retrospective weather summation, rain collectors and incident sunlight.
- `src/calendar.cpp` — season-effective time, solar altitude, sunrise/sunset, moon phase/light, sunlight and irradiance.
- `src/lightmap.cpp` — consumption of natural light/weather sight modifiers by map light caches.
- `src/weather_type.cpp/.h`, `data/json/weather_type.json`, `doc/JSON/WEATHER_TYPE.md` — immutable weather definitions, priority/conditions, precipitation, light/sight/sound/effects and duration defaults.
- `tests/weather_test.cpp` — `weather_realism`, `eternal_season`, `local_wind_chill_calculation`.

### Fields, fire, gas and emissions
- `src/field.cpp/.h` — mutable field entries, intensity/age/source and decay.
- `src/field_type.cpp/.h`, `data/json/field_type.json` — immutable field definitions/intensity levels/processors.
- `src/map_field.cpp` — per-turn field processing, gas spread, fire, terrain/item/vehicle/creature interaction, emission handling and weather-accelerated decay.
- `src/emit.cpp/.h`, `data/json/emit.json` — field emission definitions.
- `tests/field_test.cpp` — expiry, acid falling, fire spread/fire vents, wandering/radiation/fungal fields, creature effects and field API behavior.

### Scent
- `src/scent_map.cpp/.h`, `src/scent_block.cpp/.h` — scent storage, diffusion, blockers/reducers, decay and field/gas modification.
- `data/json/scent_types.json` — scent IDs and receptive species.

### Rot, plants and catch-up
- `src/item.cpp/.h`, `src/item_pocket.cpp` — temperature/rot processing and containment spoilage inputs; detailed item ownership remains Spec 05/06.
- `tests/rot_test.cpp` — `Rate_of_rotting`, `Items_rot_away`, `Hourly_rotpoints`.
- `doc/JSON/ITEM.md` — seed `growth_stages`, `growth_temp`, fruit/byproducts and terrain requirements.
- `src/map.cpp` — submap actualization and elapsed-time plant growth/restocking.
- `src/iexamine.cpp` — plant harvest behavior and harvest activity/results.

## 3. Definition data versus mutable runtime state

### 3.1 Immutable/finalized definitions

Definitions are registry-owned, stable-string-ID data loaded before world simulation and validated/finalized under Spec 18/#83.

**WeatherGeneratorDefinition**
- stable generator ID;
- mandatory base temperature, humidity, pressure and wind;
- optional wind distribution/season variation;
- per-season temperature/humidity modifiers, defaulting to zero;
- optional weather whitelist or blacklist, which are mutually exclusive.

**WeatherTypeDefinition**
- stable ID and translated/presentation metadata;
- priority and condition expression;
- required-weather predecessors;
- precipitation class and whether precipitation is rain;
- minimum/maximum duration, defaulting to 5 minutes where omitted;
- light multiplier/modifier, sun multiplier, sight/ranged/sound modifiers;
- dangerous flag and passive creature effects;
- presentation-only animation/tint/sound fields.

**FieldTypeDefinition / FieldIntensityLevel**
- stable field ID, phase and priority;
- ordered intensity levels and maximum intensity;
- per-level danger, transparency/light, concentration, effects and scent neutralization;
- half-life and linear/exponential mode;
- decay amount factor, spread percentage, outdoor/underwater acceleration;
- processor capabilities such as gas spread, fire, radiation, fungal/wandering behavior;
- cache-dirty/display metadata.

**EmissionDefinition**
- stable emission ID;
- field type, intensity, quantity and chance parameters.

**ScentTypeDefinition**
- stable scent ID;
- set of receptive species IDs; all references must validate.

**Seed/GrowthDefinition**
- plant name, fruit, optional seed reproduction (default true), byproducts;
- ordered growth-stage flag/duration pairs;
- growth temperature (default 10 C);
- required terrain flag (default `PLANTABLE`).

Definitions do not acquire runtime identity, age, position or per-instance mutation merely because they are represented as ECS-friendly data.

### 3.2 Mutable authoritative runtime state

Runtime environmental state is world/server-owned, never Godot-owned.

At minimum:
- canonical world time from Spec 01;
- world seed and gameplay RNG state/streams required for deterministic continuation;
- weather state required by the Cataclysm profile, including current/next transition information and wind-direction state where the baseline makes it stateful;
- spatial weather samples/derived observations as caches, not independent player-owned worlds;
- each live field entry: absolute tile location, field type ID, intensity, age/decay state and effect source/causer reference where present;
- scent intensity/type on authoritative spatial state required by active/background simulation;
- plant furniture/stage plus its seed item/age and fertilizer state;
- environmental scheduling anchors/last-processed times needed for unloaded catch-up;
- perishable item state remains owned by the item aggregate in Spec 05, not duplicated in the environment service.

## 4. Units and canonical update cadence

The Cataclysm profile configures Spec 01 canonical time at 10 simulation ticks per world second; 10 ticks equal one pinned-CDDA turn/world second. Wall-clock/render/network time never advances environmental rules directly.

The Cataclysm profile uses:
- temperatures as typed temperature values; weather generator calculations are Celsius/Kelvin-compatible and data accepts typed units;
- humidity as percent clamped to [0,100];
- pressure in the baseline weather scale used by weather conditions (JSON thresholds around hPa-equivalent values such as 1020);
- wind power as the baseline numeric wind scale/directional degrees;
- field intensity as positive integer levels bounded by the definition's intensity-level count;
- field age as game-time duration;
- scent intensity as non-negative integer scalar;
- precipitation rates exposed by `precip_mm_per_hour`: very-light 0.5, light 1.5, heavy 3 mm/hour rain-equivalent;
- plant and spoilage durations as canonical game-time durations.

Cadence contracts:
- field processing semantics are one CDDA processing turn = one world second. OctoGhast may schedule the work over ten canonical ticks only if the one-second result and ordering remain equivalent; otherwise execute at the deterministic one-second boundary.
- terrain/furniture emissions are checked every 10 seconds in the pinned baseline.
- exposed weather duration is data-driven: the current weather schedules its next re-evaluation by a random duration in `[duration_min,duration_max]`.
- weather wetting checks occur on the pinned 6-second cadence where applicable.
- plant growth and unloaded environmental actualization are elapsed-time based; they must not require a player input turn.
- scent diffusion is a periodic simulation operation. The pinned local-avatar optimization that stops diffusion after 1000 stationary turns is not a generic Core rule.

Periodic phase is derived from canonical simulation time and persists across save/load; reconnecting a client does not restart environmental timers.

## 5. Pinned weather generation and classification

### 5.1 Continuous weather inputs

The pinned generator derives common data from absolute map-square position, game time and world seed:
- `x = location.x / 2000`;
- `y = location.y / 2000`;
- `z = days(real_time - turn_zero)`;
- simplex-noise seed is the world seed reduced to the generator's supported seed range;
- season/year fraction is calendar-derived.

Temperature uses seasonal, daily and spatial/temporal noise. Important pinned constants are:
- coldest hour: 05:00;
- daily magnitude: 5 K;
- daily seasonal-range modifier: 2 K;
- annual seasonality magnitude: 12 K.

Conceptually:

`baseline = baseTemperature + seasonalManualModifier + dailyTerm + seasonalTerm`

and

`temperature = baseline + simplexNoise(position,time,seed) * seasonalNoiseScale`.

The exact baseline expression in `weather_gen.cpp` is the parity oracle; do not approximate it with wall-clock interpolation.

Humidity is:

`clamp(0,100, baseHumidity + seasonalHumidityModifier + 100 * (0.15*seasonality + noise(seed+101)*0.2*(-seasonality+2)))`.

Pressure is:

`basePressure + noise(seed+211) * 15 * (-seasonality+2)`.

Wind combines a noise component, base wind, pressure and randomized distribution/season variation; wind direction is stateful and selected from season-specific weighted distributions. All of these random draws use the gameplay RNG.

### 5.2 Weather-type selection

Weather types are finalized in ascending priority. Selection starts at clear. For each type:
1. its `required_weathers` must be empty or include the condition selected so far;
2. its condition is evaluated against the generated weather point and `weather_location`;
3. if true it becomes the current condition;
4. later/higher-priority matching types can supersede it.

The pinned implementation temporarily exposes the candidate weather through global game/dialogue/avatar state because the CDDA condition evaluator lacks a direct weather-point parameter. **OctoGhast must preserve condition semantics, not that global/avatar coupling.** The evaluator receives an explicit immutable environmental condition context.

### 5.3 Weather transition state

Pinned `weather_manager::update_weather` samples at the local player position, selects a type, copies temperature/wind into manager state and sets:

`nextWeather = currentTime + rng(duration_min, duration_max)`.

That single-avatar manager is reference behavior, not an OctoGhast ownership model.

## 6. OctoGhast weather adaptation and implementation contract

The authoritative environment belongs to the world. No player or connection owns "the weather".

Required multiplayer semantics:
- environmental queries are keyed by authoritative absolute position and canonical world time;
- two actors at the same location/time observe the same authoritative weather result;
- separated active regions may observe different spatial weather inputs without either player's query replacing a global "current avatar weather";
- overlapping active regions evaluate/simulate shared world environmental state exactly once;
- query order, client count, packet arrival and render frame rate must not alter weather or RNG outcomes;
- a disconnected player neither freezes nor deletes environmental state.

The Cataclysm profile must match pinned single-location scenarios exactly. Where the upstream implementation's single mutable wind/weather-manager state would make separated-location query order observable, OctoGhast must use deterministic server-owned environmental state/order keys or stable spatially keyed random streams so adding a second observer cannot perturb the first observer's result. The storage/cache granularity is an implementation choice provided this behavioral invariant and pinned parity fixtures hold.

Do not turn CDDA's 2000-map-square noise scale or weather taxonomy into a generic Core coordinate invariant. Core needs only deterministic environmental queries by world position/time plus durable scheduling/state.

## 7. Daylight, sunlight and weather-local conditions

Solar chronology is authoritative calendar state from Spec 01.

Pinned reference:
- astronomical dawn = -18 degrees solar altitude;
- nautical dawn = -12 degrees;
- civil dawn = -6 degrees;
- sunrise/day threshold = -1 degree;
- full sunlight caps at 125 light units above 60 degrees solar altitude;
- irradiance is zero below the horizon and otherwise `1000 * sin(solarAltitude)`;
- moonlight depends on lunar phase;
- weather-adjusted incident light is `max(0, sun_light_at(t) * light_multiplier + light_modifier)`;
- solar irradiance is multiplied by weather `sun_multiplier`.

Local weather modifiers apply to authoritative light/FOV/solar-energy queries. Godot may interpolate visual brightness, but gameplay visibility, solar power and other rules use server results.

Local humidity/wind preserve pinned shelter modifiers:
- sheltered wind power is zero;
- forest/forest-water overmap terrain halves wind;
- positive z adds `z * min(5, windPower)`;
- an adjacent wind blocker reduces wind to one tenth;
- outdoor light-or-heavier rain forces local humidity to 100;
- sheltered humidity uses the pinned `humidity * (100-humidity)/100 + humidity` adjustment.

## 8. Weather side effects and ordering

When a weather state applies:
- rain fills collectors using the pinned precipitation rate;
- rain accelerates outdoor field/scent cleanup through the pinned decay path;
- unroofed exposed Characters receive wetness on the 6-second cadence with pinned wetness amounts: very-light 5, light 30, heavy 60;
- weather passive field effects are evaluated for affected creatures using normal immunity rules;
- weather light/sight/sound modifiers feed the corresponding server systems.

Pinned `handle_weather_effects` is local-avatar oriented. OctoGhast applies exposure per authoritative creature/location, not only to a privileged avatar, and projects resulting state/messages only to entitled clients.

Within a canonical environment step, use deterministic ordering:
1. establish canonical time/environment sample;
2. process due weather transition/effects;
3. process field/environment state for each active cell exactly once;
4. apply creature/item/world effects in stable authoritative order;
5. commit derived cache invalidations;
6. build player-specific projections/events.

This records the required Cataclysm environment-before-later-actor causal constraint. The [canonical ordering/admission/activation architecture](./architecture-canonical-ordering-admission-activation.md) resolves the former #95 gate: the Cataclysm profile keeps controlled-Character work before this environment band, then monsters and NPCs afterward; monster/NPC per-turn budget processing remains at their reference-relative actor bands, while controlled-Character replenishment is settled later for the next controlled opportunity. Activation/catch-up uses the shared semantic-frontier/half-open-interval convention. The profile cadence does not impose 10 TPS on generic Core.

## 9. Field state and creation semantics

A tile may contain multiple field types. Each live entry owns:
- field type ID;
- intensity;
- age;
- optional effect source/causer;
- decay scheduling state.

Intensity is bounded by the field definition's maximum. Setting requested intensity <= 0 marks the entry dead; storage may retain a clamped internal value until removal, so **alive/dead state is authoritative**, not merely `intensity == 0`.

Adding a field:
- rejects null type;
- if the type is absent, creates an entry with requested intensity/age/source;
- if already present, adds intensity up to the maximum and updates source; a previously dead entry is revived with the new age;
- display priority may change the tile's displayed field but display choice is not authoritative field existence.

A newly created field whose age is exactly zero does not run ordinary processors on its first field-processing turn; it still performs decay bookkeeping. This one-turn grace/order behavior is parity-significant.

## 10. Field decay

Every processed turn increments field age by one turn.

For non-fire fields with positive half-life:
- if `linear_half_life`, the next intensity-loss time is deterministic at the current age-adjusted half-life boundary;
- otherwise the next intensity-loss time is sampled from an exponential distribution parameterized by the configured half-life;
- when due, age resets to zero and intensity decreases by one;
- changing field age resets the cached decay time so it is recomputed.

Fire retains its legacy separate stochastic decay rule: after age advances, it loses intensity when its half-life is less than a two-dice roll against current age; on loss, age resets to zero.

Weather/outdoor/water processors may add age or otherwise alter decay before the normal decay step. These mutations must retain reference ordering.

All random decay draws use the authoritative gameplay RNG service. Save/load must not silently reroll already-established decay state if doing so changes future outcomes; persist either the due decay state or sufficient deterministic RNG/scheduling state to continue equivalently.

## 11. Gases, smoke and field propagation

For gaseous fields:
- outdoor processing may add `outdoor_age_speedup`;
- intensity-level scent neutralization affects nearby scent;
- spread requires intensity > 1 and passes the type's random spread test, modified by local wind;
- the field tries to fall first when downward movement is valid;
- a destination must have no same-type field or a weaker one, and must be passable or `PERMEABLE`;
- when one intensity unit moves, age is partitioned proportionally using `sourceAge/sourceIntensity`;
- sheltered/low-wind horizontal spread is randomized; stronger wind biases spread by wind direction/blockers;
- if horizontal spread does not occur, upward spread may be attempted.

These are grid-authoritative Cataclysm-profile rules. Generic Core only requires a deterministic spatial propagation facility; it must not assume all future rulesets use 8-neighbor cellular gas movement.

Smoke/toxic gas behavior is largely data-driven through intensity-level effects, opacity/translucency, concentration, scent neutralization, half-life and spread parameters. For example pinned smoke has a 2-minute half-life, 10 percent spread, accelerated decay and inhalation/eye effects with environmental-resistance/vehicle-immunity checks.

## 12. Fire interaction contract

Fire is a field with special processing.

Pinned behavior includes:
- fire may consume/explode items on its tile;
- per turn, item consumption is capped by `fireIntensity * 2`;
- item burning produces smoke/fuel and burn products according to material/item rules;
- vehicles at the tile receive heat damage proportional to `fireIntensity * 10`;
- swimmable/water terrain ages fire rapidly (+4 minutes in the captured rule path);
- flammable terrain/furniture feeds the fire, contributes smoke and may be bashed/transformed/produce ash;
- unsupported fire over open space can fall to the z-level below and combine with existing fire, capped at the field maximum;
- fire creates/spreads heat/hot-air and smoke through its field processors;
- field intensity/age and terrain/item mutations are authoritative and ordered.

Fire spread/destruction must use map mutation APIs from Spec 12 and item mutation APIs from Spec 05. It must not directly mutate presentation objects.

Concurrent player actions against burning terrain/items resolve through the ordinary deterministic command ordering. A player attempting to move/extinguish/take an item after an earlier same-step fire mutation must validate against the resulting authoritative state and fail/revalidate rather than resurrect stale state.

## 13. Creature and environmental-hazard effects

Field intensity levels may attach effects. Application honors:
- in-vehicle / inside-vehicle / outside-vehicle immunity and chance gates;
- creature effect immunity;
- data-driven environmental immunity checks, including body-part environmental resistance and flags;
- environmental effects versus ordinary effects;
- body-part/intensity/duration data;
- effect message emission only when the effect is actually applied.

The environment system supplies exposure; Character/Creature owns health/effect state under Specs 02/10. Disease/effect progression after exposure remains in the relevant actor effect system. This spec does **not** create a second disease clock. Environmental disease/hazard sources such as toxic gas, smoke, fungal/radiation fields and weather passive effects must invoke the same authoritative effect APIs as other exposure sources.

## 14. Emissions

An emission is immutable definition data that creates/increases a field according to field ID, intensity, quantity/chance rules.

Pinned terrain/furniture emissions are checked every 10 seconds. Items can also emit fields from their active processing path.

Implementation contract:
- all emission RNG is authoritative and seeded;
- source identity/location is captured where field provenance is behaviorally relevant;
- multiple observers never multiply an emission;
- newly emitted fields follow the newborn-field ordering rule;
- emission definitions validate referenced field IDs at content finalization.

## 15. Scent

### 15.1 Pinned reference behavior

Pinned scent is a bubble-relative 2D integer map with:
- update radius 40 around the center;
- nominal z reach of one accessible z level through a documented 2D hack;
- a single active scent type plus scalar values;
- `NO_SCENT` cells blocking scent and forcing zero;
- `REDUCE_SCENT` cells transmitting/receiving at reduced weight;
- diffusion constant 100 on a 1000-based scale;
- per-decay call `value = max(0,value-1)`.

Diffusion uses a weighted 3x3 neighborhood. Normal cells use weight 10; reduced-scent cells use weight 2. For each non-blocked cell the pinned formula removes outgoing diffusion, applies extra absorption caused by reduced-scent neighbors, adds incoming weighted neighbor scent, then divides by 10000.

Gas fields can neutralize scent through the scent-block aggregation path before modifications are committed.

The baseline stops scent diffusion after the single player has not moved for 1000 turns. This is an optimization tied to one avatar, not a physical scent rule.

### 15.2 OctoGhast adaptation

Scent is authoritative world/spatial state required by AI, not a player-owned view. Therefore:
- no privileged player position controls whether scent exists or progresses;
- separated active regions maintain the scent state needed by creatures in those regions;
- overlapping active regions process any shared cell once;
- activation/deactivation and background catch-up follow Spec 12;
- a stationary or disconnected player cannot freeze scent needed by another active creature/player;
- scent state is not sent to a client unless gameplay/UI legitimately exposes it.

The Cataclysm profile preserves the pinned integer diffusion/blocker/reducer rules. Core must not encode the 40-cell radius, one-scent-type representation or 2D z hack as permanent platform invariants.

## 16. Plants, growth and harvest timing

Seed definitions provide ordered growth-stage durations. Pinned actualization compares seed age to cumulative stage duration, determines the target stage, and advances through each missed stage in sequence. It intentionally does not reverse furniture stages when time is rewound.

When multiple stages are crossed during actualization:
- each stage transition is performed in order;
- fertilizer is removed as the growth path specifies;
- intermediate transition side effects occur;
- the furniture transform is applied step-by-step rather than jumping straight to final appearance.

`growth_temp` defaults to 10 C and is checked by the planting/growth-stage rules represented by the pinned item data contract. Below the required temperature a crop is not eligible to advance/plant where the reference rule says so.

Harvest remains an actor command/activity, not autonomous background mutation. For a generic seeded plant:
- harvest requires the stored seed item to exist; missing seed is invalid/corrupt map state and the pinned path clears the broken plant representation;
- `CUT_HARVEST` requires the grass-cutting quality;
- the harvest activity costs 60 seconds in the pinned path;
- survival skill determines random plant count `rng(skill/2, skill)`, multiplied by furniture harvest multiplier and clamped to 1..12;
- seed count is at least 1 and otherwise `rng(plantCount/4, plantCount/2)`;
- resulting fruit/seeds/byproducts come from the seed definition;
- the plant furniture returns to its base state.

The 60-second cost remains Cataclysm move/activity-time semantics mapped through Spec 01/#69; opening a harvest UI does not pause the world.

## 17. Rot/spoilage environment contract

Detailed per-item rot/temperature state and identity are already authoritative in Spec 05. This spec owns the environmental inputs/history supplied to that lifecycle.

Pinned rot evidence establishes:
- rot is accumulated game-time duration, temperature-dependent;
- at 65 F / approximately 18.3 C the reference test expects roughly one hour of rot per elapsed hour;
- preserving containers/freezer conditions can prevent rot;
- unloaded elapsed time is expected to be actualized rather than ignored;
- hourly rot-point tests constrain integration behavior.

Environment API must be able to answer deterministic temperature/exposure history for an item's absolute location and containment context over an elapsed interval. An implementation may use exact stepping, historical samples or mathematically equivalent aggregation, but:
- threshold/order-sensitive changes must not be skipped;
- active and unloaded catch-up supplied equivalent conditions must converge;
- weather/temperature changes during the interval must be represented rather than replacing the entire interval with the activation-time temperature;
- container insulation/spoil multipliers are supplied by the item/pocket system, not guessed by environment.

## 18. Active regions, unloaded/background simulation and catch-up

### 18.1 Active state

The server's world-owned ActiveRegionSet from Spec 12 is the ownership boundary. Environmental systems process active authoritative cells/entities exactly once per due cadence, independent of observer count.

### 18.2 Deactivation

Before an environmental partition leaves active simulation, persist:
- durable field entries and their ages/sources/decay continuation state;
- plant/item states owned by that partition;
- durable environmental state/scheduling anchors needed to reconstruct outcomes;
- last processed canonical time.

Transient light/FOV/field caches and client replication baselines are derived and are not persistence identity.

### 18.3 Reactivation/catch-up

On activation at time T:
1. load durable partition state;
2. determine elapsed canonical time since last authoritative processing;
3. obtain deterministic weather/temperature chronology for the interval;
4. actualize elapsed plant/item/world effects in dependency order;
5. run every parity-significant threshold/transition/one-shot side effect that cannot be safely aggregated;
6. rebuild caches/indexes;
7. mark the partition processed at T;
8. only then enter ordinary active simulation.

Catch-up may be optimized analytically only when the result, RNG use and emitted durable side effects are observationally equivalent to stepwise pinned behavior.

No catch-up job may be executed once per connecting player. Concurrent activation requests for the same partition coalesce onto one authoritative transition.

## 19. Persistence and stable identity

Persisted environmental state is world state under Spec 20.

Required durable information includes, as applicable:
- canonical time/world seed and shared RNG continuation state;
- environmental generator/profile IDs and world options that change rules;
- field entries by absolute world tile with stable field type ID, intensity, age and durable source identity;
- decay due/continuation data when required to avoid reroll after load;
- plant stage/furniture and seed item state;
- environmental partition last-processed times/background anchors;
- weather transition/wind state that is not purely derivable from position/time/seed.

Do not persist:
- Godot particles/nodes/animation progress;
- socket/connection IDs;
- per-client visibility subscriptions;
- light/transparency/field lookup caches that can be rebuilt deterministically.

Field source references must use stable domain entity identity where they survive unload/save. Process memory pointers/ECS storage addresses are never durable references.

## 20. RNG and determinism

Environmental randomness must flow through the server gameplay RNG/deterministic random service. This includes:
- weather duration and wind distribution state;
- stochastic field half-life;
- gas direction/spread choices;
- fire feeding/spread/destruction rolls;
- emissions;
- harvest yields;
- any random environmental side effects.

Deterministic requirements:
- same definitions, world seed, saved state, canonical time and ordered command stream produce the same authoritative result;
- network arrival timing, client count and render frame rate cannot alter RNG consumption;
- scanning overlapping active regions cannot process/RNG-roll the same cell twice;
- iteration over hash/ECS storage order must not determine results; use stable spatial/system order or explicit deterministic keys;
- save itself must not consume gameplay RNG;
- where exact CDDA RNG-stream parity is brittle because unrelated systems share a stream, conformance may use seeded isolated fixtures or statistical/tolerance tests, but invariants and transition ordering remain exact.

## 21. Commands, activities, queries and events

### Commands/intents
Examples: ignite/extinguish a tile, harvest a plant, interact with a weather collector. Clients send intent; server validates current authoritative state and resolves in deterministic command order.

### Long-running activities
Harvesting and other actor-time environmental interactions use Spec 04/#69 activities. Environment continues progressing while an actor performs them.

### Synchronous server queries
Examples:
- `GetWeather(position,time)`
- `GetAmbientTemperature(position,time)`
- `GetIncidentLight(position,time)`
- `GetLocalWind(position,time,shelterContext)`
- `GetFields(tile)`
- `GetScentForAi(position)`

Queries are read-only and cannot advance simulation/RNG merely because a client/UI opened a screen.

### Resulting events/messages
Environment may emit domain events such as weather transition, field created/intensified/extinguished, terrain burned/transformed, item destroyed by fire, creature environmental effect applied, plant stage changed, or harvest completed. Audience/projection is computed separately; an event existing on the server does not entitle every client to receive it.

## 22. Client projection and information boundaries

Godot receives player-specific projections, not environmental ECS/world internals.

Potentially project:
- visible weather state appropriate to that player's current location;
- ambient light/weather presentation parameters;
- fields on tiles the player is entitled to observe, with display type/intensity needed for rendering and warnings;
- visible terrain/item changes caused by fire;
- owner-visible Character wetness/effects;
- interaction affordances and authoritative result messages.

Do not project:
- hidden/unseen fields or entities;
- full scent grids solely because they exist;
- undiscovered weather/environment state elsewhere in the world;
- server RNG state/seeds;
- background partitions outside interest;
- internal field processor, scheduler or cache state.

A player in region A cannot infer hidden fire/gas/scent in region B merely because another player keeps B active.

## 23. Validation and failure behavior

Content/finalization failures:
- invalid weather/field/scent/emission IDs or references => source-located load/finalization error;
- weather whitelist and blacklist both set => reject;
- malformed durations/intensities/conditions => reject;
- invalid scent receptive species => consistency diagnostic/error per shared content-validation policy;
- missing plant growth transforms/required references => data validation failure where detectable.

Runtime:
- adding null field fails without partial mutation;
- intensity clamps to definition maximum;
- dead fields are removed through the normal mutation/cache path;
- out-of-bounds/unloaded mutation requires the owning world partition to be activated/leased or is rejected; no client-local mutation fallback;
- stale concurrent commands revalidate after prior ordered mutations;
- missing plant seed in a plant tile is treated as invalid/corrupt state and follows explicit recovery/diagnostic behavior, never undefined access;
- unavailable client connection does not cancel world environmental progression.

## 24. Core contract and future evolution seams

### Generic Core should provide
- typed canonical time/duration/temperature and absolute world-position inputs;
- deterministic scheduled/periodic world work;
- partitioned active/background world ownership;
- stable IDs and registry-backed definitions;
- deterministic RNG/stream service;
- controlled map/item/creature mutation interfaces;
- persistence hooks and catch-up boundaries;
- explicit query/event/projection interfaces.

### Cataclysm profile owns
- 10 ticks = 1 CDDA turn/second mapping;
- exact weather formulas/noise constants and weather type hierarchy;
- field intensity/half-life semantics;
- grid gas/fire propagation;
- scent scalar/diffusion constants;
- CDDA precipitation/wetness/harvest/rot rules;
- CDDA JSON schemas/IDs.

### Future evolution seams
Core must permit different weather models, continuous-space fluids, richer scent species, alternative plant ecology, different action economies and client presentation without rewriting networking/persistence/scheduling fundamentals.

## 25. Black-box and conformance scenarios

The later automated suite must include at least the following.

### Weather and light
1. **Pinned weather sample golden:** fixed world seed, absolute position and time produce pinned temperature/humidity/pressure/weather class.
2. **Season/day temperature curve:** fixed location over representative day/season points matches the pinned generator within numeric tolerance.
3. **Weather priority chain:** crafted weather point satisfies multiple conditions; highest eligible ordered type wins while respecting `required_weathers`.
4. **Duration bounds:** every selected weather transition duration lies inclusively within its type's min/max and is reproducible under isolated seed/state.
5. **Separated players:** two distant players receive location-correct weather without one query changing the other's result.
6. **Overlapping players:** two players on the same tile/time receive one identical authoritative weather result and weather side effects apply once.
7. **Shelter modifiers:** sheltered wind is zero; forest/wall/elevation modifiers match pinned values.
8. **Solar golden:** representative solar altitudes hit the pinned light curve and irradiance rules; weather light modifiers apply after the solar value.
9. **Rain exposure:** exposed versus roofed Characters receive the correct 6-second wetting behavior; multiple Characters are handled independently.
10. **Retrospective collector:** unloaded rain collector catch-up over a fixed interval matches pinned `sum_conditions` behavior within documented sampling tolerance.

### Fields, gas and fire
11. **Newborn grace:** a just-created age-zero field does not run ordinary processors until the next field turn.
12. **Intensity clamp/removal:** additions clamp to max; reduction through zero kills/removes the field without leaving a visible ghost.
13. **Linear half-life:** fixed linear field loses one intensity at the configured boundary.
14. **Exponential half-life seeded:** fixed seed/state produces reproducible sampled decay.
15. **Fire legacy decay:** fire follows the dedicated dice/age rule rather than generic field half-life scheduling.
16. **Gas transfer conservation:** spreading one unit partitions source/destination intensity and age as pinned.
17. **Gas obstacle:** impermeable impassable tile blocks spread; `PERMEABLE` permits it.
18. **Gas vertical:** downward spread is preferred when valid; otherwise horizontal/upward paths follow pinned constraints.
19. **Wind-biased gas:** controlled wind/blockers produce pinned allowed/bias behavior under a seeded fixture.
20. **Smoke creature effect:** eye/mouth protection and inside-vehicle immunity prevent matching data-driven effects; unprotected target receives them.
21. **Fire item burn:** fixed fuel set produces expected consumption limit, burn products and smoke/fuel under seeded fixture.
22. **Fire terrain/vehicle:** flammable terrain feeds/transforms and vehicle receives heat damage proportional to intensity.
23. **Fire across overlapping interest:** one shared fire cell advances once even when visible to two players.
24. **Emission cadence:** terrain/furniture emitter checks on the 10-second phase and creates no duplicate field because two clients observe it.

### Scent
25. **Diffusion golden:** seeded/static 3x3/controlled grid matches the pinned integer diffusion formula.
26. **NO_SCENT barrier:** blocked cell becomes zero and prevents normal diffusion through it.
27. **REDUCE_SCENT:** reduced cells use pinned 20-percent weighting/absorption behavior.
28. **Decay:** each decay call subtracts one to a floor of zero.
29. **Gas neutralization:** configured gas intensity reduces nearby scent through the committed scent-block path.
30. **Stationary multiplayer:** one stationary player does not freeze scent needed by another active actor/region.
31. **Scent visibility isolation:** clients receive no scent grid unless an explicit gameplay surface entitles them.

### Plants, rot and background
32. **Plant cumulative stages:** seed age crossing multiple thresholds advances every intermediate transform in order.
33. **Plant cold gate:** temperature below `growth_temp` blocks the pinned growth/planting transition where applicable.
34. **Harvest yield seeded:** controlled skill/seed produces pinned clamped plant/seed counts and byproducts; 60-second activity cost is preserved.
35. **Rot standard temperature:** at 65 F / ~18.3 C, one elapsed hour gives approximately one hour rot as upstream test expects.
36. **Preservation:** preserving/freezer container fixture suppresses rot as pinned.
37. **Active/background equivalence:** identical environmental chronology processed continuously versus unload/catch-up converges on the same plant stage, field outcome and item freshness, allowing only explicitly documented stochastic-equivalence tolerance.
38. **Save/load phase:** save immediately before a due environmental transition; load resumes without resetting cadence, rerolling persisted decay state or double-running the transition.
39. **Disconnect:** disconnect/reconnect of all clients does not reset world weather, field ages, fire, plant age or perishable elapsed time under the server's configured world-running policy.
40. **Order independence from clients:** same world/seed with one versus two observing clients produces byte-equivalent durable environment state after the same canonical interval.

## 26. Acceptance mapping for #79

- [x] Environmental state variables, units and update cadence are defined — §§3–4.
- [x] Weather generation/transition and regional/time inputs are specified — §§5–8.
- [x] Field creation, intensity, propagation, interaction and decay rules are mapped — §§9–11.
- [x] Fire/smoke/gas propagation and terrain/item/creature effects are specified — §§11–13.
- [x] Scent creation/diffusion/decay interfaces are defined — §15.
- [x] Rot, growth and disease timing semantics are documented — §§13, 16–17.
- [x] Active vs unloaded/background simulation behavior is explicit — §18.
- [x] Seeded/golden parity tests cover weather transitions, fields/fire, scent and elapsed-time catch-up — §25.

## 27. Dependency contracts

This spec depends on:
- Spec 01/#66 for canonical time, one-world-clock periodic phase and deterministic scheduling;
- Spec 12/#77 for absolute positions, world-owned active regions, activation/catch-up and map mutation;
- #83 for stable registries/data loading;
- Spec 05/#70 for item identity, temperature/rot application and lifecycle;
- Spec 02/#67 for Character environmental exposure/body state;
- Spec 10/#75 for monster effects;
- Spec 20/#85 for durable world state/RNG/save barriers;
- Spec 17/#82 for data-driven condition/effect evaluation where weather/EOCs use it;
- #90 for transport/resource hardening without environmental socket coupling.

Downstream users include world generation/mapgen, vehicles, AI/pathfinding, client presentation and mod/data compatibility specs.

## 28. Investigation conclusion

No new cross-cutting architecture decision is required to complete #79. The apparent single-avatar ownership in pinned weather and scent code is directly resolved by already-established #52/#58/#77 world-owned active-region/server-authority rules: preserve the Cataclysm environmental formulas and spatial semantics, but remove the privileged-avatar ownership/scheduling assumption. Weather/scent storage and caching may be implemented differently provided the conformance scenarios above hold.

Implementation work is intentionally out of scope.



**ENV95-01 — activation at a periodic deadline:** activate a region immediately before and immediately after an environment due phase at boundary D. Per-domain frontier markers yield exactly one D occurrence and no duplicate active processing of `[D,D+1)`.
