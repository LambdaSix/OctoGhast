# Spec 07 — Crafting, recipes, requirements and disassembly

**Tracking issue:** #72  
**Parent epic:** #65  
**Programme:** #64  
**Reference baseline:** `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`  
**Status:** investigation complete / implementation-ready specification

## 1. Purpose and parity boundary

This specification defines the authoritative OctoGhast contract for recipe definitions, requirement resolution, crafting, batch crafting, practice recipes, recipe knowledge, staged/step recipes, in-progress craft state, failure, completion products/byproducts, disassembly/uncraft, salvage-facing extension points, and crafting-triggered EOCs.

The behavioural oracle is the pinned CDDA baseline above. OctoGhast does **not** reproduce the C++ object/UI model. It preserves the observable rules and data semantics while applying the established architecture from #52, #57/#66, #58/#77, #67, #69, #70, #71, #82, #83 and #85:

- the server owns recipe registries, authoritative Characters, item identities/locations, activities, canonical time and RNG;
- single-player uses the same command/query/result boundary as co-op;
- crafting time remains CDDA move/work currency mapped onto canonical simulation time, never wall-clock milliseconds;
- clients choose recipes/resources/targets through projected state but cannot consume components, reserve tools, advance progress or create results directly;
- multiple Characters may craft concurrently, including in overlapping active regions;
- stale or contended resources are resolved deterministically and fail closed;
- Godot/UI state is presentation only.

Where pinned CDDA couples crafting to the local avatar, blocking menus, or turn-gated input, this document separates the **pinned CDDA reference behaviour**, the **OctoGhast adaptation**, and the resulting **implementation contract**.

### 1.1 Reference profile versus reusable platform

The 2026-09-24 re-evaluation against #52, #57, #58, #64 and #65 makes the Core/profile boundary explicit:

1. **Pinned CDDA reference behaviour** is the evidence oracle at `e262adb299a7613b4aedc5f12c08fe0413c56a84`: recipe schemas, requirement logic, move/work formulas, proficiency and assistant effects, craft/disassembly lifecycle, learning, products/byproducts and result EOCs.
2. **OctoGhast Cataclysm profile** owns those CDDA-specific rules and content semantics. In particular, CDDA recipe JSON, `RecipeId`/`RequirementId` registries, the 100-moves-per-world-second action economy, grid/reachability interpretations inherited from the Cataclysm map profile, CDDA proficiency/batch formulas, and CDDA uncraft/recovery policy are profile rules rather than universal engine laws.
3. **Generic Core/server capability** supplies deterministic commands, activities, canonical fixed-step scheduling, stable identities/references, immutable-definition versus mutable-instance separation, authoritative resource mutation, persistence hooks, RNG services, spatial/resource-query abstractions, and player-specific projection. Core MUST NOT require that every future ruleset use CDDA recipe fields, CDDA move currency, Cataclysm skill/proficiency semantics, item-based craft instances, or CDDA's exact batch/disassembly formulas.
4. **Future evolution seams** include alternate action currencies, continuous-space work reachability, different production graphs or job systems, non-item craft/work-order representations, different reservation policies, asynchronous industrial production, and different learning/failure models. Such changes may define a new rules profile without replacing Core's authority/time/identity/persistence/projection contracts.

Reference parity remains mandatory for the Cataclysm profile; this classification prevents parity-specific constraints from becoming accidental permanent Core invariants.

## 2. Authoritative baseline evidence

Primary pinned source anchors:

- `src/crafting.cpp` — craft eligibility, speed multipliers, crafting inventory, in-progress craft lifecycle, reservation/use of tools and components, progress/failure, completion, byproducts, recipe learning and disassembly.
- `src/character_crafting.cpp` — learned recipes, available recipes from books/devices/groups and skill requirement checks.
- `src/recipe.h`, `src/recipe.cpp` — recipe/step schema, loading, validation/finalization, time/proficiency/batch calculations, results/byproducts, autolearn and requirement aggregation.
- `src/requirements.h`, `src/requirements.cpp` — tool, quality and component requirement groups, multiplication, alternatives, availability and diagnostics.
- `src/craft_command.h`, `src/craft_command.cpp` — selected recipe/batch/resource choices, preflight and creation of the in-progress craft.
- `src/recipe_dictionary.*` — recipe registries, uncraft lookup and finalization.
- `src/craft_reservation*.*` — baseline resource reservation/binding used by staged crafting.
- `tests/crafting_test.cpp` — craftability, component/tool selection, power/tool sharing, byproducts, containers, learning, timing and regression fixtures.
- `tests/recipe_test.cpp` — recipe loading/finalization and requirement aggregation fixtures.
- `tests/requirements_test.cpp` — alternative/overlapping requirement semantics, extension and actor-aware requirement checks.
- core recipe/requirement/uncraft JSON under `data/json/` and test data under `data/mods/TEST_DATA/`.

Important pinned behaviours evidenced directly in source/tests and made normative below include:

- step recipes reject root-level stepless-only fields such as `time`, `tools`, `qualities`, `proficiencies`, `batch_time_factors` and `activity_level`;
- requirement groups are AND across groups and OR within each group;
- overlapping requirements are deduplicated/expanded so one finite item/resource pool cannot satisfy incompatible simultaneous uses by accidental double-counting;
- batch time supports non-linear scaling and assistant reductions but never falls below the effective single-item time;
- missing proficiencies multiply recipe/step time rather than replacing base time;
- crafting consumes selected components into an in-progress craft representation and retains component provenance where required for reversibility/food behaviour;
- completion may produce multiple results, charge-counted merged results, byproducts, morale effects, learning rolls and `result_eocs`;
- disassembly uses an uncraft recipe, ignores proficiency time malus for duration, consumes required tool charges, removes the source item and recovers actual stored components when available.

## 3. Definition data versus runtime state

### 3.1 Immutable definition data

All loaded crafting content is immutable registry data after Spec 18 finalization.

A `RecipeDefinition` is identified by stable typed `RecipeId`. It contains, as applicable:

- recipe kind: normal recipe, practice recipe, nested/category helper, or uncraft/disassembly definition;
- result item definition ID, result count/multiplier/charges and result container/sealed/container-variant policy;
- category/subcategory and presentation metadata;
- base difficulty, primary skill and additional required skills;
- Character stat/attribute requirements where declared;
- base time or ordered step definitions;
- activity/exertion classification;
- batch scaling policy;
- required/optional proficiencies and their time/skill effects;
- component, tool and quality requirement data, including external `using` requirement references;
- recipe flags and component filters;
- autolearn/never-learn/book/reference/disassembly learning metadata;
- byproducts/byproduct groups;
- reversibility/uncraft relationship;
- result EOC IDs;
- result heating/raw-removal and other completion flags;
- practice-specific data and skill cap;
- ordered staged/step data where present.

A `RecipeStepDefinition` is ordered immutable data containing at least:

- positive base step time;
- exertion/activity level;
- its batch-scaling policy;
- step proficiencies;
- external requirement references;
- inline tool/quality requirements;
- attention/passive-work policy and any declared environment/tool behaviour.

Per the pinned loader, step recipes MUST NOT simultaneously use root fields that are defined only for stepless recipes. Invalid combinations are content-load errors, not runtime guesses.

A `RequirementDefinition` is immutable registry data identified by stable `RequirementId` when named. It consists of:

- component groups: AND between groups; OR among alternatives inside one group;
- tool groups: same group semantics; tool count sign/shape distinguishes presence-only versus charged use according to baseline data rules;
- quality groups: required quality ID, minimum level and count;
- referenced requirements combined with their multipliers.

Inheritance, `copy-from`, extension/deletion and load-order semantics belong to Spec 18 and MUST be applied before crafting finalization.

### 3.2 Mutable authoritative runtime state

Runtime crafting state is not stored by mutating recipe definitions.

A craft instance MUST carry enough authoritative state to continue identically after interruption/save-load, including:

- stable craft item/entity UID;
- recipe ID and batch size;
- creator/crafter Character ID where semantically required;
- current normalized progress/work counter;
- current step index for staged recipes;
- selected/consumed component provenance;
- reserved/bound resource decisions needed for deterministic continuation;
- remaining or scheduled tool-charge debits where the baseline spreads consumption;
- failure-point/defect/progress-loss state already rolled or scheduled;
- selected attention/passive-work plan;
- absolute canonical due/deadline/check times for passive steps;
- cached values only when the baseline semantics require snapshotting rather than live recomputation;
- result-placement/work-location identity;
- any deterministic RNG stream/counter state required by Spec 20.

Client recipe lists, filters, menu selection, highlighted alternatives, confirmation popups and progress animations are not authoritative state.

## 4. Recipe validation and finalization

After Spec 18 inheritance/merge, recipe finalization MUST:

1. resolve result item, skills, proficiencies, requirement IDs, categories, EOCs, byproduct IDs and other typed references;
2. reject invalid step/stepless field combinations;
3. reject non-positive step time;
4. reject invalid batch scaling, including a linear setup offset greater than the corresponding recipe/step time;
5. build aggregated root/step requirements;
6. build deduplicated requirement alternatives so overlapping component choices are feasible against one finite resource pool;
7. derive autolearn requirements where the simplified boolean form delegates to skill requirements;
8. establish reversible/uncraft relationships and diagnose invalid/missing targets;
9. validate result/container compatibility and content references;
10. expose deterministic diagnostics through Spec 18.

Missing or invalid referenced definitions MUST NOT degrade into an apparently craftable recipe.

## 5. Requirement semantics

### 5.1 Logical structure

For each requirement category, outer groups are conjunctive and alternatives inside a group are disjunctive.

Example:

- components `[[A x1, B x1], [C x2]]` means **(A OR B) AND C×2**, not A+B+C;
- quality groups follow the same OR-within/AND-across shape.

Requirement multiplication scales tools/components according to baseline rules; quality count/level semantics MUST follow the loaded quality requirement rather than being naively multiplied as item quantity.

### 5.2 Shared-pool and overlap correctness

Feasibility MUST be evaluated against a common authoritative resource pool. A single item/charge/power source cannot satisfy two simultaneous consumptive obligations unless the pinned rule permits that reuse.

The upstream `requirements_test.cpp` overlapping-alternative fixtures are parity tests: deduplication may yield several equivalent feasible alternatives, but it MUST NOT produce an alternative that consumes fewer resources than the original logical expression.

Tool-power queries are feasibility checks, not reservations. If two tools draw from one shared power source, final admission/reservation MUST account for the shared finite pool once and prevent double-spend.

### 5.3 Actor-aware resources

Requirement resolution is parameterized by the acting Character. Intrinsic qualities/resources granted by mutations, traits, bionics or other Character state apply only to that actor. The pinned tests explicitly distinguish actor-aware checks from inventory-only checks; no privileged-avatar grants may leak into another NPC/player Character.

### 5.4 Reachable crafting inventory

The baseline synthesizes a crafting inventory from carried/worn/nearby reachable sources. OctoGhast MUST express this as an authoritative **resource query scope**, not a copied client inventory.

The scope is constructed from:

- actor possession according to Spec 06;
- reachable map/vehicle/container items permitted by the action;
- actor-intrinsic resource/quality providers;
- permitted shared/grid/power/tool providers from dependent systems;
- range and line/reachability rules from Spec 12.

The query result may be projected to a client as choices, but the server MUST re-resolve/revalidate all chosen resources at command admission and at each later consumption boundary.

## 6. Craftability and start preconditions

A craft start command specifies at minimum:

- acting Character stable ID;
- recipe ID;
- batch size;
- intended work location/bench context;
- optional explicitly chosen resource alternatives;
- optional result-container choices where required;
- client request ID/idempotency token.

Admission MUST validate, in deterministic order:

1. actor exists, is controlled/authorized and can start an activity;
2. recipe exists and is available/known by one of the permitted knowledge paths;
3. Character skill/stat/proficiency prerequisites required for starting are satisfied;
4. recipe-specific forbidden states are absent;
5. lighting permits start;
6. morale permits start;
7. actor is not in an incompatible state such as driving where the baseline forbids crafting;
8. workbench/work-location is authoritative, reachable and suitable;
9. full batch requirements are feasible;
10. result liquid/container eligibility is satisfiable where the baseline requires preselection;
11. selected alternatives still exist and are not already reserved/consumed by earlier ordered commands;
12. required start-time components/resources can be atomically consumed/reserved.

Failure before the atomic start mutation leaves components, tools and activity state unchanged except for an explicitly documented attempted-action cost in the baseline. A network race itself never creates a cost.

## 7. Time, batch and speed formulas

### 7.1 Base/proficiency time

For a stepless recipe:

`effective_recipe_moves = base_time_moves × product(proficiency_time_maluses)`

unless a caller explicitly uses the pinned `ignore_proficiencies` mode, as disassembly does.

For a step recipe:

`effective_recipe_moves = Σ(step.base_moves × product(step proficiency maluses))`.

Book/reference bonuses and helper-held proficiencies feed the pinned proficiency calculation where applicable.

### 7.2 Batch scaling

The baseline supports at least linear and logistic batch savings.

For a linear batch policy with setup offset `O` and optional maximum batch block `M`:

`repetitions = ceil(batch / (M or batch))`

`batch_moves = repetitions × O + batch × (single_moves - O)`.

For logistic scaling, for each unit index `x = 0..batch-1`:

`scale = rsize / 6`

`logf = 2 / (1 + exp(-(x / scale))) - 1`

`unit_moves(x) = single_moves × (1 - rscale × logf)`

and total batch moves are the sum of unit moves.

Step recipes apply the step's own batch scaling after proficiency/tool-speed modifiers; totals are then summed.

### 7.3 Crafting-speed and assistant modifiers

The Character crafting-speed multiplier is derived from baseline factors including lighting, morale, manipulation/limb ability, pain where the recipe is pain-affected, enchantment/mutation modifiers, and workbench mass/volume suitability.

Pinned details include:

- non-negative morale gives no positive speed bonus from morale alone;
- negative morale produces a difficulty/skill-scaled penalty;
- `AFFECTED_BY_PAIN` multiplies by `max(0, 1 - perceived_pain/100)`;
- workbench oversize/overweight penalties bottom through the baseline interpolation and may make continued work invalid;
- darkness flags/skill surplus can permit penalized crafting in darkness where specified.

Batch time is divided by the effective crafting-speed multiplier.

Assistant adjustment occurs after batch scaling: exactly one qualifying assistant multiplies total by `0.75`; two or more by `0.60`. The result is then floored at the effective time for one item so assistants/batch savings cannot make an entire batch faster than one item.

### 7.4 Canonical-time adaptation

Pinned CDDA expresses crafting work in moves and advances it under turn/activity processing.

OctoGhast MUST preserve the same move/work totals while using Spec 01 canonical time:

- 10 canonical ticks = 1 world second/turn = 100 baseline moves;
- speed-based attended crafting consumes the actor's authoritative move budget via Spec 04;
- passive/unattended step deadlines use canonical absolute times;
- render FPS, socket latency and UI-open duration never alter work totals;
- fractional/remainder accounting MUST be deterministic and save/load stable.

## 8. Craft lifecycle

### 8.1 Start

On successful admission:

1. resolve deterministic component/tool alternatives;
2. atomically remove/consume start-time components from their authoritative locations;
3. create one authoritative in-progress craft instance carrying recipe/batch/component provenance;
4. place that craft at its authoritative held/map/work location;
5. create/assign the crafting activity or staged passive-work state;
6. apply start-time negative morale modifiers;
7. emit the actor-scoped start result/event.

Consumed components are not still independently available items unless the baseline represents them that way. Their provenance belongs to the craft instance for result nutrition/reversibility/disassembly behaviour.

### 8.2 Progress

At each due work boundary:

1. resolve current craft UID and recipe definition;
2. verify actor ownership/authorization and required work location;
3. re-evaluate continuation prerequisites that are live in the baseline: visibility, workbench suitability, morale/pain thresholds, required proficiencies, reachable tools/qualities/resources and staged-step environment requirements;
4. validate/acquire due reservations;
5. debit scheduled tool charges/resources;
6. advance work using pinned move/proficiency/speed rules;
7. award practice/skill/proficiency progress at the pinned cadence;
8. evaluate any due craft-failure point in deterministic RNG order;
9. transition step/passive state or finish when the terminal threshold is reached.

If a craft is paused by a temporarily missing staged-step environmental/resource condition, the craft remains authoritative and persists its scheduled state. Polling is driven by canonical server time. It does not spin once per rendering frame.

### 8.3 Failure

Craft failure is not equivalent to activity cancellation.

The pinned baseline stores/rolls failure points and may:

- mark a future completed result for a fault/defect;
- destroy random consumed components;
- reduce progress;
- apply morale effects;
- abort entirely if no viable components remain.

Random component destruction and progress loss are authoritative RNG decisions. Their stream/order MUST be deterministic for a supplied save/RNG state and command/tick sequence.

### 8.4 Interruption, cancellation and resume

Crafting uses Spec 04 activity semantics.

An interruption or explicit cancel MUST preserve an in-progress craft when the pinned behaviour permits later continuation. The craft's recipe ID, component provenance, progress, reservations/schedules and failure state survive.

Resume uses stable craft UID and revalidates current requirements. It MUST NOT reconstruct the craft from the client's previously displayed recipe/resource list.

If a required target/resource is permanently gone, continuation fails with a stable reason. Temporary lack of light/workbench/tool/resource may pause rather than destroy the craft when the baseline permits continuation after restoration.

Opening/closing crafting UI has no simulation effect. Disconnecting a player does not implicitly pause global time; the Character's activity/disconnect policy is governed by Specs 04 and 20.

### 8.5 Completion ordering

Normal completion MUST be atomic from the viewpoint of other authoritative commands at the same deterministic boundary.

The implementation contract is:

1. validate terminal craft still exists and is completable;
2. finalize result items from recipe and stored component provenance;
3. apply result count/multiplier/charge/container/sealed/hot/raw-removal semantics;
4. insert/place results through Spec 06 using the authoritative completion location;
5. create/place byproducts;
6. update skill/proficiency/practice state;
7. perform recipe-learning checks where crafting from a reference rather than memory;
8. apply completion-positive morale;
9. invoke each recipe `result_eoc` once per batch unit, with the crafting Character as the primary talker/context;
10. emit completion/inventory/projection events.

The craft instance is removed exactly once. Retries/replayed client requests cannot duplicate results or EOCs.

## 9. Recipe knowledge and learning

A Character may craft a recipe if it is available through the pinned knowledge paths, including:

- explicitly learned/memorized recipe;
- autolearn when its required skill thresholds are satisfied;
- an eligible carried/nearby book or electronic recipe reference;
- group/helper/reference mechanisms supported by the baseline.

Using a reference does not automatically memorize the recipe.

For the pinned craft-completion learning roll when the actor crafted using a reference and does not already know the recipe:

`learning_speed = max(primary_skill_level, 1) × max(INT, 1)`

`time_to_learn = 1000 × 8 × difficulty^4 / learning_speed`

and the baseline performs `x_in_y(recipe_time_to_craft_moves, time_to_learn)`.

That probability and its RNG draw order are parity requirements. Difficulty-zero/special recipes must follow the reference guard behaviour rather than introducing divide-by-zero or automatic learning accidentally.

`never_learn` blocks ordinary learning unless an explicitly privileged/debug path overrides it.

Autolearn state is derived from Character skills plus immutable recipe definition; it is not separately persisted as a per-recipe boolean unless the learned-recipe set itself changes.

## 10. Results, containers and byproducts

### 10.1 Results

Results are created once per batch unit, then multiplied by `result_mult` where declared. Charge-counted compatible results may be combined.

When the result must preserve used-component identity/properties (notably reversible non-charge items and food without nutrient override), the baseline splits stored component provenance across result/batch units. OctoGhast MUST retain enough provenance to reproduce later disassembly/nutrition outcomes.

Contained/container results must use the declared container type/variant/sealing semantics. Liquid result eligibility is checked against available compatible containers where required.

### 10.2 Byproducts

Legacy explicit byproducts and byproduct groups scale with batch according to their definition. Deterministic-count byproducts scale exactly with batch. Random item-group results consume authoritative crafting RNG and are testable by seeded/golden distributions.

Result and byproduct placement uses authoritative inventory/map/container rules. A placement failure MUST follow Spec 06 overflow/drop behaviour; it cannot delete product silently.

### 10.3 Result EOCs

Each `result_eoc` executes once for each batch unit in recipe order after products/byproducts are finalized according to the completion contract above.

OctoGhast adaptation:

- EOCs execute only on the authoritative server;
- talker/context is the actual crafting Character, not a global avatar;
- side effects use Spec 17 deterministic server execution;
- resulting messages/state are projected only to appropriate clients.

## 11. Disassembly / uncraft

### 11.1 Eligibility

Disassembly is an authoritative command/activity targeting a stable ItemUid/location.

Pinned preconditions include:

- item is disassemblable and has a valid uncraft recipe;
- sufficient light;
- rotten perishable items that are too spoiled are rejected;
- contained creature/pet state must be removed first;
- charge-counted items meet the minimum quantity represented by one uncraft result;
- required qualities/tools are available;
- ownership/theft consequences are evaluated;
- requested quantity for charge-counted items is valid.

Interactive confirmation of favourite/worn/theft/disassembly preview is UI policy; the underlying authoritative warnings/ownership consequences remain server rules.

### 11.2 Duration

Pinned disassembly duration is:

`uncraft_recipe.time_to_craft_moves(ignore_proficiencies) × quantity`.

Proficiency time maluses do not apply to disassembly duration in this baseline.

The duration runs as a Character activity under Spec 04/canonical time.

### 11.3 Completion and recovery

At completion:

1. revalidate target UID/location and uncraft definition;
2. copy the source state required for recovery;
3. remove the source item atomically;
4. consume disassembly tool charges;
5. if the source carries recorded actual components, recover those actual components;
6. otherwise recover the default components from the uncraft requirements;
7. apply baseline damage/recovery/random-loss rules;
8. place recovered items at the authoritative location;
9. perform learn-by-disassembly logic when eligible;
10. advance recursive/disassemble-all work only after the prior target completion is committed.

A stale target, concurrently removed item, changed ownership/location that invalidates reachability, or no-longer-satisfied tool requirement fails closed. Two Characters cannot both disassemble the same ItemUid.

### 11.4 Learn by disassembly

A recipe with `learn_by_disassembly` may become learned only if the Character meets that recipe's specified learning skill prerequisites and the pinned learning/recovery path succeeds. The exact baseline probability/RNG rule used by the uncraft path MUST be preserved and tested from the pinned source when implemented; no client-side roll is allowed.

## 12. Salvage/cutting boundary

Generic cutting/salvage actions that convert an item into materials share the following Spec 07 contracts:

- authoritative source ItemUid/location;
- requirement/tool-quality validation;
- move/activity cost;
- deterministic or seeded recovery;
- atomic source consumption versus recovered outputs;
- ownership/reachability/contention semantics.

Item-action-specific cutting formulas remain in the owning item/action implementation and must be added as fixtures when that action is ported. Spec 07 does not redefine every individual `iuse` salvage action as a recipe.

## 13. Multiplayer authority, contention and reservations

Pinned CDDA is primarily single-player/turn-gated. The following are intentional OctoGhast adaptations.

### 13.1 Resource contention

Commands admitted for the same canonical boundary use the shared deterministic command ordering from Specs 01/04, never socket arrival/thread timing.

If Characters A and B both request crafts that require the same last component/tool charge:

- the first ordered command that validates/reserves/consumes it may proceed;
- the later command revalidates against resulting authoritative state;
- the later command receives a stable failure or a server-recomputed alternative only if its command explicitly allowed automatic substitution;
- no resource is double-counted or driven negative.

### 13.2 Reservation identity

A reservation/binding references stable authoritative providers: ItemUid/container/location, Character intrinsic provider, vehicle/grid/power provider identity, or another typed resource endpoint. Collection indexes and Godot object references are forbidden.

Reservations are not ownership transfers unless the baseline actually consumes the component. They must be released on cancellation/failure and reconstructed deterministically after save/load when persistence requires them.

### 13.3 Long crafts and overlapping regions

A craft is simulated exactly once. Overlapping player active/interest regions do not duplicate progress, scheduled passive checks, skill gain, resource debit, completion, EOCs or byproducts.

A craft may remain active/persisted independently of whether its owner is visible to another client. Client interest only affects projection.

## 14. Projection and protocol surface

The server may expose projected crafting data such as:

- recipe IDs and localized/display metadata the client is permitted to know;
- craftability result plus structured missing-requirement reasons;
- legal batch range;
- estimated duration from the current projected Character/workbench/resource context;
- selectable alternatives and result-container choices;
- current actor-owned craft progress/step and pause/interruption reason;
- completion/failure messages and resulting item deltas visible to that player.

It MUST NOT expose:

- arbitrary ECS entity/component state;
- hidden items outside the player's knowledge/interest;
- another player's private inventory merely because it could theoretically satisfy a requirement;
- server RNG internals;
- mutable registry objects.

Queries are advisory snapshots. A subsequent start/resume command always revalidates authoritative state.

## 15. Persistence and reconnect

Per Spec 20, saves MUST persist:

- learned recipes and any Character progression needed to re-derive autolearn;
- in-progress craft UID, recipe ID, batch size, component provenance and progress;
- current step/passive-attention state;
- scheduled canonical due/check times;
- resource/reservation state that is semantically required for continuation;
- failure point/defect/progress-loss state;
- crafting activity state;
- RNG stream state/counters needed for deterministic continuation.

Do not save UI filters, open crafting screens, socket IDs, local progress animations or Godot node identity.

Save/load must not duplicate component consumption, reroll already-fixed failure points, reset passive deadlines, re-run completion EOCs, or lose a partially completed craft.

Disconnect/reconnect restores the player's projected view of the current authoritative craft/activity; reconnect itself does not create a new craft or rewind time.

## 16. RNG and determinism

Authoritative RNG is required for at least:

- craft failure-point scheduling/checks;
- component destruction/progress loss/defects;
- recipe learning from references;
- learn-by-disassembly where probabilistic;
- random byproduct/item-group production;
- probabilistic recovery in disassembly/salvage.

For a supplied initial world state, canonical tick/input sequence, content baseline and RNG state, OctoGhast MUST produce repeatable authoritative outcomes independent of render FPS, packet timing and client count.

Where baseline distributions are intentionally broad and exact stream equivalence is not required, statistical conformance tests MUST assert the same support/range and a justified tolerance. Deterministic branch/order tests are preferred wherever source ordering is externally observable.

## 17. Failure and validation behaviour

The following MUST fail without partial mutation unless pinned behaviour explicitly says otherwise:

- unknown recipe/requirement/content ID;
- invalid batch size;
- insufficient/missing component/tool/quality;
- overlapping requirement double-spend;
- stale selected ItemUid/resource provider;
- inaccessible moved/sealed parent container;
- missing result container for a liquid craft when required;
- actor loses required start preconditions before admission;
- duplicate/replayed start request with same idempotency key;
- craft UID no longer exists at resume/complete;
- disassembly target already removed by another actor;
- invalid step definition or impossible referenced requirement at load time.

A mid-craft live prerequisite failure follows the baseline pause/cancel/failure semantics rather than rewinding already-consumed components automatically.

## 18. Implementation interfaces

The implementation SHOULD expose transport-neutral domain contracts equivalent to:

- `QueryCraftingOptions(CharacterId, Context) -> CraftingProjection`
- `EvaluateRecipe(CharacterId, RecipeId, Batch, Context) -> Craftability`
- `StartCraft(CraftIntent) -> CommandResult<CraftUid>`
- `ResumeCraft(CharacterId, CraftUid) -> CommandResult`
- `CancelCraft(CharacterId, CraftUid) -> CommandResult`
- `AdvanceCraft(CraftUid, CanonicalBoundary) -> DomainEvents`
- `Disassemble(DisassemblyIntent) -> CommandResult<ActivityId>`
- `ResolveRequirements(Actor, RequirementSet, ResourceScope, SelectionPolicy) -> Resolution`

These are semantic boundaries, not mandated C# signatures.

The semantic ownership boundary is:

- **Cataclysm profile:** recipe/requirement definitions and validation, CDDA craftability rules, move/work calculations, proficiency/assistant/batch policy, component/tool interpretation, failure/learning formulas, product/byproduct/uncraft semantics and recipe-triggered EOCs.
- **Generic Core/server:** stable identities, deterministic command ordering, activity/scheduler primitives, canonical time, authoritative mutation transactions, resource-provider/reference abstractions, RNG stream ownership, persistence barriers, projection/audience infrastructure and transport-neutral request/result envelopes.
- **Godot/client:** presentation, filtering, selection UX, localization/rendering and intent submission; no authoritative reservation, RNG, progress or inventory mutation.

A future non-Cataclysm profile may reuse the Core contracts while replacing the Cataclysm crafting policy wholesale. These are semantic boundaries, not mandated C# namespaces or signatures.

### 18.1 Networking/session contract

Spec 07 does not define sockets, framing, authentication, queue sizes or backpressure. Those remain owned by #90. Crafting requires only the following transport-neutral contract:

- decoded client requests become validated crafting query/command DTOs before entering deterministic simulation intake;
- socket/async-I/O callbacks never mutate craft, Character, item, reservation or RNG state;
- connection identity is distinct from stable player identity, controlled Character identity and `CraftUid`;
- duplicate/replayed requests are bounded by request identity/idempotency semantics before they can duplicate authoritative mutation;
- recipe/craft projections are viewer-scoped and may be represented as replaceable state, while committed craft/disassembly results and inventory changes are reliable authoritative facts;
- malformed, oversized, rate-limited or disconnected-client traffic fails at the networking/session boundary without consuming crafting RNG or partially mutating simulation state;
- reconnect rebinds a stable player/session to existing authoritative craft/activity state; it does not recreate the craft or become part of persistence.

Spec 25 / #90 is complete and governs transport/session mechanics. Crafting consumes it without a separate network path. Its retry requirements additionally depend on [#96](https://github.com/LambdaSix/OctoGhast/issues/96) for a bounded outcome/deduplication contract across reconnect/save.

## 19. Black-box and conformance scenarios

The later automated suite MUST include at least the following.

### CRAFT-01 — Alternative groups
Given a recipe requiring `(A or B) and C×2`, A+C×2 is craftable, B+C×2 is craftable, and A alone/A+B+C×1 are not. Starting consumes exactly the selected valid combination.

### CRAFT-02 — Overlapping requirements
Use fixtures equivalent to upstream `requirements_test.cpp` survivor-telescope/triple-overlap cases. A single rock/item pool cannot satisfy two simultaneous component groups unless quantity permits it. Reported feasibility and actual consumption agree.

### CRAFT-03 — Shared tool power
Two required tools drawing from one finite shared power source cannot each count the same charges independently. Exact required total succeeds; one fewer charge fails without partial debit.

### CRAFT-04 — Batch formula
For a recipe with known linear batch offset/max-batch, assert the exact formula above for batches 1, 2 and a value crossing the max-batch block. Assert total batch time never falls below one effective unit.

### CRAFT-05 — Logistic batch formula
For a fixed `rsize/rscale` fixture, compare the summed formula with the pinned reference for several batch sizes within integer rounding tolerance defined by the source cast.

### CRAFT-06 — Assistants
Same craft/context: zero assistants = base batch time; one qualifying assistant = 75% before single-item floor; two or more = 60% before floor. Nonqualifying nearby actors do not count.

### CRAFT-07 — Proficiency time
Known proficiency removes its malus; missing proficiency multiplies time exactly as the fixture definition specifies. Step recipes apply maluses per step.

### CRAFT-08 — Step schema validation
A step recipe containing root `time` or root `tools` is rejected. A step with non-positive time is rejected. A valid multi-step recipe finalizes ordered steps and aggregated requirements.

### CRAFT-09 — Start atomicity
A craft with all resources creates one CraftUid, removes start-time components once, stores their provenance and starts one activity. A failed start leaves resources/activity unchanged.

### CRAFT-10 — Interruption/save/resume
Advance a craft to a known progress value, interrupt, save, reload and resume. Recipe/batch/components/progress/failure state are unchanged; completion consumes/produces no duplicate state.

### CRAFT-11 — Live prerequisite loss
Remove required light/workbench/tool condition mid-craft. The craft follows the pinned pause/continuation rule; restoring the condition continues from the same work state without recreating components.

### CRAFT-12 — Deterministic failure
With a fixed RNG state and under-skilled fixture, reproduce the same failure point, destroyed component/progress loss/defect result before and after save/load.

### CRAFT-13 — Results and provenance
A reversible item built from non-default actual components records/splits those components such that later uncraft recovers the actual stored components as the pinned baseline does.

### CRAFT-14 — Charge-counted results
Batch a charge-counted result. Compatible outputs combine without quantity loss; total charges equal the sum of per-batch results.

### CRAFT-15 — Byproducts
Pinned byproduct fixture such as the upstream tallow test scales deterministic byproducts exactly for batch 1, 2 and 10; random groups use seeded expected support/distribution.

### CRAFT-16 — Result EOCs
A batch of N invokes each declared result EOC exactly N times on the server with the crafting Character as talker. A replayed completion request invokes zero additional EOCs.

### CRAFT-17 — Learning from reference
An actor who can craft from a book/device but has not memorized the recipe remains unlearned until the authoritative completion learning roll succeeds. Fixed RNG reproduces the `x_in_y` result using the documented formula.

### CRAFT-18 — Autolearn
Below threshold: recipe unavailable from autolearn. At threshold: available without persisting a new explicit learned flag unless baseline learning semantics add it.

### CRAFT-19 — Disassembly eligibility
Reject non-disassemblable, rotten, creature-containing, under-quantity and missing-tool targets with no source mutation.

### CRAFT-20 — Disassembly duration
For a known uncraft recipe, duration equals base uncraft craft-time with proficiency maluses ignored, multiplied by requested quantity.

### CRAFT-21 — Actual-component recovery
Disassemble a reversible crafted item carrying actual component provenance; recover those components rather than merely the default first alternative.

### CRAFT-22 — Concurrent craft contention
Two Characters on the same canonical boundary request crafts consuming the last component. Deterministic command ordering yields exactly one successful consumption. The loser gets a stable stale/insufficient-resource result.

### CRAFT-23 — Concurrent disassembly
Two Characters target the same ItemUid. Exactly one removes/disassembles it; the later ordered command fails without outputs/tool-charge debit.

### CRAFT-24 — Overlapping active regions
Two clients observe the same crafting actor/worksite. Progress, passive wakeups, tool debit, results, EOCs and byproducts occur once, not once per observer.

### CRAFT-25 — Disconnect/reconnect
Disconnect the controlling client during a craft. Server policy continues/suspends the actor according to Spec 20 without changing craft rules. Reconnect projects the same authoritative CraftUid/progress.

### CRAFT-26 — Renderer/network independence
Run the same canonical tick/command/RNG sequence with different render FPS and packet chunking. Final craft state, inventories, learned recipes and events are identical.

### CRAFT-27 — Core/profile isolation
Load two synthetic rules profiles through the same Core command/activity/persistence harness: the Cataclysm profile uses the pinned CDDA recipe/move semantics, while a synthetic profile uses a different work-unit and recipe-definition shape. Verify deterministic authority, identity, activity scheduling and persistence operate without Core APIs requiring CDDA recipe fields, 100-move units or CDDA proficiency/batch formulas.

### CRAFT-28 — Stable player versus connection identity
Start a craft, disconnect the controlling socket, reconnect through a different connection identity and rebind to the same stable player/Character. The authoritative `CraftUid`, progress, reservations, RNG continuation and activity state are unchanged; no new craft is created.

### CRAFT-29 — Network rejection is simulation-neutral
Submit malformed/oversized/rate-rejected duplicate crafting traffic followed by a valid command at the same canonical world state. Rejected traffic consumes no crafting RNG, action budget, components, reservations or simulation-order slot beyond whatever generic #90 intake accounting defines.

### CRAFT-30 — Completion contention is atomic
At the same deterministic boundary, one craft completion produces an item/resource that another actor's command attempts to consume or move. The globally defined command/activity phase ordering produces one stable outcome; observers never see half-created products, duplicated byproducts, duplicate EOCs or a product visible before its authoritative location/index state is valid.

### CRAFT-31 — Shared provider contention across crafts
Two simultaneous crafts reserve/debit a finite shared power or tool-charge provider. Deterministic admission/progress ordering permits only obligations that fit the authoritative remaining capacity; save/load between reservation and debit preserves the same winner/loser and charge total.

### CRAFT-32 — Future spatial seam
Run the Cataclysm profile with grid-aligned reachability and a synthetic Core test provider whose resource scope is supplied through a different spatial-query implementation. Crafting Core contracts consume authoritative reachability/resource-query results without assuming integer tiles or Godot transforms; Cataclysm parity remains grid-based.

## 20. Dependencies and follow-on contracts

**Consumes:** Spec 01/#66 time/action economy; Spec 02/#67 Character skills/needs/stats; Spec 03/#68 proficiencies/progression; Spec 04/#69 activities; Spec 05/#70 items; Spec 06/#71 item locations/transfers; Spec 12/#77 map/reachability; Spec 17/#82 EOCs/talkers; Spec 18/#83 data/IDs; Spec 20/#85 persistence.

**Provides to:** construction #73 (requirements/activity/resource selection patterns), combat/item repair and ammunition systems, NPC/AI work assignment, UI #86, mod/content compatibility #84, and parity harness #88.

Construction MUST consume this requirement-resolution contract rather than invent a second incompatible component/tool/quality solver.

## 21. Intentional deviations summary

### Pinned CDDA reference behaviour
A primarily turn-gated Character drives crafting through local UI, using CDDA move-based activities, nearby crafting inventory, persistent in-progress craft items, recipe/resource selection, batch/proficiency/speed rules, and uncraft activities.

### OctoGhast adaptation
Commands, reservations, activities, craft instances, RNG and resource mutation are server-authoritative under continuous canonical time; multiple Characters may craft concurrently; no UI pauses the world; client projections are player-specific; deterministic contention replaces single-avatar assumptions.

### Implementation contract
Preserve pinned recipe data semantics, requirement feasibility/consumption, move/work formulas, batch/proficiency/speed rules, in-progress state, failure, learning, results/byproducts/EOCs and disassembly behaviour in the **Cataclysm profile**. Generic Core supplies the reusable authority, command/activity, time, identity/reference, resource-query/mutation, RNG, persistence and projection mechanisms without hard-coding those Cataclysm rules.

### Future evolution seam
Reference parity is a completeness waypoint, not a permanent crafting-design ceiling. Later OctoGhast rules may replace recipe schemas, work currencies, production graphs, learning/failure formulas, spatial reachability or work-order representation while retaining the same Core authority and continuation guarantees. Such divergence must be explicit rules/profile work, not an accidental change to Cataclysm parity.

No genuine unresolved cross-cutting architectural decision was discovered by this re-evaluation. Spec 25 / #90 owns completed networking-core mechanics; Spec 07 declares the crafting semantics that boundary carries. The later corpus audit identified #95 ordering integration and #96 retry/outcome recovery as dedicated cross-spec decisions. #95 is now resolved by the [canonical ordering/admission/activation architecture](./architecture-canonical-ordering-admission-activation.md); #96 remains the separate retry/outcome follow-up.

## #96 bounded crafting/disassembly outcome integration — 2026-09-26

[Architecture — bounded command idempotency and outcome recovery](./architecture-bounded-command-idempotency-outcome-recovery.md) now owns retry identity and persistence for consequential client-submitted operations.

Craft/start/disassembly commands MUST explicitly declare `StateReconciled`, `IntrinsicIdempotent`, or `DurableOutcome`. Any operation for which reconnect/restart retry is advertised and repetition could consume ingredients, tools/charges, time/cost, create outputs/byproducts, grant skill/proficiency effects, or consume gameplay RNG MUST use `DurableOutcome`.

For one logical durable operation:

- duplicate submission or outcome query never creates another crafting/disassembly attempt;
- ingredient/resource consumption and output creation occur at most once for that operation;
- failure/rejection and all RNG-derived results remain associated with the original attempt;
- reconnect or committed save/restart cannot reroll success/failure/output choices;
- changed-payload key reuse is rejected before gameplay execution;
- an old/expired/rolled-back history key never silently executes as a new craft.

The durable operation record is not the crafting Activity state. Once an activity has started, its in-progress state remains owned/persisted by the crafting/activity contracts; the operation ledger records the submitted command outcome.

