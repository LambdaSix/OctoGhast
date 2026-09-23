# Spec 05 — Item model and item lifecycle

Status: investigation complete / implementation-ready specification  
Parent issue: #65  
Child issue: #70  
Reference implementation: `LambdaSix/Cataclysm-DDA`  
Pinned parity baseline: `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Purpose and parity boundary

This specification defines the core item domain that OctoGhast must expose to reproduce Cataclysm:DDA item behavior at the pinned baseline. It covers item type definitions, runtime item instances, subtype capabilities, state transitions, degradation, charges, activation/ticking, perishability, use/drop hooks, faults, flags, qualities, variants, nested item contents as an item capability, and persistence identity.

The detailed algorithms for choosing pockets, ownership/location transitions, pickup/drop/wear/wield, transfer costs, overflow routing, liquid transfer, and inventory UI are intentionally deferred to #71. This spec nevertheless requires every item to expose enough content/pocket state for #71 to operate without subtype-specific special cases.

Parity is behavioral and data-contract parity. OctoGhast does not need to mirror the C++ class hierarchy, but the same JSON fixtures and equivalent state must produce the same externally observable capabilities and lifecycle results.

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

### 3.3 Contents as composition

An item MAY own zero or more pockets. Pockets are composition, not subclasses: guns, magazines, armor, containers, tools and miscellaneous items can all expose pockets of different roles.

The item model SHALL expose its pockets/contents through a uniform API. Pocket role and constraints determine what a pocket means; callers must not infer storage semantics solely from item subtype. CDDA distinguishes container-like pockets from special pockets such as magazine, magazine-well, mod, corpse, software and migration roles.

Detailed insertion choice, capacity arbitration and transfer are #71 concerns, but #70 requires pocket definitions and current nested contents to be part of item type/instance state and serialization.

## 4. Common item data contract

Every item type SHALL support the common definition fields represented by the pinned `itype`/item loader contract, including where applicable: stable string ID and inheritance; translated name/description and presentation metadata; mass, volume, length and value; material composition and phase; melee/shared physical properties; flags and qualities; use/drop actions; pocket definitions; default charges/count behavior; variants; fault metadata; and environmental/perishability fields.

Unknown or invalid references must be diagnosed during loading/finalization according to the data-loading spec. Runtime gameplay code should operate on finalized item definitions and should not need to resolve partially loaded definitions.

## 5. Capability/subtype model

CDDA's `itype` composes optional subtype slots. OctoGhast SHOULD model these as capabilities/components rather than a mutually exclusive enum, because real definitions can combine behaviors.

The minimum parity taxonomy is:

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

This is a minimum taxonomy, not a restriction. The loader must preserve supported combinations present in the pinned data set.

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

Countdown expiry and periodic processing must be save/load stable: saving and reloading cannot reset a timer or duplicate a one-shot effect. Where unloaded/background simulation catches up elapsed time, observable results must match reference elapsed-time semantics. Active state is serialized instance state.

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

Exact environmental temperature production belongs to the environment spec; #70 owns applying supplied conditions to item state. Golden tests use fixed timestamps and controlled temperatures.

## 12. Stacking, splitting and instance equivalence

Stackability is stricter than sharing an `itype_id`. State that can change observable behavior—damage, degradation, charges, active state, faults, variant, contents, rot/temperature, custom variables and other relevant metadata—participates in stack compatibility according to reference rules.

A stack merge preserves total quantity/resources and meaningful state. A split creates valid independent identity and partitions quantity/state according to reference behavior. #71 decides when/where to merge; #70 owns the item's `stacks_with`-equivalent predicate and split/merge invariants.

## 13. Serialization contract

The serialized form must reconstruct an observationally equivalent item. Conditionally as relevant, preserve: item type ID; persistent UID; quantity/charges/energy/ammunition-bearing contents; birthday/lifecycle timestamps; damage/degradation; active/countdown state; faults; variant; custom variables; rot/temperature; nested pocket contents and pocket state; corpse association or relic/artifact state; craft/disassembly provenance/progress; installed mods and software/e-file state.

Migration may use migration pockets/logic, but normalized runtime items must satisfy current invariants. Save/load round trips preserve UID and user-observable behavior.

## 14. Determinism and RNG

Construction/spawn may randomize default charges, faults, snippets, variants or other explicitly random fields. All randomness SHALL flow through the shared seedable RNG service.

Given identical definitions, starting state, time inputs and RNG seed, construction and lifecycle processing must be reproducible. Serialization persists the resulting state rather than re-rolling it during load.

## 15. Failure and edge behavior

Required edge behavior includes invalid type IDs being diagnosed; unsupported operations being safe no-ops or explicit failures matching reference semantics; failed split/ammo/energy operations avoiding partial mutation; damage/degradation clamping; conversions preserving structural validity; active processing safely deleting/transforming items; no recursive self-containment; unsupported action IDs failing validation; save/load preserving active/damaged/faulted/perishable/nested lifecycle continuity; and unusual capability combinations remaining data-driven.

## 16. OctoGhast implementation shape

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
