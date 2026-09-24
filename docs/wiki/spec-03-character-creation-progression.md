# Spec 03 — Character creation and progression

Status: investigated / specification complete; re-evaluated 2026-09-24 against current Core/profile architecture  
Tracking issue: #68  
Parent epic: #65  
Reference implementation: `LambdaSix/Cataclysm-DDA` @ `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Purpose

This page defines the implementation-ready behavioral contract for starting-character construction and long-term Character progression. It preserves pinned-CDDA rules and data semantics while adapting ownership, scheduling and presentation to OctoGhast's authoritative continuously advancing server and multiplayer model.

This is a specification, not a requirement to reproduce CDDA's C++ class/UI structure.

## Architectural prerequisites

This spec consumes, without reopening, the contracts in:

- #52, #57, #58, #64 and #65: authoritative ECS/server, canonical time, action economy, spatial ownership and parity programme.
- Spec 01 / #66: 10 canonical ticks per world second; 100 moves per pinned-CDDA turn/second; deterministic elapsed-time processing.
- Spec 02 / #67: reusable Character state, no privileged-avatar assumption, owner/private/party/world projection boundaries.
- Spec 05 / #70 and Spec 06 / #71: authoritative item identity, inventory and transfer.
- Spec 17 / #82: authoritative EOC/talker execution and audience-filtered effects/messages.
- Spec 18 / #83: immutable definitions, typed IDs, JSON inheritance/finalization/validation.
- Spec 20 / #85: authoritative world persistence, stable world-local player identity distinct from connection and controlled entity, deterministic save barriers.
- #90: transport/session/projection ownership, bounded networking and deterministic request intake; progression systems do not own sockets or connection lifecycle.
- #91: unresolved cross-world player-profile/meta-progression persistence; Spec 03 consumes that future contract rather than defining account/profile storage locally.

## Authoritative pinned-CDDA evidence

Primary runtime/data anchors:

- `src/newcharacter.cpp`, `src/character_creator_ui.*`, `src/avatar.*`: creation flow and application of selected definitions.
- `src/scenario.*`, `data/json/scenarios.json`: scenario cost, allowed start locations, profession/hobby/trait restrictions, unlock requirements, missions, EOCs, start chronology and scenario world hooks.
- `src/start_location.*`, `data/json/start_locations.json`: typed start-location definitions, terrain match rules, city/z-level constraints and flags.
- `src/profession.*`, `src/profession_group.*`, `data/json/professions.json`: profession/hobby definitions, starting items, traits, skills, addictions, CBMs, proficiencies, recipes, martial arts, spells, missions, pets/vehicles and achievement gates.
- `src/skill.*`: practical/theoretical skill state, training, book knowledge and rust.
- `src/proficiency.*`: proficiency definitions, prerequisites, learning time and partial practice state.
- `src/mutation.*`, `src/character.cpp`: trait/mutation definitions, conflicts/prerequisites/replacements, categories, thresholds, activation and runtime effects.
- `src/bionics.*`, `src/character.cpp`: bionic definitions and per-installation state/UID, installation dependencies, activation, power and EOC/proficiency/martial-art integration.
- `src/martialarts.*`, `src/character_martial_arts.*`: styles, techniques, buffs, requirements and learned-style state.
- `src/magic.*`: spell knowledge/levels as an extension point and profession-granted starting spells.
- `src/achievement.*`, `src/stats_tracker.*`: event-derived statistics, achievement/conduct requirements, pending/completed/failed state and time constraints.

Behavioral test anchors include `tests/skill_test.cpp`, `tests/mutation_test.cpp`, `tests/bionics_test.cpp` and the creation/progression tests adjacent to the above systems in the pinned tree. Normative runtime tests outrank prose when they disagree.

## 1. Definition data versus runtime state

All scenario, start-location, profession/hobby, skill, proficiency, mutation/category, bionic, martial-art/technique/buff, spell and achievement definitions are immutable registry data after Spec 18 finalization. Runtime state stores stable typed IDs plus mutable values; it must not copy definition objects into saves.

Mutable Character progression state includes at minimum:

- selected creation provenance: scenario, profession, hobbies/backgrounds and selected start-location ID;
- base stats and chosen starting traits/variants;
- skill practical level/XP, knowledge level/XP, rust accumulator and last-practiced canonical time;
- known and partially learned proficiencies, including practiced duration and fractional/remainder state;
- owned mutations/variants, activation state, category strength/progress and mutation-specific cooldown/resource state;
- installed bionic instances, each with stable Character-local UID, type ID and activation/charge/incapacitation/runtime fields required by its definition;
- known martial-art styles and style-specific runtime state that is not derivable;
- known spells and their mutable level/XP/state where the spell subsystem is enabled;
- achievement/conduct/stat state and completion/failure timestamps;
- starting unlock/provenance state needed to reproduce or audit a created Character.

## 2. Character-creation request and authority

### Pinned CDDA reference behaviour

CDDA presents an interactive creation UI which chooses scenario/start location, profession, hobbies/backgrounds, stats, traits and skills subject to data-driven restrictions, point/cost mode and achievement unlocks. Selected definitions then seed Character state and world-start hooks.

### OctoGhast adaptation

Creation is an authoritative server transaction. A client sends a `CreateCharacterIntent` containing stable definition IDs and explicit choices. The server validates the entire request against the currently loaded definition set and player unlock context, derives all starting state, allocates the authoritative Character identity, and commits atomically. The client never sends derived HP, inventory instances, bionic UIDs, calculated skills, mutation category strength or world placement as authoritative values.

Single-player uses exactly the same logical request/validation path through the in-process transport. Opening or navigating creation UI does not pause an already-running multiplayer world.

### Validation contract

A creation request is rejected without partial Character/world mutation when any of these apply:

- referenced IDs do not exist or are obsolete/invalid for creation;
- scenario has no valid allowed start location, or chosen location is not allowed/available under its terrain/city/z-level constraints;
- profession/hobby is blacklisted, excluded, not permitted by scenario, or fails a hard/unlock requirement;
- selected trait is not a starting trait, is forbidden/locked inconsistently, violates conflicts/prerequisites, or is disallowed for the target actor type;
- point/cost constraints for the selected creation mode are not satisfied;
- a required starting item/CBM/proficiency/style/spell/mission reference fails finalized registry validation;
- a random choice cannot produce a valid candidate.

Validation errors are structured and player-private. Retrying a rejected request must not consume simulation RNG or leave generated entities/items behind.

## 3. Creation pipeline and ordering

The authoritative commit pipeline is:

1. Resolve and validate immutable scenario, profession, hobby/background, start-location and explicit user selections.
2. Resolve random selections using the server's deterministic creation RNG stream; record enough RNG state for deterministic continuation/replay.
3. Allocate stable player-controlled Character identity independently of connection/socket identity.
4. Apply base demographic/stat choices and Character anatomy defaults.
5. Apply locked/selected traits and variants; enforce conflict/prerequisite/replacement rules before deriving mutation/category state.
6. Apply profession/hobby skill grants. Where multiple creation sources grant the same skill, preserve pinned-CDDA composition semantics rather than blindly summing; profession/hobby bonuses observed by creation are resolved as the baseline does.
7. Grant starting proficiencies, recipes, martial arts and spell knowledge.
8. Install starting CBMs through the authoritative bionic state path so UIDs, capacity and dependent grants are valid.
9. Materialize starting items using authoritative item creation/inventory rules, including trait-based substitutions and gender/variant data where applicable.
10. Apply starting addictions and other Character-local seeded state.
11. Establish scenario chronology/world-start parameters and resolve the chosen start location through the world/map system.
12. Create scenario/profession starting missions, pets/vehicle and other world entities where configured.
13. Execute scenario/profession EOCs through Spec 17 with the new Character as the correct talker/context.
14. Recalculate derived Character state/caches after all grants.
15. Atomically publish the Character/world commit and project only the owning player's creation result plus normally visible world state.

No client may observe a half-created Character.

## 4. Scenario and start-location contract

A scenario definition has a stable ID and may define: point cost, allowed start locations, profession blacklist/additions/whitelist, hobby restrictions, allowed/forced/forbidden traits, flags, map extra, missions, achievement requirement/hard requirement, reveal/visibility settings, EOCs, origin offset, starting vehicle, surround monster groups and default cataclysm/game start chronology.

Pinned baseline validation requires at least one `allowed_locs` entry. A hard requirement without a requirement is invalid.

A start location has stable ID, translated name, one or more terrain match targets and optional city-size, city-distance, z-level and flags. Exact terrain IDs must resolve; prefix/type/subtype/contains match definitions must match at least one finalized overmap terrain. Location selection is a world query, not client-side map placement.

In multiplayer, a newly joining player's start scenario must not reset global world chronology. Scenario-defined start chronology is world-creation policy only. Character creation into an existing world uses that world's canonical chronology and an allowed spawn policy/location. This is an OctoGhast adaptation of CDDA's single-avatar new-game assumption.

## 5. Profession, hobby and starting grants

Profession/hobby definitions are immutable templates. Relevant fields include point cost, starting cash, item groups/legacy item lists, achievement requirements, addictions, CBMs, proficiencies, recipes, traits/variants, martial arts, martial-art choices, forbidden traits, pets, hobby exclusions/whitelists, vehicle, spells, EOCs, skills, missions, age bounds and subtype.

Starting grants must be applied by domain APIs, not by direct client mutation. Item grants create authoritative item instances; CBM grants create installed bionic instances; proficiency/style/spell/recipe grants update stable-ID Character knowledge state.

Profession item substitutions based on traits are evaluated after the effective starting trait set is known.

## 6. Skills: practical experience, knowledge and rust

Each non-contextual skill has separate practical and theoretical state:

`practical_level, practical_exercise, knowledge_level, knowledge_experience, rust_accumulator, last_practiced`.

Contextual skills cannot be directly assigned as ordinary Character skill entries; they resolve through their context.

### Practical training

Pinned `SkillLevel::train(amount, catchup_modifier, knowledge_modifier, allow_multilevel)` rejects negative XP. Let:

`level_gap = max(knowledge_level,1) / max(practical_level,1)`.

If knowledge is ahead, practical catch-up XP is `amount * catchup_modifier * level_gap`. If levels match but knowledge XP is ahead of practical XP, catch-up and knowledge modifiers taper according to the baseline formula. Otherwise both begin from `amount`. Knowledge gain is capped to at most 90% of practical catch-up gain during practical training. `SKILL_TRAINING_SPEED` scales both when positive.

Practical level threshold from level L is:

`10000 * (L + 1)^2` exercise.

Crossing a threshold increments practical level; unless multilevel training is explicitly allowed, excess exercise is discarded. Practical level cannot outrun knowledge: if it does, knowledge level is raised to match and knowledge XP resets.

### Theoretical training

`knowledge_train` scales gain down as the gap between theory and practice grows, or when an NPC teacher greatly exceeds the learner. The level multiplier is `2 / (gap + 1)`. Knowledge level advances at:

`10000 * (knowledge_level + 1)^2`.

Recipe/requirement checks that explicitly use theoretical knowledge must use knowledge level, not practical level.

### Rust

Rust is elapsed-world-time behaviour, not wall-clock behaviour. There is a 24-hour canonical-time grace period after practice. Max-level skills do not rust in the pinned path. Rust affects practical exercise, not theoretical knowledge. Baseline rust uses a level multiplier `(level+1)^2`, accumulated-rust slowdown, configured rust multiplier and rust resistance; it runs on the baseline daily cadence. The pinned test establishes approximately 1% practical loss per day after the one-day grace period for the tested default level-2 case, while knowledge remains level 2.

OctoGhast schedules rust from authoritative canonical time. Disconnecting, closing UI, render slowdown or being outside a player's active region does not freeze the clock. Catch-up must be deterministic and equivalent to the required elapsed rust intervals.

## 7. Proficiencies

A proficiency definition includes stable ID/category, `can_learn`, `teachable`, `ignore_focus`, default time multiplier, skill/failure penalties, weakpoint modifiers, `time_to_learn`, required proficiencies and typed bonuses. Defaults in the pinned code include time multiplier 2.0, skill penalty 1.0, weakpoint bonus/penalty 0, and time-to-learn 9999 hours unless data overrides them.

Runtime proficiency state is:

- set of known proficiency IDs;
- partial-learning entries `{ id, practiced_duration, remainder }`.

Practice may only progress learnable proficiencies according to configured training speed and prerequisite rules. Completion promotes the ID to known and removes partial state. The remainder must be persisted so repeated short practice is not lost through duration rounding.

Practice is charged from authoritative activity/action progression. A client cannot claim elapsed proficiency time. Background/disconnected progression occurs only if the authoritative actor/activity policy says that work actually continued.

## 8. Traits and mutations

Trait/mutation definitions are immutable typed data. Runtime ownership is per Character.

Definitions may declare starting-trait eligibility, point value, profession/debug/vanity/dummy/threshold flags, variants, activation capability, starts-active, resource costs and cooldown, categories, prerequisites, replacements, additions, cancellations/conflicts, threshold requirements/substitutes, mutation EOCs/effects and granted modifiers/qualities.

### Acquisition invariant

An acquisition request is resolved server-side against the Character's current authoritative mutation set. The resolver must preserve baseline prerequisite, replacement and conflict cancellation ordering. Failed acquisition leaves state unchanged. Successful acquisition updates category strength and all derived Character capabilities before projection.

Category strength is derived from owned mutations' category membership. The pinned mutation tests establish that shared mutations contribute to every category they belong to and ties do not create a unique highest category. Threshold mutations are not ordinary freely selectable mutations and must obey category/threshold rules.

### Activation

Activated mutations have per-Character active state. Activation/deactivation validates resources, cooldown and trigger conditions, consumes baseline move/resource costs and runs resulting effects/EOCs authoritatively. Reflex/conditional activation is evaluated from authoritative Character/world context. Client UI only requests toggles and receives owner-visible state/messages.

## 9. Bionics / CBMs

A bionic type definition may specify activation/deactivation/periodic/trigger power costs, capacity, fake spell/weapon, upgrade and required bionic, installation requirement, fuels, activation/processed/deactivation EOCs, enchantments, martial arts, granted proficiencies, pseudo-items, canceled mutations, included/incompatible bionics and other body/operation constraints.

Each installed bionic is a runtime instance with a stable Character-local UID. Duplicate bionic types may therefore coexist where the baseline permits; type ID alone is not a sufficient runtime reference. New UIDs must be deterministic and collision-free across save/load. The pinned bionic test verifies allocation after existing UIDs.

Installation/uninstallation is an authoritative operation/activity. It validates required/incompatible/upgrade relationships and installation requirements against current state, then applies/removes capacity and dependent grants in baseline order. Removing capacity clamps current power if it exceeds the new maximum; the pinned test verifies this.

Activation/deactivation targets a bionic UID, validates power/state and other preconditions, applies power costs, pseudo/fake weapon/spell state and EOCs atomically, and emits audience-filtered messages/events. Concurrent requests for the same bionic are deterministically ordered by the common command scheduler; stale second requests are rejected/re-evaluated against post-first state.

## 10. Martial arts and magic extension points

Known martial-art styles are stable IDs on the Character. Styles expose data-driven techniques, buffs and requirements; techniques may require Character/equipment state, carry weighted/proc behaviour, apply effects/bonuses and invoke EOCs. Learning a style is authoritative and idempotent unless baseline data explicitly supports another state.

Creation may grant fixed styles or require a choice from a profession-defined set. The server validates the selected choice before commit.

Magic is an extension point rather than a reason to hard-code spell logic into Character creation. Professions may grant `spell_id -> starting level`; the spell subsystem owns mutable spell level/XP/cost/cooldown semantics. Character creation only validates and applies the configured starting knowledge through that API.

## 11. Achievements, conducts and statistics

Pinned CDDA statistics are derived from subscribed gameplay events. Achievements declare requirements over tracked stats, optional time bounds, hidden-by relationships, manual/EOC granting and whether the entry is a conduct. Runtime achievement state is pending, completed or failed; terminal state stores the canonical time of transition and final requirement values.

Invariant: every currently valid achievement is represented either by an active watcher while pending or a terminal stored status after completion/failure.

A conduct is evaluated by the same requirement machinery but represents maintaining a condition; violations can transition it to failed. Time-bounded requirements use simulation chronology, never client wall time.

### OctoGhast scope

Gameplay statistics and character-run achievements/conducts are authoritative and scoped to the stable player/Character run that generated their events; they are not inferred from client telemetry. Multiplayer events are routed only to trackers whose documented subject/ownership matches the event; one player's action must not accidentally complete another player's personal requirement.

Pinned CDDA scenario/profession unlock requirements are **cross-run meta-progression**: `scenario::can_pick()` and `profession::can_pick()` query `past_achievements_info`, whose loader reads completed achievements from the user achievement directory and legacy past-game/memorial data. Therefore these unlocks are not ordinary current-world Character state.

OctoGhast must keep the unlock check server-authoritative, but the durable owner of cross-world profile/meta-progression is deliberately deferred to #91. Until #91 is resolved, Spec 03 requires an abstract authoritative eligibility/profile service and must not serialize profile unlock history into a Character, socket/session object, Godot client cache, or world snapshot merely as a local convenience.

## 12. Commands, activities, queries and events

Synchronous queries: list valid scenarios/professions/hobbies/traits/start locations for a player; inspect skill/proficiency/mutation/bionic/style progress; validate a draft creation request. Queries do not mutate or consume RNG.

Commands/intents: submit character creation; request mutation/bionic/style activation where immediate; choose a creation option; request a learn/install action.

Activities: reading/study, training, proficiency practice, CBM installation/removal and other long-running progression work. Activities are actor-owned and advance from canonical simulation time/action budget under Spec 01/#69 contracts.

Events/messages: skill/proficiency learned, mutation gained/lost/activated, bionic installed/removed/activated, achievement completed/failed and creation validation/result. Domain events are authoritative; client messages are projections with explicit audience.

## 13. Determinism, RNG and concurrency

All random creation selections, mutation rolls/variant selection, training rolls and other progression RNG use server-owned deterministic streams/state covered by Spec 20. Rejected validation-only requests consume no RNG.

For equal-tick commands affecting the same Character or shared prerequisite/resource, use Spec 01 stable ordering. Resolve the first command, then validate the next against updated state. Never merge two client-side assumptions.

Progression that is mathematically elapsed-time based may catch up in batches only when the result and RNG consumption are equivalent to baseline-required interval processing. Systems with per-interval random rolls or event ordering must preserve those deterministic steps.

## 14. Persistence and reconnect

Persist all mutable state listed in section 1 plus canonical timestamps/deadlines and RNG state needed for continuation. Definition IDs are rebound through finalized registries on load; missing/obsolete IDs follow Spec 18 migration/diagnostic policy.

Disconnect does not convert a Character into client-owned or frozen state. Ongoing activity/progression follows the authoritative disconnected-character policy from Spec 20. Reconnect by the same stable player identity receives a fresh owner projection of the persisted Character; no skill/proficiency/bionic/mutation state is reconstructed from stale client DTOs.

## 15. Client projection

The Godot client receives only state needed for its player's UI and visible gameplay. Owner-private creation choices, exact XP/proficiency progress, mutation resources, bionic power/UIDs and personal achievement/conduct state are owner-visible unless another gameplay rule makes a subset observable.

Other clients may receive externally observable consequences (appearance-changing mutation, visible bionic weapon, combat style animation/event) without receiving private progression internals. Registry definition metadata needed to render menus may be distributed as immutable content/catalog data; that does not grant mutation authority.

## 16. Failure behaviour

- Invalid/stale IDs: reject with typed validation error; no partial mutation.
- Unlock/point/prerequisite failure: reject and identify the failed rule to the owning client.
- Activity interrupted: preserve only progress the baseline system actually preserves; do not award future elapsed work.
- Missing proficiency prerequisite: no completion/promotion.
- Insufficient mutation/bionic activation resource: no state toggle or side effect.
- Bionic UID not owned by actor: reject as stale/invalid reference.
- Concurrent state change: revalidate at authoritative resolution time and reject if no longer legal.
- Save/load missing required definition: migration/diagnostic policy, never silently substitute an unrelated ID.

## 17. Conformance and parity scenarios

1. **Creation happy path:** fixed scenario/profession/hobbies/stats/traits/skills produces the same baseline-derived starting skills, traits, items, CBMs, proficiencies, styles, spells, missions and chronology-sensitive hooks for a fixed data/RNG fixture.
2. **Creation atomic rejection:** forbidden trait or locked profession rejects the request and creates no Character/items/world hooks; RNG state is unchanged.
3. **Random creation replay:** same definitions, seed and request produce identical random choices and starting runtime IDs/order.
4. **Existing multiplayer world join:** creating player B does not reset global chronology or pause player A; B receives a valid server-selected start placement.
5. **Projection isolation:** player A's creation draft, exact XP, private mutations/bionics and achievements are absent from player B's DTO unless explicitly observable.
6. **Skill threshold:** practical exercise advances at `10000*(L+1)^2`; knowledge cannot remain below a practical level that overtakes it.
7. **Theory/practice separation:** theoretical book/teacher progress can exceed practical skill; a theory-based recipe requirement uses knowledge level.
8. **Skill rust:** after less than 24h since practice no rust occurs; after baseline daily processing practical XP decreases while knowledge remains unchanged; disconnect/reconnect gives the same result as continuously connected canonical time.
9. **Renderer independence:** identical canonical ticks and training inputs produce identical skill/rust state at 30 FPS, 144 FPS and headless.
10. **Proficiency short increments:** repeated sub-second/fractional practice retains remainder and reaches the same learned time as an equivalent continuous practice period.
11. **Proficiency prerequisites:** completion is blocked until required proficiency IDs are known; learning promotes partial state to known exactly once.
12. **Mutation category sharing:** shared mutations raise all listed category strengths; tied highest categories remain tied as in pinned mutation tests.
13. **Mutation conflict transaction:** gaining a mutation that replaces/cancels another produces the baseline final set and derived state atomically.
14. **Mutation activation contention:** two same-tick toggle intents are stably ordered; the second sees the first result and cannot double-charge or duplicate EOCs.
15. **Bionic UID persistence:** multiple same-type CBMs retain distinct UIDs through save/load; a newly installed CBM receives a non-colliding next UID.
16. **Bionic capacity removal:** uninstalling capacity reduces max power and clamps current power only when it exceeds the new maximum.
17. **Bionic dependency failure:** required/incompatible/installation requirement failure leaves bionic, items, power and granted state unchanged.
18. **Bionic EOC authority:** activation EOCs execute on the server with the correct Character talker; only audience-appropriate results are projected.
19. **Martial-art creation choice:** only a style in the profession's allowed choice set can be selected; learned style survives save/load.
20. **Achievement terminal state:** event-derived requirement completion stores completion/failure plus canonical transition time/final values and survives reconnect/save-load.
21. **Achievement isolation:** a personal event for player A does not satisfy player B's tracker; world/global achievements, if defined later, must be explicitly scoped rather than inferred.
22. **Creation EOC ordering:** scenario/profession EOCs observe the fully seeded Character grants required by their baseline ordering and cannot expose half-created state to clients.
23. **Stale client progression:** a reconnecting client cannot overwrite newer authoritative XP/proficiency/bionic state with cached DTO values.
24. **Save round trip:** a Character with partial skill XP/rust timer, partial proficiency, active mutation, duplicate bionics, known style/spell and pending/completed achievements round-trips with identical subsequent deterministic behaviour.

## 18. Implementation boundary and dependencies

This spec defines progression state and rules but does not implement combat, crafting, activity internals, world generation or UI. Consumers/producers are:

- Character physiology/derived capability: Spec 02.
- Time/action/activity scheduling: Spec 01 and #69.
- Items/inventory/CBM item consumption: Specs 05/06.
- EOCs/talkers: Spec 17.
- Definitions/IDs/inheritance/validation: Spec 18.
- Persistence/reconnect: Spec 20.
- Map/start placement and active regions: Spec 12/#77.
- Crafting recipes/requirements, combat, NPC teaching and full magic semantics: their dependent feature specs.
- Godot UI: renders definition catalogs and projected mutable state; never applies grants directly.

No new cross-cutting architecture decision is required by this investigation. The multiplayer adaptations above follow the already-set server authority, stable-player identity, canonical-time, projection and deterministic-ordering contracts.


## 19. 2026-09-24 re-evaluation: reference profile, generic Core and future evolution

This re-evaluation preserves the pinned-CDDA evidence above while making the architectural boundary explicit. CDDA reference parity is a completeness benchmark and major waypoint, not a permanent constraint on generic Core.

### 19.1 Pinned CDDA reference behaviour

The Cataclysm reference profile must reproduce the pinned baseline's externally visible rules, including:

- scenario/profession/hobby/start-location schemas, availability restrictions, achievement requirements and hard requirements;
- Cataclysm character-creation cost/point modes and trait/skill/profession composition rules;
- grid/overmap-based start-location selection and scenario world-start hooks;
- practical/theoretical skill XP thresholds, catch-up/knowledge ordering, 24-hour rust grace and rust cadence;
- proficiency prerequisite, practiced-duration and fractional-remainder semantics;
- mutation acquisition/conflict/category/threshold and activation rules;
- bionic type versus installed-instance identity, UID-sensitive operations, capacity/dependency rules and EOC integration;
- Cataclysm martial-art, spell-grant, achievement/conduct/stat-tracker semantics;
- cross-run achievement-based scenario/profession eligibility, including `META_PROGRESS` and hard-requirement behaviour.

These are profile rules. They are not evidence that every future ruleset hosted by OctoGhast must use Cataclysm points, skills, mutations, bionics, grid spawn rules or achievement gates.

### 19.2 OctoGhast adaptation

The following are intentional architectural adaptations rather than claims about upstream CDDA:

- character creation is an atomic authoritative server transaction instead of a local UI mutating an avatar object;
- existing-world joins cannot reset the shared world clock or implicitly replace global scenario chronology;
- creation and progression requests enter through the same transport-neutral command/query boundary in one-player and co-op servers;
- several player-controlled Characters may progress concurrently, with deterministic contention and actor-scoped state;
- progression continues or catches up according to authoritative canonical time and activity/background policy, not renderer frames, open menus or connection lifetime;
- Godot receives player-specific projected state/catalogs and never receives arbitrary ECS components or mutable registry objects;
- connection/session identity is never progression identity or persistence identity;
- server-owned world saves follow Spec 20, while cross-world profile/meta-progression ownership is explicitly separated into #91.

### 19.3 Generic Core contract

Generic Core should provide reusable capabilities, not a universal Cataclysm character model:

- immutable typed definition registries and validation;
- stable runtime entity/reference identity primitives;
- atomic authoritative command/transaction execution;
- deterministic fixed-step scheduling and profile-defined action/time conversion;
- durable-work/activity hooks;
- deterministic RNG streams and replay/save continuation;
- persistence abstractions with explicit ownership;
- event/statistic observation primitives;
- capability/knowledge/progression state containers that rules profiles may compose;
- projection/audience filtering and transport-neutral request/response contracts.

Cataclysm-specific skill threshold formulas, mutation categories, bionic schemas, martial-art definitions, character-creation point accounting and achievement-gated start options belong in the Cataclysm profile unless a later architecture decision deliberately generalises a smaller reusable primitive.

### 19.4 Future evolution seams

A future OctoGhast rules profile may, without replacing Core infrastructure:

- use different creation currencies or no point system;
- use continuous or non-grid start placement while still consuming Core spatial identity/index contracts;
- replace practical/theoretical skills with a different advancement model;
- support several simultaneous durable work/progression channels if its rules allow them;
- replace Cataclysm mutation/bionic concepts with other capability graphs;
- use account-, campaign-, server- or world-scoped unlock policy;
- evolve action economy/time mapping independently of the Cataclysm 100-move-per-second profile.

Such evolution is not a renderer-only change when it alters rules. It requires a new/changed rules profile while retaining the same server authority, identity, persistence, deterministic scheduling and projection boundaries.

### 19.5 Networking and identity contract

#90 owns transport/session mechanics. Spec 03 contributes only typed semantic operations such as creation draft queries, `CreateCharacterIntent`, progression commands/activities and owner-specific projections.

Network timing cannot choose mutation/progression order. Requests are admitted at deterministic simulation boundaries; same-state contention is resolved by the shared command ordering rules. Slow-client backpressure, reconnect and parser/framing behaviour must not alter progression outcomes.

Cross-world unlock/profile identity is not the same as world-local `PlayerId`, `CharacterId` or connection identity. The exact profile/account ownership contract is intentionally centralized in #91.

### 19.6 Additional conformance scenarios

25. **Core/profile dependency isolation:** a headless Core fixture can exercise definition loading, authoritative commands, deterministic scheduling, persistence and projection without registering Cataclysm skill/mutation/bionic/creation schemas.
26. **Cataclysm profile mapping:** loading the Cataclysm profile enables the pinned creation/progression formulas and schemas without changing Core timing, transport or persistence APIs.
27. **Alternative creation policy seam:** a test rules profile can create a Character without Cataclysm point accounting or achievement gates while using the same atomic server transaction and identity allocation path.
28. **Alternative spatial-start seam:** a non-Cataclysm test profile may supply a different deterministic start-placement policy without changing `CreateCharacterIntent`, connection/session ownership or ECS identity.
29. **Transport equivalence:** the same creation/progression request sequence through in-process and loopback transports produces identical authoritative state, RNG consumption and domain events.
30. **Observer invariance:** adding an uninvolved connected player/client does not change skill/proficiency/mutation/bionic/achievement outcomes or RNG consumption for another Character.
31. **World-save/profile separation:** loading or rolling back an older world snapshot cannot silently roll back cross-world unlock/profile state; this scenario is blocked on #91 for its concrete storage/provider contract but is normative at the boundary.
32. **Meta-progression policy fixture:** for fixed completed-achievement input, Cataclysm scenario/profession eligibility reproduces pinned `META_PROGRESS` and hard-requirement semantics; changing server/profile policy cannot be implemented by trusting client-reported unlock flags.

### 19.7 Re-evaluation resolution

No previously settled time, spatial, command/activity, persistence, EOC or networking decision is reopened. The original #68 evidence remains valid. The re-evaluation discovered one genuinely cross-cutting ownership gap—cross-world player profile/meta-progression persistence—and created #91 as its single authoritative home. Spec 03 is otherwise implementation-ready and can remain closed; implementation of profile-backed unlock persistence must consume #91 once that architecture is resolved.
