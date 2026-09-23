# Spec 16 — AI, pathfinding and autonomous decision systems

Status: investigated / implementation-ready specification  
Tracking issue: #81  
Parent epic: #65  
Reference implementation: `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Purpose and boundary

This page specifies reusable pathfinding and autonomous actor decision behavior required for OctoGhast reference parity with the pinned Cataclysm:DDA baseline.

It is deliberately behavioral. OctoGhast must preserve externally observable routing, target selection, threat response, pursuit/fleeing, obstacle interaction, follower-order and autonomous-need behavior without reproducing CDDA's C++ class structure or its single-avatar control assumptions.

This specification owns:

- shared tactical path request/result semantics and Cataclysm path-cost policy;
- deterministic route expansion/tie behavior required for parity fixtures;
- monster target selection, pursuit, fleeing, wandering and routing fallback policy;
- NPC danger assessment, decision-tree goals, needs/duty/follower/order arbitration and route following;
- door/open/unlock/bash/climb/field/trap avoidance inputs used by autonomous actors;
- unreachable/stuck/no-path behavior and path recomputation/backoff;
- autonomous item pickup/use and NPC activity assignment at the policy boundary;
- actor scheduling/performance constraints under canonical authoritative time;
- persistence of semantic AI state needed for continuation;
- deterministic/RNG requirements and multiplayer/server-authority adaptations;
- black-box parity and conformance scenarios.

This specification does **not** redefine:

- Character physiology/state (#67 / Spec 02);
- action/activity lifecycle (#69 / Spec 04);
- item/inventory transfer semantics (#70/#71);
- combat resolution (#74 / Spec 09);
- monster definition/lifecycle state (#75 / Spec 10);
- local map coordinates, terrain, occupancy, visibility and active-region ownership (#77 / Spec 12);
- content loading/typed IDs (#83);
- persistence ownership (#85 / Spec 20);
- transport/session mechanics (#90).

Detailed NPC dialogue/faction/mission content remains a later feature spec. Spec 16 only consumes those states where they affect autonomous decisions.

## 2. Architectural prerequisites

This contract inherits the following completed decisions and does not reopen them:

- **#66 / Spec 01:** one authoritative canonical clock at 10 TPS; 10 ticks = 1 CDDA world second = 100 move units; actors gain/spend move budget deterministically.
- **#67 / Spec 02:** Character behavior is actor-generic; there is no privileged reusable “avatar” actor.
- **#69 / Spec 04:** immediate actions, persistent activities, synchronous queries and emitted events are distinct; activities are actor-owned and serializable.
- **#75 / Spec 10:** monsters already have stable runtime identity, perception/attitude inputs, movement capabilities, move budget, active/background lifecycle and persistence.
- **#77 / Spec 12:** authoritative spatial state and active regions are server/world-owned; separated and overlapping player regions are supported; overlapping state is simulated once.
- **#83:** immutable definitions/registries and typed IDs are server-owned and finalized before simulation.
- **#85 / Spec 20:** saves are server-owned and capture canonical time plus deterministic continuation state, but not sockets, client objects or transient presentation state.
- **#90:** networking accepts validated requests at deterministic simulation boundaries and projects explicit state/events; network callbacks never mutate world state directly.

The architectural programme also requires a **Core versus Cataclysm-profile split**. Generic Core supplies reusable route/query/scheduler/state-machine capabilities. Pinned CDDA grid costs, monster heuristics, follower rules and NPC goal policy belong to the Cataclysm profile unless independently useful as generic engine concepts.

## 3. Authoritative pinned-CDDA evidence

Primary pinned runtime evidence:

- `src/pathfinding.h`, `src/pathfinding.cpp` — pathfinding settings, targets, fast straight-route optimization, weighted A* search, movement/obstacle costs, avoidance costs and z-level transitions.
- `src/monmove.cpp`, `src/monster.h`, `src/monster.cpp` — monster target rating/planning, anger/fear interactions, pursuit/fleeing, scent/sound fallback, path caching/backoff, door/bash/movement execution and patrol/wander state.
- `src/npcmove.cpp`, `src/npc.cpp`, `src/npc.h` — NPC short-term AI cache, danger assessment, action cascade, behavior-tree integration, follower rules, movement/door/bash logic, sound investigation, guard/follow/travel behavior, item pickup and need execution.
- `data/json/npcs/npc_behavior.json` — data-defined NPC decision tree and utility/fallback/sequential goal structure.
- monster JSON and monster flags consumed by Spec 10 — especially path settings and flags such as target prioritization, door opening, bashing and hazard avoidance.
- `tests/pathfinding_test.cpp` — route semantics, obstacle handling and grab-aware routing evidence.
- `tests/monster_test.cpp` — monster movement cost and pursuit/catch behavior.
- `tests/npc_test.cpp` — NPC danger, needs, follower and autonomous behavior evidence.

Where prose/content documentation conflicts with executable behavior, pinned runtime code/tests are authoritative.

## 4. Shared pathfinding contract

### 4.1 Route request

A tactical route request has:

- an authoritative start `SpatialCell`;
- a target consisting of a center cell plus acceptance radius;
- immutable movement/path settings derived from the actor;
- an optional avoidance predicate derived from actor knowledge/policy;
- the authoritative loaded-map snapshot for the current deterministic simulation boundary.

Cataclysm-profile target forms are:

- **point** — radius 0;
- **adjacent** — radius 1;
- **radius** — any cell within square-distance radius.

A successful route is an ordered list of cells to enter. The actor's current cell is not a movement step. Empty means either already at target or no usable route; the caller must disambiguate using current/target state.

Path results are transient derived state. Semantic destination/goal state may persist; a cached route generally need not survive save/load because it can be recomputed from authoritative map state.

### 4.2 Pathfinding settings

The Cataclysm profile exposes the following path inputs:

- per-damage-type bash strength;
- maximum route distance;
- maximum accumulated path length/cost;
- climb cost;
- allow opening doors;
- allow unlocking doors;
- avoid traps;
- allow stair/z-level climbing;
- avoid rough terrain;
- avoid sharp terrain;
- avoid dangerous fields;
- creature size for restrictive passages.

These are immutable inputs for one route computation. Runtime changes in actor effects/equipment/capability may produce different settings on the next computation.

### 4.3 Pinned traversal cost behavior

For ordinary ground without a special pathfinding flag, pinned `cost_to_pass` returns **2**.

For special cells:

1. rough terrain is impassable if `avoid_rough_terrain`;
2. sharp terrain is impassable if `avoid_sharp`;
3. size-restricted passages reject actors larger than the allowed size;
4. otherwise use the tile movement cost when non-zero;
5. when directly impassable, evaluate in order:
   - climbing where supported;
   - opening a door;
   - unlocking a door;
   - bashing if actor capability permits;
   - otherwise impassable.

Vehicle obstacles use equivalent open/unlock/bash policy with their own estimated costs.

Pinned avoidance adds:

- **500** cost for a known dangerous trap when trap avoidance is enabled, except NO_FLOOR handling which has dedicated vertical behavior;
- **500** cost for a dangerous field when field avoidance is enabled.

These are high penalties rather than universal hard prohibitions: the route may cross such a tile if alternatives are worse/unavailable. Actor-specific avoidance predicates may still hard-reject a non-target tile.

### 4.4 Search algorithm and ordering

Pinned `map::route` performs:

1. reject/return empty when start equals target center or start is out of bounds;
2. clip an out-of-bounds target to map bounds and retry;
3. on same z-level, attempt a straight-route fast path;
4. the fast path is accepted only if no cell has a relevant special path flag and the avoid predicate rejects none of its cells;
5. if geometric distance exceeds `max_dist`, return empty after the fast-path opportunity;
6. otherwise execute weighted A*-style search within a padded bounding box;
7. terminate empty if the best accumulated g-score exceeds `max_length`;
8. stop when any cell satisfies the target radius.

Neighbor expansion order at the pinned baseline is fixed:

`W, E, N, S, NE, SW, NW, SE`

represented by offsets:

`(-1,0), (1,0), (0,-1), (0,1), (1,-1), (-1,1), (-1,-1), (1,1)`.

Diagonal movement adds an extra **1** to g-score before cell traversal cost. Heuristic score uses the pinned distance term (`newg + 2 * rl_dist(candidate,target)`).

The priority queue orders by score and then its pair ordering. OctoGhast Cataclysm-profile conformance must therefore produce a stable route for fixed map/settings/input. Generic Core may expose a deterministic tie-break key rather than hard-code this exact C++ container behavior, but the Cataclysm profile must pin equivalent results for fixtures where route choice is observable.

### 4.5 Dynamic occupancy

The shared map route primarily models terrain/feature passability and actor avoidance policy; actual movement must revalidate dynamic creature occupancy immediately before mutation.

A route is never a reservation. Another actor may occupy, open, close, bash or otherwise alter a future step after the route was computed. Therefore:

- route computation is a query;
- movement is an authoritative action attempt;
- each step revalidates current spatial state;
- stale route steps may trigger attack, alternate local step, re-path, pause or failure according to actor policy.

This is essential for deterministic co-op contention.

## 5. Monster autonomous decision behavior

### 5.1 Target rating

Pinned `monster::rate_target` first computes approximate distance.

Invalid targets return effectively infinite rating when:

- distance is <= 0;
- for non-smart planning the distance is already not better than the current best;
- the monster cannot see the target.

For ordinary monsters the rating is distance.

For monsters with target-prioritization/smart planning:

`rating = integer_distance / target_power_rating`

when target power is positive. A hostile monster target receives +2 power from the planner's perspective. Lower rating is preferred.

This means ordinary monsters normally prefer nearer visible valid targets, while prioritized-target monsters can prefer a more dangerous target farther away.

### 5.2 Target set and attitude

The planner combines:

- player-controlled Character candidates;
- NPC candidates;
- hostile monster candidates reachable through creature tracking;
- faction attitudes;
- monster attitude toward each candidate;
- fear/anger/morale state;
- special triggers such as hostile seen/near, hostile weak, mating season and threatened young;
- friendly/docile/pet state.

Pinned CDDA contains direct `get_player_character()` branches because it has one privileged avatar. **That is reference behavior, not an OctoGhast Core contract.**

OctoGhast Cataclysm-profile adaptation:

- every simultaneously active player-controlled Character is considered using the same Character-facing visibility/attitude rules;
- no player is preferred because of connection order or “local avatar” status;
- where pinned behavior explicitly distinguishes avatar from NPC for a gameplay rule, the profile must identify the equivalent semantic category (player-controlled Character versus NPC), not a singleton object;
- equal-rated candidate selection must be deterministic from the supplied simulation/RNG stream and stable candidate ordering.

### 5.3 RNG in monster planning

Pinned monster planning uses RNG for observable choices, including:

- hostile-seen trigger increments;
- probability-based aggro transitions;
- random tie replacement among equally rated valid targets (`one_in(valid_targets)`);
- some wandering/sound choices and movement staggering;
- hallucination disappearance and miscellaneous special behaviors.

The Cataclysm profile must consume an authoritative deterministic RNG stream. Client execution never decides AI RNG. Candidate enumeration order must be stable, or the same random draw can select a different target.

Tests that assert an exact tie-selected target must pin seed/stream state. Tests not concerned with the exact tie winner may assert membership in the equally valid set and deterministic replay under the same seed.

### 5.4 Planning and pursuit state

Monster runtime semantic AI state includes, where applicable:

- destination;
- wander position and remaining wander urge;
- patrol route and next patrol point;
- target-related anger/morale/aggro state already owned by Spec 10;
- turns-since-target/thinking throttle state;
- pathfinding retry deadline/backoff when it affects future behavior.

The cached vector of route cells is derived and may be discarded/recomputed on load, provided continuation remains behaviorally equivalent.

### 5.5 Path recomputation and backoff

When a destination is within the actor's configured pathfinding distance and the existing route is absent/stale/not aimed at the local destination, the monster requests a route.

Pinned failure behavior:

- first failed route search increases a retry backoff from 2 seconds;
- repeated failures exponentially increase it up to **10 seconds**;
- while pathfinding is on cooldown, the monster may try a straight route;
- if that straight route contains a hard-avoided cell, it is discarded;
- a successful path resets backoff to 2 seconds.

OctoGhast maps these seconds to authoritative canonical time, not host wall time. At 10 TPS: 2 seconds = 20 canonical ticks; maximum 10 seconds = 100 ticks.

### 5.6 Movement priority and sensory fallback

Pinned monster movement describes its high-level priority as:

1. special attack;
2. sight-based tracking;
3. scent-based tracking;
4. sound-based tracking.

When a planned sight destination/path is unavailable, scent-capable monsters may choose a scent step. If still not moving and a sound wander target remains active, hostile monsters may move toward that sound target.

This order is part of the Cataclysm-profile contract. Generic Core should model these as policy strategies, not hard-code smell/sound as universal AI concepts.

### 5.7 Hazard avoidance

Monster `know_danger_at` can reject:

- lava;
- falls/open floor/pits depending on size/flying/climbing;
- sharp terrain;
- visible non-benign traps;
- dangerous fields;
- fire/electric fields where not immune.

Pinned behavior intentionally relaxes some hazard avoidance while actively attacking. The Cataclysm profile must preserve such exceptions rather than flattening all hazard knowledge into a universal impassability bit.

### 5.8 Obstacles

A monster movement step may, according to capability:

- move normally;
- open doors;
- bash an obstacle;
- climb or change z-level;
- attack an occupying hostile;
- select a different locally closer square;
- fail and spend/retain budget according to the pinned movement path.

Actual movement cost remains the Cataclysm move-cost result from Spec 10/12 and is deducted from the actor's move budget. AI choice does not replace action cost with milliseconds.

## 6. NPC autonomous decision behavior

### 6.1 Distinct policy from monsters

NPCs share map routing primitives but **do not use the monster planner**.

Pinned NPC behavior combines:

- regenerated perception/threat cache;
- danger assessment and target selection;
- an action cascade / behavior-tree goal selection;
- combat attack evaluation;
- follower and guard policy;
- sound investigation;
- physiological needs;
- camp/duty/free-time goals;
- persistent activities.

OctoGhast should therefore implement separate `MonsterDecisionPolicy` and `NpcDecisionPolicy` over common route/perception/action contracts.

### 6.2 AI cache

Pinned NPC short-term cache contains derived/transient values including:

- current danger, total danger and danger assessment;
- current target and ally;
- hostile/neutral/friendly perceived creature sets;
- sound alerts and current sound target;
- dangerous explosive regions;
- threat map;
- evaluated weapon/attack values;
- temporary guard target;
- committed behavior-tree goal;
- need plans and failed-target sets.

Not all cache fields are persistence state. The implementation must distinguish:

**Recomputable derived cache**
- visible creature classification;
- threat totals/maps;
- best current attack evaluation;
- ordinary route cells.

**Semantic continuation state**
- explicit guard/order destinations;
- follower rules;
- active/committed goal when persistence changes post-load behavior;
- need plan target and no-progress/failure history when required to avoid post-load dithering;
- sound/investigation state only if the feature's save behavior requires continuation;
- activities via Spec 04.

### 6.3 Data-defined decision tree

Pinned `data/json/npcs/npc_behavior.json` defines `npc_decision` as a fallback:

1. `npc_combat`;
2. `npc_investigate`;
3. `npc_priorities`.

Combat is gated by `npc_in_danger` and falls back between:

- flee when `npc_should_flee`;
- fight when `npc_has_target`.

Investigation produces `investigate_sound` while sound alerts exist.

General priorities are a utility node with:

- needs;
- duty;
- follow embarked;
- follow;
- player order;
- camp work;
- return to camp;
- free time.

Needs are themselves data-defined utility/sequential/fallback nodes for warmth, thirst, hunger and sleep.

The JSON behavior structure is immutable definition data loaded through Spec 18/#83. Predicate/score/goal names must validate against registered Cataclysm-profile implementations. Unknown names are content validation errors, not silent no-ops.

### 6.4 Danger assessment and engagement rules

NPC danger evaluation considers:

- visible hostile creatures;
- creature power/threat;
- distance and close-range pressure;
- weapon/confident range;
- ally/friendly support;
- follower engagement policy;
- whether an ally is forbidden to engage;
- follow-distance/retreat constraints;
- explosives and dangerous terrain/fields.

Pinned follower engagement modes include behaviors equivalent to:

- none;
- close;
- weak;
- hit;
- no-move;
- free-fire;
- all.

Follower rule flags include at least:

- use guns;
- use grenades;
- use silent;
- avoid friendly fire;
- allow pickup;
- allow bash;
- allow sleep;
- allow pulp;
- close doors;
- follow close;
- avoid doors;
- hold the line;
- ignore noise;
- forbid engage;
- alternate follow distance;
- lock doors;
- avoid locks.

These are Cataclysm-profile policy data, not generic Core laws.

### 6.5 Companion/follower ownership adaptation

Pinned CDDA phrases follower behavior in terms of “the player”. OctoGhast must not substitute a privileged connected client.

A companion/follower relationship must instead bind to stable domain identity, for example:

- owning party/faction plus a stable leader/issuer `CharacterId`, or
- an explicit order target `CharacterId`.

The exact stored representation may follow the eventual NPC/faction spec, but Spec 16 requires these invariants:

- disconnecting a socket does not erase follower rules/orders;
- reconnecting the same stable player identity can resume projection/control context;
- a follower cannot silently retarget to another player merely because that player is now the nearest connected avatar;
- if its leader is absent/unloaded, policy uses its documented hold/guard/return behavior rather than client-local assumptions.

This is an application of already-settled stable identity/server authority, not a new transport decision.

### 6.6 Orders and overrides

Player-originated companion orders are **commands/intents** to the authoritative server.

Admission validates:

- issuer authority over the NPC;
- referenced actor/order target existence;
- order schema/range constraints;
- current order preconditions where applicable.

Accepted orders mutate server-owned semantic order state. They may set/override:

- follow distance/engagement;
- hold/guard;
- go-to ordered position;
- allowed pickup/bash/door/combat behaviors;
- camp/duty assignment where owned by the relevant system.

The NPC's autonomous loop consumes this state on future scheduling opportunities. The client does not directly assign path cells or mutate NPC position.

### 6.7 Needs and activities

Pinned NPC needs can autonomously:

- seek/eat food;
- seek/drink water;
- seek warmth / make fire where possible;
- seek a sleep spot and sleep;
- forage/harvest or perform other work through activities.

Need target selection may remember a concrete target and failed targets to prevent rescanning/dithering.

When the chosen behavior is long-running work, NPC AI must **start/continue a Spec 04 activity** rather than implement a second hidden activity system.

Pinned examples explicitly protect surgery and spellcasting from ordinary re-evaluation while their own state machine is active; forage/harvest continue without behavior-tree re-evaluation while safe, but yield/cancel when danger rises. OctoGhast must preserve each activity's declared AI-interruption policy.

### 6.8 Autonomous pickup/use

Pinned NPC item pickup:

- is disabled for hallucinations;
- for player allies, obeys the `allow_pick_up` follower rule;
- requires available volume and mass capacity;
- searches a local area (pinned ordinary range 6);
- skips craft-reserved/live-craft items;
- evaluates value/whitelist suitability;
- may consider harvestable terrain when configured.

Actual acquisition must use the authoritative inventory/item-transfer contract (#71). AI may select a desired item/location, but cannot bypass item-location stability, contention or transfer validation.

If two NPCs/players race for the same item, deterministic command/activity resolution decides the winner. The loser receives an ordinary failed/stale acquisition result and must clear/replan rather than duplicating the item.

### 6.9 Guard, follow, sound and overmap travel

NPC policy supports:

- returning to a persistent guard position;
- holding a guard post;
- following a leader;
- embarking/following an embarked leader;
- investigating sound alerts;
- explicit go-to orders;
- overmap travel toward a goal;
- camp return/work/free time.

Guard positions and explicit orders are semantic absolute positions. Bubble-local path coordinates are transient projections only.

For overmap travel, local tactical routing is rebuilt for the currently active region. Long-distance travel goals therefore persist independently of one local route.

## 7. Movement execution, doors, bashing and local contention

### 7.1 Revalidation

Before each autonomous step, the server must revalidate:

- target cell still exists/is active enough to resolve;
- terrain/furniture/vehicle state;
- dangerous fields/traps as known by the actor;
- current creature occupancy;
- door/open/bash feasibility;
- activity/order validity.

### 7.2 NPC step behavior

Pinned NPC `move_to`:

- pauses if unable to move;
- tries alternate neighboring cells when the proposed tile is dangerous/forbidden;
- may randomize direction while stunned;
- rejects suspicious same-z “long steps” and clears path;
- applies move effects;
- attacks a hostile occupying creature unless policy forbids;
- otherwise may open/bash/move according to permissions.

The Cataclysm profile must preserve resulting observable action choice and move costs, while Core should express these as action attempts resolved through world services.

### 7.3 Simultaneous actors

OctoGhast processes autonomous actors in deterministic canonical scheduling order. When two actors target the same cell/object/door in one tick:

1. planning queries may both consider the pre-resolution state;
2. the first authoritative action in deterministic resolution order commits;
3. later actions revalidate against the updated state;
4. later actors may attack, choose an alternate, re-path, pause or fail according to policy;
5. no actor receives a client-side reservation merely because its client saw an earlier snapshot.

## 8. Fleeing and unreachable behavior

### 8.1 Monster fleeing

Monster fleeing is driven by attitude/morale/fear state and target planner results. The destination/step selection maximizes escape according to the monster movement policy while still respecting movement capabilities and hazards.

### 8.2 NPC fleeing

Pinned NPC decision policy chooses a flee goal when danger assessment says fleeing is warranted. `method_of_fleeing` returns the flee action when not in a vehicle; fleeing selects a good escape direction and routes/moves away from danger.

NPC repositioning and panic are not identical concepts. The implementation must keep combat reposition state separate from full flee state.

### 8.3 No path / stuck

Required outcomes are explicit; AI must never spin without consuming moves or changing state.

Pinned evidence repeatedly guards against infinite loops: NPC `move()` warns that every branch must eventually subtract moves or transition action; no-route travel commonly pauses, clears/rebuilds path, increments stuck state or changes goal.

OctoGhast contract:

- one decision execution opportunity must either consume actor move budget, start/advance an activity, commit a state transition with a future deadline, or return a scheduler-visible blocked/idle result that prevents a zero-cost tight loop;
- failed tactical route does not teleport or silently load arbitrary world state;
- repeated monster route failures use canonical-time backoff;
- NPC goal executors may mark a target blocked/impossible and clear/retarget it;
- stale/unreachable item/order targets must not remain permanent zero-cost loops.

## 9. Canonical time, scheduling and active-region behavior

### 9.1 Pinned CDDA reference behavior

CDDA processes monsters/NPCs in its turn-gated actor phase. Actors spend accumulated moves by repeatedly invoking their movement/decision logic. Various AI counters/backoffs are expressed in global turns/seconds.

### 9.2 OctoGhast adaptation

The server advances continuously at canonical 10 TPS. Autonomous actors are scheduled from their own move budgets exactly like other actors.

AI “thinking” is not a wall-clock task. A decision opportunity occurs only at a deterministic simulation boundary when the actor is eligible.

Rules:

- canonical tick/time and actor move budget are authoritative;
- action costs remain CDDA move units;
- path retry deadlines use canonical simulation time;
- opening a UI never pauses AI;
- one sleeping/disconnected player does not stop NPC/monster AI around other active players;
- multiple separated active regions may contain independently acting NPCs/monsters;
- an overlapping region is simulated once.

### 9.3 Active versus background actors

Detailed activation/background ownership is inherited from Spec 10/12.

For Spec 16:

- full tactical pathfinding is an **active-region** operation over authoritative loaded tactical state;
- deactivation invalidates transient tactical routes;
- durable semantic destination/order/mission/need state may persist;
- background progression may advance deadlines/high-level travel according to the owning world/NPC system, but must not fabricate tile-by-tile tactical interactions against unloaded state;
- on activation, the actor rebuilds tactical perception/path state from current world state before taking a local step.

## 10. Determinism and RNG

### 10.1 Deterministic inputs

A decision outcome is a function of:

- canonical tick and actor scheduling order;
- immutable definitions;
- authoritative actor state;
- authoritative map/spatial state;
- perception/knowledge state;
- follower/order/mission state;
- explicit deterministic RNG stream state.

### 10.2 Stable enumeration

Creature/item/candidate enumeration must have stable ordering before probability/tie logic. ECS storage order, hash iteration order, socket order and Godot node order are forbidden as implicit tie-breakers.

Recommended stable keys:

- `CreatureId` for runtime actors;
- stable item identity/location key;
- typed coordinate lexical order;
- content definition order where pinned data order is itself semantic.

### 10.3 RNG ownership

AI RNG is server-owned simulation RNG. It must be serializable/continuable according to Spec 20.

Client prediction may animate movement but must not independently choose target, flee direction, random stagger, pickup target or tie winner.

### 10.4 Tolerance-based tests

Where pinned behavior is intentionally stochastic:

- exact deterministic replay tests use a fixed seed/stream;
- distribution tests may assert bounds/relative behavior over many seeds;
- do not turn probabilistic upstream behavior into deterministic “always choose X” simply to simplify tests.

## 11. State ownership, identity and persistence

### 11.1 Immutable definition data

Registry-owned immutable data includes:

- behavior-tree node definitions;
- monster path/AI flags and definition inputs from Spec 10;
- NPC class/follower-policy definitions where data-driven;
- path cost constants/configuration belonging to the Cataclysm profile.

### 11.2 Mutable runtime state

Server-owned mutable semantic state includes:

- actor stable ID;
- semantic destination/guard/order/leader references;
- current patrol/wander state;
- follower rule values and overrides;
- committed goal/need plan when required for continuation;
- failed-target/no-progress state where behaviorally significant;
- path retry deadline/backoff;
- active activity state via Spec 04;
- mission/camp assignment via owning systems;
- deterministic RNG continuation state.

### 11.3 Transient state

Do not persist as authoritative truth:

- Godot transforms/animation;
- client-selected hover/path previews;
- connection/socket identifiers;
- raw ECS query/cache handles;
- short-lived visible-creature arrays that can be recomputed;
- ordinary tactical route vectors unless a parity fixture proves route caching itself is semantically required.

### 11.4 Stable references

All durable actor/item/position references must use the established stable identity/location contracts. Raw object pointers, ECS slot indices, client node IDs and bubble-local coordinates are not persistence references.

## 12. Projection and client boundary

AI decisions are authoritative server behavior.

Clients may receive only player-appropriate projection such as:

- visible actor movement/attacks/door interactions;
- visible or audible NPC speech/warnings;
- own follower order state when authorized;
- own party's relevant companion activity/goal summaries;
- path/order rejection messages for commands they issued.

Clients do **not** receive:

- hidden target scores;
- unseen creature lists;
- server threat maps;
- hidden NPC needs;
- off-screen routes;
- arbitrary ECS components;
- another player's private order/control metadata unless policy grants it.

A client may render interpolated movement between authoritative grid cells, but presentation transforms are not pathfinding inputs.

## 13. Failure and validation behavior

Implementation must produce explicit, deterministic behavior for:

- invalid behavior node/predicate/score/goal IDs at content load;
- path start out of bounds;
- target outside active bounds;
- max-distance/max-length route failure;
- impossible obstacle under actor capabilities;
- target dies/disappears/becomes friendly;
- leader/order target disconnects or unloads;
- item desired by AI is moved/consumed first;
- door/terrain changes after path calculation;
- destination becomes occupied;
- active region deactivates during a long-lived semantic goal;
- loaded route invalid after save/load;
- NPC branch would otherwise return without spending moves/changing state.

Content validation failures should be diagnosed during registry finalization. Runtime stale-state failures trigger revalidation/replanning, not undefined behavior.

## 14. Core/profile/evolution split

### Generic Core contract

Core should provide:

- typed authoritative positions/cells;
- deterministic route-query API over a caller-supplied traversal-cost provider;
- deterministic candidate/tie ordering primitives;
- stable actor/item identities;
- canonical scheduling and deterministic RNG streams;
- goal/state-machine hosting primitives where reusable;
- authoritative action/activity submission;
- active-region lifecycle hooks;
- persistence interfaces for semantic AI state.

### Cataclysm profile

The Cataclysm profile owns:

- exact grid movement/path costs;
- door/bash/climb semantics from pinned data;
- weighted search settings and hazard penalties;
- monster rate-target logic, trigger interactions and smell/sound fallback;
- NPC behavior tree, danger/engagement heuristics and follower-rule meanings;
- Cataclysm item-pickup/needs/work policy;
- pinned stochastic behavior.

### Future evolution seams

Core must not require forever:

- grid-only authoritative motion;
- CDDA's exact A* heuristic/cost values;
- one NPC behavior tree;
- smell/sound priorities;
- CDDA follower engagement enums;
- singleton-player semantics;
- tactical routes constrained to the pinned 132×132 bubble.

A future OctoGhast rules profile may replace those policies while retaining authoritative scheduling, identity, routing interfaces and projection boundaries.

## 15. Black-box / conformance scenarios

The following scenarios are intended for later automated tests.

### PF-01 — Straight open route

Given flat same-z terrain, actor A and target T inside max distance, route returns the deterministic straight path and does not invoke expensive special-tile routing semantics.

### PF-02 — Deterministic equal-cost fork

Given a symmetric obstacle with two equal-cost detours, fixed settings and no RNG, repeated runs and save/reload recomputation choose the same Cataclysm-profile route according to the pinned neighbor/tie contract.

### PF-03 — Dangerous field penalty

Given a short route through a dangerous field and a longer safe detour, an actor with `avoid_dangerous_fields` prefers the safe route when its added cost is <500; when the only route requires the field, the path may traverse it rather than treating it as universally impassable.

### PF-04 — Trap penalty

Equivalent to PF-03 for a known dangerous trap with trap avoidance enabled.

### PF-05 — Door capability

Two otherwise identical actors route toward a target behind a closed door. The actor allowed to open doors obtains a route with the expected door cost; an actor unable to open/bash/climb receives no such route.

### PF-06 — Max distance and length

A target beyond `max_dist` receives only the straight-fast-path opportunity; when blocked, route is empty. A route whose accumulated g-score exceeds `max_length` returns empty.

### PF-07 — Dynamic occupancy invalidates route

A and B independently plan through the same next cell. A moves first under deterministic scheduler order. B revalidates and does not overlap/teleport; it follows its actor-specific alternate/repath/pause policy.

### MON-01 — Nearest ordinary visible target

An ordinary hostile monster with two equally valid visible hostile Characters at different distances selects the nearer candidate and pursues using its move budget.

### MON-02 — Prioritize-target power weighting

A `PRIORITIZE_TARGETS` monster is presented a nearby weak candidate and a farther high-power hostile candidate. Selection follows `distance / power_rating` rather than pure nearest distance.

### MON-03 — Equal target deterministic RNG

Two equally rated candidates are enumerated in stable identity order. With fixed RNG state, repeated simulations select the same target; after save/load at the same boundary continuation remains identical.

### MON-04 — Fleeing

A fearful monster enters flee attitude due to the pinned morale/fear inputs. On its next eligible move opportunity it increases separation using legal movement and does not switch to pursuit unless its attitude state changes.

### MON-05 — Sight -> scent -> sound fallback

A monster first pursues a visible destination. After sight is lost and route is unavailable, a smell-capable monster follows the valid scent step; with no smell step but an active sound wander target, it follows sound. Costs remain normal movement costs.

### MON-06 — Route-failure backoff

A pathfinding monster is blocked from its destination. Failed searches schedule retries at canonical-time exponential backoff beginning at 2 seconds and capped at 10 seconds. No host-wall-clock timer participates.

### MON-07 — Door/bash obstacle

A door-opening monster opens a valid door before entering; a bashing-only monster attacks/bashes a bashable blocker; an actor with neither capability does not pass it.

### NPC-01 — Combat outranks ordinary priorities

An NPC with an ordinary need and a qualifying danger condition chooses the combat branch before needs/duty/follow/free-time. When danger clears, utility priorities resume.

### NPC-02 — Flee versus fight

Two equivalent NPC states differ only in the flee predicate inputs. One emits/executes flee; the other with a valid target executes attack selection. Both consume moves or transition state without a zero-cost loop.

### NPC-03 — Follower engagement order

A follower configured `forbid_engage` does not initiate attack on a target it would otherwise consider valid. Removing the rule at an authoritative boundary allows ordinary engagement evaluation.

### NPC-04 — Follow binds stable leader

Two player-controlled Characters are connected. A follower assigned to Character A follows A, not whichever player is nearest or locally rendered. A disconnect of A's connection does not silently rebind the follower to B.

### NPC-05 — Go-to order is authoritative

A client issues an authorized companion go-to command. Server validates and stores the order, NPC routes on a later decision opportunity, and every step revalidates occupancy. An unauthorized client's identical request is rejected with no world mutation.

### NPC-06 — Guard return

An NPC with persistent guard position is displaced while safe. It selects return-to-post, routes back, then holds/pauses at the post. Save/load preserves the semantic guard position even if the transient route is rebuilt.

### NPC-07 — Investigate sound

A safe NPC with a sound alert and no higher combat branch chooses investigate-sound, routes toward the absolute sound location and increments stuck/no-progress state if unable to advance.

### NPC-08 — Need starts activity

A safe hungry/thirsty/tired/cold NPC selects the appropriate data-defined need goal and, where work is long-running, starts a Spec 04 activity. An activity with a declared non-reevaluation rule continues until its interruption policy permits reevaluation.

### NPC-09 — Pickup respects follower rules and inventory authority

A follower with pickup disabled ignores a desirable item. After enabling pickup, it selects the item if capacity/value rules pass. If another actor takes the item before transfer resolution, NPC acquisition fails/replans without duplication.

### NPC-10 — No-path order

An NPC ordered to an unreachable location does not spin indefinitely. It eventually pauses/marks blocked or impossible/replans according to goal policy, and every scheduler execution either consumes moves or establishes a future retry/state transition.

### RT-01 — Renderer independence

Run identical AI seed, world state and admitted input sequence under two different Godot frame-rate/interpolation traces. Authoritative targets, routes, actions, positions and RNG consumption are identical.

### RT-02 — Two separated active regions

Two players occupy disjoint active regions containing autonomous actors. Both regions progress under the same canonical clock. AI in region A does not depend on player B's render/input cadence and vice versa.

### RT-03 — Overlapping active regions simulated once

Two players' active footprints overlap around the same monster/NPC. The actor receives one scheduling opportunity per authoritative schedule, not one per observing player.

### RT-04 — Disconnect/reconnect

A player disconnects while a companion has a persistent guard/follow/order state. World simulation follows the documented disconnected-leader policy; reconnect restores projection/control association without duplicating NPC state or changing its stable identity.

### SAVE-01 — AI continuation round trip

Save at a deterministic tick with a monster route-backoff deadline, NPC persistent order/goal state and known RNG stream position. Reload and continue with identical admitted inputs. Observable decisions and authoritative state match an uninterrupted run.

## 16. Acceptance-criteria mapping for #81

- **Pathfinding graph/cost inputs, allowed movement and tie-breaking** — Sections 4 and 15 PF scenarios.
- **Target/threat scoring inputs and observable priorities** — Sections 5.1-5.3 and 6.3-6.4.
- **Monster and NPC decision loops separated from shared primitives** — Sections 5, 6 and 14.
- **Fleeing, pursuit, obstacle/door interaction and unreachable-target behavior** — Sections 5.4-5.8, 7 and 8.
- **Companion order overrides and NPC work/activity assignment** — Sections 6.5-6.9.
- **Turn/move-budget and map-loading interactions** — Section 9 plus active/background scenarios.
- **RNG/tie-breaking deterministic/tolerance-based tests** — Section 10 and MON-03/PF-02/SAVE-01.
- **Scenario tests cover navigation, pursuit, fleeing, obstacles, companions and no-path cases** — Section 15.

## 17. Implementation boundary summary

A useful implementation decomposition is:

```text
Core
  deterministic route-query engine
  traversal-cost / avoidance interfaces
  stable candidate ordering
  canonical scheduler + move budget
  deterministic RNG
  stable identity/reference services
  authoritative action/activity gateway

Cataclysm
  CataclysmTraversalPolicy
  MonsterDecisionPolicy
  NpcDecisionPolicy
  NPC behavior-node predicates/scores/goals
  follower/engagement/needs policy
  Cataclysm route/target conformance fixtures

Server
  active-region ownership
  deterministic actor scheduling
  command admission
  persistence
  player-specific projection

Godot
  input/orders
  rendered/interpolated projected state
  no authoritative AI or path mutation
```

No runtime/gameplay implementation is performed by this investigation.
