# Spec 09 investigation — combat core: damage, melee, ranged and projectiles

Parent: #65  
Tracking issue: #74  
Reference baseline: `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Status and scope

This page is the implementation-ready combat specification required by #74. It covers the shared damage model, melee, blocking/dodging, techniques and martial arts, ranged aiming/firing, throwing, projectile traversal/impact, armor and weakpoints, ammunition effects, durability/ammunition mutation, combat events, deterministic RNG, persistence boundaries, multiplayer contention and black-box parity scenarios.

It does **not** make Godot authoritative, redefine CDDA move costs as milliseconds, or assume one privileged avatar/reality bubble. Environment-owned explosion/field/fire evolution remains a dependent subsystem, but combat owns the command that creates the effect and the immediate damage/effect contract described here.

Prerequisite contracts: Spec 01 (#66), Spec 02 (#67), Spec 04 (#69), Spec 05 (#70), Spec 06 (#71), Spec 12 (#77), Spec 17 (#82), Spec 18 (#83), and Spec 20 (#85).

## 2. Authoritative pinned-CDDA evidence

Primary source anchors at the pinned commit:

- `src/damage.h`, `src/damage.cpp` — `damage_type`, `damage_unit`, `damage_instance`, resistances, damage-type EOCs, JSON defaults.
- `src/melee.cpp` — Character hit roll, melee attack pipeline, critical chance, damage rolls, techniques, blocks, stamina/move costs, weapon wear.
- `src/creature.cpp` — target dodge/hit-spread calculation, melee-hit ordering, projectile hit-quality/body-part selection, shared `deal_damage`, per-damage-type effects.
- `src/monster.cpp` — monster weakpoint/armor mitigation and monster melee attack cost/dice.
- `src/ranged.cpp` — weapon dispersion, aim-per-move, firing time, malfunction/fouling, ammunition consumption, recoil/burst behavior and throwing.
- `src/projectile.cpp` / projectile interfaces — projectile traversal, obstacle/map interaction and impact resolution.
- `src/weakpoint.cpp`, `src/weakpoint.h` — weakpoint families, skill, difficulty, selection, armor/damage modifiers and weakpoint effects.
- `data/json/damage_types.json` — damage-type definitions.
- `data/json/ammo_effects.json` — projectile/ammunition effect definitions and hardcoded effect IDs.
- `data/json/martialarts.json` and martial-art/technique JSON — martial styles, buffs, techniques and triggers.
- item/gun/ammunition JSON definitions — immutable weapon, gun, ammunition, armor, coverage and penetration data interpreted by the above runtime code.

Important tests at this baseline:

- `tests/melee_test.cpp` — Character/monster hit probabilities, dodge incapacity, melee training caps, damage-type resistance effectiveness, damage-type EOCs.
- `tests/ranged_balance_test.cpp` — unskilled/competent/expert shooting accuracy, gun shot features, custom damage type, targeting/body-part distributions.
- `tests/projectile_test.cpp` — projectile traversal through obstacles and liquid-projectile effects.

These are behavioral evidence, not an instruction to port the C++ class graph.

## 3. Three-layer compatibility rule

For every combat path distinguish:

1. **Pinned CDDA reference behavior** — formulas, costs, RNG, ordering and data semantics observed at the pinned commit.
2. **OctoGhast adaptation** — continuous canonical server time, multiple simultaneous player-controlled actors, transport-neutral commands and player-specific projections.
3. **Implementation contract** — the combined rule that OctoGhast must expose.

The adaptation may change *when and under whose authority* an action resolves. It must not silently change CDDA damage, attack-cost, hit-quality, ammunition, armor or weakpoint rules.

## 4. Immutable definition data versus mutable runtime state

### 4.1 Immutable registry-owned definitions

Load/finalize through Spec 18 typed registries:

- `DamageTypeDefinition` keyed by `DamageTypeId`: name, skill, physical/melee-only/edged/environmental/material-required/no-resist flags, immunity flags, derived type/factor, bash conversion factor, on-hit EOCs and on-damage EOCs.
- weapon/item type definitions: melee damage by type, to-hit, attack time, reach, flags, categories/proficiencies, material/durability data.
- gun/ammo/gunmod/magazine definitions: gun skill, dispersion, sight behavior, recoil, firing mode/burst, operation type, ammunition requirements, projectile damage/penetration/speed/range/drop/ammo effects and malfunction/fouling modifiers.
- armor definitions: coverage/body locations, layer/material/protection values, durability/ablative behavior and flags.
- weakpoint sets/families: coverage, difficulty by attack class, conditions, armor multipliers/penalties, damage/critical multipliers, effects and proficiency modifiers.
- martial-art/style/technique/buff definitions: requirements, allowed vectors/weapons, triggers, damage/arpen/hit/block/move-cost modifiers, counters, knockback/stun/disarm and other declared effects.
- ammo-effect definitions and EOC/spell references.

Definitions are not copied into saves. Runtime records persist typed IDs plus mutable state.

### 4.2 Mutable authoritative state

Combat may mutate:

- Character/creature HP by body part, pain, effects, death state and relevant counters.
- actor move budget, stamina/energy, recoil/aim state, block/dodge attempts, martial-art transient buffs, skills/proficiency practice.
- wielded/worn item identity/location, charges/ammunition, magazine state, dirt/fouling, faults, heat, damage/durability.
- target aggro/combat memory and relevant actor-local combat flags.
- map terrain/furniture/item state affected by projectile impacts.
- active projectile resolution state only while an attack is resolving; ordinary ballistic projectiles need not become durable ECS entities unless a later feature explicitly requires time-of-flight persistence.
- authoritative events/messages before per-client projection.

## 5. Shared damage contract

### 5.1 Damage representation

A `DamageInstance` is an ordered collection of damage units. Each `DamageUnit` contains:

- `DamageTypeId type` — mandatory in JSON;
- `amount` — default 0;
- `damageMultiplier` — default 1;
- `armorPenetration` (`res_pen`) — default 0;
- `armorMultiplier` (`res_mult`) — default 1;
- `constantArmorMultiplier` — default 1;
- `constantDamageMultiplier` — default 1;
- optional barrel-length damage data.

Effective resistance for a unit follows the baseline formula:

`max(resistance[type] - armorPenetration, 0) * armorMultiplier * constantArmorMultiplier`.

Final pre-HP damage for an unimmune surviving unit is based on:

`amount * damageMultiplier * constantDamageMultiplier`

after block/armor/weakpoint mutations have changed the unit.

Do not collapse all physical damage into one scalar: bash/cut/stab and other registered damage types retain independent resistance, penetration, effects and reporting.

### 5.2 Shared application ordering

For a creature hit, preserve this observable order:

1. reject damage for dead/`CANNOT_TAKE_DAMAGE` targets;
2. establish attack context, source, target, body part and weakpoint skill/context;
3. perform block interception for melee before ordinary armor absorption;
4. apply damage-type **on-hit** effects at the pinned hook point;
5. select/apply armor and weakpoint mitigation/modification;
6. for each surviving damage unit, check type immunity and compute adjusted damage;
7. execute per-type side effects/pain calculations;
8. execute damage-type **on-damage** EOCs with pre-mitigation and post-mitigation information;
9. apply total pain subject to body-part rules;
10. apply HP damage;
11. apply selected weakpoint effects;
12. check death and subsequent on-kill behavior in the owning attack pipeline.

Damage-type on-hit/on-damage EOCs execute server-side under Spec 17 with explicit source/target talkers. They are never client scripts.

### 5.3 Armor and penetration

Armor is resolved against the selected body part/sub-body-part and attack damage type. Coverage is probabilistic where the baseline definition requires it; uncovered layers do not mitigate that hit.

For each covering layer/material, use baseline protection/durability semantics. Penetration is applied before the damage unit's armor multipliers via the effective-resistance formula above. Armor/weakpoint modifiers may alter resistance and damage independently.

Armor durability damage is an authoritative item mutation. Destruction/transform/removal must update the stable ItemUid/location graph atomically under Specs 05/06. A client may not report its own armor roll or post-hit durability as authoritative.

## 6. Melee

### 6.1 Command and validation

A player melee request is an immediate authoritative combat command containing at minimum actor ID and semantic target reference. The server resolves actor ownership, current authoritative position, target identity/position, weapon/item location, state/effects and reach at execution time.

Pinned baseline rejects or aborts attacks for cases including inability to attack, incorporeality, invalid adjacency/reach, protected submerged targets and some invalid stance/leg states. OctoGhast revalidates these at command resolution; stale client targeting never grants a hit.

Opening/using a targeting UI consumes no moves and does not pause the world.

### 6.2 Hit/miss

Pinned Character hit roll:

- begin from `get_melee_hit_base()`;
- apply farsightedness penalty (-2 when uncorrected);
- apply stance penalties: prone -8, or -2 with pseudopod grasp; ordinary crouch -2 where applicable;
- apply vehicle penalties: -10 in vehicle, another -10 when controlling it, minus absolute forward velocity;
- multiply by the character melee-attack-roll modifier;
- sample through the baseline melee hit-range distribution.

Target resolution calls `dodge_roll()` then:

`hitSpread = hitRoll - dodgeRoll - sizeMeleePenalty()`.

Immobile/`CANNOT_MOVE` targets receive +40 hit spread. A miss is `hitSpread < 0`; successful attacks continue with `hitSpread >= 0`. Misses trigger target dodge hooks where applicable.

Body-part selection is made from authoritative anatomy using hit spread and attacker high-attack capability.

### 6.3 Critical hits

Character critical chance is baseline-defined, not a generic multiplier.

Let:

- weapon chance = 0.5 for normal basis; unarmed uses `0.5 + 0.05 * unarmedSkill`; positive weapon to-hit can raise it to at least `0.5 + 0.1 * toHit`; negative to-hit subtracts `0.1 * abs(toHit)`; clamp [0,1].
- stat chance = clamp(`0.25 + 0.01 * DEX + 0.02 * PER`, 0, 1).
- skill basis = weapon melee skill (or CQB floor), plus `meleeSkill / 2.5`.
- skill chance = clamp(`0.25 + 0.025 * skillBasis`, 0, 1).
- triple chance = weapon * stat * skill.

If attack hit roll exceeds `1.5 * targetDodge`, add the pinned pairwise-double term:

`0.5 * (weapon*stat + stat*skill + weapon*skill - 3*triple)`.

Add martial-art critical-chance bonus. `scored_crit` performs one authoritative uniform float draw against this probability.

### 6.4 Technique/vector/damage order

After a hit and crit decision:

1. select target body part;
2. choose forced technique or eligible technique if specials are allowed;
3. resolve attack vector/contact area and any worn unarmed weapon;
4. construct rolled damage for all registered melee-relevant types;
5. apply technique/martial-art modifiers and special effects in pinned ordering;
6. call target melee-hit pipeline, which may block then armor/weakpoint-mitigate;
7. emit combat messages/events and practice;
8. apply weapon/shield wear for non-hallucination attacks;
9. spend stamina and move cost;
10. fire martial-art on-attack, on-crit/on-kill/on-hit style hooks at their baseline stages.

Forced counter techniques use the same authoritative melee path; they are not free client reactions.

### 6.5 Attack move cost

Pinned `attack_speed`:

- `base = weapon.attack_time(actor) / 2`;
- `skillCost = base * (15 - meleeSkill) / 15`;
- `dexBonus = DEX / 2`;
- stamina penalty grows linearly from 1 to 2 as stamina ratio falls from 25% to 0%;
- multiply by configured lift/balance modifiers;
- add skill cost, subtract dex bonus;
- apply enchantment attack-speed modifier;
- apply martial-art multiplicative then flat move-cost modifiers;
- prone multiplies by 4 (1.5 with pseudopod grasp); applicable crouch multiplies by 1.5;
- minimum result is **25 moves**; otherwise round.

Actual charged moves then apply the existing weariness/exertion combat-speed multiplier contract from Specs 01/02. Move units remain CDDA simulation currency. At 10 canonical TPS the actor may incur a negative budget and wait for later accrual; do not convert an N-move attack into an N*10ms timer.

### 6.6 Stamina

Melee burns authoritative arm energy/stamina after the attempt. Baseline total cost derives from standard weapon stamina cost, melee skill, stance malus, weapon-category proficiency modifiers and enchantments, with a minimum burn magnitude of 50. The `DEFT` miss bonus reduces miss cost before the cap.

Block stamina is separate (section 7).

### 6.7 Weapon wear

Successful or missed non-hallucination melee attempts may call baseline melee wear on the current weapon/shield. Wear is RNG-sensitive and may damage/destroy the item. If destruction changes wielded ownership/location, complete that mutation atomically before later hooks reference authoritative equipment.

## 7. Dodge and block

### 7.1 Dodge

Dodge eligibility and roll come from the target Character/Creature state. Sleep/incapacitation and other effects can remove attempts or reduce roll; pinned `melee_test.cpp` explicitly covers capable versus incapacitated Characters.

Dodge attempts are per actor state, not per connection. Disconnecting a player does not replenish or freeze combat reactions; server policy continues the Character according to the Character/AI/disconnect contract.

### 7.2 Block eligibility and ordering

Character block occurs after melee body-part selection and before ordinary damage absorption.

Baseline disallows block while sleeping/narcotized/winded/fear-paralyzed/driving, with no blocks remaining, below 2000 stamina, or against an attacker more than two sizes larger unless `BLOCK_HUGE_ATTACKS`.

A reaction check uses:

`x_in_y(meleeSkill * 20 * reactionLimbScore, 100)`.

A successful attempt decrements `blocks_left` once.

### 7.3 Block mitigation

The block chooses the best eligible shield/weapon or a martial-art-permitted limb/worn shield. It computes a block score from arm strength, unarmed/melee skill, shield blocking ability, limb score and martial-art bonuses.

Physical melee damage:

1. consumes flat martial-art block bonus first;
2. multiplies remaining physical melee damage by `logarithmic_range(0, 40, blockScore)`.

For eligible non-physical resistible damage, an item block reduces the unit to one fifth where the baseline conductivity rule allows it.

Blocking can damage the shield/worn item. It then burns leg stamina:

`PLAYER_BASE_STAMINA_BURN_RATE * 6 * ((20 - meleeSkill) / 20)`

and may trigger martial-art on-block/counter techniques. Counterattacks re-enter the normal authoritative attack path and therefore participate in deterministic ordering/RNG.

## 8. Weakpoints

Weakpoint selection is definition-driven.

Character weakpoint skill:

- melee: average of weapon-specific melee skill and generic melee skill, plus DEX/PER stat terms, multiplied by average vision/reaction limb scores;
- ranged: average weapon gun skill and generic gun skill, plus DEX/PER stat terms, multiplied by vision score;
- thrown: throwing skill for both skill terms with vision scores;
- monster attackers use monster melee skill.

Monster weakpoint families add proficiency bonus/penalty. Weakpoint definitions may change armor, armor penetration/penalty, normal damage, critical damage, coverage and apply effects/EOCs/instant-death chance. Their conditions and RNG execute only on the authoritative server.

Weakpoint practice on hit/kill is actor progression state and is persisted under Character progression.

## 9. Ranged aiming and firing

### 9.1 Command/query split

Client targeting/reticle movement is presentation. Synchronous projected queries may return legal visible/known targets and estimated aim information. The mutating operations are explicit commands such as aim/spend-moves, fire, throw, reload and change fire mode as owned by their feature contracts.

The server revalidates line/range, weapon identity/location, current mode, ammunition, actor state and target coordinate at execution time.

### 9.2 Dispersion

Pinned total gun dispersion begins with weapon gun dispersion, then adds:

- Dexterity-based ranged modifier;
- manipulation-based character dispersion modifier;
- vehicle/driving penalty where applicable;
- skill dispersion from average generic gun + weapon gun skill (archery uses the pinned 450 basis; other guns 300, both divided by the dispersion option);
- enchantment multiplier;
- underwater mismatch +150 range and x4 multiplier;
- current recoil;
- projectile/ammunition shot spread.

Do not replace this with screen-space cone angles. Authoritative dispersion uses simulation data; Godot may visualize it.

### 9.3 Aim progression

Aiming is move-priced. `gun_engagement_moves` repeatedly calls baseline `aim_per_move` from the current penalty toward a requested target penalty until threshold or no meaningful improvement. Therefore aim is actor work under canonical scheduling, not elapsed client cursor time.

Continuous-time adaptation: a player may issue an aim intent/activity and the Character spends its move budget while other actors continue. UI focus cannot stop world time.

### 9.4 Fire timing

Pinned firing attack cost includes `time_to_attack`, ready/aim-state related time and firearm operation time. Operation time adds:

- lever action 80 moves;
- single action 100;
- pump action 120;
- bolt action 160;

then multiplies by `1 - 0.05 * min(gunSkill + weaponSkill, 10)`.

Exact additional ready/aim/reload action costs remain those provided by the pinned item/action definitions and their owning specs.

### 9.5 Shot loop and ammunition

Before firing, validate gun type, choke/ammunition compatibility and available shots. Requested burst count is capped by authoritative ammunition/energy feasibility.

For each shot, preserve the baseline mutation order sufficiently to keep RNG and state parity:

1. supply/reload external shot ammo where applicable;
2. run malfunction/dirt/fouling/overheat checks;
3. emit authoritative sound event;
4. construct projectile from gun/ammo/current mode;
5. consume the required ammunition/charge;
6. compute dispersion from current authoritative recoil/actor state;
7. resolve projectile traversal/impact;
8. update hit/event counters;
9. apply per-shot recoil/delayed burst recoil and gun state;
10. continue only while gun remains valid and enough resources exist.

A malfunction may stop a burst after earlier shots have already consumed ammo and produced effects. Never roll back committed earlier shots.

### 9.6 Malfunction/fouling evidence

Pinned `handle_gun_damage` includes a dirt misfire probability:

`x_in_y(dirt^3, 1_000_000_000_000)`

unless `NEVER_JAMS`, plus gun/magazine damage-dependent jam probabilities and ammunition/fouling effects. OctoGhast must treat these as authoritative RNG-driven item mutations. Exact branches/flags must be ported from the pinned source rather than replaced with a generic “jam chance”.

## 10. Projectile traversal, hit quality and impact

### 10.1 Projectile state

A projectile resolution carries immutable-derived plus shot-specific data: impact damage, speed, range, critical multiplier, shot spread, projectile/ammo-effect IDs, drop item, source/weapon context and flags such as multishot/magic/shrapnel.

A normal projectile resolves atomically within the admitted fire/throw action against authoritative map/spatial state. If later implementation introduces persistent travel time, that requires a separate explicit design change; the baseline here is immediate trajectory resolution.

### 10.2 Spatial traversal

Trajectory uses authoritative integer/grid map coordinates from Spec 12. It checks terrain/furniture/vehicle/creature occupancy in path order and invokes map shooting/impact rules. It may damage or transform map state, strike creatures, continue/penetrate according to projectile rules, stop, or drop/embed the projectile item.

Overlapping player regions see one projectile resolution and one set of mutations.

### 10.3 Slow-projectile dodge

For non-magic projectiles with speed < 20, an eligible target gets a dodge contribution:

- `avoidRoll = dodge_roll()`;
- `diffRoll = dice(10, projectileSpeed)`;
- add `clamp(avoidRoll / diffRoll, 0, 1)` to `missed_by`.

A resulting quality >= 1 is a miss/avoid. Faster projectiles skip this ordinary dodge path.

### 10.4 Body part and hit quality

If `missed_by >= 1` a non-magic projectile is a total miss.

Otherwise selection computes `goodhit`, body part/weakpoint and a damage multiplier. For Character-like anatomy, body-part selection uses a hit value derived from `missed_by + rng_float(-0.5, 0.5)`; magic uses its special random-body-part path.

Pinned quality bands include headshot/critical/good/standard/grazing thresholds and use the projectile `critical_multiplier`, body-part critical factor and additional random hit roll. Preserve the baseline formulas in `Creature::select_body_part_projectile_attack`; they must not be approximated into a single “critical chance”.

`NO_DAMAGE_SCALING` forces damage multiplier to 1 on a hit. `NOGIB` caps extreme overkill according to the pinned ratio rule.

### 10.5 Impact and effects

After hit-quality scaling:

1. liquid projectiles apply clothing permeability scaling where applicable;
2. shared damage/armor/weakpoint pipeline resolves;
3. damaging projectile effects execute;
4. no-damage/contact effects execute subject to touch/soak rules;
5. target/map/projectile drop/embed side effects complete;
6. combat events/messages are produced.

Ammo effects can create fields, explosions, EMP/flash effects, spells and EOCs. Immediate creation/activation is combat-owned; subsequent world simulation belongs to Environment/Events specs.

## 11. Throwing

Throw is an authoritative immediate action. Baseline behavior copies/constructs the thrown projectile, charges `throw_cost` moves, burns stamina unless assisted, computes adjusted throwing skill, derives bash/other projectile damage from item mass/material/shape and modifiers, resolves dispersion/trajectory through the projectile pipeline, then transfers/drops/embeds the item according to impact rules.

The thrown ItemUid must move through Spec 06 ownership semantics: wielded/inventory -> in-flight resolution -> map/target ownership or destruction. It may not exist simultaneously in inventory and on the map.

## 12. Events, EOCs, messages and projection

Authoritative event examples include melee/ranged attacks against monsters/Characters and headshot/kill-related events. Damage types, weakpoints, ammo effects and martial arts may invoke EOCs.

Rules:

- event generation occurs from server resolution only;
- EOC talkers use the actual source/target identities and isolated context per Spec 17;
- gameplay effects are committed before projection;
- message/event audience is calculated per player using visibility/knowledge/ownership rules; do not copy baseline `get_player_character().sees(...)` as a one-avatar server rule;
- attacker owner may receive command result/weapon-state deltas; target owner receives permitted damage/effect state; observers receive only world-visible combat events;
- hidden exact HP, armor inventory, weakpoint data or arbitrary ECS components are not replicated merely because the server used them.

## 13. Continuous-time and co-op adaptation

### Pinned CDDA reference behavior

CDDA resolves combat inside a turn-gated single-player loop. The avatar UI chooses an action; attacks immediately mutate target/world state and consume moves. Many message branches consult the global player/avatar view. Projectile travel is resolved synchronously.

### OctoGhast adaptation

- combat commands are admitted/resolved by the authoritative server;
- each actor spends its own move budget under the single canonical clock;
- several actors may attack in the same canonical tick;
- the server does not pause while one client aims through UI;
- map/creature/item state is global authoritative state, not one player's reality bubble;
- messages/projections are audience-filtered per client;
- no Godot node, animation or transform participates in hit, line, armor or RNG authority.

### Implementation contract

Within one deterministic server resolution boundary, an admitted immediate attack is atomic with respect to externally observable authoritative state. Same-tick combat mutations use the existing ordering key from Specs 01/04: phase, due tick, subsystem priority, stable actor/entity ID and monotonic admission sequence. A later action revalidates the state left by earlier actions.

Examples: if actor A kills target T before B's admitted attack resolves, B receives the feature-appropriate stale/invalid-target result and does not also damage a dead target; if A destroys/transfers a weapon before B's command using it resolves, B cannot use a stale client copy.

## 14. RNG and determinism

Combat is RNG-heavy; RNG ordering is part of parity.

Authoritative draws include hit/dodge distributions, critical roll, body-part/weakpoint selection, coverage, block reaction, damage rolls, durability wear, weakpoint effects, projectile quality, slow-projectile dodge dice, malfunction/jam/fouling, burst effects, ammo effects, map impact and drop/embed chances.

Contract:

- all gameplay draws occur on server deterministic RNG streams/state covered by Spec 20;
- command validation, projection, rendering and save encoding consume no combat RNG;
- rejected stale commands consume no RNG unless the pinned rule defines the attempt itself as admitted/costed before failure;
- do not iterate hash/ECS collections in a way that changes shot/target/damage-unit RNG order;
- save/load at a safe boundary must preserve the next combat result for identical subsequent commands;
- exact-seed conformance tests are required for isolated pipelines; statistical tolerance tests are appropriate for broad hit distributions already tested statistically upstream.

## 15. Stable identity, ownership and persistence

Stable references:

- attackers/targets: stable Character/Creature/NPC IDs;
- weapons/armor/ammunition items: ItemUid plus authoritative item-location locator where externally referenced;
- content: typed string IDs;
- spatial targets: absolute simulation coordinates;
- vehicles/parts: stable IDs when struck.

Persistence must include all mutable combat state needed for continuation: HP/body state, effects, moves/stamina, recoil/aim state if baseline continuation requires it, block/dodge counters, martial-art transient effects, weapon/armor durability, ammunition/magazine contents, gun dirt/fault/heat state, embedded/dropped items and RNG state.

Do **not** persist socket IDs, targeting widgets, crosshairs, interpolation, client-predicted tracers or audio handles.

Ordinary atomic in-flight projectile resolution does not require persistence because save barriers occur between atomic mutations. A save request during a shot waits for the safe barrier; it must never serialize half a damage pipeline.

## 16. Dependencies and system boundaries

**Core/server infrastructure**

- deterministic command admission/order;
- canonical scheduling/move budget;
- stable entity/item references;
- authoritative spatial queries;
- RNG service/state;
- save barrier/transaction;
- per-player projection/event routing.

**OctoGhast.Cataclysm combat**

- damage/armor/weakpoint rules;
- melee hit/crit/technique/block/stamina/cost;
- ranged aim/dispersion/fire/recoil;
- gun malfunction/ammo mutation;
- throwing/projectile traversal/impact;
- combat-specific events/EOC context.

**Dependent feature specs**

- Character progression/effects owns deeper skill/stat/needs consequences;
- Monster/NPC specs own autonomous target selection and combat policy, but invoke this same resolver;
- Environment owns persistent explosions/fire/fields after combat creates them;
- Vehicles own vehicle-part damage beyond generic projectile collision hooks;
- Godot/UI owns targeting controls, animations, combat log rendering and effects only.

## 17. Validation and failure behavior

Definition load must diagnose/reject:

- unknown mandatory typed IDs;
- invalid damage-type references;
- malformed negative/invalid bounded fields where upstream validators reject them;
- weakpoint/martial-art/EOC references that fail registry resolution;
- incompatible gun/ammo definitions according to pinned checks.

Runtime command failures include: unauthorized actor, missing/dead target, out-of-range/reach, blocked line/invalid z-level, stale item location, unable-to-attack effect, invalid weapon/gun state, no compatible ammunition, insufficient required resource, and target/item removed by earlier deterministic contention.

Failures return structured authoritative results. They must not create phantom client-side damage. Whether moves/ammo are charged follows the exact pinned point at which the attempt becomes committed; pre-admission/stale authorization failures are free.

## 18. Black-box parity and conformance scenarios

### Shared damage

**C09-01 Damage-unit defaults** — load a unit with only `damage_type`; assert amount 0 and all multipliers 1, penetration 0.

**C09-02 Effective resistance** — fixed resistance 10, penetration 3, armor multiplier 0.5, constant armor multiplier 2; assert effective resistance = 7 before the two multipliers and final result = 7.

**C09-03 Multi-type independence** — apply bash + cut with distinct resistance/penetration and assert each mitigates independently and final dealt map preserves type totals.

**C09-04 Immunity** — apply an immune damage type; assert zero HP/pain contribution and no inappropriate post-damage mutation.

**C09-05 Damage EOC ordering** — fixture damage type with on-hit and on-damage EOCs; assert on-hit precedes mitigation/deal, on-damage receives pre/post values and executes before HP application exactly as pinned trace requires.

### Melee

**C09-06 Pinned hit probabilities** — reproduce `melee_test.cpp` statistical fixtures: baseline unarmed/plank/katana against zombie/manhack within upstream tolerances.

**C09-07 Incapacitated dodge** — capable Character can dodge statistically; sleeping/incapacitated fixture cannot, matching upstream tests.

**C09-08 Hit-spread size/immobile** — fixed RNG rolls verify `hit - dodge - sizePenalty`; add immobile flag and assert +40.

**C09-09 Critical factors** — fixed DEX/PER/skills/to-hit computes exact weapon/stat/skill/triple probability and double-crit branch threshold at `hitRoll > 1.5*dodge`.

**C09-10 Attack-cost floor** — extreme fast setup never costs <25 moves.

**C09-11 Attack cost modifiers** — fixed weapon attack time, melee skill, DEX, stamina at 100%, 25%, 0%, crouch/prone and MA modifiers; assert exact rounded pinned formula.

**C09-12 Negative move budget** — actor with insufficient current moves executes admitted melee attack, incurs pinned cost/negative budget, and becomes eligible again only through Spec 01 accrual; other actors continue.

**C09-13 Block reaction and depletion** — fixed block RNG, blocks_left=1 and sufficient stamina; first eligible hit consumes block, second cannot block until replenished.

**C09-14 Block mitigation** — known block score and mixed physical/electric damage assert flat bonus, logarithmic physical multiplier and eligible nonphysical one-fifth behavior.

**C09-15 Weapon/shield durability** — fixed RNG causes known wear; assert ItemUid remains stable if damaged and ownership updates atomically if destroyed.

**C09-16 Technique/counter** — force a technique and a block-counter; assert both use ordinary damage/cost/event pipelines and deterministic RNG order.

### Ranged/projectiles

**C09-17 Dispersion composition** — fixed gun, actor skills, recoil, ammo spread, underwater/vehicle states; assert individual sources and final range/multipliers match pinned `get_weapon_dispersion`/total dispersion.

**C09-18 Aim costs moves** — identical actor/gun/target starts at known recoil; spend N authoritative aim moves and assert recoil/penalty trajectory matches repeated `aim_per_move`, independent of render FPS.

**C09-19 Action operation time** — lever/single/pump/bolt fixtures at skill sums 0 and 10 assert 80/100/120/160 base and up-to-50% skill reduction.

**C09-20 Burst resource cap** — request burst larger than ammo; assert shots cap to feasible count, each committed shot consumes ammo once, and no negative ammo appears.

**C09-21 Mid-burst malfunction** — deterministic RNG fires first K shots then jams; assert earlier ammo/damage/events remain committed and later shots do not occur.

**C09-22 Dirt misfire** — fixed dirt/RNG validates the `dirt^3 / 1e12` branch and `NEVER_JAMS` bypass.

**C09-23 Slow projectile dodge** — speed <20 fixture with fixed dodge/dice adds clamped dodge contribution; same projectile at speed >=20 skips ordinary dodge.

**C09-24 Hit quality** — fixed `missed_by` and RNG vectors cross headshot/critical/good/standard/grazing bands and assert body part, damage multiplier and event class against pinned resolver.

**C09-25 NO_DAMAGE_SCALING/NOGIB** — assert first forces 1.0 hit scaling and second caps excessive overkill per pinned formula.

**C09-26 Projectile obstacle** — mirror `projectile_test.cpp` traversal fixture; assert obstacle mutation/stop/pass-through matches pinned behavior.

**C09-27 Liquid projectile** — mirror upstream liquid test; impermeable clothing prevents skin-contact effect/damage as applicable, permeable setup allows it.

**C09-28 Ranged statistical balance** — port representative unskilled/competent/expert scenarios from `ranged_balance_test.cpp`; compare hit-quality/range distributions within upstream tolerances, not one exact random stream.

**C09-29 Throw ownership** — throw stable ItemUid; assert inventory loses it exactly once and impact results in exactly one authoritative destination (embedded, dropped, destroyed, etc.).

### Events, persistence and multiplayer

**C09-30 Damage/weakpoint EOC context isolation** — two simultaneous attacks by different player actors trigger EOCs; assert each receives its own source/target context and no variables leak between them.

**C09-31 Audience projection** — A attacks hidden target visible to A but not B; assert B receives no forbidden target/HP/equipment state. A target visible to both can project only the allowed observable event.

**C09-32 Same-tick kill contention** — two actors attack one low-HP target in the same tick. Vary packet arrival while preserving admitted ordering key; assert identical first commit, death/event/RNG sequence and deterministic stale result for the later attack.

**C09-33 Same-item contention** — actor A transfers/destroys gun/ammo before actor B's fire command resolves; B revalidates stable item reference and fails without using stale client state.

**C09-34 Save/load next-shot determinism** — save at a safe boundary with fixed combat/RNG state; reload and fire. Assert shot result, ammo mutation, damage, weakpoint/effects and next RNG state equal uninterrupted run.

**C09-35 Disconnect during combat** — disconnect one player after an attack. Character HP/effects/cooldowns/move/stamina remain authoritative and progress under server policy; reconnect binds a new connection to the same durable player/Character identity.

**C09-36 One-player transport equivalence** — run identical melee/fire sequence through in-process single-player and loopback network transport; assert admitted commands, move costs, RNG trace and final authoritative state hashes match.

**C09-37 UI does not pause aiming/combat** — keep targeting/inventory UI open while another actor attacks. Canonical time and other actor combat continue unless explicit global pause policy is active.

## 19. Implementation sequencing

Recommended slices, all against this contract:

1. shared damage types/instances/resistance + deterministic damage application;
2. anatomy/armor/coverage/weakpoints;
3. melee hit/crit/damage/cost/stamina/wear;
4. block/dodge + techniques/martial-art hooks;
5. ranged dispersion/aim/fire/ammunition/recoil/malfunction;
6. projectile traversal/impact + throwing;
7. ammo effects/EOC/event projection;
8. multiplayer contention, persistence and differential/statistical harnesses.

Do not split these into incompatible duplicate damage pipelines.

## 20. Acceptance status for #74

- [x] Shared damage model, units, ordering and mitigation pipeline are explicit.
- [x] Melee hit/miss, crit, dodge/block, technique and stamina/move-cost formulas are captured.
- [x] Ranged dispersion/aim/trajectory/projectile/impact behavior is captured.
- [x] Armor coverage, body-part selection, penetration and damage-type interactions are specified.
- [x] Weakpoints, ammo effects and EOC/event hooks are mapped.
- [x] RNG distributions/order are documented sufficiently for statistical or deterministic conformance tests.
- [x] Item durability/ammo consumption and map/creature side effects are defined.
- [x] A suite of black-box combat vectors with expected outcomes/tolerances is specified.
- [x] Immutable definitions are separated from mutable runtime state.
- [x] Stable identity, ownership, persistence and save-boundary requirements are defined.
- [x] Continuous authoritative time, multi-player concurrency/contention and client projection adaptations are explicit.
- [x] No unresolved cross-cutting architecture decision was discovered; this spec consumes the existing Spec 01/04/12/17/20 contracts.

## 21. Source links

Pinned source:

- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/damage.h
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/damage.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/melee.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/creature.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/monster.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/ranged.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/projectile.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/weakpoint.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/json/damage_types.json
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/json/ammo_effects.json
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/json/martialarts.json
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/melee_test.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/ranged_balance_test.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/projectile_test.cpp
