# Spec 02 — Character model, stats, anatomy and needs

Status: investigated / specification complete; real-time/co-op architecture review complete  
Tracking issue: #67  
Parent epic: #65  
Reference implementation: `LambdaSix/Cataclysm-DDA` @ `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Purpose

This page defines the behavioral contract OctoGhast needs for the Cataclysm reference profile's Character/avatar state and physiological simulation. It specifies observable state, invariants, cadence, composition rules and conformance boundaries; it does not require reproducing CDDA's C++ class layout and it does not make CDDA-specific physiology a permanent Core platform invariant.

The reusable Character domain owns actor physiology and derived capability for the Cataclysm profile. Avatar-only responsibilities are presentation/input/session concerns layered on top. NPCs may use the same Character state while policy exceptions (for example optional NPC food needs) remain explicit.

This specification must be read through four layers established by #52/#57/#58/#64/#65:

1. **Pinned CDDA reference behaviour** — the formulas, data semantics, thresholds, cadence and state transitions evidenced at the pinned commit.
2. **OctoGhast Cataclysm profile** — the rules/profile implementation that reproduces that behaviour under OctoGhast's authoritative real-time/co-op architecture.
3. **Generic Core contract** — ruleset-agnostic capabilities for authoritative actors, stable identity, deterministic elapsed-time scheduling, state mutation/query, persistence hooks and projection boundaries.
4. **Future evolution seams** — anatomy shape, need/resource sets, cadence rates, constants, modifier graphs and action-currency mappings remain profile policy unless independently promoted to a genuinely reusable Core concept.

## Authoritative evidence

Primary runtime anchors at the pinned commit:

- `src/character.h`, `src/character.cpp`: Character state/API, stats, modifiers, movement-facing capability, effects and general behavior.
- `src/character_health.cpp`: damage/HP, death, healing, stamina, needs, sleep, health, vitamins and physiological scheduling.
- `src/character_body.cpp`: body temperature, wetness/drench, clothing/body interactions and body-part environmental processing.
- `src/bodypart.h`, `src/bodypart.cpp`: body-part definitions/runtime state, limb scores, wounds, encumbrance, HP, temperature and serialization.
- `src/stomach.*`: stomach/guts contents, digestion and calorie/water transfer.
- `src/effect.*`, `src/morale.*`, `src/addiction.*`, `src/vitamin.*`, `src/move_mode.*`: composable status domains consumed by Character.
- `data/json/body_parts.json`, `data/json/bodypart_graphs/`: anatomy/body-part definitions.
- `data/json/character_modifiers.json`: named derived modifiers.
- Mutation/trait, effect, vitamin, addiction and movement-mode JSON are behavioral inputs and must be consumed through the shared data/registry contract from Spec 18.
- Persistence must use the stable-ID/save rules from Spec 20.

Behavioral test anchors:

- `tests/char_healing_test.cpp`
- `tests/char_stamina_test.cpp`
- `tests/character_resources_test.cpp`
- `tests/encumbrance_test.cpp`
- `tests/health_test.cpp`
- `tests/temperature_test.cpp`
- `tests/morale_test.cpp`
- `tests/effect_test.cpp`
- `tests/addiction_test.cpp`
- `tests/character_modifier_test.cpp`

These tests are normative evidence where prose/docs disagree with runtime behavior.

## 1. Domain boundary

### 1.1 Character

A Character is a persistent creature with:

- base primary stats: strength, dexterity, intelligence and perception;
- current/derived stat modifiers;
- an anatomy instance composed from stable body-part IDs;
- per-body-part mutable HP, wounds, wetness, current/convergent temperature and treatment state;
- global resources: stamina, oxygen, pain/painkiller state, stimulation, radiation, sleepiness/fatigue, sleep deprivation, hunger/thirst presentation state, stored energy/calories and hydration/digestion state;
- lifestyle/health and cardio state;
- vitamin stores and daily intake accounting;
- effects/statuses, addictions and morale;
- movement/posture mode and capability derived from limbs, effects, equipment and environment;
- trait/mutation/bionic/enchantment inputs that modify the above;
- persistent activity/sleep-related physiological bookkeeping needed for deterministic elapsed-time updates.

### 1.2 Avatar-only layer

The avatar layer may own player-only interaction state, messages, interruption prompts, UI preferences, character-creation/session state and player-specific daily bookkeeping. Core formulas must not require UI access. Any current CDDA function that emits messages while changing physiology must be decomposed in OctoGhast into domain state transition + optional presentation event.

There is no privileged runtime avatar in the reusable Character domain. Player control is an external ownership/session relationship: zero, one, or several Characters may be player-controlled at the same canonical simulation time, and the same Character physiology/capability APIs apply to player-controlled Characters and NPC Characters. Connection/socket identity, client UI state, camera/FOV state and input queues are not Character state.

### 1.3 NPC policy

NPCs use the Character physiological model unless an explicit policy disables or simplifies a need. CDDA's `NO_NPC_FOOD` behavior is a policy input, not a reason to fork the domain model. Stasis actors do not accumulate normal needs.

## 2. Anatomy and body-part model

An anatomy is a graph/set of body-part definitions identified by stable typed IDs. A body-part definition includes, where present:

- identity, legacy identity, main part, connected part and opposite part;
- side and limb-type weights;
- sub-body-parts used for coverage/encumbrance;
- base HP and stat/health contributions to maximum HP;
- vital flag;
- hit size/difficulty and damage/protection metadata;
- limb scores and whether wounds/encumbrance affect them;
- encumbrance thresholds/limits and BMI encumbrance contribution;
- innate environmental protection, temperature offsets, drench capacity/increment/drying rate;
- healing/mending modifiers;
- pain multiplier;
- qualities, conditional flags, techniques and on-hit effects.

Runtime body-part state contains at minimum:

`id, hp_current, hp_max, wounds, wetness, temperature_current, temperature_convergent, frostbite_timer, healed_total, bandaged_damage, disinfected_damage`.

Encumbrance caches are derived and may be recomputed rather than serialized if behavior is identical.

### Invariants

1. Every runtime part references a valid body-part definition.
2. Main/connected/opposite references resolve after registry finalization.
3. `hp_max >= 1`; normal HP mutations clamp current HP to `[0, hp_max]`.
4. Recalculation of max HP preserves the current/max ratio (CDDA uses ceiling after multiplying by the new/old max ratio), then clamps.
5. A Character is dead when any vital main body part has current HP <= 0. A body with no vital parts is an invalid/diagnostic configuration rather than a normal mortal anatomy.
6. Non-vital parts at zero HP can disable limb contribution without themselves implying death.
7. Body-part IDs, not collection indexes, are the persistence and cross-system identity.

## 3. Primary and derived stats

Primary stats are integer base values STR/DEX/INT/PER plus temporary/modifier layers. Consumers must request the effective value rather than directly assuming the base value.

Maximum body-part HP is data-driven. At the reference baseline, Character recalculation computes each part from its body-part definition:

`raw = base_hp + STR*str_mod + DEX*dex_mod + INT*int_mod + PER*per_mod + lifestyle*health_mod + fat_to_hp + additive_max_hp`

then applies multiplicative max-HP modifiers. The Glass Jaw trait additionally scales head HP by 0.8. The final maximum is at least 1.

Derived capabilities must be modeled as named queries/modifiers rather than copied formulas at call sites. Examples include limb scores (grip, manipulation, balance, breathing, vision, reaction, lift), movement cost, stamina recovery, aim modifiers and carrying capability.

Modifier ordering is part of parity. OctoGhast must distinguish base values, additive changes, multiplicative changes, body-part wound/encumbrance adjustment and final clamping. Where a named `character_modifier` exists, that modifier is the contract boundary.

## 4. HP, damage, wounds, pain, healing and death

Combat owns attack resolution and damage production; Character owns application to body state and resulting incapacity/death.

### HP/death

- Damage is applied to a body-part ID.
- Vital main-part HP <= 0 produces dead state.
- HP changes crossing zero invalidate cached death state.
- Max-HP recalculation rescales current HP proportionally rather than granting/removing arbitrary absolute HP.

### Wounds

Wounds are body-part-local typed state. Body-part definitions can specify candidate wound types, damage requirements and on-hit effects. Wound progression/removal must be independently serializable and updateable.

### Pain

Pain is a global perceived resource with body-part-sensitive sources and trait/effect modifiers. Pain can interrupt player activity and sufficiently large pain bursts can wake sleeping actors; sleep traits/bionics modify that wake threshold. Pain immunity/numbness is capability/flag driven.

### Natural healing

Natural healing is a rate per turn and is strongly rest dependent. Baseline humans without healing traits heal naturally while sleeping, not while awake. The baseline rate comes from the `PLAYER_HEALING_RATE` balance option.

Lifestyle modifies sleeping natural healing linearly across the tested range: lifestyle -200 => 0x, -100 => 0.5x, 0 => 1x, +100 => 1.5x, +200 => 2x. It does not modify the tested awake trait-driven healing path.

Traits/mutations can provide awake regeneration and multiply sleeping recovery; the reference tests are the source of truth for exact trait factors.

### Medical healing

Bandage/disinfectant healing is per treated body part. Treatment on another part must not leak into the queried part's rate. Reference tests establish body-part-specific rates and combined-treatment multiplication; these fixtures should be ported directly as golden tests.

Healing cannot raise a part beyond max HP. Broken-part mending is a distinct timed process and must not be conflated with ordinary HP regeneration.

## 5. Stamina, oxygen and exertion

Stamina is an integer bounded by zero and a derived maximum. Recovery is processed over elapsed turns and clamps to that range.

Reference recovery starts from the configured `PLAYER_BASE_STAMINA_REGEN_RATE`, scaled by cardio fitness relative to cardio base, then composes:

- named stamina regeneration modifiers;
- breathing/limb capability;
- winded state (ordinary recovery is heavily reduced but not below the minimum multiplier path);
- mutations/enchantments;
- positive/negative stimulant effects;
- synthetic-lung/gill bionic behavior and power consumption.

The final recovery over a period is ceiling(`recovery_per_turn * turns`) before clamping.

Cardio fitness is a derived Character capability. NPCs and synthetic lungs currently take explicit shortcut values in CDDA; OctoGhast should preserve observable results while isolating these as policy/modifier providers.

Oxygen is separate from stamina. Recovery requires sufficient breathing limb score, not being underwater, not being grabbed, and compatible active bionic state. Underwater/breathing behavior belongs at the Character/environment boundary.

## 6. Digestion, calories, hunger and thirst

Do not model "hunger" as the energy store. CDDA separates:

- stomach contents/capacity and time since eating;
- guts/digestion;
- stored calories/healthy calorie target and body composition;
- hunger as a subjective/display state;
- thirst/hydration state.

Digestion moves nutrients/water through stomach/guts on elapsed time. Metabolic expenditure burns stored calories according to metabolic/BMR inputs.

Hunger presentation is derived from stomach fullness, time since eating and calorie deficit. At the reference baseline the code distinguishes immediate (<15 min) and recent (<3 h) eating windows and uses fullness thresholds to select engorged/full/satisfied/hungry/very-hungry/famished/starving states. Therefore UI hunger labels must not be used as authoritative nutrition storage.

Thirst normally rises according to calculated needs rates. Flags/traits such as no-thirst paths can force thirst to zero. NPC-food policy can suppress food-related need accumulation.

Extreme needs are checked before the five-minute needs update in the body-update pipeline; preserve this ordering when reproducing starvation/dehydration/exhaustion consequences.

## 7. Sleepiness, sleep and sleep deprivation

Sleepiness/fatigue is a scalar need with named threshold levels. Awake accumulation and sleeping recovery are calculated from needs rates.

The physiological scheduler updates needs in five-minute quanta. While awake, positive sleepiness rate increases sleepiness and, unless suppressed by a flag, sleep deprivation. While asleep, recovery decreases sleepiness. Sleep quality is modified by comfort and effects such as disrupted sleep; relevant bionics/flags can alter recovery and deprivation.

Actors do not accumulate ordinary sleepiness while already asleep or trying to sleep through the same path. Debug/stasis/no-needs paths are explicit exceptions.

Continuous and daily sleep are tracked separately for health/cardio bookkeeping. Sleep transitions must therefore be observable by the physiology subsystem, not merely represented as UI/activity state.

## 8. Health/lifestyle, vitamins and metabolism

Lifestyle ("hidden health") is bounded by reference behavior to approximately [-200, 200] and influences healing/cardio/disease susceptibility. Daily-health inputs accumulate and periodically feed lifestyle/health updates.

Vitamins are typed stores with data-defined decay/generation rates. Elapsed-time processing applies ticks based on each vitamin's configured rate. Daily intake accounting can affect health and resets after evaluation. Blood volume is represented through vitamin/resource mechanics in current CDDA and has hydration-sensitive regeneration behavior.

Metabolic rate affects calorie burn and body heat. Body composition/healthy calories feed BMI/fat categories, which in turn affect HP and hunger/starvation semantics.

## 9. Temperature and wetness

Temperature is per body part and stores both current and convergent/target temperature. Wetness is also per body part and bounded by the part's drench capacity.

Body temperature calculation consumes environmental and Character inputs including:

- local ambient temperature and weather;
- wind and vehicle speed;
- shelter;
- water temperature/submersion;
- sunlight and nearby fire/heat radiation;
- floor warmth while lying/sleeping;
- clothing warmth/coverage and climate control;
- mutation/trait/bionic modifiers;
- metabolism and starvation;
- sleepiness and sleep state;
- wetness.

The reference model uses a comfortable naked ambient around 19°C while active and 31°C while sleeping/resting, then adjusts per part. These are model constants/fixtures, not UI text.

Temperature transitions can create effects/morale/frostbite and can damage the actor at extremes. Environment Spec 14 owns weather/field generation; Character owns translating exposure into body-part physiological state.

## 10. Encumbrance and limb capability

Encumbrance is computed per body part/sub-body-part from worn equipment, layering conflicts, body shape/BMI and other modifiers. Runtime encumbrance exposes at least total encumbrance, armor contribution and layer penalty.

Limb scores are data-driven capability values. Wounds can reduce a score; encumbrance can further reduce scores that opt into encumbrance effects. Consumers such as movement, stamina/breathing, combat and item manipulation query limb scores rather than hard-coding "two working arms/legs".

This is the core abstraction for supporting non-human or mutated anatomies without proliferating special cases.

## 11. Effects/status composition

Effects are typed, duration/intensity-bearing statuses that may be global or body-part scoped. Their data can modify stats/resources and execute periodic behavior. Character must support:

- add/refresh/remove;
- duration and intensity bounds;
- body-part targeting;
- flags/capabilities exposed by effects;
- periodic activation on calendar turns;
- stat/resource modifiers;
- serialization by stable effect/body-part IDs.

Effects can modify hunger, thirst, sleepiness, pain, damage, stamina-adjacent behavior and many other resources. Apply effect changes through Character mutation APIs so clamps, events and caches remain consistent.

## 12. Morale and addictions

Morale is a collection of typed contributions with magnitude, cap and temporal decay. The Character exposes aggregate morale for consumers; individual sources remain distinguishable for replacement/stacking/decay behavior.

Addictions are persistent typed states with intensity/sated timing and withdrawal behavior. They can influence effects, needs, stats and morale. Exact addiction progression belongs to its data/runtime module, but storage, update scheduling and derived Character modifiers are part of this Character contract.

## 13. Movement/posture state

Movement mode is explicit state (e.g. walk/run/crouch/prone where supported), not inferred solely from speed. Movement cost and capability are derived from mode, limb scores, stamina, encumbrance, effects, carried mass, terrain and other system inputs.

Incapacitating states (downed, sleep, restraint, zero critical limb capability, death) must prevent or alter actions through capability queries. The Character model should expose reasons/results suitable for both avatar UI and NPC AI.

## 14. Physiological scheduling and ordering

The body update accepts an elapsed interval `from -> to` and uses tick counting so catch-up after time skips can process multiple intervals.

Normative cadence observed at the pinned baseline:

| Cadence | Character work |
|---|---|
| per turn / elapsed turns | stamina recovery and other immediate resource/effect work |
| 1 minute | blood-volume/heartrate/circulation indices |
| 3 minutes | mana update (extension dependency) |
| 5 minutes | weariness reduction attempt; extreme-needs check; needs; regeneration; mending; reset activity level |
| 30 minutes | health update |
| 12 hours | sleep-derived daily-health contribution |
| 24 hours | vitamin daily-intake health accounting/reset; avatar cardio/calorie-day accounting; skill-rust hook; daily sleep reset and related daily bookkeeping |

Important five-minute ordering is: reduce weariness -> check need extremes -> update needs -> regenerate -> mend -> reset activity level.

Scheduling must use the shared time/tick semantics from Spec 01. Catch-up must use elapsed tick counts, not "run once because time advanced".

### 14.1 OctoGhast authoritative-time adaptation

The cadence table and all pinned-CDDA formulas above remain normative **Cataclysm-profile** rules evidence; only their scheduling/context changes for OctoGhast. Per the reviewed Spec 01 boundary, generic Core owns one deterministic fixed-step simulation coordinate and profile-defined rate conversion. The **Cataclysm profile configures** the selected parity mapping as 10 canonical ticks = one pinned-CDDA world second/turn = 100 moves. Core APIs MUST NOT hard-code 10 TPS, 100 moves/second, or CDDA's one-/five-/30-minute physiological cadence as universal platform constants.

Physiology is advanced from authoritative elapsed simulation time for every simulated Character. A rules profile declares the relevant cadence/deadline definitions and any mapping from canonical time to its action/work currency. For Cataclysm, per-turn work corresponds to pinned CDDA one-second turns and minute/five-minute/30-minute/12-hour/24-hour work is triggered by crossed Cataclysm-profile deadlines or equivalent elapsed-interval tick counting. Implementations may batch mathematically equivalent work, but must preserve profile cadence boundaries, ordering, deterministic RNG consumption and intermediate state transitions where the pinned rule depends on them. Render frames, socket latency and client-local clocks never drive Character physiology.

No Character's needs/effects/temperature processing waits for that Character to submit an action, and no player's input turn is the clock source. Several player-controlled Characters may therefore accrue needs, digest, recover stamina, change temperature, gain/expire effects and cross thresholds simultaneously. Same-tick ordering follows Spec 01's deterministic server ordering; Character formulas are not specialized according to which player owns the actor.

A future non-Cataclysm profile may choose a different exact canonical rate, action currency, physiology cadence or even a different set of physiological resources without replacing Core scheduler, persistence, networking or projection abstractions. Such a profile is not required to preserve Cataclysm parity; the Cataclysm profile is.

### 14.2 Sleep, incapacity and continued world time

Sleep, unconsciousness, restraint and other incapacitating states alter a Character's action eligibility/capabilities; they do not pause canonical world time. A sleeping or incapacitated player-controlled Character continues receiving authoritative physiology/effect/temperature updates while other players, NPCs and world systems continue normally.

Sleeping/waiting by one player never performs CDDA's implicit single-avatar global time jump. Any accelerated/timewarp progression is a server-wide policy governed by Spec 01 and cannot be requested unilaterally by one Character. Waking, death, threshold crossings, damage and effect changes caused while the owner cannot act are authoritative transitions and must be available to the projection/event layer subject to visibility rules.

## 15. Persistence contract

Character persistence must preserve enough state that save/load followed by the same future inputs produces equivalent physiology.

Persist at minimum:

- base stats and durable stat/progression state;
- anatomy identity and runtime body-part state (HP, wounds, temperatures, wetness, treatment/mending state);
- stamina/oxygen and durable resource values;
- pain/stim/radiation/sleepiness/sleep deprivation;
- stomach/guts, stored calories/body composition and thirst;
- lifestyle/health/cardio accumulators;
- vitamins and relevant daily counters;
- effects and addictions with IDs, duration/intensity and body-part scope;
- morale entries needed to reproduce decay;
- movement mode/posture;
- sleep/daily physiological bookkeeping.

Derived caches (encumbrance cache, dead-state cache, enchantment cache, computed modifiers) should be rebuilt after load. Stable typed IDs and missing/obsolete-ID handling follow Specs 18 and 20.

Spec 20 is authoritative for save ownership and continuation semantics. The server/world save owns Character authoritative state in both one-player and multiplayer deployments. A durable world-local `PlayerId`, transient connection/session identity, and controlled `CharacterId` are distinct concepts. Disconnect destroys transport/session state, not the Character; reconnect rebinds the permitted durable identity to the same authoritative Character and receives a fresh projection. Socket IDs, packet/request queues, replication baselines, render transforms and client UI state are never Character persistence.

Character state is captured only at Spec 20's deterministic save barrier. A save MUST NOT observe a half-applied damage/healing/needs/effect transition or consume simulation RNG. Canonical time, scheduling/deadline state and RNG stream state/counters needed to continue physiology deterministically are persisted by their owning subsystem rather than duplicated ad hoc inside every Character.

## 16. Public interfaces required by dependent systems

OctoGhast should expose domain-level operations rather than mutable field access:

- query/evaluate primary and derived stats;
- enumerate/query body parts and limb scores;
- apply damage/healing/treatment/wounds;
- query alive/incapacitated/dead state;
- consume/recover stamina and oxygen;
- consume food/water and query digestive/nutrition state;
- add/remove/query effects, morale and addictions;
- set/query movement mode and capability;
- apply environmental exposure and update body temperature/wetness;
- advance physiology over an elapsed time interval;
- serialize/deserialize stable Character state;
- emit domain events for significant transitions (death, limb disabled/recovered, need threshold crossed, sleep/wake, effect added/removed) without requiring a UI.

Combat, inventory, environment, activities and UI must depend on these interfaces rather than duplicate formulas.

## 16.1 Client projection and Character-state visibility

Authoritative Character state is not synonymous with replicated client state. The server projects the minimum state needed for permitted gameplay/presentation and derives views from authoritative state rather than exposing arbitrary ECS/components.

- **Private/server-only:** deterministic RNG/scheduling bookkeeping, hidden effect internals, hidden traits/conditions, AI/internal policy state and any state whose disclosure would reveal information the observing client has not legitimately learned.
- **Owner-visible:** the controlled Character's detailed needs, stored-resource/status detail, body-part HP/wounds/treatment, pain, stamina/oxygen, sleepiness, morale/addiction detail, effects and derived capability/status information needed by the player's own UI. Owner visibility does not imply write authority.
- **Party-visible:** only information explicitly allowed by co-op policy or an in-world sharing/observation mechanic. Party membership alone must not automatically expose the owner's private physiological internals; coarse teammate status may be projected where product policy permits.
- **World-visible:** externally observable state required for other clients to render/interact correctly, such as stable actor identity appropriate to the observer, position/posture/movement mode, alive/dead/incapacitated or sleeping presentation when observable, and visible manifestations/events of wounds/effects. This remains constrained by per-player FOV/knowledge/interest rules from the spatial/projection specs.

A datum can have a more restrictive classification than its gameplay consequence: for example, a hidden effect may be private while an observable posture, animation cue or emitted event caused by it is world-visible. Projection policy must be testable independently from the Character formula that produced the state.

## 17. RNG and determinism

Some Character behavior uses random remainder/threshold rolls (need accumulation, wake-on-pain and other effects). Conformance tests must inject/control RNG or use statistical assertions where exact draw order is not part of a stable observable contract.

Elapsed-time catch-up with a fixed seed and identical starting state must be reproducible. Pure derived queries must not consume RNG.

## 18. Failure and edge behavior

Required edge handling:

- missing body-part/effect/vitamin IDs: data validation/load diagnostic according to Spec 18/20 policy;
- no vital body part: invalid anatomy diagnostic;
- HP max recalculation at zero/low HP: ratio-preserving clamp, max >= 1;
- stamina: clamp to [0,max];
- thirst/radiation and similar resources: enforce reference lower/upper bounds where their mutators do;
- disappearing equipment/environment inputs: derived caches recalculate rather than retaining stale encumbrance/warmth;
- body-part-scoped treatment/effects must not leak to other parts;
- stasis/no-needs flags must short-circuit accumulation without corrupting clocks;
- long time skips must process all elapsed scheduled ticks.

## 19. Black-box parity suite

Minimum implementation gates:

1. **Anatomy fixture** — load human anatomy/body parts; validate graph references, vital parts, opposites, subparts, base HP and limb-score definitions.
2. **HP recalculation** — vary STR and lifestyle; verify per-part max formula, Glass Jaw head modifier, proportional current-HP preservation and clamps.
3. **Death** — reduce each vital main part to zero and verify death; reduce non-vital limb to zero and verify survival plus capability loss.
4. **Natural healing** — port `char_healing_test.cpp` baseline/lifestyle cases including awake=0 baseline and -200..+200 lifestyle sleeping multipliers.
5. **Medical healing** — bandage/disinfect head/limbs/torso independently; verify no cross-part leakage and combined treatment behavior.
6. **Stamina** — port `char_stamina_test.cpp` baseline recovery, winded, breathing/encumbrance, stimulant and bionic cases; assert clamps.
7. **Needs cadence** — advance 4m59s then 1s; verify five-minute boundary behavior; advance multiple five-minute periods in one call and compare with incremental execution under controlled RNG.
8. **Digestion/hunger** — fixtures for just-ate/recent/empty stomach, calorie surplus/deficit and fullness thresholds; verify hunger labels do not mutate stored calories.
9. **Thirst/no-thirst** — normal accumulation versus suppression flags/policy.
10. **Sleep** — awake sleepiness accumulation, sleep recovery, comfort modifiers, disrupted sleep and deprivation suppression.
11. **Temperature** — port representative `temperature_test.cpp` vectors for ambient, clothing, wind/wetness, fire/shelter and sleep.
12. **Wetness** — drench/dry body parts and verify capacity, per-part isolation and temperature/morale coupling.
13. **Encumbrance** — port `encumbrance_test.cpp` layering and body-part/subpart fixtures; verify limb score impact.
14. **Effects** — body-part/global effect add/decay/remove, resource modification and save/load.
15. **Morale/addiction** — contribution stacking/decay and addiction persistence/withdrawal representative fixtures.
16. **Vitamin/health** — decay/generation tick boundary, daily intake reset and lifestyle impact.
17. **Save/load** — snapshot a physiologically nontrivial Character, reload, compare authoritative state and then advance both copies identically.
18. **Catch-up equivalence** — for deterministic inputs, N small physiology advances and one equivalent large elapsed-time advance produce equivalent state (allowing only explicitly documented RNG/tick-order differences).
19. **Avatar/NPC reuse** — same Character inputs produce same shared physiology; only documented NPC/avatar policies differ.
20. **Cross-system smoke** — damage -> pain/limb loss -> movement/stamina capability; cold/wet exposure -> body temperature/effect; eating -> digestion -> calories/hunger; sleeping -> recovery/healing.

## 20. Implementation slicing

Recommended sequence:

1. Anatomy definitions + runtime body-part state + serialization.
2. Primary stats, HP recalculation, limb scores and death.
3. Effects/modifier composition primitives.
4. Stamina/oxygen.
5. Stomach/guts, calories, hunger/thirst and vitamins.
6. Sleepiness/sleep/health/healing/mending.
7. Encumbrance integration.
8. Temperature/wetness exposure.
9. Morale/addictions.
10. Full elapsed-time scheduler and cross-system conformance suite.

Each slice should land with the corresponding parity fixtures before dependent feature work consumes it.

## 21. Dependencies and ownership boundaries

- **Spec 01 time:** owns calendar/tick semantics used by physiology cadence.
- **Spec 18 data:** owns typed IDs, loading, inheritance/finalization/validation.
- **Spec 20 persistence:** owns save envelope/migration policy.
- **Spec 05 Item:** owns item definitions; Character consumes worn/carried/consumed item effects.
- **Spec 06 Inventory:** owns item locations/transfers; Character exposes capacity/capability.
- **Spec 09 Combat:** owns attack/damage resolution; Character owns resulting body state.
- **Spec 14 Environment:** owns weather/fields/exposure sources; Character owns physiological response.
- **Spec 03 Progression:** owns acquisition/progression of traits, mutations, skills and bionics; Character consumes their modifiers.
- **Spec 21 UI:** owns presentation; Character exposes state and transition events.

## 21.1 Core/profile ownership and evolution contract

### Immutable definition/profile data

Loaded/finalized definitions are immutable for a running world and referenced by stable typed IDs. For the Cataclysm profile this includes anatomy/body-part definitions, character modifiers, effect/vitamin/addiction/movement-mode definitions, balance constants, need thresholds, temperature model constants and cadence definitions. These belong to Cataclysm content/profile policy even when generic registry/validation machinery is supplied by Core.

### Mutable authoritative runtime state

Mutable Character instance state includes body-part HP/wounds/wetness/temperature/treatment, resource values, digestion/calorie state, effects, addictions, morale, movement/posture, sleep/health bookkeeping and any deterministic per-instance remainder/deadline state required by the selected profile. It is server-authoritative, addressed through stable Character/body-part/content IDs, and changes only through deterministic simulation/domain mutation paths.

### Generic Core capability

Core may provide reusable primitives/interfaces for:

- stable actor/entity identity and typed content references;
- definition registries/finalization/validation;
- bounded scalar/resource state and body/part-like keyed state where useful without prescribing human anatomy;
- deterministic modifier/query composition;
- authoritative damage/resource/status mutation transactions and domain events;
- canonical elapsed-time/deadline scheduling with a profile-supplied mapping;
- deterministic RNG services/streams;
- persistence ownership/fixup hooks and save barriers;
- player/session/control bindings and per-observer projection contracts.

Core MUST NOT require every ruleset to have STR/DEX/INT/PER, CDDA body-part IDs, hunger/thirst/fatigue, vitamins, morale/addictions, CDDA temperature constants, 5-minute needs ticks, or the 100-move economy.

### Cataclysm-profile policy

The Cataclysm profile owns the concrete human/non-human anatomy graph semantics, primary stats and formulas, HP/death rules, needs/metabolism/digestion/sleep model, vitamin/health model, effect/addiction/morale semantics, encumbrance/limb-score interpretation, temperature/wetness model, balance constants, exact modifier ordering, and cadence mappings described by the pinned evidence. Those rules remain mandatory for reference-parity fixtures.

### Future evolution seams

A future OctoGhast rules profile may introduce different anatomy topology, continuous or differently sampled physiology, alternative resources/needs, different damage/death semantics, different stat sets, different modifier systems, different action currencies or different cadence constants. It must still use the authoritative-server, stable-identity, deterministic-time, persistence and projection boundaries already established unless a later architecture ticket deliberately changes those generic contracts.

This is an architectural classification only. It does not weaken or delete any Cataclysm reference fixture in Sections 2–20.

## 22. Re-evaluation conformance scenarios — Core vs Cataclysm boundary

21. **Cataclysm timing/profile mapping.** Run the Cataclysm profile and assert 10 canonical ticks = one reference world second = 100 moves, with the existing five-minute Character boundary occurring at the equivalent profile deadline.
22. **Alternative Core rate.** Run a minimal non-Cataclysm test profile at a different exact fixed-step rate and physiology cadence. Assert Core scheduler, Character identity, persistence and projection paths operate without assuming 10 TPS, 100 moves/second or five-minute needs ticks.
23. **Alternative physiology schema.** Define a test actor profile with a different resource/stat set and no CDDA hunger/vitamin/morale model. Assert generic actor/resource/status/persistence facilities do not require CDDA fields to exist.
24. **Definition versus instance isolation.** Load finalized Cataclysm anatomy/effect/need definitions once, instantiate two Characters, mutate one Character's HP/effects/needs and assert the immutable definitions and the peer Character instance are unchanged.
25. **Save-barrier atomicity.** Request a save while a Character physiology step is resolving a threshold/effect/healing transition. Assert Spec 20 captures either the complete pre-step or complete post-step authoritative state, never a partial transition, and save/load consumes no gameplay RNG.
26. **Reconnect identity.** Disconnect a player while its Character sleeps or is incapacitated, advance authoritative time, save/reload if applicable, reconnect with a new connection and assert the same durable PlayerId regains the same CharacterId at the correctly advanced physiological state; no socket/session identifier is required.
27. **Observer invariance.** Add/remove overlapping client observers while advancing a Character's physiology. Assert one authoritative Character update/RNG sequence occurs regardless of observer count; only projections differ according to owner/party/world visibility.
28. **Profile constant isolation.** Change a Cataclysm-profile fixture constant/cadence in a dedicated test profile and assert the resulting behavior changes only through profile data/configuration, with no Core code/API constant requiring modification.

## Acceptance-criteria disposition

- [x] Every character state variable required for parity is assigned a domain, units/type, bounds/default source and mutation boundary; exact content-defined ranges remain sourced from pinned JSON/balance data.
- [x] Derived values and modifier ordering are specified as base -> additive/multiplicative/data modifiers -> wound/encumbrance capability adjustment -> final clamp, with named modifier contracts preferred.
- [x] Body-part damage, healing, vital-part death and mending boundaries are explicit.
- [x] Needs/metabolism/sleep/temperature cadence and threshold-driving state are explicit, including five-minute ordering and elapsed-time catch-up.
- [x] Effect, morale and encumbrance interactions are mapped.
- [x] Avatar-only presentation/policy behavior is separated from reusable Character physiology.
- [x] Save/load authoritative state, cache rebuilding and black-box parity scenarios are defined.
- [x] Dependencies on items, environment, combat and time are explicit.

## Open implementation decisions (not parity ambiguity)

These may differ internally without violating parity: concrete C# class hierarchy; ECS vs aggregate implementation; cache representation; event-bus implementation; numeric wrapper types; and whether individual physiology subsystems are separate services. The acceptance boundary is the observable state transition, stable data contract and timing behavior above.


## Architecture review — continuous authoritative time and co-op

This review does **not** replace or reinterpret the pinned-CDDA Character investigation. Sections 2–13 retain the pinned formulas, thresholds, modifier composition and physiological rules. OctoGhast intentionally changes only the execution context: one continuously advancing authoritative server clock, actor-generic Character behavior, multiple simultaneous player-controlled Characters, and explicit per-observer projection.

### Conformance scenarios added by the architecture review

1. **Cadence independent of player input.** Given two equivalent Characters and the same authoritative elapsed interval/environment, one submitting no commands and one submitting commands, physiological cadence work occurs at the same canonical boundaries and produces equivalent needs/metabolism/temperature/effect progression except for consequences of the commands themselves.
2. **Two simultaneous players.** Given two player-controlled Characters in the same world, advancing N canonical ticks updates both through the same Character APIs. Neither Character is selected as a privileged avatar or clock owner, and deterministic results are independent of client render/update frequency.
3. **Separated players.** Given player-controlled Characters in distinct active regions, each receives the cadence appropriate to authoritative elapsed time and its local environmental inputs; processing one player's region must not suppress or duplicate the other's Character update.
4. **Sleeping player while peer acts.** Put Character A to sleep while Character B remains active. Advance world time through B's actions/server progression. A continues digestion, needs, sleep recovery, temperature and effect-duration/periodic processing and can wake from an authoritative wake condition without pausing B or the world.
5. **Incapacitated player while peer acts.** Incapacitate A and continue simulation with B. A cannot issue actions forbidden by capability state but continues authoritative damage/healing/effect/need processing; death or recovery transitions occur at their canonical times.
6. **No unilateral sleep time jump.** A sleeping/waiting Character cannot advance the global clock faster solely because its owner is waiting. Any acceleration is server-wide policy and preserves Character cadence/order for all actors.
7. **Projection isolation.** With A and B connected, A receives its detailed owner-visible physiology. B receives only world-visible state plus explicitly enabled party-visible state for A, further restricted by B's FOV/knowledge/interest. Hidden effects/internal counters are not replicated merely because both are players or party members.
8. **Single-player/multiplayer equivalence.** Run the same one-Character input/environment/tick trace through a one-player in-process server and through the multiplayer server path with no interacting peer. Character-authoritative outcomes are identical; only transport/projection envelopes may differ.
9. **Catch-up equivalence.** Advancing an interval through normal fixed ticks versus permitted deterministic catch-up yields equivalent Character state and threshold/event ordering, including five-minute ordering and RNG-sensitive work.
10. **Formula preservation.** Golden fixtures for healing, stamina, needs, temperature, effects, vitamins and other Character rules remain pinned to the existing CDDA evidence; architecture adaptation tests may change scheduling/context assertions but must not silently alter those formulas.
11. **Core/profile isolation.** The re-evaluation scenarios 21–28 pass: Cataclysm constants/cadences remain profile-local, alternative Core rates/physiology shapes do not require CDDA invariants, save/reconnect follows Spec 20, and observer count cannot change authoritative physiology.
