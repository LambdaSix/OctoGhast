# Spec 21 — UI, input, gameplay presentation and accessibility

## 1. Purpose and compatibility decision

This specification defines the implementation contract for OctoGhast player interaction: input actions and remapping, modal/menu behavior, gameplay screens and flows, player-specific projections, feedback/messages, resizing/focus, and accessibility.

The behavioural reference is pinned Cataclysm:DDA commit `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`. The specification preserves the pinned baseline's gameplay affordances and action semantics while intentionally replacing its turn-gated, privileged-avatar UI ownership model with OctoGhast's authoritative continuous server and Godot 2D client boundary.

This is an interaction specification, not a mandate to reproduce CDDA's ncurses/ImGui layout pixel-for-pixel.

### 1.1 Architectural classification

For every interaction described below, distinguish four layers:

1. **Pinned CDDA reference behaviour** — the externally observable affordance or state transition at the pinned baseline.
2. **Cataclysm profile policy** — CDDA-specific action IDs, grid targeting, inventory/crafting/dialogue semantics, and compatibility-facing presentation facts.
3. **Generic Core/client-server contract** — reusable input-action routing, command/query/result/projection semantics, focus/modal lifecycle, stable references, authority, deterministic ordering, and accessibility metadata.
4. **Future evolution seam** — alternate input devices, non-grid spatial profiles, richer presentation, different panel/layout systems, or rulesets that do not inherit CDDA's menu taxonomy.

Generic Core must not make ncurses windows, ImGui, CDDA action strings, integer map cells, one privileged avatar, or a paused menu loop permanent platform invariants.

## 2. Reference evidence

### 2.1 Input and keybindings

Authoritative pinned sources:

- `src/input.cpp`
- `src/input.h`
- `src/input_context.cpp`
- `src/input_context.h`
- `data/raw/keybindings.json`

The baseline loads default keybinding definitions, vehicle bindings, then user preferences. A context-specific binding overrides the default/global action binding when present; otherwise lookup falls back to the default context. User preference entries replace the loaded default bindings for the same action/context. The baseline persists customized bindings separately and contains migration logic for older keybinding versions.

`input_context` registers only the actions meaningful to the current interaction and returns action identifiers rather than requiring gameplay code to interpret physical keys directly. Contexts expose directional registration, UI-list navigation, mouse coordinates, timeout input, keybinding help, conflict detection, add/remove/reset operations, and context-local versus global bindings.

`data/raw/keybindings.json` is data-driven and contains action IDs, context/category, display names, input methods, modifiers, and one or more bindings. Keyboard character, keyboard code, mouse and gamepad forms are represented independently of gameplay resolution.

### 2.2 Generic lists, popups and focus

Authoritative pinned sources:

- `src/uilist.cpp`, `src/uilist.h`
- `src/popup.cpp`, `src/popup.h`
- `src/string_input_popup.cpp`, `src/string_input_popup.h`
- `src/ui_manager.cpp`, `src/ui_manager.h`
- `src/cata_imgui.cpp`, `src/cata_imgui.h`
- `doc/USER_INTERFACE_AND_ACCESSIBILITY.md`

The baseline uses explicit input contexts for menus and popups, with common actions including confirm/select, quit/cancel, directional navigation, page/home/end navigation, filtering, mouse selection and keybinding help. UI adaptors own redraw/resize/cursor behavior; ImGui-backed windows still participate in the adaptor lifecycle.

### 2.3 Gameplay interaction surfaces

Representative authoritative pinned sources:

- main menu: `src/main_menu.cpp`
- character creation: `src/newcharacter.cpp`, `src/character_creator_ui.cpp`
- inventory and item selection: `src/game_inventory.cpp`, `src/inventory_ui.cpp`
- advanced inventory: `src/advanced_inv.cpp`
- look/examine and default gameplay action context: `src/game.cpp`
- overmap: `src/overmap_ui.cpp`
- crafting: `src/crafting_gui.cpp`
- construction: `src/construction.cpp`
- vehicles: `src/veh_interact.cpp`
- dialogue: `src/dialogue_imgui.cpp`, `src/dialogue.cpp`
- sidebars/panels: `src/panels.cpp`, `data/json/ui/*`
- messages: `src/messages.cpp`
- help: `src/help.cpp`
- options: `src/options.cpp`
- debug UI: `src/debug_menu.cpp`

Important examples at the pinned baseline:

- the default gameplay input context names directional inputs as movement actions rather than renderer events;
- LOOK registers directional navigation, coordinate input, optional level changes and other view-specific actions;
- advanced inventory registers pane navigation, filters, sorting, source-area selection, examination and multiple transfer quantities;
- crafting registers confirm/cancel, category/tab navigation, filters, favourites, recipe help, batch controls and crafter selection;
- construction registers list navigation, category tabs, unavailable-item toggling, filter/reset and confirm/cancel;
- dialogue registers response selection, history scrolling, confirm, quit, debug actions and keybinding help;
- vehicle interaction distinguishes install, repair, refill, remove, unload, relabel and other vehicle operations, and computes availability/reasons before starting work;
- messages retain typed history, coalesce identical recent messages, support filtering, and persist message history in the baseline;
- help contains explicit screen-reader adaptations rather than treating accessibility as renderer-only styling.

### 2.4 Data definitions

Relevant pinned data includes:

- `data/raw/keybindings.json`
- `data/json/ui/*` — sidebar widgets and structured UI data such as activity, body status, compass, stamina, time, vehicle, weather and related display definitions
- help/content data loaded by the help system
- option definitions and translated strings consumed by the interaction layer

Data-defined UI content is definition/template data. Current selection, focus, scroll offset, filter text and open-dialog state are runtime presentation state.

### 2.5 Tests as behavioural evidence

Representative pinned tests include:

- `tests/advanced_inventory_test.cpp` — transfer outcomes and item-location behavior
- `tests/crafting_gui_test.cpp` — recipe availability states exposed by the crafting UI
- `tests/vehicle_interact_test.cpp` — vehicle operation requirements/availability
- `tests/options_test.cpp` — option/default behavior
- `tests/new_character_test.cpp` — character-creation results
- the broader domain tests referenced by Specs 03–20 for the commands and queries surfaced through these UIs

The upstream suite is stronger on domain outcomes than on end-to-end UI automation. OctoGhast therefore requires explicit black-box interaction tests below rather than relying on widget-level tests alone.

## 3. Core interaction model

### 3.1 Physical input -> action -> interaction intent

The client pipeline is:

```text
physical input event
    -> active input context
    -> logical action id
    -> local UI transition OR transport-neutral request/query
    -> authoritative server validation/resolution when gameplay state is involved
    -> result/event + refreshed player-specific projection
    -> presentation update
```

Gameplay systems must never depend on Godot key codes, mouse buttons, scene nodes or focused controls. They consume typed requests/commands defined by their domain contract.

### 3.2 Input action definition

An input action definition contains, conceptually:

- stable logical action ID;
- localized display name;
- default context or context-specific category;
- one or more default bindings;
- supported input device/method;
- optional modifier set;
- whether the action is global, context-local, or both;
- optional accessibility-friendly spoken description.

Cataclysm action identifiers may be preserved in the Cataclysm profile where doing so improves parity/test fixture reuse, but generic Core does not require stringly-typed CDDA names.

### 3.3 Binding resolution

For the Cataclysm profile the required resolution rule is:

1. inspect the active context for an explicit binding for the logical action;
2. if a context-local definition exists, it is authoritative for that context, including an explicit local unbind;
3. otherwise fall back to the global/default binding;
4. user customization overrides shipped defaults for the same action/context;
5. conflicting assignments must be detected before commit and either rejected or resolved explicitly;
6. reset restores the shipped definition rather than inventing a new binding.

Binding resolution is deterministic and independent of renderer frame rate.

### 3.4 Context stack and focus

At most one interaction context is the primary consumer of non-global input at a time. Nested modal surfaces push a context; closing them restores the previous context.

A context has lifecycle states:

```text
Inactive -> Active -> Suspended -> Active -> Closed
```

A modal child suspends the parent for conflicting actions but does not destroy the parent's local selection/filter state. Global actions explicitly allowed by policy may remain available.

Focus changes are local client presentation state unless the focus action itself submits a gameplay request.

### 3.5 Confirm, cancel and close

Common semantics:

- **Confirm/select** accepts the currently valid local selection and, where applicable, submits the corresponding authoritative request.
- **Cancel/quit** closes the current local interaction level without inventing a gameplay action.
- Cancelling a UI cannot roll back an authoritative action already accepted by the server.
- A confirmation dialog must distinguish "cancel before submission" from "request submitted; awaiting result".
- Closing a UI never pauses the authoritative world.

## 4. Continuous-time and co-op adaptation

### 4.1 Pinned CDDA reference behaviour

Pinned CDDA commonly runs interaction screens synchronously inside the single-player game loop. Many menus can therefore be treated as if the world waits while the player chooses.

### 4.2 OctoGhast adaptation

OctoGhast intentionally does not preserve that temporal ownership model.

- The authoritative server clock continues while any client menu, targeting overlay, inventory window, crafting browser, dialogue panel or help/options screen is open.
- Other player-controlled actors continue independently.
- AI/world systems continue according to Specs 01, 10, 14, 16 and 17.
- A client UI may become stale between display and confirmation.
- A UI must not freeze, clone or privately mutate authoritative world state to emulate upstream pause behavior.
- Any explicit global pause/acceleration policy belongs to Spec 01/server policy, never to a local widget.

### 4.3 Implementation contract

Every gameplay-affecting screen must be designed around one or more of:

- **projection subscription** — player-authorized state pushed or refreshed by the server;
- **synchronous query** — bounded request for information required to populate/validate a choice;
- **command/intent** — request to perform an action;
- **activity start/cancel request** — durable work as defined by Spec 04;
- **result/event/message** — authoritative outcome.

A screen may optimistically maintain purely local selection state, but authoritative availability, ownership, reachability, cost and final outcome come from the server/domain contract.

## 5. Authority, privacy and projection

### 5.1 Client-visible state

The Godot client receives only explicit player-specific projections required for interaction.

Examples include:

- visible/remembered map cells authorized by Spec 12;
- known overmap/mission information;
- actor-visible Character status from Spec 02;
- inventory/container views authorized by Specs 05–06;
- craftable recipe/requirement projections from Spec 07;
- construction choices/site validation from Spec 08;
- combat/targeting information permitted by Specs 09 and 12;
- NPC/dialogue state permitted by Spec 11;
- vehicle interaction state permitted by Spec 15;
- messages/events addressed to that player by Spec 17 and domain systems.

The client must not obtain arbitrary ECS components, unseen entities, hidden inventories, unrevealed map cells, server RNG state or private state belonging to another player merely because a UI could display it.

### 5.2 Stable references

Interactive projections must use stable domain references rather than Godot object identity.

Examples:

- `CharacterId` / controlled actor identity;
- stable `ItemUid` / item-location reference from Specs 05/06 (not a separate UI item identity);
- container/location references from Spec 06;
- recipe/construction/type IDs;
- NPC/mission/faction IDs;
- vehicle and vehicle-part stable references;
- authoritative `WorldPosition` / `SpatialCell` references as defined by Spec 12.

A scene node, control path, list index, entity-array offset or network connection ID is never an authoritative gameplay reference.

### 5.3 Staleness and contention

Any UI that can act on shared state must tolerate the state changing after it was displayed.

On submission the server revalidates all gameplay preconditions. A stale request is rejected or resolved according to the owning domain spec; the client then refreshes the affected projection and preserves local selection where it can still be mapped by stable ID.

Examples:

- another player takes the selected ground item;
- crafting ingredients are consumed elsewhere;
- a construction tile changes;
- a vehicle part is removed;
- an NPC moves out of interaction range;
- a target leaves line of sight;
- a container closes or becomes inaccessible.

The UI must show a meaningful rejection/result. It must not silently apply the action to a different object occupying the same list index/cell.

## 6. Immutable definitions versus mutable state

### 6.1 Definition/template data

Immutable or reloadable definitions include:

- action metadata and shipped default bindings;
- localized action labels;
- UI widget/sidebar definitions;
- recipe/construction/dialogue/type definitions consumed by projections;
- help content;
- option schema/allowed values;
- presentation metadata such as status labels/icons where defined by content.

These are addressed by stable IDs and follow Specs 18–19 for content/profile loading.

### 6.2 Mutable authoritative runtime state

Authoritative runtime state includes the world/actor/item/NPC/vehicle/activity state already owned by domain specs. UI does not duplicate it.

### 6.3 Mutable client runtime state

Examples:

- active input context stack;
- focused control;
- selected row/tab/pane;
- scroll offsets;
- local filter/search text;
- expanded/collapsed groups;
- local cursor/camera target;
- pending confirmation draft before submission;
- presentation animation/interpolation state.

This state is non-authoritative.

### 6.4 Durable client preferences

Keybindings, accessibility settings, UI scale, panel layout, preferred units and similar presentation preferences may be persisted as client/profile preferences. They are not part of the authoritative world save.

If an option is explicitly made account-synced, its cross-world ownership belongs to Spec 28 / #91 rather than Spec 20's world-save transaction. Device-local preferences remain client state by default.

## 7. Gameplay screen catalogue and contracts

## 7.1 Main menu and session entry

Required capabilities:

- new game / character creation;
- load/join a world or session;
- world/session management appropriate to product policy;
- settings/options;
- help;
- credits/about as product content;
- quit/disconnect.

In OctoGhast, "single-player" starts or connects to a one-player authoritative server through the same logical client/server contract as co-op.

Main-menu UI is not authoritative world state.

## 7.2 Character creation

Consumes Spec 03 and Spec 28 / #91.

Required interaction state includes scenario/profession/background/traits/stats/skills/name/appearance/start choices exposed by the Cataclysm profile, tab/category navigation, validation summaries, randomization where supported, template/profile operations where supported, and final submission.

Randomization that changes the resulting authoritative character must use the authoritative creation/RNG contract from Spec 03. Client-only cosmetic shuffling is permitted only if it cannot alter authoritative rules state.

Submission is validated by the server against current content/profile policy. Closing the UI before submission creates no character.

## 7.3 Inventory and item selection

Consumes Specs 05–06.

Required capabilities include:

- browse carried/worn/wielded/contained items;
- item details;
- filtering/sorting;
- quantity selection;
- context-appropriate item actions;
- stable selection across projection refresh where the item still exists;
- explicit unavailable/rejected reason where useful.

A normal inventory screen is a projection/query surface. A transfer/drop/take/wear/wield/etc. action becomes a command or activity request according to its domain contract.

## 7.4 Advanced inventory

Preserve the pinned affordance of comparing/selecting source and destination areas including actor inventory, worn state, containers, ground cells and vehicle cargo where authorized.

The Cataclysm profile should support:

- two-pane/source-destination navigation;
- surrounding-cell selection;
- ground versus vehicle storage distinction;
- filter/reset;
- sort;
- examine;
- single/variable/stack/all transfer quantities;
- favourite/autopickup presentation where those systems are implemented.

Transfers inherit Spec 06's deterministic contention, stale rejection and move/action-cost rules. The UI never performs direct relocation.

## 7.5 Examine/look

Consumes Specs 12–15 and relevant domain specs.

The look cursor is client presentation state over a server-authorized spatial projection. It may navigate visible/remembered cells and request detail projections for the focused location.

The UI may expose terrain/furniture/items/creatures/vehicle/field/trap/mission/zone facts only to the extent the owning projection permits.

Actions initiated from examine become normal commands/queries; closing look mode does not alter the world.

## 7.6 Targeting and ranged interaction

Consumes Specs 09 and 12.

The targeting overlay owns only local cursor/aim selection. Server-authorized targeting data provides currently valid candidates, range/line-of-sight/visibility facts and any permitted predicted costs.

Final fire/throw/attack submission identifies the acting character, weapon/item/mode where relevant, and target reference/position. The server recalculates authoritative legality and outcome at command resolution.

If the target moved or became invalid before resolution, behavior follows Spec 09; the client must not fire against stale hidden state.

Targeting overlays must not reveal entities outside the viewer's current permitted projection.

## 7.7 Crafting

Consumes Spec 07 and Spec 04.

Required capabilities:

- category/tab browsing;
- search/filter/reset;
- recipe details and requirements;
- available/unavailable state with textual reason;
- favourite state if supported;
- batch size;
- crafter choice where permitted;
- related/nested recipe navigation;
- start crafting.

The UI displays a projection of recipe knowledge and current requirement satisfaction. Confirm starts an authoritative activity request. Resource consumption/reservation and stale contention remain Spec 07 concerns.

## 7.8 Construction

Consumes Spec 08 and Spec 04.

Required capabilities:

- category/tab browsing;
- available/unavailable visibility toggle;
- filter/reset;
- staged construction details and requirement display;
- target/site selection;
- authoritative start request.

The selected site uses Spec 12 position semantics. In the Cataclysm profile it is grid-cell based; generic Core does not require all future construction rules to be grid-only.

## 7.9 Overmap

Consumes Specs 12–13 and mission/world knowledge from Spec 11.

The overmap UI is a player-knowledge projection, not direct world inspection.

Required affordances include pan/navigation, level changes where supported, center/recenter, notes/markers, search, route/travel selection, mission/known-point display and detail inspection according to profile policy.

Remembered/known information must remain player-scoped. One player's overmap discoveries are not automatically exposed to another player unless a deliberate sharing rule says so.

## 7.10 Vehicle interaction

Consumes Spec 15 and Specs 04/06/07 as applicable.

Required operations include the implemented Cataclysm-profile vehicle actions such as examine/overview, install, repair, refill, remove, unload, relabel/rename, crew and other supported part operations.

The UI may display task availability/reason, requirements and estimated Cataclysm action/activity cost. Starting work submits an authoritative request and normally creates an activity. Vehicle/part identity must remain stable across refresh and persistence.

## 7.11 Dialogue

Consumes Spec 11 and Spec 17.

Required interaction state includes:

- speaker identity/details authorized for the player;
- dialogue history;
- current response choices;
- disabled/unavailable choices with non-color-only reason where relevant;
- history scrolling;
- confirm/select;
- quit/end conversation;
- any explicitly supported debug tooling.

A dialogue response is a server-authoritative command/context action. Conditions and effects are evaluated on the server against the current talker/context state. The client may not evaluate hidden conditions using arbitrary world data.

If state changes invalidate a response before submission/resolution, the server rejects or recomputes according to Spec 11/17 and returns a fresh projection.

## 7.12 Sidebar/HUD

The HUD is a read-only presentation over authorized projections plus client-local state.

Candidate Cataclysm-profile information includes Character health/status/needs, wielded item/ammo, time/weather, movement mode/speed, vehicle state, compass/threat summaries, messages and other pinned sidebar facts.

The exact layout is not a parity invariant. Information availability and semantics are.

## 7.13 Messages and notifications

Pinned CDDA distinguishes message types, retains history, coalesces repeated messages, can suppress repeated sidebar display via cooldown, and exposes a filterable message log.

OctoGhast contract:

- authoritative gameplay systems emit structured events/message intents with audience;
- projection maps those to each entitled client;
- clients render localized text/visual/audio feedback;
- repeated-event coalescing is allowed only when it does not erase gameplay-significant facts;
- important state must not be communicated solely through transient toast timing;
- reconnect does not require replaying unlimited presentation history, but authoritative/unread/required notifications must follow the relevant domain/session contract.

World persistence of presentation-formatted message history is not required merely because pinned CDDA serializes its message log. If retained for parity, store semantic/audience information where practical rather than server-rendered UI objects.

## 7.14 Help and keybinding help

Help must be reachable from relevant contexts and be navigable without requiring a mouse.

Contextual keybinding help is generated from the currently active logical action set and effective bindings, so remapped controls are represented accurately.

Help content is local/read-only and does not pause the server.

## 7.15 Options/settings

Options are classified before storage:

- **client presentation/input options** — local/profile preference;
- **accessibility options** — local/profile preference;
- **world/rules options** — authoritative server/world configuration;
- **server/session administration options** — server policy and permission-controlled.

A client must not mutate world/server options by writing a local settings object. Authoritative option changes use permission-checked server requests and explicit results.

## 7.16 Debug UI

Debug UI is an administrative/developer surface, not a privileged client backdoor.

Read operations consume explicit diagnostic projections/queries. State mutation becomes authenticated/authorized server debug commands executed at deterministic simulation boundaries and recorded sufficiently for tests/audit where required.

Production clients must not receive hidden world state merely because debug UI code exists.

## 8. Modal interruption and asynchronous results

### 8.1 Local modal state machine

For a gameplay-affecting confirmation flow:

```text
Browsing
 -> SelectionReady
 -> Confirming
 -> Submitted
 -> AwaitingResult
 -> Applied | Rejected | Superseded
 -> Browsing/Closed
```

Cancel from `Browsing`, `SelectionReady` or `Confirming` is local.

After `Submitted`, closing the window does not retract the request unless the domain exposes a distinct cancellation command.

### 8.2 Server-side interruptions

Activities can be interrupted/cancelled according to Spec 04. The UI observes that state; it does not own it.

If an actor becomes incapacitated, disconnected, loses access, or otherwise cannot complete an action, the server/domain emits the resulting state. The client updates or closes the corresponding interaction surface.

### 8.3 Disconnect/reconnect

On disconnect:

- local modal/focus/scroll/filter state may be discarded or retained locally as a convenience;
- connection identity is detached from stable player identity per #90/#85;
- authoritative actor/activity state follows its domain/persistence contract.

On reconnect the UI is rebuilt from fresh projections. A stale pre-disconnect list index or Godot object reference must never be reused as authority.

## 9. Resizing, focus and presentation transforms

UI layout must be recomputed when the viewport/window changes size.

Required invariants:

- resizing cannot mutate simulation state;
- focus remains on the same semantic item/action where possible, using stable IDs rather than row number;
- if the focused object disappears, selection moves by deterministic local UI policy and never silently retargets a submitted command;
- text reflow and panel relocation preserve information;
- mouse/touch hit regions derive from current layout;
- Godot interpolation/camera transforms never alter authoritative `WorldPosition`/`SpatialCell`.

The client may animate/interpolate state between server projections. Hit testing that submits gameplay requests must map back to an authoritative reference/position from the latest usable projection rather than treating the visual transform as authority.

## 10. Accessibility contract

Pinned `doc/USER_INTERFACE_AND_ACCESSIBILITY.md` is authoritative evidence for the baseline's screen-reader concerns.

### 10.1 Information equivalence

Every gameplay-significant distinction must have a non-color-only representation.

Examples:

- unavailable action: disabled state plus text/reason or equivalent semantic label;
- item freshness/condition: text/icon accessible name, not color alone;
- hostile/friendly/selected state: semantic label/state, not tint alone;
- warning/critical status: text/symbol/accessible property in addition to color.

### 10.2 Focus and reading order

Every interactive surface must expose:

- one meaningful current focus;
- deterministic keyboard/controller focus traversal;
- semantic role/name/value/state for controls and list items;
- current selection;
- disabled/unavailable state and reason where meaningful;
- predictable reading order.

For screen-reader mode, layouts must avoid forcing the user through large changing lists before hearing the selected item's details. The pinned guidance's principle applies: selected entry plus its details may be presented as the primary reading unit and noisy list panes suppressed/restructured.

### 10.3 Cursor/focus behavior

The baseline relies on terminal cursor placement because screen readers begin reading near the cursor. Godot does not need to reproduce terminal mechanics, but it must reproduce the outcome: accessibility focus must move to the semantically important changed/selected content and remain stable across redraws.

### 10.4 Dynamic updates

Dynamic projections must announce meaningful changes without flooding the user.

Coalescing is permitted for high-frequency replaceable state, but critical events, command results, damage/status changes requiring response, and modal errors must be discoverable.

### 10.5 Input accessibility

All required gameplay operations must be possible without a mouse.

Remapping must cover keyboard/controller actions exposed by the current profile. No core gameplay action may require a hard-coded physical key inaccessible to remapping unless the platform reserves it.

### 10.6 Screen-reader privacy

Accessible labels are generated from the same player-specific projection as visible UI. Accessibility APIs must not leak hidden entities, unrevealed map data, private player state, or debug-only information.

## 11. Feedback and error contract

Every submitted authoritative request produces one of:

- accepted/completed result;
- accepted/activity-started result;
- rejected result with stable machine reason and user-facing explanation;
- superseded/stale result where applicable;
- permission failure;
- transport/session failure distinct from gameplay rejection.

The client must distinguish a gameplay rejection from network loss.

User-facing feedback may combine:

- inline state;
- message log entry;
- modal confirmation/error;
- HUD indicator;
- accessible announcement;
- optional visual/audio effect.

The same semantic result should drive these views so they cannot disagree about success/failure.

## 12. RNG and determinism

The interaction layer must not consume authoritative gameplay RNG for:

- hover effects;
- menu ordering;
- cosmetic animation;
- local focus changes;
- opening/closing screens;
- filtering/sorting;
- accessibility announcements.

Any UI operation that requests a rules-level random outcome, such as Cataclysm-profile character randomization, submits an authoritative request to the owning domain. RNG stream state and ordering then follow that domain's deterministic contract.

Given the same authoritative projections and logical input-action sequence, local UI state transitions used by conformance tests must be deterministic except for explicitly cosmetic presentation.

## 13. Persistence

### 13.1 World save

Spec 20 remains authoritative. World saves do not contain:

- open windows;
- focus;
- local filters;
- scroll offsets;
- Godot node identity;
- render transforms/interpolation;
- sockets/connections;
- client keybinding objects.

They do contain any authoritative activities/world state that an open UI may have initiated.

### 13.2 Client/profile preferences

Persist independently as appropriate:

- custom bindings;
- accessibility mode/settings;
- UI scale/font preferences;
- panel/HUD layout;
- preferred units;
- local presentation options.

These preferences must be versioned/migratable. Missing/corrupt preference data falls back to defaults without corrupting the authoritative world.

### 13.3 Player identity

Preferences may be device-local or explicitly account-scoped, but must never use connection/socket identity as stable player identity. Server-scoped account ownership is Spec 28 / #91; rules/content-profile identity remains a separate concept.

## 14. Cross-system dependencies

- **Spec 01 / #57/#66:** canonical time continues while UI is open; pause/timewarp is server policy.
- **Spec 02 / #67:** Character status projection and visibility classes.
- **Spec 03 / #68:** creation/progression semantics and authoritative randomization.
- **Spec 04 / #69:** command/query/event/activity distinction and cancellation.
- **Specs 05–06 / #70/#71:** item identity, inventory projection and transfer contention.
- **Spec 07 / #72:** recipe availability, requirements and crafting activities.
- **Spec 08 / #73:** construction target/project semantics.
- **Spec 09 / #74:** targeting, melee/ranged/projectile legality and outcomes.
- **Spec 10 / #75:** monster state exposed through player visibility.
- **Spec 11 / #76:** NPC/dialogue/mission/faction interaction state.
- **Spec 12 / #77/#58:** authoritative position, player-specific FOV/knowledge, active regions.
- **Spec 13 / #78:** overmap/world knowledge and map presentation facts.
- **Spec 14 / #79:** weather/field/scent/environment projection.
- **Spec 15 / #80:** vehicle identities, parts and activities.
- **Spec 16 / #81:** AI acts independently while player UI remains open.
- **Spec 17 / #82:** event/talker context, audience and message projection.
- **Specs 18–19 / #83/#84:** action/content IDs, definitions, localization/content packs.
- **Spec 20 / #85:** save/reconnect/world ownership.
- **Spec 25 / #90:** transport/session lifecycle, deterministic inbound handoff, bounded queues/backpressure and player/session binding. **#92** owns production authentication/security policy; **#96** owns request outcome/retry recovery across reconnect/save.
- **Spec 28 / #91:** server-scoped `AccountId`, account meta-progression and any explicitly account-synced UI preferences; ordinary device-local preferences remain client state.
- **Spec 22 / #87:** visual assets, tiles, audio and localization implementation of these semantic presentation contracts.
- **Spec 23 / #88:** automated parity harness for the black-box scenarios below.

## 15. Black-box, conformance and parity scenarios

### UI21-01 Context-local binding overrides global

Given a global action binding and a different binding for the active Cataclysm UI context, only the context-local binding triggers that action while the context is active. Closing the context restores global behavior.

### UI21-02 Missing local binding falls back globally

Given an action with no context-local override, the effective binding is the default/global binding.

### UI21-03 Explicit local unbind

Given a globally bound action and a user-created local unbind, the action is unavailable in that context while remaining bound elsewhere.

### UI21-04 Remap conflict

Assign an input already used by another effective action. The client reports the conflict and requires explicit resolution; it never silently leaves two ambiguous active actions where the profile forbids it.

### UI21-05 Reset binding

Customize a binding, reset it, restart the client and verify the shipped effective default is restored.

### UI21-06 Toolkit independence

Drive the same logical action sequence through keyboard and a second input adapter. The same local state transitions and server requests are produced.

### UI21-07 Modal stack

Open gameplay -> inventory -> item detail -> confirmation. Cancel each layer in reverse order and verify focus/context return to the previous semantic selection.

### UI21-08 Menu does not pause world

Player A opens inventory and remains idle for 50 canonical Cataclysm-profile ticks. Player B, AI and scheduled world systems continue. Player A receives refreshed state without a local second clock.

### UI21-09 Inventory stale item

Player A selects a ground item. Player B takes it before A confirms. A's transfer command is rejected as stale/missing by stable reference; no other item in the same cell/index is transferred.

### UI21-10 Advanced inventory deterministic contention

Two players submit valid transfers for the same item at the same deterministic resolution boundary. Spec 06 ordering chooses one outcome; the other receives an explicit stale/contention rejection and refreshed panes.

### UI21-11 Filter/sort is local

Changing inventory filter/sort order emits no simulation command, consumes no action cost and consumes no gameplay RNG.

### UI21-12 Look respects visibility

A client moves the look cursor across known/unknown/hidden cells. The detail projection never exposes entities or state outside that player's permitted FOV/knowledge contract.

### UI21-13 Target moved before fire

A target is valid when selected and invalid/moved before command resolution. The server applies Spec 09 legality; the client receives the authoritative reject/recomputed result rather than using stale client targeting data.

### UI21-14 Crafting availability refresh

A recipe is shown craftable; another actor consumes a shared required resource before confirmation. Start-craft is revalidated and rejected or adjusted exactly per Spec 07, then the requirement projection updates.

### UI21-15 Construction site contention

Two actors choose the same construction site. The authoritative project/site rules determine the result; both UIs reconcile from stable site/project references.

### UI21-16 Dialogue stale response

A response is displayed, but its server-side condition becomes false before submission. The response is rejected/recomputed by Spec 11/17 and a new response projection is returned without exposing hidden condition state.

### UI21-17 Overmap knowledge isolation

Two separated players have different explored/remembered regions. Each overmap projection contains only their permitted knowledge. Opening overmap on one client does not expand the other's knowledge or active region.

### UI21-18 Vehicle part stale reference

A client selects a vehicle part for repair; another authoritative action removes/replaces it. Confirmation cannot retarget by mount/list index; it rejects the stale part ID/reference.

### UI21-19 Activity start then close UI

Start a valid crafting/construction/vehicle activity and close the initiating screen immediately. The authoritative activity continues according to Spec 04; closing the screen does not cancel it.

### UI21-20 Disconnect during activity

Disconnect while an authoritative activity exists. Local UI disappears; activity/disconnected-character policy follows Specs 04/20. Reconnect reconstructs state from fresh projection.

### UI21-21 Request submitted then window closed

Submit a command and close the UI before the result arrives. The server resolves the request once. Reopening/refreshing reflects the result; no duplicate command is generated.

### UI21-22 Resize preserves semantic focus

Resize while a list/detail screen is focused on a stable item/recipe/vehicle-part ID. After reflow, focus remains on that semantic object if present; no simulation state changes.

### UI21-23 Focused object disappears

Focused projected object disappears during refresh. UI follows deterministic local fallback and announces the change; it does not silently submit against the replacement row.

### UI21-24 Non-color-only unavailable state

For an unavailable craft/action/item, verify a screen reader or text-only test can obtain the unavailable state and reason with color removed.

### UI21-25 Screen-reader selected-detail flow

In screen-reader mode, navigate a list with changing detail. Accessibility focus announces the selected entry and relevant details without requiring traversal of every visible list row after each move.

### UI21-26 Full keyboard operation

Execute representative main-menu, character creation, inventory transfer, examine, crafting, dialogue, options and help flows with no mouse input.

### UI21-27 Keybinding help reflects remap

Remap an action, open contextual keybinding help, and verify the displayed/spoken binding is the effective remapped binding.

### UI21-28 Player-specific messages

Cause a private result for player A and a world-visible event near A/B. Projection delivers each message/event only to entitled clients. Accessibility output uses the same audience-filtered data.

### UI21-29 Message coalescing preserves semantics

Generate repeated replaceable notifications and a distinct critical event. Repeated messages may coalesce; the critical event remains independently discoverable.

### UI21-30 Client options versus world options

Change UI scale locally: no server world mutation occurs. Attempt a world/rules option change without authority: server rejects it. Authorized change follows server policy and is projected to affected clients.

### UI21-31 Debug authority

An unauthenticated/non-admin client cannot read hidden debug state or submit mutation commands. An authorized debug command enters the deterministic server command path rather than mutating ECS state from a network/UI callback.

### UI21-32 Randomization ownership

Request Cataclysm-profile character randomization twice under a deterministic seeded test. The authoritative creation system consumes the defined RNG stream; merely opening/changing UI tabs consumes none.

### UI21-33 Save while menu open

Save at a deterministic server barrier while a client has a menu open. Reload restores authoritative world/activity state but not the open modal/focus/scroll state.

### UI21-34 Reconnect identity

Disconnect and reconnect with a new socket/session connection. Player identity and controlled entity are restored per #85/#90; client-local Godot references from the old connection are not treated as valid authority.

### UI21-35 In-process/network equivalence

Run a representative interaction sequence through the in-process single-player transport and socket transport. Given the same ordered logical requests, authoritative results are equivalent.

### UI21-36 End-to-end new-game flow

From main menu: create one-player server/session -> create Cataclysm-profile character -> enter world -> inspect surroundings -> pick up an item -> open inventory -> start a representative activity -> observe message/HUD feedback. All authoritative state changes occur through the server boundary.

### UI21-37 End-to-end co-op contention flow

Two clients join one world, occupy overlapping interest regions, inspect the same item/container, and race a transfer. Only one authoritative mutation occurs, each client receives a viewer-appropriate result, and neither client receives unrelated ECS state.

### UI21-38 Help/options while world advances

Open help then client-only options during live play. Canonical simulation continues; no action budget is spent solely for viewing local help/settings. Any authoritative world option change is a separate permission-checked request.

### UI21-39 Cataclysm grid versus future position profile

Run look/target UI against the Cataclysm grid profile, then a Core test profile exposing a non-grid authoritative `WorldPosition`. Interaction code consumes profile-provided selectable spatial references and does not require Godot/world positions to be integer cells.

### UI21-40 Presentation replacement

Render the same captured semantic projection with two different client layouts. The available actions, accessible labels and submitted server requests remain equivalent, demonstrating that parity tests do not depend on visual toolkit geometry.

## 16. Implementation sequence

1. Define toolkit-neutral logical input action/context interfaces and preference persistence.
2. Define client projection/view-model DTOs and stable-reference rules for interaction surfaces.
3. Implement common modal/focus/list/filter/confirmation semantics and accessible metadata.
4. Wire main/session entry and character creation through the server boundary.
5. Implement HUD/messages/help/options shells.
6. Implement inventory/advanced inventory/examine/targeting using Specs 05/06/09/12.
7. Implement crafting/construction/vehicle/dialogue using their existing domain command/activity contracts.
8. Implement overmap/player-knowledge interaction.
9. Add screen-reader/focus automation and non-color-only conformance tests.
10. Add in-process versus socket interaction-equivalence tests and concurrency/staleness suites.
11. Leave visual asset/tiles/audio/localization realization to Spec 22 without changing the semantic contracts here.

## 17. Explicit non-goals

This specification does not:

- prescribe Godot scene hierarchy or control classes;
- require visual pixel parity with pinned CDDA;
- require ncurses or ImGui;
- implement tilesets, sprite fallback, soundpacks or translation asset pipelines owned by Spec 22;
- define domain formulas already owned by Specs 02–17;
- define production socket framing/backpressure owned by #90;
- make all user preferences world-save state;
- allow client UIs to query arbitrary ECS state;
- make opening a menu pause the world.

## 18. Definition of done

Spec 21 is implementation-ready when:

- logical action/context/remapping semantics are toolkit-neutral and deterministic;
- gameplay screens/flows and their required state/actions are catalogued;
- modal/focus/confirm/cancel/interruption behavior is explicit;
- inventory, map/look, targeting, crafting, construction, vehicle, overmap and dialogue interactions are contracts over existing domain specs;
- continuous-time/co-op authority, stale-state and contention behavior is explicit;
- messages/feedback/audience rules are explicit;
- accessibility requires information equivalence, semantic focus, screen-reader-safe reading order and non-color-only state;
- UI state is clearly separated from authoritative simulation state and persistence;
- RNG and stable-reference requirements are explicit;
- the black-box suite covers single-player one-server flow, co-op contention, disconnect/reconnect, resizing, accessibility and transport equivalence;
- no unresolved cross-cutting architecture decision remains local to this specification.

