# Spec 06 investigation — Inventory, pockets, containment and item transfer

**Tracking issue:** #71  
**Parent epic:** #65  
**Reference baseline:** `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Purpose

This page records the implementation contract for inventory ownership, item locations, containment and transfer. The pinned CDDA baseline remains the authoritative **reference evidence** for Cataclysm-profile behavior, but the platform boundary is intentionally broader: generic Core owns stable identity/location relationships and deterministic atomic transfer infrastructure, while pocket shape, capacity, ranking, reachability and move-cost policy belong to the active rules profile.

The document therefore classifies important rules as:

1. **Pinned CDDA reference behaviour** — evidence from the pinned baseline.
2. **OctoGhast Cataclysm-profile policy** — the ruleset that reproduces that evidence for the reference-parity milestone.
3. **Generic Core contract** — ruleset-agnostic identity, transaction, persistence and projection infrastructure.
4. **Future evolution seam** — behavior a later rules profile may replace without replacing Core infrastructure.

This is an architectural re-evaluation of completed evidence, not a fresh upstream investigation.

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

## Classification summary

### Pinned CDDA reference behaviour

The source/tests listed above establish CDDA pocket taxonomy, pocket admission, priority/ranking, nested capacity checks, stacking/charge semantics, liquid and magazine rules, item-location behavior, reachability and move/access costs.

### OctoGhast Cataclysm-profile policy

The Cataclysm profile consumes that evidence and reproduces CDDA-style pockets, capacities, priorities, liquids, magazine wells, grid-derived reachability and move/action costs. The selected Cataclysm timing mapping comes from Spec 01/#57; this spec does not define a universal Core tick rate or action currency.

### Generic Core contract

Core must provide stable item/container identity, a containment/ownership graph, atomic ownership/location transitions, deterministic command ordering/contention, stale-precondition rejection, persistent stable references, authoritative projection boundaries, and transaction/conservation invariants. Core APIs must not require CDDA pocket types, CDDA volume/weight formulas, a 100-move action economy, or integer-grid reachability.

### Future evolution seam

A later rules profile may use slot-based equipment, mass-only storage, continuous-space reachability, arbitrary container capability predicates, different stack semantics, or a different time/action economy. Such a profile should reuse the same identity, transaction, contention, persistence and projection machinery while supplying different policy evaluators.

## Domain model

### Item locations and ownership

**Pinned CDDA reference / Cataclysm profile:** an item has one immediate location kind: invalid/nowhere, character, map, vehicle, or container. Container locations recursively reference a parent item, so the effective root owner/location is obtained by walking parents.

**Generic Core contract:** every live item has exactly one authoritative immediate owner/location relationship, and containment is an acyclic ownership graph. Concrete owner/location kinds are profile/domain adapters over that graph rather than hard-coded assumptions in the transaction engine.

Character possession includes wielded, worn and pocket-contained items. Map ownership is a tile stack. Vehicle ownership includes cargo items and vehicle-part base items. Container ownership is an item inside a specific pocket of another item.

A location handle is not an immortal object reference. CDDA explicitly documents `item_location` as invalidatable by operations including copying. A parity implementation therefore needs validity checks and must never silently retarget a stale handle to a different item.

### Stable identity and persistence

Serialized item locations encode enough information to recover character/map/vehicle/container locations. Current baseline tests use item UIDs so a map, character or vehicle-cargo reference survives index shifts/restacking; old serialized locations without a UID fall back to the historical index behavior. Vehicle base-item locations are a special invariant and omit ordinary item index/UID. Expired locations serialize as nowhere/null rather than a plausible but wrong target.

OctoGhast does not need the same JSON shape internally, but persisted references must have equivalent outcomes: resolve the same live item when it still exists, resolve nested parents recursively, tolerate collection reordering where the reference identity is stable, and fail closed when the target no longer exists.

## Pocket/container contract

Unless explicitly marked otherwise, the rules in this section are **pinned CDDA reference behaviour implemented by the OctoGhast Cataclysm profile**, not universal Core storage laws. Core exposes policy hooks/results for admission, ranking, capacity, quantity and access evaluation but does not encode the CDDA formulas or taxonomy itself.

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

**Pinned CDDA reference / Cataclysm profile:** pockets carry a base `moves` cost (defaulted by pocket data; tests/source treat it as part of pocket behavior). Obtaining an item from a nested container adds the relevant pocket access cost and may include parent acquisition cost depending on where the parent resides. Character enchantment/modifier rules can modify obtain cost.

**Generic Core contract:** a transfer may return a profile-defined action/work cost token/result that is consumed by the normal action/activity scheduler. Core does not define `moves`, 100-move turns, or pocket-access formulas.

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

**Pinned CDDA reference / Cataclysm profile:** a transfer is permitted only when its source can be obtained/reached under the calling action and its destination is accessible and accepts the item. For the Cataclysm profile, tactical reachability ultimately consumes the grid/cell semantics specified by #58/#77.

**Generic Core contract:** the transfer transaction accepts a profile-provided reachability/access decision over opaque authoritative spatial/location context. It must not require integer coordinates, tile adjacency or a specific distance metric in its public interface. A future non-grid profile may therefore substitute continuous-space, graph-based or other reachability without replacing transfer identity/transaction infrastructure.

 Failure is non-destructive: the source remains owned at its prior location/quantity, destination state is unchanged, and no move cost should be charged unless the surrounding action explicitly defines an attempted-action cost.

Invalid/stale item locations resolve as invalid and must not mutate a coincidentally matching item. Recursive containment must reject cycles/self-containment and should impose a safe nesting/depth strategy for persistence and traversal.

## OctoGhast authoritative real-time/co-op adaptation

This section is an **OctoGhast architecture adaptation**, not a claim about pinned-CDDA networking. The pinned-CDDA containment, capacity, selection, quantity-conservation, reachability and move-cost rules above remain the rules-parity baseline. OctoGhast changes who may request a transfer, when it resolves, and what state is projected across the transport boundary.

### Authoritative transfer commands and activities

Clients never mutate authoritative item ownership, pocket contents, charges, worn/wielded state, map stacks or vehicle cargo. A client submits a transfer intent/command identifying the acting Character, operation, source reference, requested quantity and destination selector/reference. The server resolves it at a deterministic simulation boundary through the same validation/transfer path used by AI and in-process single-player clients.

A transfer that is instantaneous under the relevant CDDA rule resolves atomically when the actor is eligible and can pay its CDDA move cost. A transfer represented by CDDA as pickup/fill/haul or other persistent work becomes or advances an actor-owned activity; intermediate client UI state is not authoritative ownership. Completion/repetition revalidates the authoritative source, destination, reachability and quantities before each ownership mutation.

Single-player uses the same command/result contract through in-process transport. Network latency and render frame timing must not change simulation ordering or bypass validation.

### Continuous server time and CDDA move costs

Per Spec 01/#57, the **Cataclysm profile configuration** maps 10 canonical ticks to one Cataclysm world second/turn and 100 Cataclysm moves, with speed-100 accruing 10 moves per tick. Inventory operations retain the pinned-CDDA move/action/access costs described above; those costs remain Cataclysm-profile simulation currency rather than wall-clock milliseconds.

Generic Core does not hard-code 10 TPS, 100 moves per world second, speed-100 accrual, or even the existence of a `moves` currency. It supplies deterministic fixed-step scheduling and profile-defined action/work-cost integration. An alternative timing profile can use the same transfer transaction service with a different rate/cost mapping.

The server schedules/resolves a transfer only when the actor has the required action budget or according to the activity contract. A successful mutation and its cost accounting belong to one authoritative resolution. A request rejected because its preconditions became stale before resolution does not debit the successful-transfer cost; any explicit attempted-action cost must be separately specified by the underlying CDDA action rule rather than invented as a networking penalty.

### Deterministic contention and stale requests

Transfer commands carry stable actor/item/location identifiers plus enough expected-source context to detect staleness (for example source owner/location, quantity/stack identity and destination identity where relevant). Client sequence/request IDs provide idempotent result correlation but are not simulation authority.

Commands admitted for the same canonical boundary are ordered by the shared deterministic command/scheduler ordering contract, never socket arrival race, thread timing or client frame timing. The first ordered command that validates may mutate the item/container. Every later contender revalidates against the resulting authoritative state.

Consequences are fail-closed:

- two actors targeting the same indivisible item: at most one succeeds; later requests receive a stable rejection such as source-missing/source-changed/not-reachable;
- competing partial transfers from one charge stack: each ordered command sees the remaining authoritative quantity and may succeed only for a quantity permitted by the command contract; no overdraw or duplication is possible;
- a moved parent container invalidates a request whose reachability/location precondition depended on its old parent/root, even if the nested item UID still exists;
- a destination that fills, seals, moves, becomes inaccessible or otherwise ceases to accept the item before resolution causes rejection/recalculation only where the command explicitly permits server-side destination selection;
- retries with the same request ID return/associate with the already-determined result and must not repeat the mutation.

Rejection returns a reason and current permitted projected state sufficient for the client to refresh, but never silently retargets a stale reference to a different item or hidden location.

### Stable references across persistence and DTO boundaries

The server owns stable item identity and canonical item-location resolution. Persisted references use the save/load contract described above and in #85. Transport DTOs may carry opaque stable item IDs/reference tokens and projected locator context, but they are capabilities to *request resolution*, not serialized ECS object references, memory addresses, collection indices, Godot node IDs or permission to mutate state.

A stable item UID surviving a move does not mean an old location assertion remains valid: identity and expected location are separate preconditions. On reconnect/save-load, the server may resolve a persisted stable reference to the same item, while a previously issued client DTO must still be revalidated against current visibility, reachability and location before use.

Godot/client object identity is presentation-only. A client may replace/recycle view models without affecting authoritative item identity.

### Visibility and client projection

Projection is least-authority and viewer-specific. The server exposes only item/container information the viewer is entitled to know under FOV/knowledge, ownership/access and interaction rules; it never sends arbitrary ECS components or all nested contents merely because a root container is replicated.

- **Character inventory/worn/wielded:** the controlling player receives the detail needed for inventory actions. Other players receive only world/party-visible equipment or summaries required by gameplay; private pocket contents are not implicitly replicated.
- **Map stacks:** projected only when the viewer's current visibility/knowledge policy permits that tile/item information. Remembered knowledge is distinct from live authoritative contents and cannot authorize a transfer.
- **Vehicle cargo:** projected when the vehicle/cargo interaction is visible/known and accessible under the applicable rules; hidden/unobserved cargo is not globally replicated.
- **Nested contents:** disclosure is recursive only through containers/pockets the viewer may inspect. A visible outer item does not automatically reveal every nested item. Closed/sealed/otherwise non-inspectable contents remain omitted or summarized as required by gameplay.
- **Activities/reservations:** clients may receive owner-visible progress/result state needed to present an in-flight transfer, but internal scheduler/ECS data and other players' private targets are not exposed.

A command may reference only identifiers/tokens previously projected or otherwise legitimately available to that player, but possession of such a token is never sufficient: authoritative resolution still checks current visibility where required, reachability, ownership and containment rules.

### Atomic mutation and results

A successful transfer is one authoritative transaction from the perspective of observers: validate current source/destination, determine quantity/destination/cost, mutate ownership/charges/pocket state, debit/schedule the actor cost, update stable-reference state, then publish result/domain events and fresh projections. Clients must not observe a durable duplicated or ownerless intermediate state.

Failure before commit is non-destructive as in the pinned-CDDA rule. If an internal failure occurs while committing, the authoritative operation must roll back/fail atomically rather than exposing partial source removal. Transport disconnect after submission does not undo a command already admitted/resolved; reconnect observes authoritative outcome via state/result reconciliation.

## Proposed OctoGhast implementation boundary

### Generic Core

Use separate reusable concepts for:

1. **Stable item/container identity** — authoritative runtime identity independent of ECS storage layout, DTO identity or presentation objects.
2. **Ownership/location relationship** — exactly-one immediate authoritative owner/location edge plus acyclic containment traversal.
3. **Transfer transaction/command** — validate expected source/destination/version context, invoke active-profile policy, atomically mutate ownership/quantity, integrate profile-defined work cost, update stable references, then publish results/projections.
4. **Deterministic contention** — canonical ordering plus revalidation; socket arrival order is never authority.
5. **Serializable stable reference** — identity plus locator/precondition context sufficient to restore or fail closed after persistence.
6. **Projection contract** — viewer-specific DTOs/tokens with no ECS, socket, memory-address or Godot object identity leakage.
7. **Policy interfaces/results** — admission, capacity, quantity/merge, destination ranking, reachability and action/work-cost decisions supplied by the active rules profile.

### Cataclysm profile

The Cataclysm profile supplies:

- CDDA pocket definitions/types and mutable pocket state;
- CDDA volume/weight/ammo/liquid/magazine constraints;
- CDDA pocket priorities and destination ranking;
- CDDA stacking/charge merge/split behavior;
- CDDA grid-derived reachability/accessibility;
- CDDA move/access/action-cost computation and its Spec 01 timing mapping;
- Cataclysm-specific character/map/vehicle/container location adapters.

### Future evolution seam

A different rules profile may change any of the policy bullets above while retaining stable Core identity, atomic transactions, deterministic contention, persistence and projection. Core transfer interfaces must therefore avoid fields such as tile coordinates, CDDA pocket enums, `moves`, volume units or magazine-well assumptions unless they are wrapped in profile-specific request/policy data.

This preserves CDDA behavior for the reference milestone without copying its object graph or making its storage model the permanent platform model.

## Acceptance/conformance scenarios

The following black-box scenarios should gate completion. Existing Cataclysm parity/co-op scenarios remain required, plus the Core/profile-isolation scenarios at the end:

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
- Two players request the same map item for the same simulation boundary; deterministic server ordering allows exactly one ownership transfer, the loser receives a stale/source-changed rejection, and replaying the same inputs yields the same winner.
- Two actors concurrently request charges from one stack; ordered authoritative resolution conserves quantity and never permits total successful quantity to exceed the source amount.
- A player queues a nested-item transfer, then another actor moves the parent container before resolution; the queued request fails closed because its expected location/reachability is stale even though the nested item identity still exists.
- A requested destination pocket becomes full/sealed or moves out of reach before resolution; the server rejects without source mutation or successful-transfer move debit unless the command explicitly requested automatic destination reselection.
- Re-sending an already resolved transfer request ID is idempotent and does not duplicate, split or move the item twice.
- Single-player in-process and networked co-op transports given the same canonical command sequence produce equivalent ownership, quantities, move-budget debits and transfer results.
- A speed-100 actor performing a costed transfer under continuous 10-TPS server time becomes eligible according to accumulated CDDA moves; changing client FPS/network delay does not change the rule cost or authoritative outcome.
- An inventory owner can receive actionable nested-content projection while another nearby player receives only permitted visible equipment/container summaries; the second client cannot infer private nested contents from DTOs.
- A map stack or vehicle cargo leaving a player's live visibility is no longer authoritative live client state; remembered presentation cannot be used to transfer an item without server revalidation.
- A client DTO containing a stable item token survives presentation/view-model replacement, but after the item moves its old location assertion is rejected until refreshed; no Godot node/object identity participates in resolution.
- Save/load or reconnect can restore/resolve canonical stable item identity while stale pre-save/pre-disconnect transfer assertions are still revalidated against current authoritative location and visibility.
- **Core/profile isolation:** instantiate the Core transfer engine with a test profile that has no CDDA pocket enum, volume/weight formula, move currency or grid coordinates; stable identity, atomic transfer, stale rejection and deterministic contention still operate unchanged.
- **Alternative timing profile:** run equivalent transfers through a profile whose canonical rate/action-cost mapping differs from Cataclysm's 10-TPS/100-move mapping; ownership and contention results remain correct while eligibility/cost scheduling follows that profile.
- **Future non-grid reachability profile:** provide a continuous-space or graph-based reachability evaluator to the same Core transfer infrastructure; no integer `SpatialCell` or tile-adjacency field is required by the generic transfer command/transaction interface.
- **Deterministic multiplayer contention:** two players target the same indivisible item at one canonical boundary under both Cataclysm and a minimal test profile; canonical ordering chooses exactly one successful transaction and replay produces the same result independent of network arrival timing.
- **Reconnect with stable references:** disconnect after receiving an item/container reference, mutate unrelated transport/session state, reconnect under the same stable player identity, and resolve the same authoritative item/container identity subject to current visibility/location revalidation; no socket/connection identity is part of the reference.
- **Persistence round-trip without transport/Godot leakage:** save and reload a world containing nested items and in-flight/queued transfer-relevant state; the round trip restores Core item/container identity and profile-owned containment state but contains no socket handle, connection ID, serializer object identity, Godot node/resource ID or client view-model identity.
- **Profile-local Cataclysm grid rule:** Cataclysm reachability may use #77 `SpatialCell` semantics internally, but substituting another profile does not change Core reference/transaction types.
- **Profile-local Cataclysm pocket rule:** Cataclysm priority/capacity/liquid/magazine behavior can be replaced by another admission/ranking policy without changing deterministic transaction ordering or stable-reference resolution.

## Persistence and reconnect alignment

Per #85, the authoritative server owns world persistence. Persist stable item/container identity, ownership/containment relationships, Cataclysm-profile pocket/runtime state needed for behavior, relevant activity/scheduler state, and deterministic state required for continuation. Do **not** persist transport connections, socket/parser/buffer state, client DTO instances, Godot objects or transient interest/projection caches.

Stable player identity, connection identity and controlled entity remain distinct. Reconnect may rebind a stable player to current projections, but previously issued transfer assertions are not grandfathered: current ownership, location, reachability and visibility are revalidated.

## Networking/session alignment

Per #90, this subsystem defines transport-neutral transfer request/result/projection semantics only. It does not choose sockets, framing, serializers, parser state machines, backpressure policy or connection-resource design. Network callbacks must hand validated requests into the deterministic simulation boundary; they never mutate inventory directly. Connection/session hardening remains centralized in #90.

## Dependencies and follow-on work

This spec depends on the completed core Item, Character, local-map/spatial (#58/#77), persistence (#85) and typed-ID contracts under #65, and consumes Spec 01/#57 for profile timing. It inherits #90 for transport/session concerns without defining local socket/protocol mechanics. It is a prerequisite for crafting/requirements, activities that consume/move items, ranged reload behavior, construction material consumption and inventory UI parity.

The later activity/input specs should own command interruption and UI selection details, while this spec remains authoritative for ownership, containment validation, transfer atomicity, quantities, stable references and storage-selection outcomes.
