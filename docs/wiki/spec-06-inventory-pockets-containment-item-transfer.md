# Spec 06 investigation — Inventory, pockets, containment and item transfer

**Tracking issue:** #71  
**Parent epic:** #65  
**Reference baseline:** `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Purpose

This page records the behavioral contract OctoGhast should reproduce for inventory ownership, item locations, pockets/containers, stacking/charges and transfer operations. It describes externally observable semantics rather than requiring a one-for-one C++ port.

## Authoritative evidence

Primary runtime sources at the pinned baseline:

- `src/item_pocket.{h,cpp}` — pocket types, admission constraints, capacity, pocket ranking, sealing, spilling/overflow, charge restacking and access cost.
- `src/item_contents.{h,cpp}` — an item's collection of pockets and recursive containment.
- `src/item_location.{h,cpp}` — serializable item handle across character, map, vehicle and nested-container locations.
- `src/character_inventory.cpp`, `src/inventory.{h,cpp}` — character storage, stash/transfer and inventory aggregation.
- `src/pickup.{h,cpp}` — pickup orchestration and activity-facing transfer behavior.
- `src/advanced_inv*.{h,cpp}` — map/vehicle/inventory source and destination transfer UI semantics.
- `src/handle_liquid.{h,cpp}` — liquid source/destination selection and transfer.
- `src/pocket_type.h` — pocket taxonomy.

Behavioral tests include `tests/item_pocket_test.cpp`, `tests/item_location_test.cpp`, `tests/item_pickup_test.cpp`, `tests/item_contents_test.cpp`, `tests/advanced_inventory_test.cpp`, `tests/liquid_handler_test.cpp`, and reload tests.

## Domain model

### Item locations and ownership

An item has one immediate location kind: invalid/nowhere, character, map, vehicle, or container. Container locations recursively reference a parent item, so the effective root owner/location is obtained by walking parents. OctoGhast should model this as a location/ownership graph with exactly one immediate owner for every live item instance.

Character possession includes wielded, worn and pocket-contained items. Map ownership is a tile stack. Vehicle ownership includes cargo items and vehicle-part base items. Container ownership is an item inside a specific pocket of another item.

A location handle is not an immortal object reference. CDDA explicitly documents `item_location` as invalidatable by operations including copying. A parity implementation therefore needs validity checks and must never silently retarget a stale handle to a different item.

### Stable identity and persistence

Serialized item locations encode enough information to recover character/map/vehicle/container locations. Current baseline tests use item UIDs so a map, character or vehicle-cargo reference survives index shifts/restacking; old serialized locations without a UID fall back to the historical index behavior. Vehicle base-item locations are a special invariant and omit ordinary item index/UID. Expired locations serialize as nowhere/null rather than a plausible but wrong target.

OctoGhast does not need the same JSON shape internally, but persisted references must have equivalent outcomes: resolve the same live item when it still exists, resolve nested parents recursively, tolerate collection reordering where the reference identity is stable, and fail closed when the target no longer exists.

## Pocket/container contract

### Pocket taxonomy

The baseline pocket kinds are `CONTAINER`, `MAGAZINE`, `MAGAZINE_WELL`, `MOD`, `CORPSE`, `SOFTWARE`, `E_FILE_STORAGE`, `CABLE`, `MIGRATION`, and `EBOOK`. Some are ordinary storage; others have specialized semantics. `MIGRATION` is specifically a load-time compatibility pocket that may temporarily hold otherwise invalid contents so overflow can be resolved later.

### Admission constraints

Pocket admission is a structured decision, not a boolean. Observable failure categories include wrong specialized pocket/mod, non-watertight liquid, non-airtight gas, too large, too heavy, below minimum size/length, no remaining volume, insufficient remaining weight support, missing required flag and incompatible ammo type.

Constraints include pocket type, phase, watertight/airtight properties, maximum/minimum item size/length, volume, weight, ammo restrictions, item/type/flag restrictions, holster cardinality and existing contents. Liquids and gases bypass ordinary mouth-size volume checks, but liquid storage still requires watertight containment. A watertight pocket already containing one liquid accepts compatible additional liquid and rejects a different liquid or a non-liquid. A non-liquid already present similarly prevents introducing liquid. Frozen-liquid cases have additional compatibility rules.

Magazine pockets constrain ammunition; magazine wells hold compatible magazines. Ammo-restricted pocket capacity is derived from ammo restrictions rather than simply the raw volume field.

### Capacity and nesting invariants

Insertion must satisfy both the selected pocket and every enclosing container after the insertion. A nested pocket that locally fits an item is not valid if the resulting child makes an ancestor exceed its volume or weight capacity. This invariant is important during automatic pocket selection and is directly covered by the pickup regression tests.

Capacity calculations distinguish total and remaining volume/weight and support charge-counted items. Partial insertion is valid where only part of a charged stack fits. Capacity shrinkage can leave an existing ammo pocket overfilled; the implementation reports zero remaining ammo capacity rather than a negative quantity, and overflow handling later reconciles invalid contents.

### Pocket preferences and deterministic selection

Automatic storage is affected by player pocket settings: priority, item/category whitelist and blacklist, disabled state, unloadability and collapsed presentation state. Explicit priority overrides lower-level ranking factors. The baseline then considers suitability such as item/phase-specific fit, available capacity/nesting and ultimately access/obtain cost. Holsters and positive-priority pockets receive special treatment in selection.

Parity requires deterministic selection for the same state and settings. OctoGhast should expose a pure-ish ranking decision that can be scenario-tested rather than allowing collection iteration order to decide the destination.

### Access cost

Pockets carry a base `moves` cost (defaulted by pocket data; tests/source treat it as part of pocket behavior). Obtaining an item from a nested container adds the relevant pocket access cost and may include parent acquisition cost depending on where the parent resides. Character enchantment/modifier rules can modify obtain cost. Transfer commands must debit moves through the normal action/activity system rather than mutate inventory for free.

## Transfer semantics

### Pickup

Pickup is activity-backed. Storage must already be available: baseline tests show that attempting ordinary pickup without usable storage does not implicitly wear or wield the item. Automatic storage must choose a pocket whose insertion also preserves all ancestor capacities.

Several C++ APIs copy the source value when placing it into inventory (`wear_item`, `i_add`, `item::put_in` are explicitly regression-tested). Therefore pointer/reference identity is not a portable parity contract. The observable contract is item state/UID preservation where appropriate, source removal when the transfer succeeds, correct destination ownership, and invalidation/refresh of handles that referred to the old instance.

Charge-counted pickup may fully or partially merge into existing stacks. Conservation is mandatory: destination gain plus source remainder equals the pre-transfer quantity. Partial capacity leaves the untransferred charges at the source.

### Drop, wear and wield

These are ownership transitions, not duplication. A successful operation must remove the item/quantity from its previous owner and establish exactly one new owner. Removing a worn item must also update worn state. Dropping/spilling resolves to map ownership at the effective position. Wield/wear eligibility belongs to their respective character rules, while this subsystem owns the atomic transfer and reference consequences.

### Map and vehicle cargo

Map stacks and vehicle cargo are first-class item owners. Advanced inventory transfers between character inventory, nearby map areas and vehicle cargo while enforcing source reachability, destination capacity and character carrying limits. Vehicle cargo references must remain bound to the intended item across removal/reordering of neighboring cargo entries when stable identity is available.

### Overflow and spilling

When a pocket becomes invalid/over-capacity, overflow is recursive. Child contents are checked first; contents that cannot remain are moved toward an enclosing pocket where possible or dropped/spilled to the map. Migration pockets are an exception used to load legacy/temporarily invalid contents before reconciliation. Overflow behavior must not lose quantities or duplicate items.

Containers that will spill, unsealed spill-prone pockets, and liquid-specific cases must route contents through the appropriate spill/liquid handling rather than treating all contents as ordinary solids.

### Liquids

Liquid transfer has distinct destinations: compatible item container, vehicle tank, keg/ground, or consumption where applicable. A target item must provide compatible watertight storage; incompatible mixed liquids are rejected. Map/vehicle liquid transfers may become long-running fill-liquid activities. Immediate item/ground operations charge moves (the baseline contains explicit fixed move debits in liquid handling), while vehicle refill behavior uses the vehicle refill/activity timing path.

For parity, liquid quantity must be conserved, phase changes must invalidate no-longer-applicable transfer actions, and frozen-liquid flags/state must be normalized when liquid is successfully moved into valid storage.

### Magazines and ammunition

Magazine and magazine-well pockets are specialized containment paths. A magazine well accepts compatible magazine items; magazine pockets enforce ammo type/capacity. Reloading checks current contents as well as compatibility, and different/incompatible ammunition—especially liquid versus non-liquid—cannot be silently mixed. Reload/unload move timing is covered by dedicated reload tests and should be consumed as a dependency when the ranged/reload spec is implemented.

## Stacking, charges and identity

Stackability and containment are separate. Pocket `restack` combines stack-compatible items/charges inside a pocket; transfer may merge a charge-counted source into an existing destination stack. OctoGhast should specify stack merge as a quantity transformation with deterministic survivor identity. Tests should assert conservation and reference invalidation rather than assume a C++ list node survives.

Split operations create a distinct item/stack identity for the separated quantity. A handle to the original item must continue to refer only to the original survivor, not whichever split happens to occupy its old index.

## Reachability and failure behavior

A transfer is permitted only when its source can be obtained/reached under the calling action and its destination is accessible and accepts the item. Failure is non-destructive: the source remains owned at its prior location/quantity, destination state is unchanged, and no move cost should be charged unless the surrounding action explicitly defines an attempted-action cost.

Invalid/stale item locations resolve as invalid and must not mutate a coincidentally matching item. Recursive containment must reject cycles/self-containment and should impose a safe nesting/depth strategy for persistence and traversal.

## Proposed OctoGhast implementation boundary

Use separate concepts for:

1. **Item identity/state** — stable runtime identity plus item definition/charges/state.
2. **Item owner/location** — character slot/root inventory, map tile stack, vehicle cargo/base, or parent item+pocket.
3. **Pocket definition** — immutable data-driven constraints/costs.
4. **Pocket runtime state** — contents, seal state, preferences, capacity modifiers.
5. **Transfer service/command** — validate source, choose/validate destination, compute quantity/cost, atomically mutate ownership, emit result/events.
6. **Serializable item reference** — stable identity plus enough locator context to restore or fail closed.

This fits OctoGhast's ECS direction while preserving CDDA behavior without copying its object graph.

## Acceptance/conformance scenarios

The following black-box scenarios should gate completion:

- Pick up a solid item from a map tile into worn storage; source disappears, destination owns one equivalent item and moves are charged.
- Pickup with no valid storage fails without silently wearing/wielding or deleting the source.
- Two candidate pockets choose the same destination repeatedly; player priority overrides ordinary fit/access ranking.
- A locally valid inner pocket is rejected when insertion would overflow its outer container.
- Full and partial charged-item pickup merge conserve total charges and leave the correct remainder at source.
- Split then transfer creates distinct identities; stale references never retarget by index.
- Map, character and vehicle-cargo serialized references survive unrelated item removal/reordering; a deleted target resolves invalid.
- Nested-container reference round-trip resolves the same target through its parent chain.
- Watertight empty container accepts liquid; a non-watertight container rejects it.
- A container holding liquid accepts compatible liquid and rejects a different liquid or a solid.
- Magazine well accepts only compatible magazines; magazine pocket enforces ammo type and capacity.
- Overflow after capacity reduction preserves every item/charge by moving it to a valid ancestor or map spill location.
- Pickup/drop/wear/wield each leave exactly one owner for the transferred item.
- Advanced-inventory transfer between reachable map/vehicle/character locations obeys carrying and destination capacity.
- Invalid source/destination or stale handle is non-destructive.
- Save/load preserves pocket contents, seal/settings state required for behavior, stable item references and charge quantities.

## Dependencies and follow-on work

This spec depends on the completed core Item, Character, local-map, persistence and typed-ID contracts under #65. It is a prerequisite for crafting/requirements, activities that consume/move items, ranged reload behavior, construction material consumption and inventory UI parity.

The later activity/input specs should own command interruption and UI selection details, while this spec remains authoritative for ownership, containment validation, transfer atomicity, quantities, stable references and storage-selection outcomes.
