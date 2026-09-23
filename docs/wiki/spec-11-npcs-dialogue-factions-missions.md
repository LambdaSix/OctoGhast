# Spec 11 — NPCs, dialogue, factions and missions

Status: investigated / specification complete  
Tracking issue: #76  
Parent epic: #65  
Reference implementation: `LambdaSix/Cataclysm-DDA` @ `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Purpose

This page defines the implementation contract for persistent intelligent non-player Characters, authored/social interaction, factions, dialogue, trade, missions, followers and companion work in the OctoGhast Cataclysm reference profile.

It is a behavioural specification, not a port of CDDA's `npc : Character` class hierarchy. Generic Core supplies persistent actors, stable identity, commands/activities, typed registries, variables/events, inventory transfer, spatial queries and persistence. The Cataclysm profile supplies the pinned NPC attitudes/opinions, faction semantics, dialogue/trial/content schema, mission goals and compatibility rules. The authoritative server owns all runtime mutation.

## Architectural context and dependencies

This spec consumes rather than redefines:

- Spec 01 / #66: canonical authoritative time, 10 ticks per world second, CDDA move/action currency and deterministic same-tick ordering.
- Spec 02 / #67: reusable Character physiology/capability; NPCs are Characters with policy/state layered on top, not a second physiology model.
- Spec 04 / #69: commands/intents, persistent activities, interruption/cancellation and resulting messages/events.
- Spec 05/06 / #70/#71: stable item identity/location, pockets and authoritative transfers/contention.
- Spec 12 / #77: world-owned active regions, authoritative WorldPosition/SpatialCell, per-player visibility/knowledge.
- Spec 16 / #81: autonomous NPC/monster AI, scoring/pathfinding and deterministic decision execution.
- Spec 17 / #82: talker/context resolution, EOC execution, variable scopes, authoritative side effects and audience-filtered projection.
- Spec 18 / #83: typed IDs, registries, JSON inheritance/finalization/validation.
- Spec 20 / #85: server-owned saves, stable player identity, deterministic continuation, disconnect/reconnect and save barriers.
- #90: transport/session lifecycle and bounded protocol resources.

## Authoritative pinned evidence

Primary runtime anchors at the pinned commit:

- `src/npc.h`, `src/npc.cpp`, `src/npcmove.cpp`: NPC persistent/runtime state, attitudes, faction membership, follower/work/guard state, generation, body catch-up and interaction.
- `src/npc_opinion.h`: per-NPC opinion tuple `trust, fear, value, anger, owed, sold`.
- `src/dialogue.h`, `src/dialogue.cpp`, `src/npctalk.h`, `src/npctalk.cpp`, `src/dialogue_chatbin.h`: dialogue topics/responses, trials, conditions/effects, talker context, mission selection and conversation effects.
- `src/talker_npc.*`: NPC talker adapter used by conditions/effects/EOCs.
- `src/faction.h`, `src/faction.cpp`: faction templates, mutable factions, relations, membership, food/wealth, pricing and persistence.
- `src/mission.h`, `src/mission.cpp`, `src/missiondef.*`: mission definitions, instances, assignment, goal processing, deadlines, success/failure and follow-ups.
- `src/npctrade.*`, `src/npctrade_utils.*`: valuation, willingness, debt/favor and item transfer.
- `src/npc_class.*`: NPC class distributions, shop item groups, price rules, restock interval/work hours and validation.
- `src/mission_companion.*`: off-map/companion mission state and resolution.
- `data/json/npcs/npc.json`: NPC templates binding class, attitude, mission/duty, initial talk topic, faction and optional offered mission.
- `data/json/npcs/factions.json`: faction definitions and directional relationship flags.
- `data/json/npcs/missiondef.json`: mission definitions, goals, deadlines, dialogue, start/end/fail effects and follow-ups.
- `data/json/npcs/npc_behavior.json`: behaviour-tree priorities for combat, investigation, needs, duty, following, player orders, camp work and free time.
- NPC dialogue JSON under `data/json/npcs/`: `talk_topic` definitions with dynamic lines, conditional responses, trials and effects.

Important tests/evidence:

- `tests/npc_test.cpp`: NPC needs/load catch-up, behaviour, shop/work schedule, item use and follower behaviour fixtures.
- `tests/mission_test.cpp`: condition-goal processing and automatic find-item mission processing, including the invariant that one item cannot satisfy two simultaneous missions in one batch.
- JSON test dialogue topics exercise player/NPC traits/effects, location, role/class, ally count, follower rules and needs conditions.

Where runtime and prose disagree, pinned runtime/tests win. Where an exact formula is not covered here, port a focused differential/golden fixture before relying on an inferred rule.

## 1. Reference model versus platform model

### 1.1 Pinned CDDA reference behaviour

CDDA models NPCs as Character-derived persistent actors with additional social/AI state. Dialogue is a talker-context graph evaluated against the current player/NPC pair. Missions are world objects with unique IDs, one assigned player Character ID and optional giver/target NPC IDs. Factions combine definition-like values with mutable world reputation/resources. Many interactions are initiated from a blocking single-player UI and frequently refer to “the player/avatar”.

### 1.2 OctoGhast Cataclysm profile

The Cataclysm profile preserves observable NPC attitudes, follower rules, dialogue branching/trials/effects, mission goals/deadlines/rewards, faction relations/reputation, trade valuation and companion semantics. CDDA action costs and activity durations remain simulation currency.

### 1.3 Generic Core contract

Core needs reusable capabilities, not CDDA-specific enums:

- stable persistent actor identity and actor-to-controller/session relationships;
- typed immutable definitions and mutable instances;
- relationship/reputation stores addressable by stable subject/object IDs;
- contextual conversation evaluation with explicit participants;
- authoritative commands and transactional effects;
- objective/quest instances with stable IDs, owners, lifecycle and deadlines;
- deterministic scheduling and RNG streams;
- persistent work/assignment state;
- explicit per-recipient projections.

Core must not require every future game to use CDDA's `NPCATT_*`, `MGOAL_*`, faction reputation tuple, tile-aligned positions, or one mission owner.

### 1.4 Future evolution seams

Keep Cataclysm-specific attitude enums, mission-goal taxonomy, barter formulas, follower rule vocabulary and faction fields in the profile. Future OctoGhast rules may support richer relationships, party/shared quests, continuous-space actors, different economies or different dialogue models without changing Core identity/persistence/authority contracts.

## 2. Immutable definitions versus mutable runtime state

### Immutable/finalized definition data

After registry finalization, these are content definitions:

- NPC template ID and template defaults: name/suffix, class, gender/age/stat overrides, personality defaults, initial attitude/duty/chat/faction/mission offer;
- NPC class: stat/personality/skill distributions, trait/spell/bionic/proficiency inputs, shop groups, price rules, restock interval, work hours and sell-belongings policy;
- faction template: ID/name/description, initial reputation values, size/power, currency, price rules, relationship flags, monster faction, epilogue rules and initial food/wealth policy;
- dialogue topic graph: topic IDs, dynamic-line expressions, speaker effects, responses, visibility conditions, trials, success/failure effects and next topics;
- mission type: ID/name/description, goal, origins, difficulty/value, deadline expression, urgency, generic-reward policy, item/group/container/monster/NPC target parameters, follow-up, dialogue text, goal condition and start/end/fail effects;
- behaviour tree/rule definitions and follower-rule vocabulary.

Definitions use stable typed IDs and validation from Spec 18. Runtime state must never mutate a shared definition to represent one NPC, faction, conversation or mission.

### Mutable runtime state

At minimum persist when semantically relevant:

**NPC instance**
- stable Character/entity ID and template/class references;
- Character state from Spec 02 and inventory/equipment from Specs 05/06;
- authoritative position and active/background status;
- current/previous attitude and duty/mission;
- faction membership;
- personality;
- `op_of_u`-equivalent social opinion state: trust, fear, value, anger, owed, sold;
- follower rules and overrides;
- known/discovered relationship state;
- persistent guard post, base/camp association, long-term goal/order state;
- current authoritative activity/work assignment;
- dialogue chatbin state: first/special topics, offered/assigned mission references, selected training where it must survive interruption;
- shop restock deadline and other authoritative schedule deadlines;
- companion mission identity, origin/role/destination, departure/return times, exertion/travel and mission inventory;
- persistent complaint/social cooldowns and other behaviour that affects future observable choices.

Ephemeral pathfinding caches, threat evaluation caches, UI history, hotkeys, rendered portrait state, socket/session state and transient dialogue widgets are not save authority.

**Faction instance**
- stable faction ID;
- mutable likes/respects/trusts toward the relevant Cataclysm player relationship;
- known state;
- mutable size/power/wealth/food supply and theft policy where runtime changes them;
- membership keyed by stable Character IDs;
- any mutable relationship/reputation values introduced by content effects.

**Mission instance**
- stable mission instance ID distinct from mission type ID;
- mission type ID;
- lifecycle status: `yet_to_start`, `in_progress`, `success`, `failure`;
- assigned player Character ID;
- giver/related NPC ID and target NPC ID;
- target position/dimension;
- goal-specific item/group/container/monster/species/recruit-class data and counts;
- deadline;
- value/reward state;
- step/progress and follow-up type;
- variables/state required by scripted conditions/effects.

**Conversation**
A live conversation context is runtime server state keyed by a conversation/session ID and explicit alpha/beta talker stable IDs. It contains the topic stack/current topic, selected mission/training references, per-conversation variables/conditionals and any pending choice metadata. It is not a client-owned object.

Ordinary conversational navigation need not survive server restart unless an effect/activity has already committed durable state. Durable effects commit immediately through their owning domain.

## 3. NPC identity, faction membership and social state

NPC identity is stable across active-region unload/load, save/load and client DTO boundaries. Connection IDs and Godot object IDs are never NPC identity.

Pinned CDDA distinguishes:

- **attitude**: current behavioural stance such as neutral/talk/follow/lead/wait/mug/kill/flee/heal/activity/guard-related duties;
- **personality**: aggression, bravery, collector and altruism, normally constrained to -10..10 for generated NPCs;
- **opinion of player**: trust, fear, value, anger, owed and sold;
- **faction membership/relations**: faction identity and directional relationship flags;
- **follower rules**: engagement, aim, CBM reserve/recharge and boolean ally rules.

These are separate state dimensions. “Friendly” must not collapse them into one boolean.

NPC state transitions caused by attack, theft, dialogue, recruitment, mission completion or faction changes must go through authoritative mutations that update all affected dimensions atomically enough that subsequent AI/dialogue queries see a coherent result.

The Cataclysm profile preserves named attitude semantics. Generic Core exposes relationship/capability queries and does not embed the enum.

## 4. Factions

### Definition and instance

Pinned faction definitions include initial `likes_u`, `respects_u`, `trusts_u`, `known_by_u`, size, power, food supply, wealth, currency, price rules, monster-faction mapping, epilogue conditions and directional relationship flags.

Relationship flags include:

- kill on sight;
- watch your back;
- share my stuff;
- share public goods;
- guard your stuff;
- lets you in;
- defend your space;
- knows your voice.

A missing/false flag is not equivalent to a reciprocal relationship. Relations are directional data and must be queried from the subject faction toward the other faction.

Membership uses stable Character IDs. Adding/removing membership cannot change the Character's identity.

### Multiplayer adaptation

Pinned `likes_u/respects_u/trusts_u` are effectively avatar-facing global fields. OctoGhast must not silently reinterpret one connected player as “u”. For Cataclysm parity in a co-op world, faction reputation toward players is keyed by stable player/Character identity unless a content effect explicitly targets a shared/global faction value. Existing pinned content that says `u` resolves to the initiating talker/player context from Spec 17.

Faction-to-faction relations remain world-global. Player-specific reputation is private/owner-visible unless gameplay explicitly exposes it.

## 5. Dialogue graph and talker contract

### Topic evaluation

A `talk_topic` has a stable string ID. Dialogue evaluates:

1. explicit alpha/beta talkers and context;
2. the current topic's dynamic line;
3. speaker effects where defined;
4. candidate responses in definition order;
5. each response condition/show condition;
6. response text and optional trial;
7. selected response;
8. trial success/failure;
9. corresponding ordered effects/opinion changes;
10. next topic (`TALK_NONE` keeps/returns according to graph semantics; `TALK_DONE` ends).

Response order is observable and must be deterministic. Conditions are queries and must not mutate state. Effects are authoritative mutations and execute in declared order.

A response can be hidden when its condition fails or shown disabled/with a reason according to `show_always` / `show_condition` semantics. The client receives only the already-authorized response projection needed to render the choice; it does not receive arbitrary server predicates, hidden variables or ECS state.

### Trials

Pinned trial types are none, lie, persuade, intimidate, skill check and condition. `difficulty` is a base success percentage for probabilistic social trials before Character/trait/bionic/etc. modifiers; skill/condition trials use their corresponding contract.

Trial chance calculation is deterministic for a given authoritative state. A probabilistic roll consumes the server-owned deterministic RNG stream exactly once when the choice is resolved, never when merely rendering/previewing responses. Reopening a UI or reconnecting must not reroll an already-resolved choice.

### Talker/EOC integration

All dialogue conditions/effects use Spec 17's explicit talker context. `u` means the initiating player-controlled talker for that conversation, not a process-global avatar. `npc`/beta means the addressed NPC (or other explicit beta talker). Player, NPC, context, faction and global variables retain their defined scopes.

Dialogue effects may invoke EOCs, mission mutations, faction/opinion changes, inventory transfers, activity starts, NPC rule changes, spawn/map effects and messages. They execute server-side; only resulting authorized projections are sent to clients.

### Continuous-time/co-op adaptation

Opening dialogue never pauses canonical time. A conversation is not an ECS/world lock. While a player is choosing a response, NPC physiology, world events and other actors continue.

Each player-NPC conversation has isolated context. Two players may have simultaneous read/query contexts involving the same NPC. Mutating choices are commands resolved at deterministic simulation boundaries. If one choice makes another stale (NPC dies/leaves/becomes hostile, item/mission no longer available, relationship condition changes), the later command is revalidated and rejected or regenerates the available topic/responses; it must not apply effects against stale client assumptions.

## 6. Mission definitions and lifecycle

### Definition fields

Pinned mission definitions support:

- origins: game start, opener NPC, any NPC, secondary/follow-up, computer;
- goals including go-to position/type, find item/any mission item/item group, find monster/NPC, assassinate, kill monster(s)/type/species/nemesis, recruit NPC/class, computer toggle, talk to NPC and arbitrary condition;
- difficulty/value/urgency;
- fixed or ranged deadline;
- target item/group/container/count, NPC/class, monster/species/count and overmap destination;
- follow-up mission type;
- dialogue strings;
- start/end/fail scripted effects;
- dynamic goal condition;
- generic reward policy.

Definition loading validates referenced IDs and goal-specific requirements after registries finalize.

### Instance lifecycle

Normative lifecycle:

`yet_to_start -> in_progress -> success`
or
`yet_to_start/in_progress -> failure`.

Assignment binds the instance to one stable player Character ID in the pinned model. Assignment initializes deadline/goal/start effects as defined. A mission cannot be concurrently assigned to two different player Characters unless a future explicit shared-mission Core feature is used; the Cataclysm profile preserves single-assignee semantics.

`is_complete` is a query. `wrap_up` commits completion effects/rewards/status. Some goals can be detected automatically; others require reporting to the relevant NPC. A goal becoming true must not imply that end/reward effects have already executed unless pinned behaviour does so.

Failure is terminal and executes fail effects once. Success is terminal and executes completion/end effects once. Follow-up creation/availability occurs according to pinned content after successful completion, not on mere objective detection.

### Goal processing and ordering

Mission processing runs against authoritative current state and stable IDs. Deadline expiry uses canonical world time, not wall-clock/client time. When a deadline and a completion-causing event occur at the same canonical boundary, use Spec 01 deterministic phase/order; conformance tests must pin the chosen Cataclysm-profile ordering and replay it identically.

Find-item processing must reserve/consume objective evidence deterministically: the pinned test demonstrates that one item cannot complete two simultaneous find-item missions in the same processing batch. OctoGhast must therefore process candidate missions in stable order and prevent the same item instance from satisfying multiple exclusive objectives in one resolution pass.

Kill/talk/recruit/computer objectives are driven by authoritative events/state, not client claims.

### Multiplayer ownership

A mission's assignee is a stable player Character ID, not connection ID. Disconnect does not unassign it. Another player completing a world event does not automatically receive/complete someone else's mission unless the pinned objective is world-global and the mission's condition becomes true for its assignee under the content rules.

Mission UI/projection is owner-scoped by default. World changes caused by mission effects are projected normally according to visibility/knowledge. Mission text/private progress is not broadcast merely because players share an active region.

## 7. Trading and barter

Trade is an authoritative transaction over real item locations and Character inventories.

Pinned trade APIs distinguish:

- adjusted item price from buyer/seller and item;
- final trading price;
- NPC debt/credit (`owed`) and total sold/donated value;
- willingness to accept the balance;
- whether the NPC can physically fit received items;
- faction/NPC-class price rules, whitelists/blacklists and shop groups;
- whether the NPC sells worn/wielded belongings;
- trust-gated shop groups;
- shop restock interval/work hours;
- item transfer, including map-origin items and escrow paths.

### Transaction contract

A client may request a proposed trade and query a quoted projection. The authoritative commit revalidates:

1. both actors still exist and are eligible to trade;
2. referenced item locations/revisions are still valid;
3. quantities/charges still exist;
4. sell/buy policy still permits each item;
5. final authoritative price/balance;
6. receiver capacity/pocket constraints;
7. faction/trust/work-shift constraints;
8. no competing transfer already consumed/moved the item.

On success, item transfers and owed/balance updates commit as one logical transaction. On failure, no partial authoritative transfer remains. Inventory transfer mechanics use Spec 06.

Quotes are not permanent locks. If another player buys the same unique item first, the stale trade is rejected/requoted deterministically.

Shop restock uses canonical deadlines. It does not depend on opening the trade UI and cannot be triggered repeatedly by reconnect/UI reopen. RNG used to populate stock is authoritative and deterministic from the appropriate stream/state.

## 8. Followers, orders, duty and work

Pinned follower state includes:

- engagement modes: none/close/weak/hit/all/free-fire/no-move;
- aim modes: convenient/spray/precise/strictly precise;
- CBM recharge/reserve policies;
- ally rules such as guns, grenades, silent weapons, friendly-fire avoidance, pickup, bashing, sleeping, pulping, door/lock handling, follow distance, noise handling and engagement forbiddance;
- override-enable/override values for temporary danger policies;
- pickup whitelist;
- guard post, go-to order, camp/base assignment and NPC mission/duty;
- job priorities for multi-activities.

Commands that change follower rules/orders are authoritative commands addressed to a stable NPC ID and validated against the issuing Character's relationship/permission. They do not directly mutate AI from the client.

Spec 16 consumes these values as policy inputs. Combat danger may temporarily override configured rules where pinned behaviour does; overrides remain distinguishable from the player's stored preference.

Guard/work/camp actions that take time use Spec 04 activities and Spec 01 canonical time. A player opening the rules UI does not pause the follower.

## 9. Companion/off-map missions

Pinned companion missions are distinct from ordinary quest instances. Runtime state includes mission ID, origin/role, optional destination, departure/expected-return times, travel/work duration/exertion and mission inventory.

OctoGhast represents an away companion as the same stable NPC identity in a background/off-map assignment state, not as a new NPC on return. The NPC cannot simultaneously be interactable in an active region and away on a companion mission.

Departure is an authoritative state transition after equipment/inventory requirements are validated. Return eligibility is based on canonical world time. Outcome RNG is server-owned and deterministic. If resolution requires intermediate world simulation rather than a closed-form result, that requirement must be explicit for the specific mission; otherwise deterministic elapsed-time resolution is preferred.

Reconnect has no special effect on companion timers. Save/load restores the same deadline/state/RNG continuation.

## 10. NPC generation, shop schedules and RNG

NPC generation from template/class may roll stats, personality, skills, traits, inventory and other distributions. Supported distribution forms include constants, one-in, dice, integer RNG ranges, sums and products.

All such rolls consume an authoritative deterministic generation stream. Stable content order and stable RNG consumption are required for replay/save continuation; client rendering/querying must consume no simulation RNG.

Shop stock/restock, random mission selection, probabilistic dialogue trials, companion outcomes and any random NPC behaviour use explicit authoritative RNG domains/streams or an equivalent deterministic sequencing contract. Save state must contain enough RNG state/counters to continue identically per Spec 20.

Work hours are canonical world-clock policy. The pinned NPC class default is [0,24), with wraparound intervals such as [22,6). Shift reconciliation must use authoritative chronology for both active and reloaded/background NPCs.

## 11. Time, active/background simulation and disconnect

### Pinned CDDA reference behaviour

CDDA commonly evaluates NPC actions in the active map around the avatar, catches NPC state up on load, and presents dialogue/trade through blocking local UI. Mission deadlines use game chronology.

### OctoGhast adaptation

- Server/world active regions may be disjoint or overlapping and are not owned by one avatar.
- Active NPCs in any active region progress under the same canonical clock and AI scheduler.
- An NPC covered by overlapping player regions is simulated exactly once.
- Background/unloaded NPC physiology, schedules, missions and companion work catch up from authoritative chronology according to their owning specs before current state is projected.
- Dialogue/trade/UI never pause the world.
- Player disconnect does not freeze NPCs, faction state, mission deadlines or companion work.
- Reconnect rebinds the stable player identity and receives fresh authorized projections; it does not restore stale UI assumptions.

## 12. Commands, queries, activities and events

### Commands/intents

Examples:

- start/end conversation;
- choose dialogue response;
- accept/reject/turn in mission;
- propose/commit trade;
- recruit/dismiss follower;
- change follower rule/order/guard post;
- assign/cancel NPC work or companion mission;
- request training/service.

Every mutating request carries stable target IDs and enough revision/context identity to detect stale state. The server revalidates at execution.

### Synchronous queries

Examples:

- currently available dialogue line/responses;
- trial chance preview where the pinned UI exposes it;
- mission summary/progress visible to the player;
- trade quote and refusal reasons;
- follower rule/status view;
- faction information the player knows.

Queries consume no RNG and mutate no simulation state.

### Activities

Training, reading, crafting, construction, work and other duration-bearing interactions use Spec 04 actor-owned activities. A dialogue effect may start an activity, but the activity thereafter progresses independently of the dialogue UI.

### Events/messages

State transitions emit domain events/messages such as mission assigned/completed/failed, faction reputation changed, NPC recruited/dismissed/hostile, trade committed/rejected, follower order changed and companion departed/returned. Presentation messages are derived/projection output, not mutation authority.

Audience is explicit: private dialogue/mission/trade results normally go only to the initiating player; spatially observable speech/actions may go to other entitled clients; global faction/world effects use their own projection rules.

## 13. Persistence and stable references

Persist all durable NPC/faction/mission/work state described above using stable typed IDs. Raw pointers, ECS storage indices, Godot node IDs, connection IDs and bubble-local coordinates never cross save/protocol boundaries.

Mission references in NPC chat state persist as mission instance IDs and are resolved after mission registry/world load. Missing references are diagnosed and safely dropped/quarantined according to Spec 20 migration policy; they must not alias a different mission.

NPC item references use Spec 06 stable item-location identity. Absolute world/overmap positions use Spec 12 coordinate types.

At a save barrier, all committed dialogue effects/trades/orders/mission transitions before the barrier are included; uncommitted client choices are not. Transport queues and open UI state are excluded.

## 14. Validation and failure behaviour

Content validation must diagnose at least:

- duplicate/invalid NPC template, class, faction, dialogue topic or mission type IDs;
- unresolved class/faction/chat/mission/follow-up references;
- invalid faction relationship targets;
- invalid mission goal-specific item/group/NPC/monster/species/destination IDs;
- malformed deadline/range/distribution values;
- dialogue responses with invalid next topics, trial skills, conditions or effects;
- impossible shop item groups/price-rule references;
- invalid work-hour bounds or follower-rule values.

Runtime failures are deterministic and non-corrupting:

- target NPC missing/dead/unloaded when an interaction command resolves;
- conversation condition changed after projection;
- mission giver/target disappears;
- mission deadline expires while UI is open;
- duplicate completion/turn-in request;
- trade item moved/consumed by another actor;
- insufficient inventory capacity or balance;
- follower order target invalid/unreachable;
- companion already assigned/away;
- faction/member reference missing after migration.

Reject stale commands with a structured reason and fresh state/revision where useful. Never partially apply a multi-effect transaction merely because the client had previously rendered it as valid.

## 15. Client projection and privacy

Godot receives DTOs/projections, never NPC ECS/components wholesale.

A player's projection may include, when entitled:

- visible NPC identity/display information, authoritative position and presentation-relevant status;
- speech/messages audible/visible to that player;
- that player's conversation line/responses and permitted trial information;
- that player's mission list/progress;
- tradeable item projections and quote information necessary for the current trade;
- follower rules/orders for NPCs the player may command;
- faction information/reputation that player knows.

Do not expose hidden dialogue conditions, private variables, another player's mission state, another player's faction reputation, off-screen NPC inventory, AI threat caches, path plans or arbitrary faction/member state.

## 16. Conformance and parity scenarios

1. **NPC template instantiation** — load a pinned NPC template with class, faction, attitude, duty and first talk topic; instantiate twice; verify shared definitions remain immutable while each instance receives independent stable identity/runtime state.

2. **NPC save/load round trip** — persist an NPC with Character needs, inventory, opinion, faction, follower rules, guard post, work state, chat missions and restock deadline; reload and assert externally observable behaviour/state is unchanged while ephemeral AI/path/UI caches may rebuild.

3. **Faction directional relations** — define A→B and B→A differently; verify relationship queries preserve direction and missing flags do not become reciprocal.

4. **Player-scoped faction reputation** — players A and B interact with the same faction; an effect changes A's `u` reputation only; verify B's private reputation is unchanged while shared faction-to-faction/world values remain common.

5. **Dialogue branching** — a topic has responses conditioned on player trait, NPC trait, location, NPC class and follower rule; verify exact visible/disabled response set and definition order for each fixture.

6. **Dialogue context isolation** — A and B simultaneously converse with the same NPC with different player variables/traits. Verify each receives responses computed from its own alpha context and neither context/selected mission leaks to the other.

7. **Stale dialogue mutation** — A sees a trade/recruit/mission response; B changes the NPC/faction state before A selects it. Verify A's command is revalidated, invalid effects do not run and A receives refreshed state/rejection.

8. **Probabilistic trial determinism** — fixed state + RNG seed produces the same lie/persuade/intimidate result and downstream effects across headless replay and save/reload; rendering the option repeatedly consumes no RNG.

9. **Ordered dialogue effects** — response executes multiple scripted effects. Verify declared order, one execution, correct talker scopes and resulting next topic.

10. **Mission assignment lifecycle** — reserve a mission, assign to Character A, verify `in_progress`, stable assignee/giver IDs, start effects and deadline; reject assignment to B under Cataclysm single-assignee semantics.

11. **Condition goal/reporting** — reproduce pinned mission test: unmet condition stays active; met condition with an NPC giver reports complete but does not apply completion reward until wrap-up/turn-in; start-origin condition mission may auto-process according to pinned behaviour.

12. **Find-item exclusivity** — two active find-item missions require one physical item instance. Run one mission-processing batch and assert exactly one mission completes, in stable deterministic order.

13. **Deadline while UI open** — open mission dialogue immediately before deadline, advance canonical time past deadline, then submit success response. Server revalidates against current mission state and cannot resurrect/apply success to a failed mission unless pinned rules explicitly allow it.

14. **Mission disconnect/reconnect** — assign mission to stable player identity, disconnect past several ticks/deadline, reconnect through a new connection; assignment remains bound to Character identity and current success/failure/deadline state is projected.

15. **Mission event attribution** — two players fight the same target relevant to only A's mission. Authoritative death/event processing updates mission instances according to their explicit goal/assignee semantics, not “currently local avatar”.

16. **Follow-up mission** — complete and wrap up a mission with a follow-up; verify follow-up availability/creation occurs once and only after the required success transition.

17. **Trade transaction** — quote a multi-item trade, commit, verify exact item ownership/charges, capacity and owed/balance update atomically.

18. **Trade contention** — A and B receive quotes for the same unique NPC item; A commits first; B's stale commit is rejected/requoted with no duplicated item or partial balance change.

19. **Trade capacity failure** — receiver cannot fit an item at commit time; transaction fails without losing/moving any item or altering debt.

20. **Trust-gated shop inventory** — change faction trust across a shop-group threshold; verify group availability follows pinned `trust <= trusts_u` semantics and response projection changes only after authoritative mutation.

21. **Shop restock canonical time** — advance across restock deadline without opening UI; stock updates at most once for that deadline using deterministic RNG. Reopen/reconnect cannot reroll it.

22. **Follower recruitment/dismissal** — successful recruit changes faction/attitude/follow state and permissions coherently; dismissal removes follow behaviour without changing NPC stable identity.

23. **Follower rules** — set engagement/aim/boolean rule and temporary danger override; AI consumes effective rule, stored preference remains distinguishable, save/load preserves durable configuration.

24. **Guard/go-to order contention** — two authorized commands target the same follower in one tick; stable command ordering yields one deterministic final order and event sequence.

25. **NPC activity under continuous time** — assign long-running work/training; another player continues acting; activity consumes CDDA-compatible action/time semantics and progresses without dialogue/UI remaining open.

26. **Separated active regions** — NPC A near player A and NPC B near player B both simulate under one clock; no privileged-avatar/reality-bubble assumption and no ECS-wide client projection.

27. **Overlapping active regions** — both players cover one NPC; NPC physiology/AI/shop schedule advance exactly once per canonical step while each player receives independently filtered projection.

28. **Background schedule catch-up** — unload an NPC before a work-shift/restock/need deadline, advance world time, reactivate; reconcile/catch up once before current projection, with no duplicate restock/activity effects.

29. **Companion mission identity** — send NPC away, save/reload, advance past return time and return them; same stable NPC returns with preserved mission inventory/outcome and cannot also exist in active space while away.

30. **Companion RNG continuation** — save immediately before outcome resolution; repeated load with identical saved RNG state produces the same outcome and RNG consumption.

31. **Faction membership persistence** — recruit/remove member, save/load, verify membership keyed by stable Character ID and relationship checks unchanged.

32. **NPC death during conversation** — another actor kills the NPC while a dialogue is open; subsequent choice is rejected, conversation terminates, no effects target a dangling reference and death is projected according to visibility.

33. **Private projection** — A has an active mission/private dialogue variable involving a visible NPC also seen by B. B receives the NPC's public visible state but not A's mission text, hidden responses or variables.

34. **One-player server parity** — execute dialogue, mission, trade and follower scenarios through the in-process one-player transport and through loopback network transport; authoritative outcomes are identical.

35. **Malformed/stale protocol references** — invalid conversation ID, NPC ID, mission ID, item location or response revision is rejected before domain mutation and cannot access arbitrary world/ECS state.

## 17. Implementation-ready acceptance criteria

#76 is complete when the later implementation can be built directly against this contract and demonstrate:

- persistent NPC runtime state, stable identity and definition/runtime separation;
- explicit faction identity, directional relations, membership and player-scoped reputation adaptation;
- dialogue topic/response/condition/trial/effect/context semantics with deterministic ordered execution;
- mission definition schema, stable instances, assignment, objectives, deadlines, completion/failure/rewards/follow-ups;
- transactional trade/barter using authoritative item locations and inventory transfer;
- follower orders/rules and NPC work/activity interfaces;
- companion/off-map assignment identity/time/RNG semantics;
- explicit EOC/talker/event integration through Spec 17;
- authoritative continuous-time/co-op execution with no UI pause or privileged avatar;
- persistence, disconnect/reconnect and background catch-up;
- per-player projection/privacy and stale-command/contention handling;
- deterministic RNG ownership;
- automated black-box coverage equivalent to or stronger than the 35 scenarios above.

## 18. Non-goals and compatibility boundary

This spec does not require:

- reproducing CDDA's `npc : Character` inheritance or pointer-heavy chatbin/mission storage;
- copying the curses/ImGui dialogue/trade UI;
- making CDDA attitude/mission enums generic Core laws;
- binary compatibility with CDDA save files;
- redesigning AI/pathfinding already owned by Spec 16;
- redesigning EOC/variable semantics already owned by Spec 17;
- redesigning sockets/backpressure owned by #90.

Reference parity requires equivalent externally observable Cataclysm-profile behaviour and data semantics at the pinned baseline. Any later intentional gameplay divergence should be recorded as a profile/product decision rather than silently changing this reference contract.
