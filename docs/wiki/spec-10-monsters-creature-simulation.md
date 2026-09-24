# Spec 10 — Monsters and creature simulation

Status: investigated / implementation-ready specification  
Tracking issue: #75  
Parent epic: #65  
Reference implementation: `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Purpose and boundary

This page specifies the monster entity, monster-definition, spawning, lifecycle and creature-specific simulation contract required for OctoGhast parity with the pinned Cataclysm:DDA baseline.

It is intentionally behavioral. OctoGhast must reproduce the pinned rules and data semantics without reproducing CDDA's C++ object graph. The authoritative simulation/server owns every monster in both single-player and co-op. Godot consumes player-specific projections and never owns monster simulation state.

This specification owns:

- monster/species/faction/group definition data;
- runtime monster state and stable identity;
- spawn-group selection and instance creation;
- monster senses and the inputs used by attitude/targeting;
- movement capability and move-cost integration;
- regular and special attack lifecycle integration;
- weakpoint/death/drop/corpse hooks;
- evolution, reproduction, revival-facing contracts and unloaded catch-up;
- active-region activation/deactivation and persistence behavior;
- deterministic/RNG requirements and player-specific projection.

Detailed reusable AI/pathfinding/target-scoring policy remains owned by #81 (Spec 16). This specification defines the monster-side state, capability, attitude and target-input contract that Spec 16 consumes.

## 2. Architectural prerequisites

This contract inherits, and does not reopen, the following completed/reviewed decisions:

- #66 / `spec-01-game-loop-time-scheduling.md`: canonical authoritative time, CDDA move budgets and deterministic simulation ordering.
- #67 / `spec-02-character-model-stats-anatomy-needs.md`: actor-generic Character semantics and no privileged-avatar assumption.
- #69 / `spec-04-action-dispatch-activity-framework.md`: command/activity/event boundaries and move-cost accounting.
- #70 and #71: stable item identity/location and authoritative transfer semantics.
- #74 / `spec-09-combat-core-damage-melee-ranged-projectiles.md`: shared damage, armor, weakpoint and attack-resolution rules.
- #77 / `spec-12-local-map-coordinates-spatial-simulation.md`: server/world-owned active regions, authoritative occupancy/spatial indexes, overlap simulated once, and player-specific visibility projection.
- #82 / `spec-17-events-talkers-eoc-runtime.md`: server-side event/EOC execution and Monster talker support.
- #83: server-authoritative immutable registries, typed definition IDs, inheritance/finalization and validation.
- #85 / `spec-20-persistence-save-load-migration.md`: server-owned world saves, stable runtime identity, one persistence owner per entity, and active-region state distinct from durable world state.
- #90 remains the owner of transport/backpressure/session mechanics. This spec only states monster-domain request/projection semantics and never depends on a socket type.

## 3. Authoritative pinned-CDDA evidence

Primary runtime evidence at the pinned baseline:

- `src/monster.h`, `src/monster.cpp` — runtime monster state, construction, movement capability, attitude, turn processing, load/unload catch-up, polymorph, upgrade/reproduction and death.
- `src/mtype.h`, `src/mtype.cpp`, `src/monstergenerator.cpp` — immutable monster/species definition model, defaults, validation, special-attack construction, upgrade/reproduction schema and finalization.
- `src/mongroup.h`, `src/mongroup.cpp` — monster-group definitions, time/season/event spawn eligibility, weighted selection, packs and subgroup recursion.
- `src/monfaction.h`, `src/monfaction.cpp` and `data/json/monster_factions.json` — monster-faction inheritance and resolved attitude matrix.
- `src/weakpoint.*`, `src/mondeath.*` — weakpoint and death/corpse/drop behavior.
- `src/savegame_json.cpp` — serialized monster runtime state and legacy migration behavior.
- `doc/JSON/MONSTERS.md` — declarative MONSTER contract and content-author documentation.
- `data/json/species.json` plus baseline monster/group content directories — species flags/triggers and representative data.
- `tests/monster_test.cpp` — movement/move-budget and monster behavior evidence.
- `tests/weakpoint_test.cpp` — weakpoint probability, damage and composition-order evidence.
- `tests/creature_test.cpp` — species relationship evidence.

Where prose documentation and executable source disagree, the pinned runtime/load code is authoritative. One concrete example is `tracking_distance`: the documentation says a default of 3, while the pinned loader supplies default **8** with a minimum accepted value of 3. OctoGhast must use 8 for omitted data at this baseline.

## 4. Immutable definitions versus mutable runtime state

### 4.1 Monster type definition

A `MonsterTypeId` identifies an immutable, registry-owned definition after content finalization. At minimum the compiled definition must be able to express the pinned fields relevant to monster behavior:

- identity, translated name/description, symbol/color/looks-like;
- species/categories, material composition, phase, body type, size/volume/weight;
- default monster faction;
- HP, base speed, aggression, morale and `aggro_character`;
- vision day/night, scent tracking/ignoring and hearing/vision/movement flags;
- tracking distance and path settings;
- movement skills/capabilities such as climbing, swimming, digging, flying/submerging;
- regular attack cost, melee skill/dice/damage, dodge, block and grab strength;
- armor, weakpoint sets/inline weakpoints and weakpoint-family metadata;
- attack effects, special defenses and ordered special-attack definitions;
- regeneration/emissions;
- death behavior, corpse type, death drops, harvest/dissection/decay;
- revival forms, zombify/fungalize transformations;
- fixed/group evolution configuration;
- reproduction configuration and season flags;
- starting ammo, mount/mech/pet-specific definition data and other capability flags.

Definitions are shared, immutable server data. A client may receive a stable definition ID and specifically projected presentation metadata, but must not receive registry internals or unrelated definitions merely because the server loaded them.

### 4.2 Important pinned defaults/validation

The pinned `mtype::load` contract includes these externally important defaults and bounds:

| Field | Pinned load behavior |
|---|---|
| material | flesh when omitted |
| color | white |
| volume | 62499 ml, minimum 0 |
| weight | 81499 g, minimum 0 |
| phase | SOLID |
| difficulty adjustment | 0, minimum 0 |
| hp | required/effective minimum 1 |
| speed | 0 when omitted, minimum 0 |
| aggression | 0, valid -100..100 |
| morale | 0 |
| tracking_distance | **8** when omitted, minimum 3 |
| mountable_weight_ratio | 0.2 |
| attack_cost | 100 moves, minimum 0 |
| melee_skill / melee_dice / melee_dice_sides | 0, non-negative |
| grab_strength | 1, non-negative |
| status_chance_multiplier | 1.0, valid 0..5 |
| bleed_rate | 100 |
| vision_day | 40 |
| vision_night | 1 |
| regenerates | 0 |
| absorb_ml_per_hp | 250 |
| split_move_cost | 200 |
| absorb_move_cost_per_ml | 0.025 |
| absorb_move_cost_min | 1 |
| absorb_move_cost_max | -1 meaning no limit |
| aggro_character | true |

The loader requires valid name/default-faction/symbol data according to its inheritance state. Content inheritance, `copy-from`, `extend`, `delete`, relative/proportional mutation and deferred typed-ID validation are governed by Spec 18/#83.

### 4.3 Species definition

Species are immutable typed definitions that can contribute:

- description;
- fear/anger/placate triggers;
- bleed field;
- footsteps;
- flags.

A monster can belong to multiple species. The baseline `same_species` predicate is true when the two monster types have **any species ID in common**.

### 4.4 Monster-faction definition

Monster factions are immutable definitions with:

- stable faction ID/name;
- optional base faction;
- explicit `friendly`, `neutral`, `by_mood` and `hate` relations.

Finalization recursively inherits parent relations; child entries overwrite inherited entries. A faction is friendly to itself by default unless explicitly overridden. Cycles in `base_faction` are diagnosed and broken during finalization. The resolved runtime lookup must be deterministic and registry-order-independent.

### 4.5 Monster-group definition

A monster group is immutable selection data containing:

- group ID and default monster;
- weighted entries, each targeting a monster type or subgroup;
- entry frequency and cost multiplier;
- pack minimum/maximum;
- spawn metadata;
- start/end time bounds;
- optional holiday/event restriction;
- condition strings including day/night/dawn/dusk and season;
- group frequency total/event-adjusted totals;
- replacement-group/time metadata and safe/animal metadata where present.

Group definitions do not own spawned monsters.

### 4.6 Runtime monster instance

A runtime monster is server-owned mutable state with a stable `CreatureId` independent of position, ECS storage slot, client object, connection or Godot node.

Required mutable state includes, as applicable:

- `CreatureId`, `MonsterTypeId`, authoritative absolute position and spatial owner;
- current HP, current/base speed and current move budget/remainder;
- current faction, friendliness/pet state, aggression, morale and `aggro_character`;
- active effects and relevant effect caches;
- goal/wander/patrol/path state needed for deterministic continuation;
- special attacks keyed by stable attack ID with `cooldown` and `enabled`;
- ammo and monster-held/mounted/tack/storage/mech items;
- upgrade enabled state and next upgrade day/time;
- reproduction enabled state and next baby timer;
- biosignature and udder timers;
- hallucination, dead, underwater and other creature lifecycle flags;
- mission associations and runtime narrative hooks;
- inventory/dissectable inventory and drop-suppression state;
- summon/lifespan state;
- last authoritative simulation time used for background catch-up;
- any RNG-relevant durable values whose omission would change deterministic continuation.

Pinned CDDA serializes many of these fields directly. OctoGhast may normalize the representation, but save/load must restore equivalent observable continuation.

## 5. Spawn-group selection

### 5.1 Pinned CDDA reference behavior

For `MonsterGroupManager::GetResultFromGroup`:

1. Evaluate the group's event-adjusted frequency total.
2. Draw `spawn_chance = rng(1, freq_total)`.
3. Iterate entries in definition order.
4. Skip entries whose spawn conditions are currently invalid. A skipped invalid entry does **not** consume weight from `spawn_chance`.
5. For each valid entry:
   - if `entry.frequency < spawn_chance`, subtract its frequency and continue;
   - otherwise select that entry and stop scanning.
6. If the selected entry has `pack_maximum > 1`, draw pack size uniformly from inclusive `[pack_minimum, pack_maximum]`; otherwise pack size is 1.
7. A subgroup recursively performs the same selection.
8. At the outermost level, if no valid entry is selected, return the group's default monster with pack size 1 and decrement a supplied quantity by one.
9. Quantity-budget calls decrement quantity according to the entry cost multiplier and pack behavior exactly as the pinned overload specifies.

Spawn eligibility:

- `calendar::turn < start_of_cataclysm + starts` => invalid.
- If not forever, `calendar::turn >= start_of_cataclysm + ends` => invalid. The end is exclusive.
- No time-of-day condition => valid at any time of day.
- DAY/NIGHT/DAWN/DUSK entries use the pinned sunrise/sunset-derived ranges and the baseline test is strict `turn > range_start && turn < range_end`.
- If one or more season conditions are present, at least one must match the current season.
- Holiday/event entries require the configured event spawn policy and matching current holiday.

### 5.2 OctoGhast adaptation

Group selection executes only on the authoritative server. It uses canonical simulation time, never a client's wall clock. A client may request an action that can cause spawning, but the request contains intent/context, not a client-selected result.

Because several active regions may overlap, a spawn opportunity attached to one authoritative world event/site is evaluated exactly once. Interest subscriptions do not create additional spawn rolls.

### 5.3 Implementation contract

Expose a deterministic service conceptually equivalent to:

`SelectMonsterGroup(groupId, SpawnContext, ref QuantityBudget, RngStream) -> ordered SpawnResult[]`

`SpawnContext` must contain canonical time/date/season/event policy and any spatial/environment inputs used by the group. The result must contain stable definition IDs and pack/spawn data, never runtime object pointers.

RNG call order is part of seeded conformance: the weighted roll occurs before pack-size selection, and subgroup recursion consumes RNG in traversal order.

## 6. Instance creation and stable identity

### 6.1 Pinned CDDA reference behavior

A newly constructed monster initializes from its type:

- current HP = type HP;
- current/base speed = type speed;
- initial moves = type speed;
- aggression/morale/faction from the type;
- each special attack receives an initial cooldown drawn uniformly from `0..evaluatedCooldown`;
- upgrade/reproduction/biosignature state is enabled from type capabilities;
- reproduction timer starts at `now + baby_timer` when eligible;
- some type capabilities perform additional spawn-time RNG/state initialization, e.g. aquatic fish population, mech battery charge and mount items.

### 6.2 Privileged-avatar baseline assumption

Pinned CDDA evaluates some special-attack cooldown expressions with a dialogue frame containing the monster and `get_avatar()`. That is a single-player host assumption, not a portable rule that one avatar must globally exist.

### 6.3 OctoGhast adaptation and contract

- Allocate a stable `CreatureId` before the entity becomes externally observable.
- Resolve definition data from the immutable registry.
- Initialize mutable state in one deterministic spawn transaction.
- Any expression requiring a talker/context must receive an explicit authoritative invocation context. It must not bind an arbitrary connected player as a global avatar.
- Where the pinned expression semantically depends only on the monster, results must be player-independent.
- Where content genuinely requires a character/talker, the spawning cause supplies that actor explicitly; absence follows the EOC/talker missing-capability contract.
- Insert the fully initialized entity atomically into authoritative occupancy/spatial indexes.
- Emit/project a spawn event only to clients entitled to observe it.

Two clients cannot independently instantiate the same spawn. If simultaneous intents trigger mutually exclusive spawn state, ordinary deterministic equal-tick command ordering decides which authoritative precondition still holds.

## 7. Time, move budget and active simulation

### 7.1 Pinned CDDA reference behavior

CDDA gives each active monster speed-derived moves and lets it repeatedly execute monster movement/decision work while it has sufficient budget. `tests/monster_test.cpp` explicitly models this by adding the monster's speed each one-second turn and calling `move()` while its move budget remains non-negative.

Regular melee uses the monster's defined `attack_cost` (default 100 moves). Terrain movement cost, swimming/climbing/digging/staggering and obstacle interaction feed the same move-budget economy.

Per active global turn `monster::process_turn` includes observable state changes such as:

- reset available blocks;
- periodic field emission phase-locked through `calendar::once_every`;
- decrement each enabled special-attack cooldown by one turn, never below zero;
- maintain/remove grab effects;
- process monster-specific environmental effects;
- run shared `Creature::process_turn`.

### 7.2 OctoGhast adaptation

The server advances continuously on Spec 01's canonical tick clock; no player's input gates monster progression. Preserve CDDA moves as simulation currency.

Using the Cataclysm-profile configuration of 10 ticks per world second (not a universal Core rate), speed/move accrual is scheduled deterministically from canonical elapsed simulation time. Do not reinterpret `attack_cost=100` as “100 wall-clock milliseconds.”

A monster in an active region is scheduled once even if several players' interest regions overlap it.

### 7.3 Implementation contract

The monster scheduler must:

- accrue the same move budget over one world-second that the pinned type speed would accrue;
- execute an action only through the authoritative simulation phase/order defined by Spec 01;
- preserve negative/remaining move debt across ticks;
- process once-per-CDDA-turn behavior exactly once per authoritative world second, phase-locked to canonical time;
- make all environmental/combat mutations atomic at the simulation phase boundary;
- never advance because a client renders, polls, opens a UI or sends an unrelated request.

## 8. Senses, capability and target inputs

### 8.1 Vision

Pinned `monster::sight_range(light_level)`:

- if it cannot see, vision is impaired, or it is an unsuitable non-aquatic submerged creature: range = 1;
- at zero light: type `vision_night`, after enchantment modifiers;
- at or above default daylight: type `vision_day`, after modifiers;
- otherwise linearly interpolate:
  `range = (light * vision_day + (defaultDaylight - light) * vision_night) / defaultDaylight`
  using the baseline integer result, then apply modifiers.

Actual perception still depends on LOS/transparency/light interfaces from Spec 12. Hearing/smell capability flags and scent tracked/ignored definitions are monster inputs to environment/AI systems, not client-side hints.

### 8.2 Movement capability

Monster type flags and move-skill data determine whether an instance can fly, climb, dig, swim/submerge, avoid configured traps/danger and traverse a candidate tile. Occupancy and terrain mutation remain authoritative map operations.

The client may animate a path but cannot decide passability or final coordinates.

### 8.3 Attitude and faction inputs

Pinned monster-to-monster attitude combines:

- player-friendly state;
- resolved monster-faction relation;
- current aggression/morale;
- special effects/flags.

For unaligned monsters, a faction HATE relation yields hostile; NEUTRAL or low mood can yield neutral; otherwise hostile. Friendly monsters are friendly to one another.

Monster-to-Character attitude additionally consumes the **specific Character being evaluated**: friendliness, NPC/player state, traits/mutations, character faction and monster species/faction effects can change effective anger/morale and the resulting FRIEND/PASSIVE/FLEE/IGNORE/FOLLOW/ATTACK state.

### 8.4 Multiplayer correction

There is no single “the player” target. Every player-controlled Character is just an authoritative Character candidate.

Any target/attitude query must therefore be:

`GetAttitude(monsterId, targetCreatureId, authoritativeContext)`

and cannot read a global avatar. Different Characters may legitimately receive different attitudes from the same monster because their traits/factions/effects differ.

Detailed candidate enumeration, target scoring, threat priority, pursuit/flee path choice and tie-breaking are deferred to #81. This spec requires #81 to consume explicit per-target attitude/perception/capability inputs and stable IDs.

## 9. Special attacks, defenses and weakpoints

### 9.1 Definition contract

Pinned special attacks are ordered definition data keyed by attack ID. The loader supports:

- registered/hardcoded `monster_attack`;
- `leap`;
- `melee`;
- `bite`;
- `gun`;
- `spell`;
- `polymorph_special`;
- `eoc`.

Every attack actor requires a cooldown expression. Invalid/unknown attack types fail content loading. Duplicate/override/inheritance semantics follow Spec 18.

### 9.2 Runtime contract

Each instance holds independent `{ cooldown, enabled }` state per special attack.

- Available means the definition exists, the instance entry is enabled and cooldown == 0.
- Active per-turn processing decrements positive enabled cooldowns by one CDDA turn.
- A normal reset evaluates the configured cooldown.
- The RNG reset variant draws `rng(0, evaluatedCooldown)`.
- Save/load persists cooldown and enabled state.
- When loading older/incomplete saves, a definition attack missing from instance state is initialized according to the pinned compatibility rule.

OctoGhast must not store cooldown only in the immutable type.

### 9.3 Weakpoints

Weakpoint sets are definition data composed in source order; later matching set entries override earlier ones and inline weakpoints override matching set weakpoints. `tests/weakpoint_test.cpp` is normative statistical evidence for coverage and damage effects.

Actual hit selection, armor interaction and weakpoint damage pipeline are owned by Spec 09. Monster instances provide their finalized weakpoint definition/family inputs to that combat pipeline.

### 9.4 EOC and event integration

EOC special attacks and death EOCs execute server-side under Spec 17 with explicit monster/target talkers. Resulting state mutation is authoritative. Messages and audiovisual effects are projected only to clients whose audience/visibility contract allows them.

## 10. Damage, death, drops and corpses

### 10.1 Death transition

Pinned `monster::die` is idempotent: if already dead, it returns without repeating death effects.

The observable death pipeline includes:

1. release/dismount riding relationships as applicable;
2. mark the monster dead and record killer;
3. emit character-kills-monster event/XP where eligible; revived-marked monsters grant zero kill XP;
4. resolve grab cleanup;
5. if a QUEEN, mark relevant nearby same-group overmap groups dying;
6. notify mission death hooks;
7. hallucinations disappear without ordinary corpse/drop processing;
8. emit death message/effect;
9. run configured death spell/EOC in its defined phase;
10. apply overkill modifier where relevant;
11. determine whether corpse creation is suppressed (including the pinned submerged aquatic case);
12. execute configured corpse type: NORMAL, BROKEN, SPLATTER or NO_CORPSE;
13. continue configured death-drop/harvest-facing behavior according to the death implementation.

The exact combat damage that reaches zero HP is defined by Spec 09; this section owns the one-shot monster lifecycle transition and its side effects.

### 10.2 Concurrency contract

At the authoritative simulation layer, only the first atomic transition from alive to dead may execute death hooks. Simultaneous attacks resolved later in deterministic command/simulation order observe the already-dead state and cannot duplicate XP, EOCs, drops or corpses.

### 10.3 Corpse identity and revival linkage

A corpse is an item/world entity owned by the item/map contracts, not a lingering monster object. It must carry the stable monster-type/lifecycle metadata required by corpse, harvest and revival rules.

Pinned CDDA's runtime references to monsters sometimes use location as a legacy surrogate identity; `savegame_json.cpp` explicitly contains a TODO noting the absence of unique monster IDs. OctoGhast must **not** copy that limitation: durable references use `CreatureId`, while a corpse created by death is a new item identity linked by explicit origin/type metadata rather than object address.

## 11. Evolution, polymorph and reproduction

### 11.1 Polymorph/type transition

Pinned `monster::poly(newType)`:

- computes current HP fraction using old max HP;
- optionally generates inventory/death-drop-related state first;
- replaces the type;
- sets moves to zero;
- replaces base speed, aggression, morale and faction from the new type;
- sets new HP to integer `oldHpFraction * newMaxHp`;
- rebuilds special attacks and initializes their cooldown to the evaluated configured value (not the random spawn-time 0..cooldown roll);
- refreshes upgrade/reproduction/biosignature/aggro flags.

**Implementation contract:** the runtime `CreatureId` survives polymorph. Type transition is mutation of one authoritative entity, not delete+spawn, unless a specific pinned rule explicitly despawns/dies and creates other entities.

### 11.2 Evolution timing

Pinned evolution:

- `age_grow > 0` yields that many days to the next age growth.
- Otherwise `half_life` is scaled by `EVOLUTION_INVERSE_MULTIPLIER`.
- The stochastic next-upgrade delay begins with one guaranteed day. Repeatedly, up to the implementation limit:
  - `one_in(2)`: add uniform `rng(0, scaledHalfLife)` and return;
  - otherwise add one scaled half-life and continue.
- Failure to obtain a terminal draw within the guard disables future upgrades.
- A newly initialized upgrade day is pinned either relative to today or to the cataclysm start depending on the call/type rule.
- When catch-up finds several upgrades already due, it loops through due transitions until the resulting type is not yet due or cannot upgrade.
- Fixed `into` and weighted `into_group` are mutually exclusive in data.
- Group evolution may create additional monsters when multiple-spawn is configured.
- An upgrade resolving to null either despawns or dies according to `despawn_when_null`.

### 11.3 Reproduction

Pinned reproduction definition:

- optional `baby_count`;
- `baby_timer` as integer days or typed duration;
- one `baby_type`: monster, monster group, egg, or egg group;
- declaring more than one baby type is diagnosed;
- separate season `baby_flags`.

Runtime catch-up:

1. Return if reproduction is disabled or no timer definition exists.
2. Draw the invocation's female roll once with `one_in(2)`.
3. While `baby_timer <= current time`:
   - evaluate season against the scheduled baby time;
   - advance a catch-up chance sequence beginning at 1, then 3, 5, ...;
   - if season, female and `one_in(chance)` succeed, draw offspring count `rng(1, baby_count)` and produce the configured child/egg output;
   - increment the baby timer by the configured period even when no offspring are produced.

Do not silently “improve” this by inventing a permanent biological-sex field: that would change the pinned RNG semantics. If later product design wants a different model, it needs an explicit parity deviation.

## 12. Unload, background catch-up and active regions

### 12.1 Pinned CDDA reference behavior

When an active monster unloads, CDDA records `last_updated = calendar::turn`.

On load it first performs upgrade/reproduction/biosignature/refill hooks, then calculates `dt = currentTurn - last_updated`. For positive elapsed time it performs compressed catch-up including:

- anger/morale relaxation toward type baseline;
- background healing;
- recovery of reduced base speed proportional to healing;
- special-attack cooldown reduction by elapsed turns.

Pinned background healing when explicit `regenerates <= 0` includes:

- non-dormant REVIVES monsters: `0.02 * maxHP / turnsPerHour` per turn;
- flesh/insect-flesh/vegetable-material living creatures: `0.005 * maxHP / turnsPerHour` per turn;
- final elapsed heal uses `roll_remainder(regen * elapsedTurns)`.

Anger/morale catch-up first moves values outside a 15-point band toward baseline at one point per four elapsed turns, then consumes the remaining elapsed budget at approximately one point per eight turns, with the pinned floor/ceil asymmetry. `aggro_character` may probabilistically reset for types that are not intrinsically character-aggressive.

### 12.2 OctoGhast active-region adaptation

CDDA performs this around one local reality bubble. OctoGhast uses #77's world-owned union of active regions:

- entering/leaving one player's interest must not duplicate/despawn an entity if another active region still contains it;
- overlapping active regions simulate the monster exactly once;
- deactivation records the canonical simulation time for the persistent monster/background record;
- later activation applies one deterministic catch-up from the last simulated time to current canonical time;
- projection subscription/unsubscription is not monster creation/destruction;
- inactive/background ownership is server/world persistence, never a disconnected client's responsibility.

### 12.3 Background representation

The implementation may compact inactive monsters into region records rather than keeping full active ECS components, provided:

- stable `CreatureId` and type/state required for exact continuation survive;
- elapsed-time lifecycle rules produce parity-compatible outcomes;
- no entity exists simultaneously as two authoritative active/background copies;
- activation is an atomic ownership transition;
- RNG consumed during catch-up is deterministic and saved as required.

## 13. Revival lifecycle

Pinned content can mark corpses/types as revivable and can declare `revive_forms` with conditional alternate monster types/groups. Death/revival also interacts with the revived marker, which prevents repeated kill XP.

Implementation contract:

1. Death creates/suppresses the corpse according to the pinned death rules.
2. A revivable corpse retains the type/revival metadata required to evaluate the pinned revival rule later.
3. Revival eligibility is evaluated against canonical authoritative time and explicit world/talker context.
4. A successful revival is an authoritative spawn transaction at the corpse/world location, subject to current occupancy and map validity rules.
5. Consume/transform the corpse exactly once; allocate a new runtime `CreatureId` for the revived creature.
6. Apply the revived marker/state required to suppress kill XP on its next death.
7. Conditional `revive_forms` resolve through finalized typed IDs/groups and consume server RNG in deterministic order.
8. If the location is inactive, revival timing/state belongs to the authoritative background scheduler/region record rather than a client timer.

Detailed item rot/corpse timing comes from Spec 05 and environment rules from Spec 14; this spec owns the monster creation/result side.

## 14. Persistence

Monster persistence follows Spec 20's world-owned save-set model.

Persist enough state to resume without observable reset, including:

- stable `CreatureId`;
- type/faction IDs and absolute authoritative position;
- HP/speed/moves/effects and life/dead state as required;
- aggression, morale, friendliness and aggro flags;
- target/goal/patrol/path state if still valid for continuation;
- special attack cooldown/enabled map;
- ammo and nested owned items by the item persistence contract;
- upgrade/reproduction/biosignature/udder timers and enable flags;
- summon/lifespan and last-updated canonical time;
- mission/narrative references;
- mount/drag/grab relationships through stable IDs, never memory pointers or Godot IDs;
- deterministic RNG state/stream position where the world RNG strategy requires it.

Pinned save files use absolute position for at least one serialized weak monster reference because the C++ baseline lacks stable monster IDs. This is evidence of upstream representation, **not** an OctoGhast contract. Spec 20's stable runtime-reference rule supersedes that implementation limitation.

Load ordering:

1. content registries/factions/types finalized;
2. persistent regions/entities materialized with stable IDs;
3. item/entity relationship fixups resolved;
4. spatial occupancy inserted;
5. active-region activation/catch-up applied exactly once;
6. client projections emitted only after authoritative state is valid.

Missing required type IDs are load errors/migration cases, not silent replacement with an arbitrary monster. Invalid saved faction follows the domain migration/diagnostic policy; baseline falls back to factionless with a warning.

## 15. RNG and determinism

Monster simulation is RNG-heavy. Determinism is required at the authoritative server boundary, not by allowing clients to reproduce hidden RNG.

Seeded or stream-controlled conformance must cover at minimum:

- monster-group weighted selection;
- pack-size selection and subgroup recursion;
- spawn-time special cooldown;
- mech/fish/other spawn random initialization where behaviorally relevant;
- movement stochasticity such as stumbling/shambling;
- attack/weakpoint/combat rolls through Spec 09;
- special-attack RNG;
- evolution next-upgrade sequence and group result;
- reproduction sex/chance/count/group rolls;
- unload/load `roll_remainder` healing and aggro reset;
- revival form/group selection.

Rules:

- RNG is consumed only by authoritative simulation code.
- Equal starting snapshot + content + canonical inputs + deterministic request ordering + RNG state must reproduce the same authoritative state transition sequence.
- Iteration over unordered ECS/storage collections must not accidentally define RNG order. Use stable simulation ordering (e.g. stable entity ID/phase-defined order) where multiple monsters are processed in one phase.
- Saving must preserve whatever RNG continuation state the chosen world/stream strategy requires.
- Statistical tests are appropriate where the baseline itself specifies a distribution rather than a particular seeded draw, but seeded golden tests should additionally pin call ordering for OctoGhast.

## 16. Multiplayer authority, contention and projection

### 16.1 Authority

Clients submit intents such as attack/interact/target requests. They never directly set monster HP, faction, position, cooldown, death, spawn, evolution or target state.

Autonomous monster decisions are server simulation work, not client commands.

### 16.2 Contention examples

- Two players attack the same low-HP monster in one canonical tick: deterministic authoritative ordering permits one death transition only.
- Two actors attempt to occupy/block the same tile with a monster: Spec 12 atomic occupancy decides the first valid mutation; later action replans/fails against the new state.
- Two clients interact with the same pet/mount/corpse: validate authoritative version/location/ownership when the command resolves, not when the UI opened.
- A client targets a monster that has evolved, moved, died or left visibility: the command uses stable `CreatureId` plus current authoritative validation; stale requests reject/fail without mutating a replacement entity.

### 16.3 Projection

A client receives only the minimum monster state it is entitled to perceive/know, for example:

- stable ephemeral/protocol entity reference mapped to authoritative `CreatureId` as policy allows;
- visible position/type/presentation identity;
- observable attitude/injury/status cues;
- server-approved animations/events/messages.

Do not replicate hidden HP exact values, hidden target/path plans, cooldowns, AI internals, inventory, off-screen entities, or unrelated region state unless a gameplay/debug permission specifically exposes them.

Visibility is per player. One player's LOS does not authorize another player's projection.

Disconnecting a player does not pause, unload or transfer authority over monsters except insofar as server active-region policy changes because no player interests that region; any such deactivation follows the ordinary background contract.

## 17. Dependency and service contracts

The monster system requires these interfaces/concepts, without prescribing concrete C# class layout:

- `IMonsterTypeRegistry` / typed content registry from Spec 18.
- `IMonsterGroupSelector` consuming canonical `SpawnContext` and authoritative RNG.
- `ICreatureIdentityAllocator` for stable `CreatureId`.
- `ISpatialWorld` / occupancy and coordinate services from Spec 12.
- `ISimulationClock` and move-budget scheduler from Spec 01/#57.
- `ICombatResolver` from Spec 09.
- `IEffectRuntime` / Creature effect model.
- `IEventBus` and `IEocRuntime` from Spec 17.
- item/corpse/inventory services from Specs 05/06.
- persistence/background-region service from Spec 20.
- environment/light/field/scent service, with detailed autonomous rules in Spec 14.
- `IMonsterDecisionPolicy` / pathfinding contract supplied by completed Spec 16 / #81.
- player-specific projection/interest service from #77/#90 architecture.

Cross-system calls must pass stable IDs/value objects and explicit contexts, not direct client/UI objects.

## 18. Validation and failure behavior

Content/load validation must diagnose or reject:

- invalid/missing typed IDs after finalization;
- monster-faction parent cycles and unresolved faction relations;
- invalid special attack type/format or missing required cooldown;
- invalid numeric bounds from the pinned loader;
- simultaneous `upgrades.into` and `upgrades.into_group`;
- reproduction with multiple mutually exclusive baby types;
- invalid weakpoint/family references;
- recursive/invalid monster groups according to registry validation;
- invalid saved required monster type/reference that cannot be migrated.

Runtime failure behavior:

- failed movement leaves authoritative position unchanged and consumes cost only where the pinned attempted action does so;
- stale target/entity references fail against current stable identity, never retarget by position;
- duplicate death is a no-op for death side effects;
- invalid spawn position/group output is rejected/handled by the owning spawn caller without partial entity insertion;
- a failed persistence fixup cannot leave a duplicate half-active entity;
- client disconnect cannot roll back already committed monster state.

## 19. Black-box and parity scenarios

These scenarios are the minimum later automated conformance suite. Tests should use the pinned data/test fixtures where practical and record the exact baseline commit.

### M10-01 — Definition/runtime separation

Load one MONSTER definition, spawn two instances, mutate HP/aggression/special cooldown on one, and assert the other instance and immutable type remain unchanged. Save/load both and verify independent continuation.

### M10-02 — Loader defaults and validation

Load a minimal inherited monster fixture and verify pinned defaults including attack cost 100, aggression 0, morale 0, tracking distance **8**, vision 40/1 and grab strength 1. Verify invalid aggression, invalid unknown special attack, both evolution targets, and multiple reproduction baby types produce the expected load diagnostic/failure class.

### M10-03 — Species overlap

Using fixture types sharing exactly one species ID, assert `same_species` is true; assert false for disjoint species. Reproduce the intent of `tests/creature_test.cpp`.

### M10-04 — Faction inheritance

Create parent/child factions with inherited and child-overridden relations. Finalize and assert the resolved friendly/neutral/by-mood/hate matrix. Add a parent cycle and assert deterministic diagnostic/cycle break behavior.

### M10-05 — Weighted spawn group

With a fixed RNG stream, verify weighted selection scans valid entries in definition order, then pack selection. Repeat with the selected entry made time-invalid and prove its weight is skipped rather than subtracted.

### M10-06 — Spawn boundary times

For starts/ends and DAY/DUSK/DAWN/NIGHT fixtures, test just-before, exact-boundary and just-after values. Assert start is inclusive under the baseline comparison, end is exclusive, and time-of-day range endpoints are excluded by the strict baseline comparison.

### M10-07 — Group default fallback

Make all entries invalid and assert outer selection returns exactly the group default with pack size 1 and applies the pinned quantity fallback.

### M10-08 — Spawn initialization

Spawn a known monster with seeded RNG. Assert HP/speed/moves/aggression/morale/faction initialize from type and each special cooldown is inside inclusive `0..configuredCooldown`. Assert two instances have independent cooldown state.

### M10-09 — Continuous-time move parity

Run one canonical world second for speed-100 and representative non-100 monsters. Assert move accrual/action count matches the pinned 100-moves-per-second economy. Repeat over many seconds and compare total movement cost to the corresponding `tests/monster_test.cpp` fixture/tolerance.

### M10-10 — Active overlap exactly once

Put one monster in the overlap of two player active regions. Advance N canonical seconds. Assert cooldown, regeneration/emission cadence and move accrual advance N times, not 2N. Remove one player and assert no activation/deactivation occurs while the second still keeps the region active.

### M10-11 — Vision interpolation

For a monster with known day/night ranges, test zero light, full daylight, midpoint light and vision-impaired/submerged cases. Assert the pinned interpolation and minimum impaired range behavior before LOS filtering.

### M10-12 — Per-character attitude isolation

Place two simultaneous player-controlled Characters with different relevant traits/factions near the same monster. Evaluate attitude to each specific target and assert one target's modifiers do not leak into the other's result. No global-avatar lookup may decide both.

### M10-13 — Atomic movement contention

Schedule two authoritative creatures for the same destination tile at the same simulation phase. Assert deterministic ordering, one occupancy winner and no duplicated/overlapping authoritative occupancy.

### M10-14 — Special cooldown lifecycle

Initialize a special with known cooldown, advance active canonical seconds and assert decrement to zero without going negative. Disable it and assert no decrement/use. Save/reload and assert exact cooldown/enabled state survives. Deactivate the region for elapsed time and assert background catch-up reaches the same zero point.

### M10-15 — Weakpoint composition/statistics

Port the representative `tests/weakpoint_test.cpp` fixtures: later sets/inline weakpoints override by ID; repeated attacks meet the baseline coverage/damage distributions within statistical tolerance.

### M10-16 — Single death under simultaneous attacks

Two players deliver lethal attacks in deterministic same-tick order. Assert one alive->dead transition, one kill/death event sequence, one corpse/drop set, and no duplicate death EOC/XP.

### M10-17 — Hallucination death

Kill a hallucination. Assert it disappears and ordinary corpse/drop processing does not occur.

### M10-18 — Corpse suppression

Kill an eligible aquatic/swimming monster underwater on a SWIM_UNDER tile and assert corpse suppression matches pinned behavior; compare with the same monster on ordinary terrain.

### M10-19 — Polymorph continuity

Polymorph a damaged monster into a different-max-HP type. Assert stable `CreatureId`, pinned integer HP-ratio transfer, moves reset, new base speed/faction/aggression/morale, and special cooldowns reset to evaluated configured values.

### M10-20 — Seeded evolution

Using fixed evolution settings and RNG, verify next-upgrade calculation, fixed/group transformation, catch-up across multiple overdue stages and null-target death/despawn policy. Assert stable ID survives ordinary type transitions.

### M10-21 — Reproduction catch-up

Set a baby timer several periods in the past and fixed RNG. Assert one female roll for the invocation, chance denominators 1/3/5/... across overdue cycles, season evaluated at each scheduled baby time, timer always advances, and spawned children/eggs use configured type/group.

### M10-22 — Unloaded catch-up equivalence

Deactivate a monster for a known elapsed interval. On activation assert:
- cooldown reduced by elapsed turns;
- anger/morale moved toward baseline using the pinned 4-turn then 8-turn rules;
- healing uses the correct background rate/roll;
- upgrade/reproduction catch-up executes before ordinary active work;
- `last_updated` becomes current canonical time.

### M10-23 — Separated active regions

Two players occupy distant regions containing different monsters. Advance the server. Assert both active sets simulate under one clock with no privileged region/avatar and each client receives only its own visible projections.

### M10-24 — Disconnect/reconnect

Disconnect one player while another keeps the world running. A monster in the disconnected player's now-inactive region transitions to background once. Later reconnect/reactivate and assert one catch-up, same stable creature identity and no duplicate spawn. If another player kept the region active, assert the monster never deactivated.

### M10-25 — Save/load round trip

Create a monster with non-default HP, anger/morale, effect, special cooldown, upgrade/baby timers, inventory and target/goal. Save at a barrier, reload and assert stable identity and observable continuation. Transport/socket/client interpolation state must be absent.

### M10-26 — Stale client reference

Client observes monster A at tile X. A moves/dies and monster B later occupies X. Client sends an action referencing A. Assert the server does not retarget B by coordinate; it rejects/fails based on stable identity/current validity.

### M10-27 — EOC/death audience isolation

Trigger a monster special/death EOC producing state and messages. Assert effects execute once on the server, and only clients satisfying visibility/audience rules receive corresponding projections/messages.

### M10-28 — Revive cycle

Kill a revivable monster, persist/reload the corpse, advance to a valid revival, and assert one authoritative revived spawn with a new `CreatureId`, correct type/form selection and revived marker. Kill it and assert zero repeated kill XP according to pinned behavior.

## 20. Implementation slicing guidance

A safe later implementation sequence is:

1. typed immutable monster/species/faction definitions and validation;
2. stable runtime Monster entity + persistence DTO;
3. group selection and spawn transaction;
4. move-budget/senses/capability integration;
5. regular combat/weakpoint/death/corpse integration;
6. special attacks/EOC hooks;
7. evolution/reproduction/revival;
8. active/background transition and elapsed-time catch-up;
9. player-specific projection;
10. Spec 16 autonomous AI/pathfinding on top of these contracts.

No slice should introduce client-owned monster state or a special single-player simulation path.

## 21. Resolved architecture assessment

This investigation found no new unresolved cross-cutting architectural decision.

The principal upstream mismatches are already covered by established OctoGhast architecture:

- CDDA's one-avatar dialogue/attitude assumptions become explicit target/talker contexts.
- CDDA's one reality-bubble load/unload lifecycle becomes server-owned active-region/background ownership.
- CDDA's lack of stable monster IDs is replaced by Spec 20's stable runtime identity.
- turn-gated monster scheduling becomes continuous canonical scheduling while retaining CDDA move and one-second periodic semantics.
- local UI visibility calls become player-specific projection/audience checks.

These are adaptations of scheduling/ownership/context, not changes to the underlying pinned monster rules.

## 22. Acceptance-criteria traceability

#75 criterion | Covered by
---|---
Monster data schemas/runtime state separated | §§4, 14, M10-01/02
Spawn-group selection/conditions/RNG | §5, §15, M10-05/06/07
Senses/movement/target inputs | §§7-8, M10-09/11/12/13
Special attacks/weakpoints/death/drop contracts | §§9-10, M10-14/15/16/17/18/27
Evolution/revival lifecycle | §§11, 13, M10-19/20/21/28
Map load/unload/persistence | §§12, 14, M10-10/22/23/24/25
Combat/faction/EOC/event/environment dependencies | §§2, 8-10, 13, 17
Parity scenarios for spawn/pursuit/attacks/death/drop/revival/save-load | §19; detailed autonomous pursuit policy remains deliberately owned by #81, while this spec pins the monster-side pursuit inputs and movement economy required by it

## 23. Post-spec Core/profile classification

The pinned evidence and all M10 scenarios remain mandatory for the Cataclysm reference profile. Reference parity is a foundation/completeness benchmark, not the final limit of creature design.

- **Reference behaviour / Cataclysm policy:** monster/species/faction/group schemas, HP and morale/anger fields, senses/flags, 100-move costs, one-second cooldown processing, special-attack maps, weighted spawn rules, evolution/reproduction formulas and corpse/revival/XP rules belong to Cataclysm. In particular the reproduction invocation roll is preserved; generality does not authorize replacing it with a new biological model.
- **Core contract:** stable runtime identity, immutable definition references, authoritative lifecycle/mutation, deterministic scheduler/RNG access, spatial indexing, active/background ownership, save continuation and explicit projection. Core need not define a Monster subtype, CDDA size taxonomy, species flags or a monster-only scheduler.
- **Intentional adaptations:** explicit invocation actors replace a global avatar; stable CreatureId replaces position-based durable identity; one server simulates separated/overlapping regions and filters projections per viewer.
- **Evolution seams:** another profile may use different actor capabilities, growth/reproduction, sensing, geometry or cooldown currency while reusing Core. Polymorph identity retention and new identity on Cataclysm revival remain the profile's concrete rules.

**M10-AUD-01:** instantiate a non-Cataclysm creature fixture with different capabilities, cadence and deterministic non-grid position; exercise spawn/move/save/load/despawn through the same Core contracts without registering Cataclysm monster fields. Separately retain M10's pinned spawn/cooldown/reproduction/death/revival fixtures unchanged.

Global scheduling/activation integration follows [#95](https://github.com/LambdaSix/OctoGhast/issues/95); this does not reopen #75's completed evidence.
