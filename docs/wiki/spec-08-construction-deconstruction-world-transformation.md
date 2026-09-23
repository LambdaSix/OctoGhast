# Spec 08 — Construction, deconstruction and world transformation

**Tracking issue:** #73  
**Parent epic:** #65  
**Programme:** #64  
**Reference baseline:** `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`  
**Status:** investigation complete / implementation-ready specification; runtime implementation not started

## 1. Purpose and parity boundary

This specification defines OctoGhast's authoritative contract for construction definitions, construction groups/categories, placement eligibility, requirements, staged world projects, interruption/resume/cancel, deconstruction, terrain/furniture transformation, construction side effects, persistence, deterministic concurrency, NPC/basecamp use and presentation-facing projection.

The behavioural oracle is the pinned CDDA baseline above. OctoGhast does **not** need to reproduce CDDA's construction UI, C++ object layout, local-avatar reality bubble, or blocking query dialogs. It must preserve the observable rules and data semantics while applying the architecture already settled by #52, #57/#66, #58/#77, #69, #70, #71, #72, #82, #83 and #85:

- the authoritative simulation/server owns construction definitions, world tiles, unfinished projects, item consumption, activities, canonical time and gameplay RNG;
- single-player is a one-player server using the same command/query/result boundary as co-op;
- construction duration remains CDDA move/work currency and is advanced by the authoritative action/activity scheduler rather than wall-clock timers;
- multiple player-controlled Characters and NPCs may act simultaneously in separated or overlapping active regions;
- authoritative map coordinates and mutations are independent of Godot transforms;
- clients receive only player-specific construction options, progress and resulting visible world changes, never arbitrary ECS/world state;
- stale commands and contested resources/sites are revalidated and resolved deterministically on the server.

Where the pinned game assumes one avatar, turn-gated input, a blocking prompt or one avatar-owned loaded map, this document labels the upstream rule, the OctoGhast adaptation, and the implementation contract separately.

## 2. Dependencies and ownership boundaries

This specification depends on, and does not reopen, the following completed contracts:

- **Spec 01 / #66** — canonical authoritative time, 10 TPS, 100 moves/second compatibility budget and deterministic action scheduling.
- **Spec 04 / #69** — command/query/activity/event separation, actor-owned current activities/backlogs, interruption, stable activity targets and deterministic execution.
- **Spec 05 / #70** — authoritative item identities and lifecycle.
- **Spec 06 / #71** — item locations, requirement-facing inventory views, transfers, contention and stale references.
- **Spec 07 / #72** — requirement groups, tools/qualities/components, crafting inventory semantics and deterministic requirement selection.
- **Spec 12 / #77** — absolute integer/grid coordinates, server/world-owned active regions, controlled terrain/furniture mutation and cache invalidation.
- **Spec 17 / #82** — authoritative events/EOCs, invocation context, audience filtering and deterministic effects.
- **Spec 18 / #83** — JSON loading, inheritance, stable typed string IDs, registries, finalization and validation.
- **Spec 20 / #85** — authoritative world saves, activity/RNG persistence, reconnect semantics and exclusion of transport/presentation state.

Construction owns the **project/site lifecycle and construction-specific transformation ordering**. The map system owns physical tile mutation and derived-cache correctness. Inventory owns item identity/location and atomic consumption. Activity infrastructure owns scheduling/interruption. Character progression owns skill storage/practice. Vehicle/appliance systems own the durable objects created by their construction completion hooks. EOC/event infrastructure owns data-driven cross-system effects when invoked.

## 3. Authoritative pinned-CDDA evidence

Primary source anchors inspected at the pinned commit:

- `src/construction.h` — immutable construction definition shape and mutable `partial_con` runtime state.
- `src/construction.cpp` — loading/finalization/validation, eligibility, site selection, component/tool commitment, completion ordering, pre/post/do-turn specials, deconstruction, time/helper scaling and world transformations.
- `src/activity_actor.cpp` and `src/activity_actor_definitions.h` — `build_construction_activity_actor`, shared project progress, missing-site/skill failure, serialization and multi-construction activity.
- `src/iexamine.cpp` — resume and cancel behavior for unfinished construction.
- `src/savegame_json.cpp` — durable submap serialization of unfinished projects.
- `src/construction_category.cpp`, `src/construction_group.cpp` — category/group registries.
- `data/json/construction_category.json`, `data/json/construction_group.json` — category/group definitions.
- `data/json/construction/*.json` — concrete construction definitions, including staged builds and appliance placement.
- `data/json/deconstruction.json` — explicit removal/deconstruction constructions.
- `data/json/construction/misc.json` — generic deconstruction constructions and miscellaneous transforms.
- terrain/furniture definitions loaded by `mapdata.*` — generic deconstruction metadata used by `done_deconstruct`.
- `tests/submap_load_test.cpp` — direct regression evidence that partial-construction position, progress, definition identity and committed component items survive submap load.

Notably, the pinned tests do not provide a broad dedicated construction gameplay test suite. For behavior not directly covered by an upstream test, this specification treats the source implementation and pinned JSON as authoritative and requires OctoGhast differential/golden fixtures before implementation is considered parity-complete.

## 4. Definition model: immutable data

### 4.1 Construction identity

Each construction is a registry definition identified by a stable string `construction_id`. Runtime integer registry indexes are implementation-local accelerators only and MUST NOT cross save, network or mod/content compatibility boundaries.

A construction definition contains, at minimum:

| Field | Pinned semantics / default |
|---|---|
| `id` | stable construction string ID |
| `group` | mandatory construction-group ID |
| `category` | category ID; default `OTHER` |
| `required_skills` | list/map of skill ID -> minimum level |
| legacy `skill` + `difficulty` | accepted when `required_skills` is absent; skill defaults to fabrication and difficulty is mandatory |
| `time` | base work duration parsed into moves; object default is 0, while runtime progress clamps base/effective total to at least 100 moves |
| inline requirements | `tools`, `qualities`, `components`, parsed through the shared requirement system |
| `using` | one requirement ID or list of `[requirement_id, multiplier]`; merged during finalization |
| `pre_note` | explanatory presentation text |
| `pre_terrain` | zero, one or many permitted terrain/furniture IDs |
| `pre_flags` | flags required at the target; entries may force terrain rather than furniture evaluation |
| `pre_special` | zero or more named built-in site predicates |
| `post_terrain` | optional terrain/furniture ID written on ordinary completion |
| `post_flags` | completion behavior flags such as `keep_items` |
| `byproducts` | optional item-group definition |
| `do_turn_special` | named per-progress hook |
| `post_special` | zero or more named completion hooks |
| `explain_failure` | named placement failure explanation |
| `activity_level` | default `MODERATE_EXERCISE` |
| `vehicle_start` | default false |
| `on_display` | default true |
| `dark_craftable` | default false |
| `strict` | default false |

The baseline infers whether `pre_terrain` / `post_terrain` refer to furniture by an `f_` prefix. OctoGhast may compile this to an explicit typed layer discriminator internally, but content parsing must accept the pinned data and produce the same meaning.

### 4.2 Groups and categories

Construction groups and categories are independent registries with stable string IDs and mandatory translated names. A group is the user-facing/project-family identity: multiple concrete construction definitions may share a group and differ by prerequisite state, material path or stage.

The pinned categories include `ALL`, `APPLIANCE`, `CONSTRUCT`, `FURN`, `DIG`, `REPAIR`, `REINFORCE`, `DECORATE`, `FARM_WOOD`, `TOOL`, `WINDOWS`, `BULK`, `OTHER`, `DECONSTRUCT` and `FILTER`. Category ordering is content-defined presentation metadata; it is not world-state identity.

### 4.3 Requirement aggregation

The loader creates an inline requirement identity `inline_construction_<construction id>`, loads direct component/tool/quality requirements into it, then during finalization adds every `using` requirement multiplied by its declared count.

Requirement semantics are exactly those of Spec 07:

- requirement groups are AND across groups and alternatives are OR within a group;
- one finite resource cannot be double-counted for simultaneous incompatible uses;
- item/charge/quality availability is evaluated through the authoritative crafting-inventory/requirement view;
- selected concrete component items and tool charges are server-side authoritative choices/commitments.

`vehicle_start` definitions receive a valid initial vehicle-frame component alternative during finalization.

### 4.4 Data validation

Pinned validation/finalization establishes the following requirements:

1. `group` must resolve to a valid construction group.
2. `category` must resolve to a valid category.
3. every required skill ID must resolve.
4. the aggregate requirement ID must resolve.
5. each `pre_terrain` ID must resolve in the inferred terrain/furniture registry.
6. `post_terrain`, when present, must resolve in the inferred registry.
7. named pre/post/do-turn/failure specials are selected from a closed built-in name table; an unknown special is a JSON load error.
8. normal generic-factory inheritance/override/finalization behavior follows Spec 18.

OctoGhast MUST fail content validation deterministically and diagnostically. It must not silently replace an unknown special, group, category, skill or terrain/furniture reference with a no-op.

## 5. Mutable runtime model: world-owned construction project

### 5.1 Pinned CDDA reference behaviour

The pinned `partial_con` stored on a map/submap tile contains:

- construction definition identity;
- progress counter;
- the actual component item objects consumed when the project was created.

The project is stored by map position, not inside the worker. Its progress counter ranges from 0 through 10,000,000; the baseline displays `counter / 100000` as integer percent. The pinned activity actor serializes only the construction location, while the project state itself is serialized with the submap.

The source explicitly handles a player and NPC working on the same construction and lets a different Character resume an existing project. Cancelling an unfinished project refunds its stored component items and removes the partial construction.

### 5.2 OctoGhast adaptation

The authoritative world/submap store owns unfinished construction. A connection, UI panel, player session or Godot node does not.

Define a durable logical record equivalent to:

```
ConstructionProject {
    site: AbsoluteMapSquare
    construction_id: ConstructionId
    progress_units: 0..10_000_000
    committed_components: [OwnedItemState]
    revision: monotonic concurrency/version token
}
```

A dedicated globally unique project UUID is optional; the stable logical reference for this baseline is the absolute site plus project revision because the pinned world permits at most one unfinished construction per tile. If future features require moving projects, cross-tile projects or long-lived references after removal, introduce a stable project ID centrally rather than exposing an ECS entity ID.

### 5.3 Invariants

- At most one unfinished construction project exists at one absolute map square.
- A project's `construction_id` does not change in place.
- Its committed components are no longer available in inventories/map stacks for unrelated use.
- `progress_units` never decreases through ordinary work and is clamped to 10,000,000.
- Removing/cancelling/completing the project is an authoritative mutation with one deterministic winner.
- Multiple worker activities may refer to the same project.
- Project state persists independently of whether any worker is currently attached or connected.

## 6. Construction discovery and eligibility

### 6.1 Definition-level eligibility

For a Character to be able to build a concrete definition, the pinned baseline requires:

1. required skill minima are met;
2. the aggregate requirements can be made from the Character's construction/crafting inventory;
3. unless the caller is already checking a specific site, at least one adjacent site can satisfy the construction's location predicates.

The baseline debug/hammerspace trait bypasses ordinary skill/resource checks. Debug behavior is an administrative/testing capability in OctoGhast and must never be inferred from a client request.

Fine-detail vision normally requires `fine_detail_vision_mod < 4`; if not, a group remains usable only when a matching construction definition is `dark_craftable`. Resuming an unfinished project in the pinned `iexamine` path rejects when fine-detail vision is worse than the allowed threshold unless in debug mode.

### 6.2 Site eligibility

Pinned `can_construct(definition, site)` rejects a site when:

1. the tile is reserved as a craft site or craft-resource provider;
2. any named `pre_special` predicate fails;
3. `pre_terrain` does not match;
4. any required `pre_flags` do not all match on the required terrain/furniture layer;
5. a non-empty `post_terrain` already equals the current target layer, because the construction would do nothing.

The normal player placement path considers the eight adjacent same-z tiles and never the actor's own tile.

Representative built-in preconditions include:

- empty/flat/unblocked checks involving furniture, creatures, traps, items, vehicles and open air;
- structural support checks;
- stable/support-below/single-support checks;
- floor/no-floor-above checks;
- generic deconstructibility;
- z-level upper/lower bounds;
- no-trap/no-wiring checks;
- flowing-water/channel context;
- matching ramp/stair terrain relationships.

These predicates are behavioral APIs, not client-side hints. The server MUST evaluate them against current authoritative world state at command resolution time.

### 6.3 Query versus command

OctoGhast exposes two different operations:

- a **query** returning the caller-visible construction groups/definitions and eligible projected target sites from a snapshot/revision;
- a **start/continue/cancel command** that revalidates the target and all authoritative resources before mutation.

The UI may optimistically highlight sites, but a prior eligibility query is not a reservation and cannot authorize a later stale command.

## 7. Starting a new project

### 7.1 Pinned CDDA reference behaviour

On a valid target the baseline:

1. rejects if a `partial_con` already exists there and directs the actor to continue it instead;
2. chooses/consumes each required component alternative;
3. stores those consumed item objects in the new `partial_con`;
4. installs the `partial_con` on the target tile;
5. consumes required tool charges;
6. invalidates crafting/weight caches;
7. assigns a build-construction activity targeting the **absolute** construction location.

### 7.2 OctoGhast authoritative transaction

The baseline's sequential C++ calls occur in a single-player command context. In co-op, OctoGhast MUST make project creation an atomic authoritative transaction:

1. resolve actor identity and absolute target;
2. acquire/validate the site's mutation claim in deterministic server command order;
3. revalidate site predicates, skills, vision rule and requirement availability;
4. resolve concrete component/tool alternatives;
5. atomically consume components and tool charges and create the project;
6. assign the actor's construction activity;
7. emit result/events and projected inventory/site changes.

If any required step fails, no partial component/tool consumption and no orphan project may remain. This is an OctoGhast concurrency adaptation: it preserves the pinned successful-state outcome while preventing two simultaneous clients from both spending or creating against one site.

Two simultaneous **new project** commands for the same empty site cannot both succeed. The first authoritative command that commits wins; the later command is rejected as stale/occupied without additional resource loss.

## 8. Continuing, cancelling and contested work

### 8.1 Continue

Pinned CDDA allows examining an unfinished project and assigning a build activity directly, without consuming its components again. The worker's skill is checked again while work runs.

OctoGhast continuation therefore:

- references the existing site/project revision;
- does not consume project components again;
- checks that the construction definition still resolves;
- checks worker skill and visibility/interaction constraints when beginning;
- permits multiple Characters to work the same project concurrently.

### 8.2 Cancel

Pinned cancellation refunds every component stored in the partial construction to the cancelling Character's position, then removes the partial project.

OctoGhast preserves the semantic result but adapts item placement through Spec 06's authoritative map/item APIs. Cancellation is a world mutation command and must serialize against work/completion:

- if cancellation commits first, all worker activities targeting that project subsequently observe it missing and terminate;
- if completion commits first, cancellation fails because there is no unfinished project to cancel;
- components are refunded exactly once.

The client is never allowed to synthesize the refund.

### 8.3 Missing or invalidated project while working

Pinned activity processing stops if the project no longer exists. It also rechecks required skills every work step and cancels if the worker no longer meets them.

OctoGhast MUST fail closed in those cases. No progress is applied after the failed validation and no completion outputs are duplicated.

A change to surrounding world state does **not** cause the pinned actor to rerun every original placement predicate each turn. Therefore ordinary work does not automatically abort merely because a support/adjacency predicate later changes, unless a specific do-turn/completion rule or another world system invalidates/removes the project. This distinction must be preserved in parity tests.

## 9. Work, time and progress

### 9.1 Pinned CDDA formula

For each construction work update:

- `base_total_moves = max(100, definition.time)`;
- `current_total_moves = max(100, definition.adjusted_time())`;
- `delta_progress_moves = worker_available_moves * base_total_moves / current_total_moves`;
- reconstruct current base-work progress from the site's old counter;
- consume the worker's current moves;
- run the construction's `do_turn_special`;
- round the new normalized progress to the 0..10,000,000 counter;
- complete when the counter reaches 10,000,000.

The pinned adjusted-time calculation:

1. starts with `definition.time`;
2. counts skilled crafting helpers;
3. one helper multiplies time by 0.75;
4. two or more helpers multiply time by 0.40;
5. applies construction time scaling;
6. runtime clamps the effective total to at least 100 moves.

`CONSTRUCTION_SCALING == 0` uses the calendar season ratio; otherwise it uses `CONSTRUCTION_SCALING / 100.0`. These are **work-cost modifiers**, not a request to make the activity complete after real-world minutes.

### 9.2 OctoGhast continuous-time adaptation

Construction is a speed-based long-running activity under Spec 04. Canonical server ticks allocate/consume actor move budget; the construction activity converts consumed moves into the same normalized site progress.

The implementation contract is:

```
base = max(100, base_definition_moves)
effective = max(100, adjusted_definition_moves_for_worker_context)
old_work = old_progress_units * base / 10_000_000
new_work = old_work + consumed_actor_moves * base / effective
new_progress_units = round(new_work / base * 10_000_000)
clamp to 10_000_000
```

Use deterministic fixed-point/rational arithmetic or a demonstrably parity-equivalent numeric implementation. Progress rounding and contribution ordering are observable under concurrent workers and MUST be stable.

No UI being open pauses this work. A disconnected player's activity follows the general Character/disconnect policy from Specs 04/20; the project itself always remains durable world state.

### 9.3 Multiple simultaneous workers

Pinned source explicitly anticipates multiple Characters working one project. OctoGhast therefore does not impose an exclusive “one worker” lock.

For a canonical tick with several contributors:

- each eligible worker contributes once according to deterministic actor/activity ordering;
- contributions mutate the one shared progress counter;
- the first contribution that reaches completion performs the completion transaction;
- later contributors in that same tick observe the project missing/completed and stop, with no second completion, XP award, byproducts or transform.

The upstream `adjusted_time()` helper discovery is avatar-centric. OctoGhast must resolve helpers relative to the working Character/activity context rather than a privileged avatar. The same pinned one-helper/two-plus-helper multipliers apply to eligible assisting Characters; an actor actively scheduled as an independent worker must not be accidentally counted twice through a client-local “avatar helper” assumption.

## 10. Per-work special effects

Pinned built-ins are closed names. At this baseline the do-turn table contains:

- no-op;
- `do_turn_deconstruct`;
- `do_turn_shovel`;
- `do_turn_exhume`.

Examples of observable behavior:

- deconstruction may issue its baseline confirmation at the first progress step;
- shovel work emits periodic digging sound and can trigger an unknown trap at the site;
- exhumation layers morale/trait logic and possible vomiting on top of shovel behavior.

### OctoGhast adaptation for blocking prompts

CDDA can stop inside a turn and ask the local avatar a question. The authoritative server cannot block world time awaiting a modal Godot response.

Any confirmation required before meaningful work side effects must be represented by the command/activity state machine defined in Spec 04: the server emits an owner-visible interaction request or requires confirmation as command data, the actor activity waits/does not commit that branch until resolved, and all other actors/world regions continue.

No other player's connection may answer the prompt, and a disconnect cannot leave a global simulation lock.

## 11. Completion ordering and world mutation

### 11.1 Pinned ordinary completion order

At the pinned baseline the completion path is observably ordered:

1. resolve the project and its construction definition;
2. award construction skill practice;
3. award practice to helper/watcher Characters using baseline helper behavior;
4. remove the partial project **except** special appliance/vehicle constructions whose post-special still needs committed components;
5. unless `post_flags` contains `keep_items`, find eligible adjacent item-placement tiles and move all items off the construction site to **one randomly selected** eligible adjacent tile;
6. apply `post_terrain` to furniture or terrain if present;
7. generate declared construction byproducts;
8. emit the “finished construction” message;
9. clear the worker activity and set baseline recoil side effect;
10. run `post_special` hooks;
11. perform baseline multiple-construction follow-up behavior.

The construction skill practice amount for each required skill at difficulty `d` is:

`floor((10 + 15*d) * (1 + base_time_moves / 180000.0))`

with the practice cap/limit argument `floor(d * 1.25)`.

### 11.2 Item displacement and `keep_items`

Without `keep_items`, completion gathers adjacent radius-one tiles other than the site for which the map allows item placement, picks one eligible destination by gameplay RNG, and moves all current site items there. If no destination exists, the pinned code reports a diagnostic and does not perform displacement.

With `keep_items`, items remain on the site. Generic deconstruction definitions use `keep_items`, so recovered/deconstructed contents are not swept away by generic construction completion.

### 11.3 Byproducts

Pinned construction-definition byproducts use the item-group system and gameplay RNG. The baseline calls the spawn using the completing worker's current tile, while generic deconstruction metadata drops are spawned at the deconstructed site.

For parity, OctoGhast MUST distinguish these two origins:

- definition-level construction `byproducts`: completing actor position, matching the pinned call;
- generic terrain/furniture deconstruction drops/base item: target site.

If later upstream parity evidence justifies changing this, that is a baseline change, not a silent local cleanup.

### 11.4 Authoritative mutation transaction

OctoGhast completion must be serialized as one authoritative logical transaction for contention purposes. It may call several subsystem APIs internally, but no client may observe an impossible half-state such as “project removed but old terrain still current” as a committed revision.

Within that transaction preserve the pinned semantic ordering because post-specials may inspect/mutate the already-transformed world.

All terrain/furniture writes MUST go through Spec 12's controlled mutation API, which:

- validates absolute bounds/layer;
- updates persistent tile data;
- invalidates movement, transparency/light, path/spatial and other affected derived caches/indexes;
- preserves unrelated tile layers unless the construction rule explicitly changes them.

Item movement/spawning MUST use Specs 05/06 APIs so item identity, active-item indexes, luminance and persistence remain consistent.

## 12. Staged construction

Pinned content models many staged projects as separate construction definitions connected by terrain/furniture state, usually sharing one group.

Example: brick wall:

1. `constr_brick_wall_halfway`: dirt -> halfway wall, consumes its stage requirements and time;
2. `constr_brick_wall`: halfway wall -> finished wall, consumes another stage's requirements and time.

A “stage” is therefore normally a **completed world transform followed by a new project**, not one partial project containing a list of hidden phases.

Implementation contract:

- each concrete definition has its own requirements, base time, progress project and completion effects;
- group identity may present the chain as one user-facing goal;
- after a stage completes, the next definition becomes eligible only if the resulting world state satisfies its prerequisites;
- cancellation refunds only the currently unfinished project's committed components, never components consumed by already-completed stages;
- save/load records the actual current terrain/furniture plus any current unfinished project, so no hidden stage ordinal is needed for ordinary chains.

## 13. Generic deconstruction versus explicit removal constructions

The pinned baseline contains two complementary mechanisms.

### 13.1 Generic metadata-driven deconstruction

`constr_deconstruct` and `constr_deconstruct_simple` in `construction/misc.json` are construction activities whose completion calls `done_deconstruct`.

Eligibility:

- furniture takes precedence over underlying terrain;
- ordinary generic deconstruction rejects furniture marked `EASY_DECONSTRUCT` so the simple path handles it;
- target furniture/terrain must expose compatible deconstruction/base-item metadata.

Furniture completion:

1. verify it has disassembly data;
2. replace furniture with its declared `furn_set` or clear furniture;
3. spawn `base_item` when defined, otherwise roll its deconstruction drop group;
4. apply optional deconstruction skill practice;
5. for liquid-container furniture, attempt to put the one liquid item on the tile into recovered watertight containers;
6. remove signage metadata.

Terrain completion:

1. require deconstruction metadata;
2. when `deconstruct_above` is set, refuse if furniture occupies the tile above, otherwise recursively deconstruct above first;
3. set the declared replacement terrain;
4. spawn base item or deconstruction drop group;
5. apply optional deconstruction skill practice.

For deconstruction skill metadata with min/max and multiplier, the pinned practice amount is:

`floor(multiplier * (5/6) * (10 + 7.5 * (min + max)))`

and it is awarded only when the actor's current skill is at least the configured minimum, with the configured max passed as the practice cap.

### 13.2 Explicit construction-defined removals

`data/json/deconstruction.json` contains many ordinary `type: construction` definitions in category `DECONSTRUCT` with explicit skills, time, `using` requirements, byproducts, prerequisite terrain and post terrain.

These follow the normal construction project lifecycle rather than generic mapdata `deconstruct` metadata. OctoGhast MUST support both mechanisms because content chooses between them.

## 14. Special world-transform completion hooks

The pinned post-special table includes transformations beyond simple terrain replacement, including vehicle creation, appliance placement, wiring, graves, digging/mining stairs, ramps, matching upper/lower terrain, roof addition/removal, signs/targets and other bespoke effects.

General contract:

- specials are server-side trusted behavior selected by validated content names;
- they receive the authoritative target site and completing Character context;
- their mutations obey the owning subsystem's authoritative APIs and deterministic ordering;
- specials may produce items, vehicles/appliances, creatures, events, character costs/morale or additional tile mutations;
- if a special consumes committed component provenance (notably appliance/vehicle creation), the project remains available until that special has extracted it and then removes the project;
- special failure must not duplicate committed components or completion outputs.

This specification does not duplicate vehicle physics/appliance power semantics; those belong to their feature specs. It does require construction to hand off the committed base item exactly once and to create the resulting durable object through the authoritative vehicle/appliance boundary.

## 15. RNG and determinism

Construction consumes gameplay RNG in at least these observed places:

- random adjacent destination for ordinary completion item displacement;
- item-group byproduct/drop generation;
- grave/exhumation special outcomes and spawned item counts;
- item degradation/damage in grave results;
- any delegated item-group, spawn or post-special behavior using RNG.

Requirements:

1. server RNG is authoritative; clients never roll construction results;
2. RNG calls occur only after the relevant authoritative branch is known to execute;
3. deterministic command/activity ordering determines which contested operation consumes RNG first;
4. save/load preserves all RNG state required by Spec 20 so the same continuation yields the same results;
5. a failed/stale command rejected before its random branch consumes no construction-result RNG;
6. completion is exactly-once, so concurrent workers cannot consume completion RNG twice.

Where a pinned helper chooses from an ordered candidate sequence, OctoGhast must define stable candidate ordering before the RNG draw; ECS/hash iteration order is not acceptable.

## 16. NPC, basecamp, mapgen and EOC integration

### 16.1 NPCs and shared Characters

Pinned construction logic is already Character-based in significant paths and explicitly handles NPCs working the same project. OctoGhast generalizes this without a privileged avatar:

- any authoritative Character satisfying the same domain rules may start/continue a project;
- NPC activities use the same project state, progress currency and world mutation APIs as player-controlled Characters;
- player ownership/connection status does not change construction physics;
- helper resolution is Character/activity-contextual rather than `get_avatar()`-contextual.

Basecamp or automation systems that request construction are **command producers/schedulers**, not alternate mutation engines. They must use the same construction eligibility, requirement commitment, project and completion services.

### 16.2 Mapgen/worldgen

Mapgen may author initial terrain/furniture directly through the mapgen contract before a world state becomes live, but runtime construction is not “mapgen”. A construction post-special that needs map-like transformation must use live authoritative mutation APIs so caches, events, persistence and replication are correct.

### 16.3 EOCs/events

Pinned construction has bespoke C++ post/do-turn specials rather than a generic EOC field on every construction definition at this baseline. Nevertheless construction side effects may publish events or call systems that themselves invoke EOCs.

Spec 17 governs those invocations:

- execute on the authoritative server;
- bind the actual constructing Character/site context, never “local avatar” by implication;
- serialize shared-state effects with normal deterministic server order;
- project resulting messages/state only to permitted observers.

A future content extension adding construction EOC fields belongs in the EOC/data-schema authority; do not invent unsupported JSON fields locally for this parity slice.

## 17. Map invalidation, replication and client projection

### 17.1 Authoritative map effects

Construction can change passability, transparency, support, roofs/z-level connectivity, furniture, traps indirectly, items, vehicles/appliances and creature presence. Every such mutation must trigger the owning map subsystem's normal invalidation/index maintenance.

Construction code MUST NOT manipulate path/visibility/light caches directly as an alternative to the map mutation contract, except through explicit map-service hooks where the target subsystem requires it.

### 17.2 Replication

A construction result is not broadcast as raw server state.

Per interested client, project/world projection may include only what that player is entitled to know, such as:

- visible/known construction option and target eligibility;
- owner/participant activity/progress;
- visible unfinished-project marker when the tile is observable;
- visible terrain/furniture/item/vehicle changes;
- semantically addressed construction messages/sounds/events.

A player in another separated active region does not receive hidden construction state merely because the server simulates it. FOV, knowledge and remembered-map updates follow Spec 12. Private failure/resource diagnostics follow the initiating actor/session.

### 17.3 Client staleness

Commands should carry enough snapshot/revision identity to diagnose stale intent. Typical rejections include:

- site changed since projection;
- another project now occupies it;
- resource moved/was consumed;
- actor no longer meets skill/vision/activity constraints;
- target is no longer interactable/reachable under the command contract;
- referenced project completed/cancelled.

Rejected commands return a structured authoritative result suitable for UI refresh; they do not partially mutate world state.

## 18. Persistence and reconnect

### 18.1 Pinned CDDA reference behaviour

Submap save data persists each partial construction's:

- tile position;
- progress counter;
- construction identity;
- committed component items.

The pinned `submap_construction_load` regression test verifies two projects reload at their positions with exact counters, construction identities and component counts.

The build activity actor separately serializes its absolute construction location.

### 18.2 OctoGhast implementation contract

Persist world-owned project state with:

- absolute site/submap placement;
- **stable string construction ID**, not registry integer index;
- exact normalized progress units;
- exact committed component item state/identity required by Specs 05/06;
- project revision or equivalent conflict-generation value if needed after reload.

Persist each attached Character activity through Spec 04/20 with its stable site/project target and scheduler progress/remainder state.

Do not persist:

- socket/session IDs;
- open construction UI/filter state;
- Godot nodes/transforms;
- client-only target highlights;
- visibility/FOV caches;
- transient locks that can be reconstructed from durable command/activity state.

On load:

1. definitions/registries are finalized first;
2. project IDs are resolved;
3. missing/invalid project definitions are handled by the centralized migration/error policy from Specs 18/20, not silently remapped by numeric index;
4. world regions and derived caches rebuild;
5. Character activities resolve their project targets;
6. missing targets terminate deterministically without duplicating refunds/results.

Disconnecting a player does not delete the project. Reconnect projects the current permitted state. Whether that Character's current construction activity remains active while disconnected follows the common disconnected-Character simulation policy; no special construction-only wall-clock timer is created.

## 19. Failure and validation behavior

Implementation must explicitly handle:

- unknown construction/group/category/skill/requirement/special IDs at content load;
- no valid adjacent site;
- target fails a pre-special, terrain or flag predicate;
- post terrain already present;
- too-dark construction where the definition is not dark-craftable;
- absent/contended components or tool charges;
- an unfinished project already occupies the target;
- project disappears during work;
- worker loses required skill;
- project cancellation racing completion;
- two new projects racing for one site;
- multiple workers reaching completion in one tick;
- no valid adjacent destination for displaced site items;
- invalid/deconstructed furniture/terrain at generic deconstruction completion;
- deconstruct-above blocked by furniture;
- special hook cannot create its durable vehicle/appliance result;
- save references missing content after load/migration.

All failures are server-authoritative, deterministic for the same state/order/RNG, leave ownership indexes consistent, and never duplicate/refund resources more than once.

## 20. Black-box and parity scenarios

The following scenarios are normative acceptance fixtures. Tests may use smaller synthetic definitions where that isolates behavior, but values and ordering must match the pinned rules.

### Definitions and validation

1. **Construction schema load** — load a definition using `required_skills`, direct tools/qualities/components, `using`, pre/post terrain, flags and activity level. Assert aggregate requirements and defaults after finalization.
2. **Legacy skill form** — load `skill` + `difficulty` without `required_skills`; assert equivalent one-skill requirement.
3. **Bad registry references** — unknown group/category/skill/terrain/furniture/requirement produce deterministic validation diagnostics.
4. **Bad special name** — unknown pre/post/do-turn/failure special is rejected at load, not treated as no-op.
5. **Stable-ID reload** — change registry load order while preserving string IDs; saved project still resolves to the same construction.

### Eligibility and requirements

6. **Adjacent-only placement** — actor can target eligible same-z neighboring tiles but not its own tile or a non-adjacent tile through the normal start command.
7. **Pre-state matrix** — verify allowed and rejected targets for pre-terrain, furniture-vs-terrain flags, post-state-already-present and representative `check_empty` occupancy/trap/item/vehicle cases.
8. **Structural support** — pin at least one `check_support` fixture with fewer than two supports rejected and a qualifying supported fixture accepted.
9. **Dark construction** — insufficient fine-detail vision rejects an ordinary definition but allows an otherwise-equivalent `dark_craftable` definition.
10. **Alternative requirements** — two valid component/tool alternatives produce a server-chosen/declared concrete commitment consistent with Spec 07 and no double-counting.

### Start, interruption and resume

11. **Atomic start** — successful start consumes component items/tool charges exactly once, stores the actual components in the project and assigns an activity. Inject failure/contestation before commit and assert no partial spending/project.
12. **Occupied site** — second attempt to start a different project on a tile with an unfinished project rejects and consumes nothing.
13. **Interrupt/resume** — advance to a known counter, interrupt activity, advance world time with no worker, resume with the same or another Character and assert progress continues from the stored counter.
14. **Cancel refund** — cancel at partial progress; project disappears and each committed component is restored exactly once; completed-stage resources are not refunded.
15. **Skill lost during work** — lower the worker below a required skill before the next contribution; work stops with no additional progress.

### Timing and shared work

16. **Base progress formula** — synthetic definition with known move cost and no helpers/scaling reaches exact pinned counters for a sequence of move contributions.
17. **Helper modifiers** — same project with zero, one and at least two skilled helpers uses 1.00, 0.75 and 0.40 adjusted-time factors before construction scaling and the 100-move clamp.
18. **Continuous scheduler parity** — run the same sequence of total actor moves through one-player-server ticks and a pinned-turn differential harness; final progress/completion is identical despite different UI timing.
19. **Two-worker cooperation** — two Characters contribute to the same site in one authoritative timeline; progress is the deterministic ordered sum and components are not consumed a second time.
20. **Same-tick completion race** — two workers are scheduled when only one contribution is needed. Exactly one completion transaction runs; the second worker stops on missing/completed project; XP/byproducts/transformation/RNG occur once.

### Staged builds and world transformations

21. **Brick-wall stages** — from dirt, complete the pinned halfway brick-wall definition, assert halfway terrain and first-stage resource/time effects; then complete the final definition and assert finished wall. Cancel during stage two and prove stage one remains completed.
22. **Item displacement** — put items on a site before completion. Without `keep_items`, seeded RNG chooses one eligible adjacent destination and all site items move there; with `keep_items`, they remain.
23. **No displacement destination** — surround a site with locations that cannot accept items; completion still follows pinned behavior without corrupting/dropping the items.
24. **Terrain/furniture cache invalidation** — warm movement/LOS/light/path queries, complete a construction changing a blocking/transparent layer, and assert next authoritative queries see the new state without manual client/cache reset.
25. **Byproduct origin** — complete a definition with construction `byproducts` while the worker occupies its valid work position; assert definition byproducts spawn at the completing actor position per pinned source.
26. **Special ordering** — fixture post-special observes the already-applied post terrain and the activity already cleared, matching the pinned completion order.

### Deconstruction

27. **Generic furniture deconstruction** — deconstruct a furniture target with metadata; assert replacement/clear furniture, base item or drop-group output, signage cleanup and optional practice.
28. **Easy deconstruction split** — `EASY_DECONSTRUCT` furniture is rejected by ordinary generic deconstruction and accepted by the simple deconstruction path.
29. **Terrain deconstruct-above** — deconstruct target with `deconstruct_above`; furniture above blocks the operation; without blocking furniture, above is recursively handled before the base terrain replacement.
30. **Explicit removal construction** — run a representative `data/json/deconstruction.json` definition and assert it uses ordinary requirements/project/time/byproduct/post-terrain behavior rather than generic metadata drops.
31. **Liquid-container recovery** — deconstruct eligible liquid-container furniture with one liquid item and recovered watertight containers; assert the baseline fill behavior and leftover liquid handling.

### Persistence and multiplayer

32. **Project save/load** — save a partially completed project with committed component state, unload/reload, assert absolute site, stable construction ID, exact progress and component identities/state match; resume to the same result.
33. **Worker + project save/load** — save while a Character is actively constructing; after reload the activity resolves the same site/project and continuation neither repeats component spending nor loses progress.
34. **Disconnect/reconnect** — disconnect a player with an unfinished project; project remains authoritative; another eligible Character can continue it; reconnect receives current permitted state without replaying old private UI prompts.
35. **Same-site start contention** — two clients submit start commands for one empty site from the same projected revision. Deterministic server order yields one project and one resource spend; loser receives stale/occupied rejection.
36. **Cancel/complete contention** — cancellation and final work contribution contend in one tick. Stable server ordering determines one outcome; never both refund and completion.
37. **Separated active regions** — player A constructs in region A while player B constructs in disjoint region B. Both progress under the same canonical time; neither unloads/pauses the other; each receives only permitted projections.
38. **Overlapping visibility isolation** — two players share an active region but only A has LOS/knowledge of a remote construction transform. The server mutates once; A receives current visible change, B does not gain hidden state until B legitimately observes it.
39. **UI non-pause** — A leaves a construction menu open without issuing a command while B/world activities continue. No server construction/world time pauses.
40. **Godot isolation** — move/interpolate a client-side visual node; target eligibility, project site identity and completion use only authoritative integer absolute coordinates.

## 21. Implementation slices

A practical implementation sequence is:

1. construction category/group/definition registry adapters on Spec 18;
2. construction requirement compilation on Spec 07;
3. site predicate service over Spec 12;
4. world-owned `ConstructionProject` storage/persistence;
5. atomic start/continue/cancel commands;
6. construction activity actor and deterministic progress formula;
7. ordinary terrain/furniture completion with item displacement/byproducts;
8. generic metadata deconstruction;
9. built-in pre/do-turn/post-special host adapters;
10. NPC/basecamp command integration;
11. client DTO/projection and stale-command diagnostics;
12. differential/conformance fixtures above.

Do not implement construction by exposing mutable ECS components to the Godot client or by running separate single-player-only mutation paths.

## 22. Completion checklist for #73

This repository specification addresses every ticket criterion:

- [x] Construction/deconstruction data contracts and validation.
- [x] Eligibility and requirement resolution.
- [x] Terrain/furniture transitions and item/byproduct effects.
- [x] Time/activity/interruption/resume.
- [x] Map invalidation/cache/update responsibilities.
- [x] Actor/NPC and EOC/mapgen integration.
- [x] Save/load requirements for in-progress construction.
- [x] Black-box parity scenarios covering staged builds, invalidated/contended sites, deconstruction, interruption/resume, continuous time and multiplayer.

No new unresolved cross-cutting architecture decision was discovered. The required multiplayer adaptations fit the already-settled command/activity, active-region, item-contention, stable-ID, event and persistence contracts.
