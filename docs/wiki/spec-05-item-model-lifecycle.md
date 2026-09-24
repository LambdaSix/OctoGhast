# Spec 05 — Item model and item lifecycle

Status: investigation complete / implementation-ready specification  
Parent issue: #65  
Child issue: #70  
Reference implementation: `LambdaSix/Cataclysm-DDA`  
Pinned parity baseline: `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Purpose and parity boundary

This specification defines the Cataclysm reference-profile item domain and the smaller reusable Core capabilities required to host it. The pinned CDDA model is the behavioural/completeness oracle, not the permanent definition of OctoGhast Core. It covers item type definitions, runtime item instances, subtype capabilities, state transitions, degradation, charges, activation/ticking, perishability, use/drop hooks, faults, flags, qualities, variants, nested item contents as an item capability, and persistence identity.

The detailed algorithms for choosing pockets, ownership/location transitions, pickup/drop/wear/wield, transfer costs, overflow routing, liquid transfer, and inventory UI are intentionally deferred to #71. This spec nevertheless requires every item to expose enough content/pocket state for #71 to operate without subtype-specific special cases.

Parity is behavioral and data-contract parity. OctoGhast does not need to mirror the C++ class hierarchy, but the same JSON fixtures and equivalent state must produce the same externally observable capabilities and lifecycle results.

### 1.1 Architecture-review boundary

The pinned-CDDA investigation above remains the rules oracle. The real-time/co-op review changes **when and where** those rules are scheduled and **who may mutate/project** item state; it does not replace CDDA item formulas, thresholds, action costs, rot/temperature rules, countdown semantics, stacking rules, or subtype behavior.

OctoGhast therefore distinguishes:

1. **Pinned CDDA rule:** lifecycle calculations and observable item behavior described by this spec.
2. **OctoGhast scheduling/authority adaptation:** a continuously advancing authoritative server clock, world-owned active regions, background catch-up, server-owned item identity/mutation, deterministic request ordering, and explicit per-client projection.
3. **Presentation:** Godot may create/destroy/recycle arbitrary client objects for projected items. Those objects have no simulation identity or mutation authority.

## 2. Authoritative evidence at the pinned baseline

Primary source anchors:

- `src/item.h`, `src/item.cpp`: runtime `item`, persistent UID, charges, damage/degradation, activation/deactivation, conversion, ammo operations, type/capability queries, naming/info, contents and lifecycle behavior.
- `src/itype.h`: immutable item-type definition and subtype slots/capabilities.
- `src/item_factory.h`, `src/item_factory.cpp`: item JSON loading, finalization, subtype construction, categories, item groups, variants, faults and runtime templates.
- `src/item_contents.h`, `src/item_contents.cpp`: per-item pockets/contents aggregate and nested item capability surface.
- `src/item_pocket.h`, `src/item_pocket.cpp`: pocket state owned by items; detailed transfer policy belongs to #71.
- `src/item_location.h`, `src/item_location.cpp`: serializable handles to item instances independent of current location; detailed ownership transitions belong to #71.
- `src/savegame_json.cpp`: save/load integration for item-bearing world state.
- `doc/JSON/ITEM.md`: item JSON authoring contract.
- `data/json/items/`: core item definitions and representative subtype combinations.
- `tests/item_test.cpp`: core item behavioral regression evidence.
- `tests/item_contents_test.cpp`: nested contents, overflow and content-state evidence.
- `tests/item_pocket_test.cpp`: pocket capability fixtures; use only the item-owned capability boundary here, leaving transfer algorithms to #71.
- `data/mods/TEST_DATA/items.json`: stable synthetic fixtures used by upstream tests.

All source references in this document refer to commit `e262adb299a7613b4aedc5f12c08fe0413c56a84`.

## 3. Domain model

### 3.1 Item type vs item instance

OctoGhast SHALL separate an immutable registered item type from a mutable item instance.

An **item type** is addressed by a stable string ID (`itype_id` semantics) and contains shared definition data: name/description, physical properties, materials, phase, category, flags, qualities, use actions, pocket definitions, variants, and zero or more subtype capability records.

An **item instance** references exactly one item type and stores per-instance state. At minimum this includes persistent identity, birthday/age-relevant timestamps, charges or energy where applicable, damage and degradation, active/countdown state, temperature/perishability state where applicable, faults, instance variables, variant selection, ownership-sensitive metadata, and nested contents.

Changing an instance's type through a supported conversion must preserve instance identity and all state that the reference behavior preserves. Type conversion must not silently manufacture a new logical item.

### 3.2 Persistent identity

Each runtime item instance SHALL have a persistent unique identifier equivalent in purpose to CDDA's `item_uid`. Identity is distinct from stack equivalence: two items may stack while remaining separate logical instances until a merge operation intentionally coalesces them.

References that must survive relocation or save/load SHALL target stable item identity/location semantics rather than process memory addresses. Exact cross-location reference behavior is specified by #71; serialization requirements are shared with the persistence spec.

Copying an item for gameplay purposes must have explicit identity semantics. A clone that represents a newly created item receives a new UID; deserialization of an existing saved item restores its saved identity. Implementations must prevent accidental identity duplication through ordinary object copying.

The authoritative server owns allocation, lookup, mutation and retirement of `ItemUid`. An ECS entity ID/component address, managed-object reference, socket/session ID, Godot node/resource instance ID, scene path or client-generated token MUST NOT be the persistent item identity. Internal ECS handles may index an item while it is loaded, but any handle that crosses a persistence, transport, unload/reload or client boundary resolves through stable domain identity and current authoritative location/ownership.

Clients never create authoritative item identity by instantiating a presentation object. Creation, split, merge, conversion, destruction and transfer are server mutations; the server returns resulting stable identities/projections as appropriate.

### 3.3 Contents as composition

An item MAY own zero or more pockets. Pockets are composition, not subclasses: guns, magazines, armor, containers, tools and miscellaneous items can all expose pockets of different roles.

The item model SHALL expose its pockets/contents through a uniform API. Pocket role and constraints determine what a pocket means; callers must not infer storage semantics solely from item subtype. CDDA distinguishes container-like pockets from special pockets such as magazine, magazine-well, mod, corpse, software and migration roles.

Detailed insertion choice, capacity arbitration and transfer are #71 concerns, but #70 requires pocket definitions and current nested contents to be part of item type/instance state and serialization.

## 4. Common item data contract

Every item type SHALL support the common definition fields represented by the pinned `itype`/item loader contract, including where applicable: stable string ID and inheritance; translated name/description and presentation metadata; mass, volume, length and value; material composition and phase; melee/shared physical properties; flags and qualities; use/drop actions; pocket definitions; default charges/count behavior; variants; fault metadata; and environmental/perishability fields.

Unknown or invalid references must be diagnosed during loading/finalization according to the data-loading spec. Runtime gameplay code should operate on finalized item definitions and should not need to resolve partially loaded definitions.

## 5. Capability/subtype model

CDDA's `itype` composes optional subtype slots. OctoGhast SHOULD model these as capabilities/components rather than a mutually exclusive enum, because real definitions can combine behaviors.

The minimum Cataclysm-profile parity taxonomy is:

| Capability | Required behavior/data surface |
|---|---|
| Generic/container | physical properties, pockets, flags, qualities, actions |
| Armor | body coverage/portion data, encumbrance/protection/warmth and wearable behavior inputs |
| Gun | compatible ammo, firing modes, handling/reload inputs, gunmods, fouling/overheat state hooks |
| Ammo | ammo type, count/charges, damage/projectile data and casing/linkage behavior |
| Magazine | compatible ammo and magazine pocket/capacity behavior |
| Tool | charge/energy use, tool qualities and use-action behavior |
| Toolmod / gunmod | installation compatibility and modifiers exposed to host item |
| Comestible | nutrition/consumption metadata, spoilage/rot, temperature/freshness-sensitive state |
| Book | skill/recipe/reading metadata and read-state-facing identity |
| Bionic item | installable bionic reference and install-facing metadata |
| Seed / brewable | growth/processing identifiers and timers where represented by item state |
| Artifact/relic | generated or attached special effects and persistent generated state |
| Corpse | associated monster type, death/birthday state, revival/rot hooks |
| Craft/disassembly work item | recipe/component provenance and progress-bearing state |
| Electronic storage/software | memory capacity, stored e-files/software and browsed/read state where applicable |

This is a minimum Cataclysm-profile parity catalogue, not a generic Core restriction. The Cataclysm loader must preserve supported combinations present in the pinned data set; future profiles may define different capability vocabularies without changing generic ECS, identity, scheduling, persistence or projection infrastructure.

## 6. Charges, counts, ammo and energy

Items have multiple resource models and they must not be conflated.

**Count-by-charges items** represent a quantity in one item instance. Splitting creates a new instance containing the requested quantity while leaving the valid minimum in the source; failed splits do not mutate the source.

**Ammo-bearing items** expose compatible ammo types and current ammo through their pockets/capabilities. Setting ammo replaces existing ammo according to reference rules and may create/use a required magazine. Unsetting ammo removes ammunition while preserving an empty detachable magazine where reference behavior does so.

**Energy-storage items** expose bounded energy. Adding energy clamps at capacity; an attempted removal beyond zero reports the deficit consistently with `item::mod_energy`.

Tool charges, ammunition quantity, stack count and electrical energy SHALL remain distinct typed concepts even if UI presentation sometimes calls several of them "charges".

## 7. Damage, degradation, faults and repair state

Item damage is current condition damage. Degradation is a persistent lower bound on recoverable condition. Setting ordinary damage SHALL constrain damage so it cannot violate the degradation floor or maximum-damage bound. Setting degradation SHALL clamp it to its legal range and raise current damage when current damage is below the new degradation floor.

A privileged/internal force-set operation may bypass ordinary damage checks for migration/testing, but gameplay code must not use it as normal repair/damage behavior. Repair may reduce damage only to the degradation floor unless an explicit reference mechanic changes degradation.

An item instance may carry zero or more fault IDs. Fault eligibility comes from type/material/subtype rules and spawn modifiers; faults can alter display and behavior. Spawn/item-group definitions can attach faults probabilistically, so seeded parity tests must control RNG and compare resulting fault sets.

## 8. Active state, countdowns and ticking

An item may be inactive or active. Activation/deactivation is an explicit state transition and may convert the item to a paired type, invoke use behavior, schedule processing, or refuse/no-op when unsupported.

Active/ticking items SHALL participate in the shared active-item processing mechanism. Processing cadence uses the reference time model, never wall-clock timers. A tick may mutate charges, temperature, rot, countdown, faults, contents or type; invoke an action; emit effects; or destroy/remove the item.

For OctoGhast, all lifecycle due-times, countdowns and periodic predicates are evaluated against canonical authoritative time. Under the Cataclysm profile selected by #66, 10 canonical ticks map to one CDDA world second/turn and 100 moves; that mapping is profile configuration, not a generic Core invariant. The server's fixed simulation ticks are scheduling opportunities; they do not redefine CDDA durations or formulas. A render frame, client clock, network arrival time or local Godot timer cannot advance an item.

While an item's owning world region is active, the server processes due item work exactly once even if several players' active/interest regions overlap. Character-owned or container-nested active items follow the authoritative owner/context scheduling path and likewise cannot be ticked once per observing client.

When an item leaves active simulation, the authoritative state records enough lifecycle timing context (for example last-processed absolute time and any absolute due/wakeup time) to resume correctly. On load/reactivation, background catch-up computes elapsed canonical simulation time and applies the same pinned-CDDA lifecycle rules. Implementations MAY batch or analytically advance periods when that is observationally equivalent; they MUST step through boundaries when ordering, environmental inputs, RNG, transformations, one-shot effects, or interactions make batching non-equivalent. Catch-up ends at the activation boundary before ordinary active processing, preventing both skipped time and double processing.

Background/unloaded state is still authoritative world state. It is not a client approximation and does not freeze merely because no player observes it. If an exact environmental input is unavailable while unloaded, the owning world/environment subsystem must provide the deterministic historical/aggregate input contract required by the pinned item rule; #70 does not substitute a constant client-visible temperature.

Countdown expiry and periodic processing must be save/load stable: saving and reloading cannot reset a timer or duplicate a one-shot effect. Active state and lifecycle scheduling anchors are serialized authoritative instance state.

## 9. Use, drop and processing extension contracts

Item definitions may bind named use actions. OctoGhast SHALL expose a registry-backed action contract rather than hard-code dispatch into each subtype.

A use action receives actor/world context and the item instance, may validate availability, consume time/resources, mutate item/actor/world, activate/deactivate/convert the item, and may consume/destroy it. Failed validation must not partially apply costs unless the reference action does so.

Drop actions execute at the corresponding lifecycle point. Thrown-impact actions are distinct. Tick/process actions run from active-item processing and must return enough outcome information for the owner/container/map to safely remove a destroyed item.

Action IDs are data-facing compatibility contracts. Unsupported action IDs must fail loading/validation with actionable diagnostics rather than silently doing nothing.

## 10. Materials, flags, qualities, categories and variants

**Materials:** items may use one or multiple registered materials. Material data contributes to physical/damage/burning and other rules; gameplay queries material capabilities rather than item-name special cases.

**Flags:** flags are stable string-ID capabilities/markers. Type flags and instance/custom flags remain distinguishable where reference behavior distinguishes them.

**Qualities:** qualities expose a quality ID and level consumed by crafting, repair and interactions. The item API exposes effective quality; requirement aggregation belongs to consuming systems.

**Categories:** categories are data-driven where explicitly defined and otherwise may be derived by capability. The pinned factory fallback distinguishes guns, magazines, ammo, tools, clothing/armor, drugs/food, books, mods, bionics, weapons and other. Category must not become the primary runtime type discriminator.

**Variants:** an item type may expose named variants and an instance may select one. Variant selection is persistent instance state and must not accidentally change logical identity/type.

## 11. Perishability, rot and temperature

Perishable/comestible/corpse state must track the time/temperature inputs required to reproduce freshness and spoilage. The lifecycle supports birthday/age, accumulated rot, temperature state, pocket/container spoilage multipliers, frozen/cold/hot transitions where behaviorally relevant, transformation/removal at thresholds, and elapsed-time catch-up after unloaded periods/save-load.

Exact environmental temperature production belongs to the environment spec; #70 owns applying supplied conditions to item state. Rot and temperature progression consume authoritative elapsed `WorldTime` plus authoritative environmental history/current conditions; they never consume client/render elapsed time. Active processing and unloaded catch-up must converge on the same observable freshness/temperature state when given equivalent environmental inputs and RNG history. Golden tests use fixed timestamps and controlled temperatures.

## 12. Stacking, splitting and instance equivalence

Stackability is stricter than sharing an `itype_id`. State that can change observable behavior—damage, degradation, charges, active state, faults, variant, contents, rot/temperature, custom variables and other relevant metadata—participates in stack compatibility according to reference rules.

A stack merge preserves total quantity/resources and meaningful state. A split creates valid independent identity and partitions quantity/state according to reference behavior. #71 decides when/where to merge; #70 owns the item's `stacks_with`-equivalent predicate and split/merge invariants.

## 13. Serialization contract

The serialized form must reconstruct an observationally equivalent item. Conditionally as relevant, preserve: item type ID; persistent UID; quantity/charges/energy/ammunition-bearing contents; birthday/lifecycle timestamps; damage/degradation; active/countdown state; faults; variant; custom variables; rot/temperature; nested pocket contents and pocket state; corpse association or relic/artifact state; craft/disassembly provenance/progress; installed mods and software/e-file state.

Migration may use migration pockets/logic, but normalized runtime items must satisfy current invariants. Save/load round trips preserve UID and user-observable behavior.

## 14. Determinism and RNG

Construction/spawn may randomize default charges, faults, snippets, variants or other explicitly random fields. All randomness SHALL flow through the shared seedable RNG service.

Given identical definitions, starting state, time inputs and RNG seed, construction and lifecycle processing must be reproducible. Serialization persists the resulting state rather than re-rolling it during load.

All item mutations occur on deterministic authoritative simulation boundaries. Requests received from multiple players are admitted into the common command pipeline and assigned the server's deterministic ordering key before validation/mutation. A request validates against the latest authoritative item identity, location/owner, quantity and state at its resolution point. If an earlier ordered request moved, consumed, destroyed, merged, split or otherwise invalidated the target, a later request fails/revalidates according to the action contract and MUST NOT mutate a stale client snapshot. Network packet arrival callbacks do not mutate item state asynchronously.

For a fixed initial state and the same ordered command stream, item outcomes and emitted events must be identical independent of transport backend, render rate or which clients observe the item.

## 15. Failure and edge behavior

Required edge behavior includes invalid type IDs being diagnosed; unsupported operations being safe no-ops or explicit failures matching reference semantics; failed split/ammo/energy operations avoiding partial mutation; damage/degradation clamping; conversions preserving structural validity; active processing safely deleting/transforming items; no recursive self-containment; unsupported action IDs failing validation; save/load preserving active/damaged/faulted/perishable/nested lifecycle continuity; and unusual capability combinations remaining data-driven.

## 16. OctoGhast implementation shape — Cataclysm profile over generic Core

A suitable C# design is a deep item module with a small public surface:

- `ItemTypeId` and `ItemTypeRegistry` for finalized immutable definitions.
- `ItemType` as common definition plus capability records.
- `Item` as mutable instance with `ItemUid`, `ItemTypeId`, condition/resources/lifecycle state and `ItemContents`.
- Capability records such as `ArmorSpec`, `GunSpec`, `AmmoSpec`, `MagazineSpec`, `ToolSpec`, `ComestibleSpec`, `BookSpec`.
- Typed values for damage/degradation, charges, energy, mass, volume, length, temperature and time.
- `ItemActionRegistry` for use/drop/tick IDs.
- `ItemLifecycleService` for orchestration spanning entity/world concerns.
- `ItemSerializer` integrated with shared persistence/versioning.
- `ItemContents` as the boundary consumed by #71's transfer/location service.

Avoid exposing mutable internals or making callers switch on concrete item classes.

### 16.1 Client projection contract

The server projects item state through explicit DTOs/events rather than serializing ECS components or the full `Item` aggregate. The minimum projection is purpose-specific and may contain, when the receiving player is entitled to know it:

- stable opaque item reference/UID suitable for subsequent commands;
- type/variant and display identity needed to render/name the item;
- authoritative location/ownership reference only to the precision the client is allowed to know;
- visible quantity/charges/ammo/energy and condition state needed by the current UI;
- visible active/countdown/freshness/temperature/fault state where gameplay/UI exposes it;
- visible nested-content summary or child projections required by inventory/container UI;
- interaction/capability affordances the client needs to present, without exposing hidden rule data;
- a projection revision/version or equivalent command precondition token where useful for stale-request detection.

Projection MUST exclude arbitrary ECS component bags, server object references, internal scheduler/index handles, hidden contents, secret variables, undiscovered state and implementation-only caches. The server remains responsible for validating every command even when a client was previously projected an affordance.

World visibility/knowledge from #77 and owner/private/party/world policy from the protocol/player-state specs gate whether an item or field is projected at all. A projected item disappearing from interest does not destroy the authoritative item; a later projection may bind the same `ItemUid` to a completely different Godot presentation object.

## 17. Acceptance/conformance suite

1. **Generic identity round trip:** two instances have distinct UIDs; save/load preserves one UID/state.
2. **Count-by-charges split:** verify quantities, invalid split behavior and distinct resulting identity.
3. **Damage/degradation floor:** verify clamping, repair floor and maximum/destruction boundary.
4. **Faulted item:** deterministic fault set persists and repair transition hooks match.
5. **Activation lifecycle:** representative tool/device advances controlled turns with matching charge/state transitions.
6. **Countdown one-shot:** save before expiry, reload, advance, effect fires exactly once.
7. **Gun/ammo/magazine composition:** integral and detachable magazine cases match compatible ammo and set/unset behavior.
8. **Energy storage:** below/above capacity additions and below-zero removals match clamp/deficit semantics.
9. **Comestible perishability:** controlled time/temperature/spoilage multiplier matches freshness/rot before and after save/load.
10. **Variant persistence:** non-default variant survives save/load.
11. **Nested contents persistence:** full item tree and identities round-trip; transfer choice remains #71.
12. **Stack equivalence:** vary damage, fault, variant, active state, contents, rot and variables independently and match upstream outcomes.
13. **Use-action dispatch:** representative action IDs match validation, costs, mutation and consume/destroy outcomes.
14. **Subtype matrix:** representative armor, gun, ammo, magazine, tool, comestible, book, bionic, seed/brewable, corpse and relic/artifact fixtures expose expected capabilities.
15. **Deterministic spawn:** randomized charges/faults/variants under fixed seed produce matching complete state.
16. **Authoritative active cadence:** run a representative active/countdown item under canonical server time at different render/network frame rates; identical authoritative tick history produces identical item state and one-shot timing.
17. **Overlapping observers tick once:** two players' active/interest regions overlap an active map item; advancing N canonical turns processes the item exactly N rule opportunities, never once per player.
18. **Unload/reactivate catch-up:** process an item actively to T1, unload its region while world time advances to T2, reactivate, and compare against an equivalent continuously active oracle. Countdown, charges, rot and temperature match where supplied environmental history is equivalent and one-shot effects occur once.
19. **Background threshold crossing:** an unloaded perishable/countdown item crosses a transform/destruction/expiry boundary; activation materializes the correct post-boundary state/events without replaying the transition twice.
20. **Server identity vs Godot identity:** destroy/recreate/recycle the client's presentation node and reconnect/reproject; commands still address the same server `ItemUid`, while a stale UID for a destroyed/merged item cannot retarget a new item.
21. **Minimum projection:** inspect a visible item projection and prove it contains only the documented DTO fields needed by that view; ECS component collections, scheduler/index handles, hidden contents and server object identities are absent.
22. **Visibility isolation:** two clients with different visibility/knowledge permissions receive different projections of the same authoritative world without duplicating or mutating the item.
23. **Concurrent contention:** two players submit valid requests against the same item in the same simulation interval. Pin the deterministic server ordering key; the first resolved request succeeds, the second is revalidated against resulting state and either succeeds on the remainder or fails explicitly. Reversing the authoritative order reverses the eligible outcome, independent of socket callback order.
24. **Single-player transport equivalence:** execute the same item command/lifecycle trace through in-process one-player transport and network transport; authoritative mutations and projections are equivalent.

Where upstream behavior is ambiguous, execute the pinned implementation/test fixture and retain a differential golden result as the conformance oracle.

## 18. Dependencies and follow-on boundaries

Depends on completed typed-ID/registry/JSON-loading, time/turn, persistence and core Character contracts.

Direct dependents include #71 inventory/pockets/transfers, crafting, construction, combat, vehicles and gameplay UI.

#71 owns the location/ownership graph, pocket selection, capacity/reachability policy, transfer algorithms/costs, pickup/drop/wear/wield, liquids, cargo, hauling and stable `item_location` behavior across moves. It consumes the item identity, stackability, pocket definitions and contents state defined here.

## 19. Completion mapping to issue #70

- [x] Core item entity/value model and identity semantics are defined.
- [x] Each item subtype's data contract and runtime capabilities are catalogued.
- [x] Composition/inheritance of subtype behavior is specified.
- [x] Damage, degradation, charges, active state, rot and ticking rules are explicit.
- [x] Use/drop/tick action extension contracts are specified.
- [x] Item flags, qualities, materials and variants have defined semantics.
- [x] Serialization and stable item-reference requirements are defined.
- [x] Parity fixtures cover representative subtype combinations and lifecycle transitions.

### Architecture review required — minor

- [x] Active/ticking/countdown/rot/temperature progression is expressed against authoritative simulation time, including active and unloaded/background handling.
- [x] Item mutation/identity is server-authoritative and independent of ECS storage and Godot/client object identity.
- [x] Minimum purpose-specific client item projection is defined without exposing arbitrary ECS/internal state.
- [x] Deterministic concurrent-player contention against the same item is specified and covered by conformance scenarios.
- [x] Affected specification text and tests/scenarios are updated; pinned-CDDA rules are preserved and the OctoGhast scheduling/replication adaptation is explicit.

## 20. 2026-09-24 architecture/programme alignment re-evaluation

This is a targeted architectural reinterpretation of the completed investigation against #52, #57, #58, #64 and #65. The pinned source/data/test evidence and the completed server-time/co-op review remain authoritative and are not repeated.

### 20.1 Controlling boundary

Where earlier sections use broad wording such as "item domain", "capability" or "OctoGhast SHALL", read them under this classification:

1. **Pinned CDDA reference behaviour** — the exact item schemas, subtype combinations, formulas, ordering, state transitions and observable lifecycle outcomes evidenced at `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`.
2. **Cataclysm profile policy** — the OctoGhast rules module that reproduces those semantics. CDDA subtype taxonomy, flags, qualities, categories, variants, charges/ammo/energy meanings, damage/degradation/fault rules, tool/gun/comestible/corpse/relic behaviour, pocket roles, stacking rules and rot/temperature formulas live here unless separately justified as generic.
3. **Generic Core capability** — reusable authoritative identity/reference plumbing, immutable-definition versus mutable-instance infrastructure, deterministic fixed-step scheduling/deadlines, command admission/mutation ordering, RNG services, persistence hooks and explicit projection/audience boundaries.
4. **Future evolution seam** — later rules profiles may define different item taxonomies, resources, durability, lifecycle cadences, containment or spatial interaction without replacing Core identity/scheduling/persistence/network boundaries.

Generic Core therefore MUST NOT contain a closed engine-wide enum for armor/gun/ammo/magazine/tool/comestible/book/bionic/etc.; MUST NOT give CDDA flags, qualities, categories or materials engine-global semantics; MUST NOT require all item-like objects to expose CDDA charges, ammunition, energy, damage/degradation, faults, rot or temperature; and MUST NOT hard-code the Cataclysm 10-TPS / 100-moves-per-world-second mapping.

### 20.2 Classification matrix

| Concern | Pinned CDDA reference behaviour | Cataclysm profile contract | Generic Core capability | Future evolution seam |
| --- | --- | --- | --- | --- |
| Definition/instance | `itype` definition plus mutable `item` state | Preserve pinned fields/defaults/inheritance/finalization and observable state | Immutable registered definitions referenced by stable IDs; mutable authoritative instances | Other profiles may use different schemas/capability vocabularies |
| Stable identity | persistent item UID/location/reference semantics | Preserve item identity through conversion, relocation and save/load as specified here/#71 | Stable authoritative identity/reference resolution independent of ECS address, client or Godot | Non-item domains may reuse identity plumbing without adopting CDDA item rules |
| Subtype taxonomy | armor/gun/ammo/magazine/tool/comestible/book/etc. slots/combinations | Required parity catalogue in §5 | No subtype names are Core invariants | Profiles may add/remove/recombine capabilities |
| Flags/qualities/categories/materials | data-driven CDDA IDs and consuming rules | Preserve pinned meanings/validation | Generic typed-ID/registry infrastructure only | Different profiles may define different marker/query systems |
| Charges/ammo/energy/counts | several distinct CDDA resource models | Preserve split/clamp/ammo/magazine/tool semantics | Generic deterministic state/value primitives only where independently useful | Profiles may use different resource/accounting models |
| Damage/degradation/faults | CDDA condition floor, fault eligibility and repair semantics | Preserve §7 behaviour | Generic authoritative state + validation; no universal damage model | Alternative durability/repair models remain possible |
| Active/countdown/tick | CDDA processing and one-shot transitions | Preserve rule cadence/outcomes against authoritative time | Deterministic deadlines/cadences, elapsed-time catch-up hooks, ordering | Profiles may use different cadences or no ticking items |
| Rot/temperature | CDDA perishability/environment formulas | Preserve reference formulas/thresholds with authoritative environmental inputs | Time/deadline execution and persistence continuity only | Alternative spoilage/thermal models remain profile-local |
| Pockets/contents | CDDA pocket roles/constraints | Preserve item-owned pocket schema; #71 owns transfer policy | Stable containment/reference infrastructure only if independently reused | Other profiles may use different containment models |
| 10 TPS / 100 moves | CDDA has one-second turns and 100 moves; no host-TPS requirement | #66 selects 10 canonical ticks = 1 Cataclysm second = 100 moves | Fixed-step deterministic scheduling with profile-defined mapping | Different profile rates/action currencies require no item rewrite |
| Projection | upstream has no network projection boundary | viewer-specific item DTO/event projection | purpose-specific projection/audience infrastructure | client presentation evolves independently |

### 20.3 Definition/template data versus mutable runtime state

For the Cataclysm profile, immutable finalized definition data includes the pinned item schema, subtype capability records, flags/qualities/material/category/variant/action definitions, pocket definitions, resource capacities/defaults and lifecycle rule parameters. Mutable authoritative instance state includes `ItemUid`, selected type/variant reference, quantities/resources, damage/degradation, faults, active/countdown anchors, rot/temperature state, custom variables, nested contents, generated relic/corpse/craft state and persisted scheduling anchors.

Core owns the generic storage, identity, registry, scheduling and persistence mechanisms, not the CDDA meanings of those fields. Runtime instances reference finalized definitions by stable typed ID.

### 20.4 Authority, persistence, RNG and concurrency retained

The previous architecture review remains unchanged: the server owns item creation, identity allocation, mutation, split/merge/conversion/destruction and lifecycle progression; clients send intents and receive purpose-specific projections; unloaded/background item state remains authoritative and catches up against canonical time; deterministic same-boundary contention uses the shared command pipeline with stale-request revalidation; save/load preserves identity, lifecycle state and scheduling anchors while excluding transport/presentation state under #85; and seeded randomness is never re-rolled merely because an item reloads or reprojects.

### 20.5 Additional conformance scenarios

25. **Core/profile taxonomy isolation** — run a minimal non-Cataclysm Core test profile with stable authoritative objects and lifecycle deadlines but no gun/tool/comestible/flag/quality concepts; Core scheduling, persistence, identity and projection still operate.
26. **Alternative resource model seam** — use a profile-defined resource that is neither CDDA charges, ammo nor energy; no Core API requires CDDA resource semantics.
27. **Profile-rate isolation** — run generic lifecycle/deadline infrastructure under the Cataclysm 10-TPS mapping and a test profile with a different exact fixed-step mapping; only Cataclysm converts to 100 moves/world-second.
28. **Definition-schema isolation** — register a non-Cataclysm object definition through shared registry infrastructure without CDDA materials/flags/qualities/categories/variants; Core finalization/reference/persistence succeeds.
29. **Cataclysm taxonomy parity remains complete** — run representative subtype scenario 14 and prove the boundary split did not remove or weaken pinned CDDA capability/data requirements.
30. **Observer/transport invariance** — add/remove observers, recycle Godot item nodes and switch in-process/network transport while holding admitted commands/time/RNG fixed; authoritative identity and Cataclysm lifecycle state are identical.
31. **Future capability extension without Core edit** — add a synthetic profile-local item capability unknown to Core, persist/project it through profile adapters, and prove no engine-wide subtype enum change is required.

### 20.6 Resolution

No genuine cross-cutting architectural contradiction was found. The required correction is classification: CDDA item subtype taxonomy and its flags/qualities/charges/damage/rot/tool/gun/etc. semantics are Cataclysm-profile policy, while Core provides reusable identity, definition-instance, deterministic lifecycle scheduling, persistence/RNG, command-ordering and projection capabilities. This consumes #66's clarification that 10 TPS / 100 moves is Cataclysm-profile configuration rather than a permanent platform invariant.
