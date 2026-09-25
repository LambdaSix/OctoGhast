# Spec 27 — Godot 2D presentation boundary, interpolation and reconciliation

Parent: #65  
Tracking issue: #94  
Reference baseline: `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## 1. Status and purpose

This specification defines the implementation contract for the OctoGhast Godot 2D presentation boundary: how player-specific authoritative projections become local render state, how movement and camera motion may be interpolated, how transient visual events are animated, and how a client reconciles corrections without ever becoming authoritative.

It consumes the architecture already settled by #52, #57, #58, #64 and #65 and the completed Specs 01, 12, 20, 21, 22, 25 and 26. It does not reopen the authoritative clock, spatial ownership, persistence, networking, visibility or UI contracts.

The central invariant is:

> Authoritative simulation state is server-owned. Godot owns only local presentation state derived from explicit player-specific projections.

For the pinned Cataclysm profile, authoritative gameplay remains grid/cell based. Godot may render smooth transforms between authoritative samples, but those transforms do not change collision, LOS, targeting, action cost, activity progress, scheduler state, map occupancy or any other simulation rule.

## 2. Classification: reference, adaptation, Core and future seams

### 2.1 Pinned CDDA reference behaviour

The pinned baseline renders the current local world from discrete map-space coordinates and observer-relative visibility. Transient bullet, hit, explosion, cursor, line and weather effects are presentation overlays. Their wall-clock delays and idle tile animation timing are presentation concerns layered around already-resolved gameplay state.

The baseline does not provide a network interpolation model because the authoritative simulation and renderer live in one process. Therefore any OctoGhast snapshot buffering, interpolation, prediction and reconciliation policy is an intentional client/server adaptation rather than upstream behaviour.

### 2.2 OctoGhast Cataclysm profile

The Cataclysm profile preserves:

- grid-authoritative `WorldPosition` / `SpatialCell` gameplay semantics;
- Cataclysm visibility/knowledge restrictions;
- semantic presentation IDs and tileset lookup inputs from Spec 22;
- authoritative movement/action costs and canonical time from Spec 01;
- server-owned current state, active-region and projection decisions from Specs 12/25/26.

The profile permits smooth Godot rendering between authoritative grid states without redefining those states.

### 2.3 Generic Core/client contract

Generic Core/server provides only ruleset-neutral capabilities needed by presentation:

- stable authoritative identity;
- authoritative sample time/revision metadata;
- semantic projections and events;
- visibility/interest authorization;
- transport-neutral snapshot/delta/result delivery;
- deterministic command/result ordering;
- explicit discontinuity/correction semantics where interpolation cannot be inferred safely.

The generic client presentation layer owns:

- a local projection cache;
- render-history samples;
- `PresentationTransform`;
- interpolation/camera/animation clocks;
- local asset mapping;
- local UI/presentation preferences;
- correction smoothing or snapping.

Core simulation must not depend on Godot scene nodes, transforms, frame callbacks, animation players, cameras or rendering frame rate.

### 2.4 Future evolution seams

The contract must remain usable if a future OctoGhast profile adopts:

- deterministic sub-cell authoritative positions;
- continuous movement;
- different collision geometry;
- persistent projectiles;
- non-grid worlds;
- different renderer technology;
- higher/lower simulation rates;
- different replication cadence.

Accordingly, interpolation is defined from authoritative samples, not from an assumption that all future movement is one tile per 100 ms.

## 3. Authoritative pinned-CDDA evidence

All evidence below is pinned to `e262adb299a7613b4aedc5f12c08fe0413c56a84`.

### 3.1 Discrete map-space render inputs

`src/cata_tiles.cpp`:

- `cata_tiles::draw(..., const tripoint_bub_ms &center, ...)` receives a discrete bubble-map coordinate as the view center;
- the renderer derives its tile-space origin from that coordinate;
- visible-map drawing consults map visibility caches and the avatar's discrete map position;
- transient overlays such as bullets, cursors and highlights are stored/drawn at `tripoint_bub_ms` positions.

Representative pinned anchors:

- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/cata_tiles.cpp#L588
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/cata_tiles.cpp#L1490
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/cata_tiles.cpp#L4406
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/cata_tiles.cpp#L4766

Reference implication: pinned gameplay/render inputs are discrete logical positions. The baseline is not evidence that a future Core platform must be permanently integer-positioned.

### 3.2 Wall-clock presentation delay is not simulation time

`src/animation.cpp` constructs animation delay from the `ANIMATION_DELAY` option and waits using `std::chrono::steady_clock`, pumping input events while waiting. Bullet animation may intentionally skip the artificial delay during target practice. Visibility is checked before rendering a point.

Representative pinned anchors:

- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/animation.cpp#L55
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/animation.cpp#L102
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/animation.cpp#L124
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/animation.cpp#L512

Reference implication: the baseline already distinguishes real-time visual delay from gameplay chronology. OctoGhast must preserve that distinction more strictly because the server continues authoritatively while a client renders.

### 3.3 Idle tile animation uses presentation wall time

`src/cata_tiles.cpp` marks animated tiles and selects idle-animation frames from `std::chrono::steady_clock`, approximately targeting 60 presentation frames per second. This is explicitly described in the source as user-turn/presentation animation timing.

Pinned anchor:

- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/cata_tiles.cpp#L2320

Reference implication: presentation frame choice is not an authoritative simulation RNG/time stream and must not become one in OctoGhast.

### 3.4 Transient animation state is disposable presentation state

The tile renderer uses flags and temporary structures such as `do_draw_bullet`, `do_draw_hit`, `do_draw_line`, cursors, highlights, weather overlays and async animation layers. Corresponding `void_*` methods clear this state after/when appropriate.

Pinned anchors:

- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/cata_tiles.cpp#L4406
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/cata_tiles.cpp#L4475

Reference implication: visual effect lifetime can be client-local provided authoritative gameplay outcome/event semantics are already settled and audience-filtered.

### 3.5 Tileset data controls visual lookup, not gameplay authority

`src/tileset_loader.cpp` and `doc/TILESET.md` define tile metadata such as sprite dimensions, offsets, pixel scale, foreground/background choices, weighted variants, multitiles and contextual layers.

Pinned anchors:

- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/tileset_loader.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/TILESET.md

These are immutable presentation definitions/resources after a client presentation generation is loaded. They are not authoritative world state.

### 3.6 Visibility and memory are observer-relative prerequisites

Representative pinned tests:

- `tests/vision_test.cpp` exercises observer/light/camera-dependent visibility;
- `tests/map_memory_test.cpp` exercises durable remembered-tile behaviour independently of current visibility.

Pinned anchors:

- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/vision_test.cpp
- https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/map_memory_test.cpp

Spec 26 owns the multiplayer adaptation of these semantics. This spec consumes the already-filtered projection and must not create client-side visibility from hidden authoritative state.

No dedicated upstream network interpolation contract exists at this baseline; that is expected because pinned CDDA is local/single-process.

## 4. Data ownership and state categories

### 4.1 Immutable authoritative/profile definitions

Examples:

- Cataclysm content definition IDs;
- movement/terrain/profile rules;
- projection field schemas;
- semantic presentation cue IDs;
- authoritative content-generation identity.

Owned by server/profile content systems and Specs 18/19/22.

### 4.2 Immutable client presentation definitions

Examples:

- selected tileset/presentation pack definition;
- sprite lookup/fallback tables;
- animation definitions;
- local shader/material/resource configuration;
- local camera-style configuration;
- client interpolation policy defaults.

These may be reloaded as a new client presentation generation. They never mutate authoritative ECS/world state.

### 4.3 Mutable authoritative runtime state

Examples:

- `WorldPosition`;
- derived `SpatialCell`;
- actor/item/vehicle/world state;
- current visibility/knowledge owned by server/world/player;
- canonical simulation tick/time;
- activities/schedules;
- authoritative event outcomes;
- projection revisions/epochs required to communicate current state.

### 4.4 Mutable client presentation runtime state

Examples:

- `PresentationTransform`;
- render-history sample buffer;
- camera transform/velocity;
- animation player state;
- local particles;
- temporary effect lifetime;
- selected visual variant cache;
- local interpolation/extrapolation clock;
- local UI cursor/focus;
- presentation cache keyed by stable projected identity.

This state may be discarded at any time and reconstructed from a fresh projection baseline.

## 5. Coordinate and transform contract

Three concepts must remain distinct:

```text
authoritative WorldPosition
        ↓ profile mapping
derived SpatialCell / occupancy
        ↓ player-specific projection
client PresentationTransform
```

### 5.1 WorldPosition

Authoritative simulation location. For the Cataclysm profile it remains grid-aligned per Specs 12/#58.

### 5.2 SpatialCell

Derived authoritative indexing/occupancy bucket. It is not a renderer coordinate.

### 5.3 PresentationTransform

Client-only continuous transform used to render a projected object.

For the initial Cataclysm profile a client may render:

```text
authoritative samples:
tick 100 -> (10,10,0)
tick 101 -> (11,10,0)

presentation frames:
(10.00,10.00) -> (10.23,10.00) -> (10.71,10.00) -> (11.00,10.00)
```

At every intermediate frame, authoritative occupancy remains whichever confirmed simulation state the server owns. The intermediate pixels cannot be used for:

- collision;
- reachability;
- melee adjacency;
- projectile collision;
- trap triggering;
- LOS/FOV;
- item pickup;
- interaction range;
- move cost;
- active-region ownership;
- save state.

## 6. Projection input contract

Spec 25 owns transport/protocol mechanics. This specification requires the following semantic information, regardless of wire encoding.

### 6.1 Projection baseline

A full projection baseline/snapshot carries:

- stable `ProjectionBaselineId` or equivalent epoch;
- authoritative sample tick/time;
- projection schema/content generation as already required by owning specs;
- all currently authorized projected objects/cells needed to establish current client state.

A reconnect or forced resync creates a new baseline epoch. Client interpolation history from an older epoch must not cross into the new epoch.

### 6.2 Replaceable object sample

Conceptual shape:

```text
EntityPresentationSample
    StableRef
    ProjectionRevision
    AuthoritativeSampleTick
    WorldPosition
    SpatialCell?             // only when authorized/useful
    Facing/semantic visual state?
    MotionContinuity         // Continuous | Discontinuous | Unknown
    PresentationState        // semantic, audience-safe fields only
```

The exact DTO shape belongs to Spec 25 implementation, but these semantics are required.

### 6.3 Removal / hidden transition

A projection removal is keyed by stable projected identity and revision/baseline. On removal:

1. the client removes the object from interactive/current-world presentation;
2. interpolation history for that visibility instance is destroyed;
3. no hidden future samples are retained or predicted;
4. a later re-entry is treated from its newly authorized state, not interpolated from the last hidden location.

### 6.4 Transient authoritative presentation event

Examples:

- hit;
- muzzle flash / projectile cue;
- explosion visual cue;
- bash effect;
- sound-derived visual indicator;
- activity/result feedback.

An event carries:

- stable event/correlation identity sufficient for de-duplication;
- authoritative occurrence tick/order;
- semantic cue type;
- only viewer-authorized source/target/location facts.

The client owns duration/easing/particle count/pixel treatment. Gameplay is already resolved.

## 7. Revision, ordering and stale-data rules

### 7.1 Baseline epoch

A message from an obsolete `ProjectionBaselineId` is ignored.

### 7.2 Per-object monotonic revision

For the same projected object/visibility instance, lower or duplicate revisions cannot overwrite a newer accepted revision.

### 7.3 Authoritative sample tick

Interpolation uses authoritative sample ticks, not network receipt timestamps, to determine the logical interval between samples.

Receipt time may be used only for local pacing/jitter-buffer estimation.

### 7.4 Missing predecessor

If the client has only one accepted authoritative sample, it renders that sample directly. It must not invent a prior location.

### 7.5 Gap/coalescing

Spec 25 may coalesce replaceable updates. Therefore two received samples may be separated by several canonical ticks. Interpolation may visually bridge the confirmed endpoints only when `MotionContinuity=Continuous` and the client has sufficient authorized information to do so.

The client must not infer undisclosed intermediate visited cells for gameplay, knowledge or interaction.

### 7.6 Unknown continuity

`MotionContinuity=Unknown` is conservative: snap to the newest authoritative sample or use a transition that reveals no unconfirmed path. Do not assume a straight-line traversal.

## 8. Initial interpolation policy

The first implementation should use **confirmed-sample interpolation with no authoritative extrapolation**.

### 8.1 Render timeline

Maintain a small ordered buffer of accepted authoritative samples per projected object.

The presentation clock selects a render point behind the newest known authoritative sample by a configurable client interpolation delay derived from observed projection cadence. It then interpolates between the nearest bracketing accepted samples.

Correctness does not depend on an exact delay value. For the Cataclysm profile, an initial default of approximately one ordinary projection interval is suitable, but the implementation must derive behaviour from sample timestamps rather than hard-code “100 ms means one game action”.

### 8.2 Interpolation function

For samples at ticks `T0 < T1`:

```text
alpha = clamp((renderTick - T0) / (T1 - T0), 0, 1)
PresentationTransform = Interpolate(P0, P1, alpha)
```

The interpolation curve may be linear or a local easing curve, provided:

- endpoints are exact;
- it is monotonic between endpoints for ordinary movement;
- it does not overshoot into unauthorized cells;
- it does not affect authoritative logic;
- test mode can select a deterministic curve.

### 8.3 No extrapolation by default

If render time reaches/passes the newest authoritative sample and no newer sample is available, hold the newest confirmed transform.

This avoids inventing unseen movement during packet delay/backpressure.

Future optional local prediction is permitted only under section 10.

### 8.4 Discontinuities

Snap immediately to the newest sample when:

- baseline epoch changes;
- `MotionContinuity=Discontinuous`;
- z-level/scene transition is designated discontinuous;
- object is newly visible and no authorized predecessor exists;
- the previous sample was removed/hidden;
- the client detects an unrecoverable revision gap and performs resync;
- the server explicitly marks teleport/forced relocation/correction as discontinuous.

A client may apply a short purely visual fade/camera easing around a snap, but must not show a fabricated traversed path.

### 8.5 Vehicles and multi-cell/multi-tick movement

Vehicle presentation may include orientation and richer profile-specific motion hints where the authoritative vehicle spec permits them. The generic rule remains sample-driven: no Godot rigid-body transform becomes authoritative and no unsent intermediate collision path is inferred.

## 9. Camera and viewport contract

The camera is wholly client-local presentation state.

A camera may:

- follow the controlled Character projection;
- lag/smooth relative to the anchor;
- pan for look/targeting modes;
- zoom;
- shake;
- use local dead zones;
- use local accessibility preferences.

A camera may not:

- move the controlled actor;
- grant active-region ownership;
- extend FOV/knowledge;
- reveal hidden server state;
- alter target legality;
- change canonical time.

Interest/prefetch may be technically larger than the camera, but Spec 26 visibility/knowledge filtering still applies before data reaches the client.

## 10. Optional local prediction

Spec 25 permits purely cosmetic prediction. This specification constrains it.

### 10.1 Allowed prediction

A client may begin a local visual motion after submitting a command if:

- the command does not become authoritative locally;
- the prediction is keyed to the request/correlation ID;
- gameplay queries continue to use confirmed projection state;
- the predicted path reveals no hidden information;
- the prediction can be cancelled/reconciled immediately.

### 10.2 Initial recommended scope

For the first implementation, prediction SHOULD be limited to the locally controlled actor's simple movement presentation and MAY be disabled entirely until confirmed-sample interpolation is stable.

### 10.3 Rejection/correction

If the server rejects the request or confirms a different result:

- discard the predicted endpoint;
- reconcile to the newest authoritative sample;
- use snap or bounded correction smoothing;
- never change authoritative state to preserve a client animation.

### 10.4 No prediction for hidden actors

Do not extrapolate hidden/removed entities, enemy motion outside projection, undisclosed projectiles or world changes not explicitly projected.

## 11. Correction and reconciliation policy

### 11.1 Small visual correction

When a newer confirmed continuous sample differs slightly from the current predicted/presented transform, the client may converge over a bounded local duration.

### 11.2 Authoritative discontinuity

For teleports, visibility re-entry, map load/resync, large unknown corrections or explicit discontinuities, snap to authority.

### 11.3 Interaction state

Clickable/targetable interaction references use stable authoritative/projected identity, not the interpolated transform. If a sprite is visually between cells, its authoritative interaction location is the latest permitted logical state exposed by the owning interaction projection.

The UI may choose a visual hitbox spanning the sprite, but command submission identifies the stable entity/target reference and the server revalidates.

## 12. Visibility, privacy and information non-leakage

This section is mandatory, not a rendering optimization.

### 12.1 Authorized inputs only

The presentation client can interpolate only state already present in its player-specific projection.

It must not receive:

- hidden entity positions for “smoothness”;
- future positions for look-ahead;
- server spatial-index contents;
- other players' private state;
- unseen map mutations;
- unrevealed target identities.

### 12.2 Re-entry reset

When an entity leaves visibility and later re-enters, the client must not interpolate from the last visible position to the new one across the hidden interval unless the server explicitly authorizes a path/history representation.

Default Cataclysm-profile behaviour is snap/newly present from the new authorized sample.

### 12.3 Remembered map

Remembered terrain/overmap knowledge may be rendered from Spec 26 durable knowledge, but it is not evidence that current entities/terrain changes are still present.

### 12.4 Camera/animation side channels

Camera shake, danger indicators, particle effects, audio visualization and similar presentation must be driven only by viewer-authorized semantic events/context. They cannot reveal hidden event sources.

## 13. Transient animation contract

### 13.1 Server/domain responsibility

The authoritative side resolves gameplay and emits an audience-filtered semantic result/event.

Examples:

- attack resolved;
- projectile impact occurred;
- explosion occurred;
- damage applied;
- door bashed;
- activity completed.

### 13.2 Client responsibility

The client chooses:

- sprite/effect resource;
- local duration;
- easing;
- particle count;
- frame timing;
- screen shake;
- animation layering;
- whether optional disposable effects are dropped under local overload.

Animation completion does not gate simulation progression unless the gameplay spec explicitly defines a long-running authoritative activity.

### 13.3 Event loss/backpressure

Spec 25 distinguishes reliable facts/events from disposable presentation hints. A gameplay consequence that must be understood by the player cannot be reclassified as disposable merely because it is animated.

If a disposable visual cue is dropped, the resulting authoritative state projection must still be sufficient to recover correct current presentation.

## 14. Render frame rate and canonical time

The renderer may run at 30, 60, 144 Hz or variable refresh. None of these alter:

- canonical simulation tick;
- action budget accrual;
- activity progress;
- scheduled events;
- RNG stream;
- visibility;
- persistence.

Changing rendering frame rate may change only local visual smoothness and presentation-only animation frame sampling.

When the server is globally paused by Spec 01 policy, the client may continue UI/camera/cosmetic animation if desired, but authoritative sample time does not advance. Any animation semantically tied to world progression must obey the event/state semantics chosen by its owner; purely decorative idle animation may continue.

Acceleration/timewarp changes authoritative samples more rapidly in host wall time. The client must still treat sample ticks as authoritative and may coalesce/snap when the presentation cannot usefully display every intermediate state.

## 15. RNG and determinism

### 15.1 Authoritative RNG isolation

Interpolation, camera smoothing, particles, sprite animation and cosmetic variation consume no authoritative simulation RNG.

### 15.2 Stable cosmetic selection

Where visual stability matters, use a stable presentation hash or client-local deterministic chooser based on permitted semantic identity, as Spec 22 requires. Redrawing/reconnect should not cause avoidable flicker.

### 15.3 Test determinism

Client conformance tests must allow injection of:

- deterministic render clock;
- deterministic interpolation curve;
- deterministic cosmetic chooser;
- deterministic projection sequence.

This lets rendering-boundary tests be exact without coupling production visuals to the authoritative RNG stream.

## 16. Persistence and reconnect

### 16.1 World save exclusions

Do not persist in authoritative world saves:

- Godot node IDs;
- `PresentationTransform`;
- interpolation buffers;
- camera state;
- local animation progress;
- texture/audio/font resources;
- network receipt timestamps;
- local prediction state.

### 16.2 Client/profile preferences

Local preferences such as camera smoothing, reduced motion, animation speed, interpolation delay bounds and zoom remain client/device state by default. If a product later elects to account-sync a preference, that cross-world ownership follows Specs 21/22 and Spec 28 / #91; it is never world authority.

### 16.3 Reconnect

On reconnect:

1. bind the connection to the stable player identity per Specs 20/25;
2. receive a fresh projection baseline;
3. discard old baseline interpolation/history;
4. instantiate presentation state from current authorized projection;
5. resume interpolation only after enough new same-baseline samples exist.

A reconnect never rewinds server time to match a client's old animation state.

## 17. Single-player and multiplayer equivalence

Single-player is a one-player authoritative server using in-process transport.

The Godot presentation layer must consume the same semantic projection/result/event contract in both modes.

Forbidden single-player shortcut:

```text
Godot node -> direct ECS mutation/read -> render
```

Required logical flow:

```text
input
  -> request
  -> authoritative simulation
  -> player-specific projection/result/event
  -> client projection cache
  -> PresentationTransform/animation
  -> render
```

The in-process transport may optimize serialization away, but it may not bypass authority, visibility filtering, stable identity/revision or reconciliation semantics.

## 18. Concurrency and contention

Several players may affect the same authoritative object.

Presentation must represent the authoritative ordering/result rather than preserving each client's expectation.

Example:

1. two clients submit mutually exclusive interaction/movement requests;
2. server resolves in deterministic order;
3. one succeeds and one is rejected/stale;
4. each client receives its own result plus viewer-authorized updated projection;
5. any local predicted animation is reconciled to the authoritative outcome.

No client transform lock or animation reservation grants simulation ownership.

## 19. Failure and edge behaviour

- **Out-of-order update:** ignore older per-object revision.
- **Obsolete baseline:** ignore and request/await current baseline.
- **Unknown object delta:** do not invent object state; request/await resync according to Spec 25.
- **Missing asset:** use Spec 22 fallback; gameplay remains valid.
- **Slow renderer:** drop/reduce disposable local effects; render newest valid current projection.
- **Slow network client:** Spec 25 may coalesce replaceable samples; client must tolerate larger sample gaps.
- **Entity removed:** clear interaction and interpolation history.
- **Entity dies/despawns during interpolation:** authoritative removal/result wins immediately; optional death animation can play from the last authorized/current presentation anchor.
- **Teleport while interpolating:** cancel interpolation and snap/fade to new sample.
- **Projection visibility loss:** remove immediately according to policy; do not complete the visual path into hidden state.
- **Server pause:** authoritative sample tick freezes; local UI remains responsive.
- **Time acceleration:** presentation may skip visual intermediates but final authoritative state/order remains correct.
- **Content/presentation-pack reload:** keep authority stable; rebuild local visuals from semantic IDs.
- **Godot scene reload:** reconstruct from current projection without server-world reload.

## 20. Dependency contracts

### Spec 01 / #66 and #57 — time

Provides canonical sample tick and pause/acceleration policy. Presentation time is separate.

### Spec 12 / #77 and #58 — spatial

Provides authoritative `WorldPosition`, derived `SpatialCell`, index semantics and Cataclysm grid profile.

### Spec 20 / #85 — persistence

Provides stable world/player/entity continuation and excludes presentation/transport state from saves.

### Spec 21 / #86 — UI/input

Provides stable interactive references, request submission, modal/focus/accessibility and stale UI handling.

### Spec 22 / #87 — graphics/audio/localization

Provides semantic asset lookup, fallback, cosmetic RNG separation and client presentation resources.

### Spec 25 / #90 — networking

Provides projection/message classes, baseline/snapshot delivery, coalescing/backpressure, stable identity, session/reconnect and transport equivalence.

### Spec 26 / #93 — active regions/visibility

Provides server-owned active regions, interest/visibility/knowledge authorization and projection enter/update/leave semantics.

No dependency may call Godot presentation state to answer an authoritative gameplay question.

## 21. Implementation-facing client components

Names are conceptual, not mandatory class names.

### ProjectionStore

Responsibilities:

- accept current-baseline snapshots/deltas/removals/events;
- enforce revision ordering;
- expose immutable current projected semantic state to UI/render systems;
- never expose hidden server state.

### PresentationHistory

Responsibilities:

- retain a bounded ordered sample history per currently projected stable object;
- clear on baseline change/removal/re-entry;
- provide bracketing samples for interpolation.

### PresentationTransformSystem

Responsibilities:

- compute render transforms from confirmed samples;
- apply discontinuity rules;
- optionally reconcile local prediction;
- never write authoritative state.

### CameraController

Responsibilities:

- convert presentation anchors into viewport transforms;
- obey local preferences/accessibility;
- not affect interest/FOV/authority.

### PresentationEventPlayer

Responsibilities:

- de-duplicate semantic event IDs;
- map events through Spec 22 asset definitions;
- play local transient effects;
- permit dropping only explicitly disposable cues.

### ReconciliationController

Responsibilities:

- correlate optional local prediction with authoritative results;
- cancel/reconcile prediction;
- trigger snap/fade/correction policy.

## 22. Black-box and conformance scenarios

### Reference/parity evidence

**P27-01 — discrete draw anchor**  
Given a pinned Cataclysm actor at a known `tripoint_bub_ms`, verify the renderer consumes that discrete logical location; no continuous gameplay position is required by the reference renderer.

**P27-02 — wall-clock bullet delay**  
Trigger a visible bullet animation under two `ANIMATION_DELAY` settings. The visual delay changes while the already-resolved gameplay result/time rule does not.

**P27-03 — target-practice bullet delay exception**  
Exercise the pinned target-practice condition and verify visual bullet delay may be skipped without changing authoritative shot outcome.

**P27-04 — idle tile wall-time animation**  
Hold simulation state constant while wall time advances; an animated tile may change presentation frame without changing world state.

**P27-05 — tileset offset/pixel scale**  
Load a fixture with non-default sprite offset/pixel scale. Rendering changes; authoritative position/occupancy does not.

### Boundary and interpolation

**G27-01 — WorldPosition versus PresentationTransform**  
Move a Cataclysm actor from cell A to adjacent cell B. During visual interpolation, authoritative occupancy/interaction remains server-defined; no intermediate presentation coordinate is queryable as authoritative gameplay state.

**G27-02 — render-rate independence**  
Replay the same authoritative projection sequence at 30, 60 and 144 render FPS. Final projection state and authoritative trace are identical; only sampled presentation transforms differ.

**G27-03 — canonical-rate independence**  
Run a non-Cataclysm test profile with a different fixed simulation rate. The same sample-driven client interpolation code works without assuming 10 TPS/100 ms.

**G27-04 — confirmed-sample interpolation**  
Provide samples `T0/P0` and `T1/P1` with continuous motion. At deterministic render times between T0 and T1, presentation lies monotonically between exact endpoints.

**G27-05 — no newest-sample extrapolation**  
Stop delivery after P1. After the render timeline reaches P1, the client holds P1 rather than inventing P2.

**G27-06 — coalesced projection gap**  
Deliver T0 then T4 because replaceable state was coalesced. The client tolerates the gap and either interpolates authorized continuous endpoints or snaps per continuity policy; it does not synthesize gameplay events for T1-T3.

**G27-07 — discontinuous teleport**  
While interpolating ordinary motion, receive a discontinuous teleport sample. Pending interpolation is cancelled and the object appears at the authoritative destination without showing a fabricated traversed path.

**G27-08 — z-level discontinuity**  
Receive a designated discontinuous z-level transition. The client snaps/rebuilds the scene representation; no between-floor collision path is invented.

### Revision and stale handling

**G27-09 — out-of-order object revisions**  
Accept revision 12, then receive revision 11. Revision 11 cannot rewind either current projection state or interpolation history.

**G27-10 — obsolete baseline**  
After reconnect baseline B2 is installed, receive a delayed B1 update. It is ignored.

**G27-11 — unknown delta**  
Receive a delta for an object absent from the current baseline. Client does not invent missing fields and follows resync/refresh policy.

**G27-12 — removal during interpolation**  
An entity leaves projection while visually between samples. It is removed according to the authoritative visibility transition and its interpolation history is cleared immediately.

### Privacy/visibility

**G27-13 — hidden movement non-leakage**  
Player A sees an entity leave visibility, the entity moves several cells while hidden, then re-enters. A's client receives no hidden positions and does not interpolate across the hidden path.

**G27-14 — overlapping regions, different projections**  
Two players share one authoritative active region but have different FOV. Each client interpolates only its authorized entity set; active state does not imply replicated state.

**G27-15 — camera cannot extend knowledge**  
Pan/zoom the camera beyond current visible/remembered area. No extra authoritative cell/entity detail appears solely because it is inside the viewport.

**G27-16 — hidden event source**  
Trigger an event outside visual knowledge but audibly perceivable per domain rules. Client may receive the authorized audible semantic cue but no hidden visual source position/entity identity.

### Prediction/reconciliation and contention

**G27-17 — optional local move prediction accepted**  
Predict a local movement request cosmetically; server accepts matching destination. Prediction converges to the authoritative sample without authority divergence.

**G27-18 — optional local move prediction rejected**  
Predict a local movement request; server rejects due to stale contention. Client returns to the latest authoritative sample and displays the rejection; no local occupancy mutation persists.

**G27-19 — two-player contention**  
Two players attempt an exclusive interaction with the same object. Server deterministic result controls both clients; each reconciles any local expectation/prediction to its result and updated projection.

**G27-20 — correction while animating**  
Receive a small continuous authoritative correction during local prediction. Client converges within the configured bounded correction window, ending exactly on authority.

### Pause, acceleration and performance

**G27-21 — explicit server pause**  
Pause the authoritative one-player server. Canonical sample tick and world state freeze; camera/UI may continue local animation. Resume without accumulated simulation drift from render time.

**G27-22 — multiplayer client cannot local-pause world**  
Open a modal client UI for player A while player B continues. A's renderer/UI state changes only locally; server and B progress.

**G27-23 — accelerated host pacing**  
Run identical N canonical ticks at normal and approved accelerated host pacing. Client may render fewer intermediate frames, but authoritative samples/final state are equivalent.

**G27-24 — slow renderer**  
Artificially stall Godot rendering while projections continue. On recovery the client discards obsolete disposable visual work and presents the newest valid state without mutating/replaying simulation.

**G27-25 — slow network/coalescing**  
Apply Spec 25 outbound coalescing. The client handles skipped replaceable transforms without requiring every simulation tick to be delivered.

### Persistence/reconnect and transport equivalence

**G27-26 — world save excludes presentation state**  
Save while an entity is mid-interpolation and camera is offset. Load world; authoritative state is restored exactly, but interpolation/camera state is rebuilt locally rather than restored from world save.

**G27-27 — reconnect fresh baseline**  
Disconnect while movement is in progress. World continues per authoritative policy. Reconnect to a fresh baseline at a later tick; old interpolation history is discarded and rendering resumes from current authorized state.

**G27-28 — in-process versus loopback**  
Feed identical accepted command/projection sequences through in-process and loopback transports. Projection semantics and final presentation anchors are equivalent modulo network arrival jitter; no direct ECS shortcut exists in-process.

**G27-29 — presentation-pack divergence**  
Two clients use different tilesets/animation settings for the same authoritative state. Gameplay/projection results remain identical.

**G27-30 — cosmetic RNG isolation**  
Change particle/sprite cosmetic randomization seed. Authoritative RNG trace and simulation outcome remain unchanged.

### Scene and lifecycle

**G27-31 — Godot scene reload**  
Destroy/recreate the presentation scene while server continues. Rebuild from a fresh/current projection without restarting or rewinding world simulation.

**G27-32 — death/despawn while interpolating**  
An entity dies/despawns while visually in transit. Current authoritative removal/result wins; any death animation is local and cannot delay entity removal from gameplay.

## 23. Acceptance mapping for #94

- [x] Pinned CDDA rendering/animation reference behaviour and evidence documented.
- [x] Pinned reference behaviour separated from OctoGhast adaptation and implementation contract.
- [x] `WorldPosition` / `SpatialCell` separated from client-only `PresentationTransform`.
- [x] Snapshot/delta/event semantic inputs and stable identity/revision requirements explicit.
- [x] Interpolation, discontinuities, snapping, correction and bounded optional prediction policy defined.
- [x] Animation/camera/render timing separated from canonical time and authoritative RNG.
- [x] Visibility/knowledge/interest non-leakage rules explicit.
- [x] Stale/out-of-order/removal/contention correction behaviour explicit.
- [x] Disconnect/reconnect and persistence exclusions/rebuild defined.
- [x] In-process single-player and network multiplayer use the same logical presentation contract.
- [x] Concrete black-box/conformance scenarios G27-01–G27-32 plus pinned-reference scenarios P27-01–P27-05 defined.
- [x] Dependencies with Specs 01/12/20/21/22/25/26 explicit.
- [x] No new unresolved cross-cutting architecture decision identified.

## 24. Implementation sequencing guidance

A low-risk implementation order is:

1. implement `ProjectionStore` and baseline/revision rejection with no smoothing;
2. render current authoritative projected state in Godot from stable semantic IDs;
3. add bounded per-object `PresentationHistory`;
4. add confirmed-sample movement interpolation only;
5. add discontinuity/removal/reconnect reset rules;
6. add local camera smoothing;
7. add semantic transient event animation;
8. add optional local-player cosmetic prediction only after reconciliation tests pass;
9. optimize coalescing/jitter behaviour after profiling.

Do not begin by making Godot physics or scene transforms authoritative. Do not make interpolation correctness depend on receiving every server tick.

## 25. Completion statement

This investigation finds no contradiction with the settled OctoGhast architecture and no new cross-cutting decision requiring a separate architecture ticket.

The deliberate divergence from pinned CDDA is limited to the presentation topology: pinned CDDA draws discrete authoritative state directly in-process, while OctoGhast renders explicit player-specific authoritative samples through a client boundary and may smooth them locally. The underlying Cataclysm gameplay/spatial/visibility rules remain profile-authoritative.
