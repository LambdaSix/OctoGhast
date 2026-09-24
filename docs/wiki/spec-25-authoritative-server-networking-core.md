# Spec 25 — Authoritative server, transport, session and projection boundary

## 1. Purpose and status

This specification defines the implementation contract for OctoGhast's authoritative game-server boundary, in-process single-player transport, cooperative socket transport abstraction, connection/session lifecycle, deterministic request intake, bounded networking resources and player-specific projection.

It completes the specification work owned by #90 and supplies the networking/server contract consumed by #60 and #62. It does **not** implement sockets or gameplay/runtime code.

The Cataclysm reference baseline is pinned to:

`LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`.

The governing split is:

1. **Pinned CDDA reference behaviour** — source/tests define action, move-cost, interaction, visibility and gameplay-result semantics that requests ultimately invoke.
2. **OctoGhast Cataclysm profile** — preserves those Cataclysm gameplay rules behind authoritative commands/activities/queries.
3. **Generic Core/server contract** — owns transport-neutral messages, authoritative request admission, deterministic ordering, session/player/entity identity separation, bounded transport resources and projection delivery.
4. **Future evolution seam** — transport backend, serializer, authentication provider, rules profile and presentation client can change without changing the simulation authority model.

CDDA parity is a reference milestone, not a permanent platform ceiling. Networking infrastructure must therefore not encode Cataclysm-specific movement, grid, action-currency or UI assumptions into generic Core APIs.

## 2. Architectural prerequisites and ownership

This specification consumes, rather than reopens:

- #52 — authoritative ECS/server/client direction;
- #57 / Spec 01 — canonical time, deterministic simulation intake and Cataclysm 10-TPS profile mapping;
- #58 / #77 / Spec 12 — authoritative WorldPosition/SpatialCell, active-region and visibility/projection ownership;
- #69 / Spec 04 — commands, activities, synchronous queries and result/event semantics;
- #71 / Spec 06 — authoritative item-transfer contention/stale rejection;
- #82 / Spec 17 — event audience/context isolation;
- #85 / Spec 20 — persistence, stable world-local PlayerId, reconnect and exclusion of sockets/presentation state;
- #86 / Spec 21 — Godot interaction/projection boundary and stale-UI handling;
- #88 / Spec 23 — deterministic in-process/loopback conformance testing;
- #89 / Spec 24 — host/platform/runtime resource layout;
- #91 — cross-world profile/account/meta-progression identity and persistence where required.
- #92 — authentication, credential/provider and public-server security policy.

### Ownership table

| Concern | Owner |
| --- | --- |
| Socket accept/connect, framing, bytes, buffer pools | transport backend |
| Connection lifecycle and parser state | networking Core |
| Authentication-provider integration | #92 security/auth architecture; not gameplay |
| Live connection -> stable PlayerId binding | server session layer |
| PlayerId -> controlled CharacterId binding | authoritative server/world policy |
| Gameplay validation and mutation | simulation/domain systems |
| Canonical tick and equal-tick ordering | server simulation intake |
| FOV/knowledge/interest authorization | projection/domain policy |
| DTO construction and message classification | projection/protocol layer |
| Godot nodes, interpolation, input devices | client presentation |
| Saved world/player/entity state | Spec 20 persistence |
| Cross-world account/profile state | #91 |
| Socket IDs, buffers, parser offsets, connection metrics | never world-save state |

No network callback, Godot callback or serializer callback may directly mutate authoritative ECS/world state.

## 3. Pinned CDDA reference evidence

### 3.1 Local action dispatch is reference behaviour, transport is not

Pinned source evidence:

- [`src/input.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/input.cpp) defines local input contexts/keybindings and maps physical input to logical action identifiers.
- [`src/action.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/action.cpp) defines the logical gameplay action identifier surface.
- [`src/game.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/game.cpp) is the single-process game orchestration path which consumes those actions and invokes avatar/world behaviour.
- [`tests/move_cost_test.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/move_cost_test.cpp) verifies observable movement costs, including ordinary walking and movement-mode modifiers.

The relevant parity rule is the *resulting gameplay behaviour and action economy*. OctoGhast does not preserve the upstream single-process ownership boundary as a product constraint.

### 3.2 Example preserved rule

The pinned movement-cost tests demonstrate that an ordinary normalized walking move can consume 100 moves, crouching can consume 200 in the tested setup, and prone movement can consume 600 before further encumbrance effects. These are Cataclysm-profile rule outcomes, not milliseconds and not transport timings.

A `MoveRequest` therefore carries intent (for example direction and preconditions), not a client-selected elapsed duration or authoritative move cost. The server invokes the Cataclysm movement resolver and applies the pinned rule cost through Spec 01/04 scheduling.

### 3.3 Turn-gated UI assumption

Pinned CDDA commonly executes local interaction screens synchronously in the same process as the single-player game loop. OctoGhast intentionally changes that temporal relationship: client menus do not own or pause the canonical server clock. Spec 21 is authoritative for UI adaptation.

## 4. Topologies

### 4.1 Single-player

Single-player is exactly one connected player session using an in-process transport:

```text
Godot client
   -> IClientTransport
   -> InProcessTransport
   -> server ingress queue
   -> deterministic simulation intake
   -> command/query/activity resolution
   -> player-specific projection
   -> InProcessTransport
   -> client
```

The in-process backend MAY pass typed immutable DTOs without byte serialization. It MUST NOT bypass:

- message type validation;
- session/player binding;
- authorization;
- deterministic intake ordering;
- gameplay precondition validation;
- projection filtering;
- result/rejection semantics.

Calling ECS/domain methods directly from Godot because the server shares a process is forbidden.

### 4.2 Cooperative/dedicated server

```text
client -> TCP transport -> framing/parser -> bounded ingress
                                             |
                                    deterministic intake
                                             |
                                      authoritative server
                                             |
                                  player-specific projection
                                             |
client <- TCP transport <- bounded egress <---+
```

Cataclysm gameplay systems cannot distinguish in-process from socket transport.

## 5. Protocol model

### 5.1 Logical envelope

The application protocol uses a strongly typed envelope conceptually containing:

- protocol major/minor version;
- message kind/type;
- connection-local message sequence;
- optional request/correlation ID;
- payload length (wire transports only);
- typed payload.

The logical envelope is serializer-independent.

### 5.2 Initial M1 wire transport decision

The initial production socket backend is **TCP using modern .NET socket APIs**, behind replaceable `IClientTransport` / `IServerTransport` / connection abstractions.

Rationale:

- the initial protocol requires reliable ordered delivery for commands/results and world-join control traffic;
- TCP already supplies stream reliability while the application explicitly tests fragmentation/coalescing;
- transport replacement remains possible because no simulation code references TCP types;
- QUIC/reliable-UDP variants are future profiling/product decisions, not Core invariants.

This decision does not make TCP a permanent platform law.

### 5.3 Initial framing

Wire messages use a length-prefixed binary frame. Normative logical fields are version, message type, sequence/correlation metadata and typed payload. Exact serializer code generation is not fixed by this spec.

Required parser properties:

- fixed-size bounded header sufficient to reject impossible versions/types/lengths before payload allocation;
- unsigned payload length;
- complete-frame validation before deserialization into expensive domain DTOs;
- no trust in client-supplied collection/string counts;
- partial frame state separate from connection/session state.

Default implementation limits for M1:

- pre-auth maximum frame payload: 64 KiB;
- active-session maximum frame payload: 1 MiB;
- maximum UTF-8 string payload field unless a message defines a lower cap: 64 KiB;
- maximum generic collection elements unless a message defines a lower cap: 16,384;
- decompressed-size limit, if compression is later enabled: same logical message cap unless explicitly versioned higher.

Limits are configuration values with hard server maxima. Raising a deployment limit cannot bypass compiled structural safety checks.

### 5.4 Versioning

- Protocol **major** mismatch: reject during negotiation before world join.
- Protocol **minor** mismatch: allowed only when declared feature negotiation proves required message semantics are compatible.
- Unknown mandatory message type: protocol error.
- Unknown optional capability: ignore/decline via negotiated capability rules.
- World/content compatibility is separately checked using Spec 18/19/20 build/content provenance; successful wire negotiation does not imply save/content compatibility.

## 6. Message semantics

Messages are classified by semantic contract, not merely by transport reliability.

### 6.1 Client -> server

- **Command/intent request** — asks the server to attempt an authoritative mutation/action.
- **Activity request** — starts/cancels durable work through Spec 04.
- **Synchronous query** — requests bounded authorized information; does not mutate gameplay state or consume authoritative RNG. Metrics, rate-limit counters and disposable query caches are infrastructure bookkeeping, not a gameplay mutation exception. A query that would trigger generation or another semantic transition must hand off to its owning authoritative operation.
- **Session/control message** — negotiation, join, leave, heartbeat, capability exchange.
- **Social/admin message** — separately capability/authorization checked.

Every gameplay-affecting request includes a correlation ID and stable domain references needed by the owning spec. Client array indices, Godot node IDs and socket IDs are invalid gameplay references.

### 6.2 Server -> client

Three egress classes are normative:

1. **Reliable ordered facts/results** — accepted/rejected request results, inventory transaction results, activity lifecycle facts, death, durable messages, world-join/session transitions. Never silently coalesced away.
2. **Replaceable current state** — current projected actor transforms/cells, current visible stats, current weather presentation state, current visible-entity snapshot fields. Older unsent values for the same semantic key MAY be replaced by newer ones.
3. **Disposable presentation hints** — cosmetic hints whose omission does not alter authoritative understanding. MAY be dropped under backpressure.

A message carrying information needed to understand a gameplay consequence is not disposable merely because it has a visual representation.

### 6.3 Ack/reject/result

Every correlated gameplay request reaches one terminal externally visible state:

- `Accepted` with authoritative result/reference;
- `Rejected` with stable machine-readable reason and optional safe refresh hint;
- `Superseded` only for explicitly replaceable client requests;
- `SessionClosed` if the connection/session ended before admission.

Transport receipt is not gameplay acceptance.

## 7. Deterministic request intake

### 7.1 Boundary

Transport/parser threads enqueue validated immutable request DTOs. The authoritative simulation drains admitted requests only at the Spec 01 deterministic server intake phase.

### 7.2 Equal-tick ordering rule

**Post-spec qualification:** [#95](https://github.com/LambdaSix/OctoGhast/issues/95) owns reconciliation of this admission key with the actor execution keys in Specs 01/04/09, AI/system precedence and bounded candidate selection. The following recorded network-intake rule is not yet a complete cross-source contention contract.

For requests assigned to the same canonical intake tick:

1. sort by stable world-local `PlayerId`;
2. then by that session's monotonically increasing client request sequence;
3. then by protocol message-type stable numeric discriminator as a final total-order tie-breaker.

AI/system commands are inserted through the same command scheduler using a separate deterministic source class and stable actor/entity identity; they do not pretend to be network players.

Connection/socket identity and arrival-thread ordering are never tie-breakers.

A reconnecting player retains `PlayerId` but begins a new connection-local transport sequence. The session layer maps accepted requests to a server-maintained per-player admission sequence so reconnect cannot create ambiguous ordering with already admitted work.

### 7.3 Stale commands and contention

Ordering does not guarantee success. The server revalidates domain preconditions at resolution time. If player A's earlier ordered request consumes/moves a shared resource, player B's later request receives the owning domain's stale/conflict rejection without mutation.

## 8. Identity and session lifecycle

Keep distinct:

- `ConnectionId` — ephemeral transport instance;
- `SessionId` — live server attachment/lifecycle identity;
- `ProfileId`/account identity — cross-world identity when configured; owned with #91/auth provider;
- `PlayerId` — stable world-local player identity persisted by Spec 20;
- `CharacterId` — authoritative controlled world entity;
- client-local presentation identity — never authoritative.

### 8.1 Connection state machine

```text
Accepted
 -> Negotiating
 -> Authenticating
 -> JoiningWorld
 -> Active
 -> Disconnecting
 -> Closed
```

Failure at any stage records a reason and transitions toward `Closed`; invalid messages for the current state are rejected.

Parser state (header bytes read, payload bytes read, malformed-frame state) is orthogonal to the session lifecycle.

### 8.2 Pre-auth resource promotion

Before successful authentication/authorization and world join, a connection receives only a small resource class:

- 64 KiB maximum individual payload;
- small bounded ingress/egress queues;
- no active-region lease;
- no world snapshot cache;
- no controlled entity;
- no gameplay queries;
- no large projection production.

Resource promotion occurs only after session policy has accepted world join.

### 8.3 Authentication ownership

#90 does not define user-account credential storage. Internet-facing authentication, credential recovery, trust providers and abuse/security policy are a distinct cross-cutting concern. M1 in-process tests may use a trusted synthetic/local provider. LAN/friend and public-dedicated authentication MUST be specified by the #92 before public networking is considered production-ready.

This does not weaken server authority: even a trusted local session still binds through PlayerId and uses normal request validation.

### 8.4 Disconnect

On disconnect request or transport loss:

- stop admitting new gameplay requests from that connection;
- preserve requests already admitted to the canonical simulation queue;
- allow a finite drain of already-enqueued reliable egress where the transport remains writable;
- do not enqueue new nonessential replaceable/disposable output for the disconnecting connection;
- detach `ConnectionId`/`SessionId` from PlayerId;
- retain PlayerId, CharacterId and world state according to Spec 20/character policy;
- release connection-owned projection caches and transport resources.

Default graceful-drain ceiling: 2 seconds host time. This is transport cleanup policy, not simulation time and does not pause the canonical clock.

## 9. Reconnect

Reconnect is session reattachment, not socket resurrection.

After identity verification and world compatibility checks:

1. bind the new connection/session to the existing stable PlayerId;
2. resolve current controlled CharacterId according to persisted/live server policy;
3. create a fresh projection baseline/snapshot for that player;
4. reset transport-local sequence/parser/buffer state;
5. continue canonical simulation from current server time—never rewind to the disconnect time.

Client interpolation, stale UI selections and unsubmitted local actions are not restored as authoritative state.

Disconnected-character simulation policy is owned by Spec 20/Character/world policy. Networking only preserves identity and reattachment semantics.

## 10. Bounded queues and backpressure

### 10.1 Required budgets

Each connection has independent bounded:

- ingress bytes;
- ingress message count;
- egress bytes;
- egress reliable message count;
- replaceable-state key count;
- requests admitted per canonical interval;
- query cost/budget.

Server-wide aggregate budgets also exist so many individually legal clients cannot exhaust process memory.

Initial M1 defaults:

- pre-auth queued ingress: 256 KiB / 128 messages;
- pre-auth queued egress: 256 KiB / 128 messages;
- active queued ingress: 2 MiB / 2,048 messages;
- active reliable egress: 4 MiB / 4,096 messages;
- replaceable-state cache: 16,384 semantic keys per active player;
- absolute aggregate budgets: deployment-configured, mandatory and observable.

These defaults are tuning values, not gameplay semantics. Tests verify boundedness, not a permanent exact byte count.

### 10.2 Exhaustion policy

Ingress:

- malformed/oversized: reject frame; close connection for protocol violations that make continued parsing unsafe;
- request-rate overflow: throttle/reject before expensive gameplay lookup;
- persistent ingress queue overflow: disconnect with overload reason.

Egress:

- replace newer state over older state with same semantic key;
- drop disposable hints first;
- never silently discard reliable facts;
- if reliable egress cannot drain within configured bounds, disconnect slow client while preserving authoritative world state.

The simulation never blocks waiting for a client's socket send buffer.

## 11. Rate limiting and hostile input

Layered gates:

1. cheap connection/IP acceptance gate;
2. negotiation/frame-rate cap;
3. authentication-attempt cap;
4. active-session request/query cap;
5. gameplay validation/authorization.

Rate limiting is infrastructure protection, not proof of gameplay validity.

Malformed frames are rejected before:

- ECS queries;
- active-region activation;
- expensive content lookup;
- password/provider work beyond the appropriate auth gate;
- large allocation proportional to peer-provided sizes.

## 12. Projection and interest boundary

### 12.1 Security property

A client receives only information that the authoritative server determines the player may know. Interest management is therefore both bandwidth policy and confidentiality/authority policy.

Projection consumes:

- Spec 12 FOV/LOS, active-region and remembered-map knowledge;
- ownership/access rules from inventory/item systems;
- private/owner/party/world visibility classifications from domain specs;
- global information deliberately exposed by rules.

Overlapping player active regions simulate each authoritative entity once. Each player may still receive a different projection of that same state.

### 12.2 Projection keys and coalescing

Replaceable state is keyed by stable semantic identity, e.g. `(PlayerId, CharacterId, field-group)`, `(PlayerId, EntityId, transform)`, or `(PlayerId, SpatialCell, visible-tile-state)`.

Coalescing is allowed only when the replacement is observationally equivalent to receiving intermediate states for gameplay understanding. Reliable causal events are emitted separately where intermediate facts matter.

### 12.3 What is never projected by default

- arbitrary ECS components;
- server RNG stream state;
- unseen entities;
- hidden inventories;
- unrevealed map/overmap cells;
- another player's private Character/profile state;
- server-only scheduler queues;
- socket/session internals;
- persistence implementation metadata not explicitly needed by client compatibility checks.

## 13. In-process transport contract

The in-process backend is a first-class conformance implementation, not a testing shortcut.

It MUST:

- support the same logical envelope/message DTOs;
- use bounded ingress/egress queues;
- preserve canonical intake ordering;
- preserve request/result correlation;
- pass through the same session/player authorization path;
- create the same player-specific projections;
- exercise disconnect/reconnect semantics;
- expose equivalent metrics at the logical-message level.

It MAY:

- skip byte framing and serialization;
- use immutable object references/value copies internally;
- execute transport pump calls synchronously from the host loop.

A conformance test that succeeds only because the in-process backend hands a mutable domain object across the boundary is invalid.

## 14. Serialization and immutable definition/runtime-state split

Protocol/message schemas are immutable definitions for one negotiated protocol generation. Runtime connection/session state is mutable but transient.

Immutable/versioned definitions include:

- message type IDs;
- field schemas;
- protocol version/capabilities;
- parser limits;
- stable rejection reason IDs;
- projection schema versions.

Mutable transient state includes:

- parser offsets;
- queues/buffers;
- ConnectionId/SessionId;
- transport sequence counters;
- backpressure state;
- metrics/high-water marks.

Mutable authoritative persistent state includes PlayerId/CharacterId/world data only through Spec 20, never by serializing the network session object.

## 15. RNG and determinism

Networking consumes **no authoritative gameplay RNG** for:

- connection IDs;
- transport sequence IDs;
- retry/backoff;
- serialization;
- fragmentation;
- queue admission;
- projection coalescing.

If security tokens require cryptographic randomness, that RNG is security infrastructure and isolated from deterministic simulation RNG streams.

For an identical canonical admission trace (assigned tick, stable source/order and semantic payload), content generation, active-region/lease inputs and initial authoritative/RNG state, simulation results must be identical regardless of:

- packet fragmentation/coalescing;
- socket callback thread;
- host network jitter that leaves that admission trace unchanged;
- serializer buffering;
- renderer frame rate;
- in-process versus loopback transport.

## 16. Persistence

Persist:

- stable world-local PlayerId;
- controlled CharacterId relationship according to world policy;
- canonical time, schedules, activities, RNG and authoritative world state via Spec 20;
- any durable gameplay facts produced before save barrier.

Do not persist:

- ConnectionId/SessionId;
- sockets;
- parser state;
- sequence counters that are transport-local;
- byte buffers;
- queue high-water marks;
- transient projection caches;
- client interpolation/UI state.

Save barriers operate at deterministic simulation boundaries and do not wait for arbitrary socket queues to empty. A reliable result that has already mutated world state but has not yet reached a disconnected client is reconstructed from authoritative projection/snapshot/result history as defined by the owning system, not by persisting raw socket buffers.

## 17. Godot/client boundary

Godot:

- converts local input into logical requests;
- maintains local focus/selection/interpolation;
- consumes explicit projections/results/events;
- may predict purely cosmetic movement/interpolation if it can reconcile to authority;
- cannot mutate ECS, set authoritative position, award items, advance canonical time or inspect hidden server state.

Opening UI does not pause the world. A stale UI submission is revalidated by the server.

## 18. Observability

Minimum per-server/per-connection metrics:

- connections by lifecycle state;
- bytes/messages in/out;
- ingress/egress depth and high-water marks;
- rejected/oversized/malformed frames;
- rate-limit/throttle hits;
- dropped disposable and coalesced replaceable messages;
- reliable egress saturation/disconnects;
- request admission tick and intake latency;
- disconnect reason;
- reconnect count;
- protocol version/capability distribution.

Payload logging is off by default, redacts credentials/secrets and must not bypass player privacy.

## 19. Dependency contracts

### Spec 01 / time
Networking supplies accepted requests to a deterministic intake phase. It does not define Cataclysm move-to-tick conversion or pause/timewarp policy.

### Spec 04 / commands and activities
Protocol request classes terminate at existing command/activity/query contracts; transport does not invent a second gameplay action model.

### Spec 12 / spatial and visibility
Networking consumes active-region/interest and visibility projection decisions; it does not own world activation.

### Spec 20 / persistence
Session rebind uses durable PlayerId/CharacterId/world state while transport state is excluded from saves.

### Spec 21 / UI
UI consumes stable IDs, queries, results and projections and tolerates staleness.

### #91 / profile
World-local PlayerId remains distinct from cross-world profile/account identity.

### #60
Implementation routes local player and AI-selected actions through common authoritative command resolution. Network and local clients differ only before command admission.

### #62
Vertical-slice tests prove in-process/loopback equivalence and bounded lifecycle behavior.

## 20. Explicitly deferred cross-cutting decisions

The original review separated production authentication/security into #92 and cross-world profile ownership into #91. The subsequent corpus audit also found [#95](https://github.com/LambdaSix/OctoGhast/issues/95) (admission/execution/activation integration) and [#96](https://github.com/LambdaSix/OctoGhast/issues/96) (bounded command deduplication/outcome recovery). Those tickets own the unresolved decisions; the completed transport/framing/resource/projection evidence remains valid.

The following decisions remain established, subject to the explicit integration qualifications above:

- initial socket transport: TCP;
- application framing: bounded length-prefixed typed binary envelope;
- network intake key: PlayerId then admitted request sequence then stable type discriminator; #95 must settle its relation to actor execution and cross-source contention;
- reliability classes: reliable facts, replaceable state, disposable hints;
- initial queue/frame caps: defined above and configurable within hard maxima;
- coalescing: only replaceable state with stable semantic keys;
- reconnect: new session binds stable PlayerId/current CharacterId, fresh projection, no clock rewind;
- protocol versioning: major negotiation + compatible minor capabilities;
- minimum metrics: section 18.

## 21. Conformance scenarios

### NET25-01 — Single-player crosses authority boundary
Given one local Godot client and an in-process server, when the player submits a movement request, then no client code mutates Position directly; the server resolves movement and returns a projection/result.

### NET25-02 — In-process/loopback rule equivalence
Run the same seeded initial world and accepted request sequence through in-process and TCP loopback transports. Authoritative world state, action costs, canonical eligibility and gameplay events are identical.

### NET25-03 — Pinned movement cost survives transport
Using the pinned movement fixture equivalent to ordinary normalized walking, the server resolves the Cataclysm-profile action cost; changing network latency does not change that cost.

### NET25-04 — Fragmented frame
Deliver a valid TCP frame one byte/chunk at a time. No request is admitted until the complete frame exists; result equals unfragmented delivery.

### NET25-05 — Coalesced frames
Deliver several complete frames in one receive. Each is parsed exactly once and admitted according to deterministic intake.

### NET25-06 — Oversized pre-auth frame
Declare a payload above the pre-auth maximum. Reject before allocating proportional payload memory or performing world lookup; world state is unchanged.

### NET25-07 — Impossible collection count
A structurally valid frame contains a collection count beyond message limits. Reject deterministically without large allocation or world mutation.

### NET25-08 — Async callback isolation
A network callback completes mid-tick. Inspect authoritative state before the next intake phase: unchanged. After intake, the validated request may resolve.

### NET25-09 — Equal-tick multiplayer contention
Two players submit valid transfers for the same unique item for the same intake tick. Ordering follows stable PlayerId/admission sequence; one succeeds and the other receives the domain stale/conflict result.

### NET25-10 — Arrival-thread invariance
Repeat NET25-09 with opposite socket callback completion order while preserving assigned intake tick and admitted sequences. Authoritative result is unchanged.

### NET25-11 — Hidden state isolation
Two players occupy overlapping active regions, but only one has LOS/knowledge of a hidden entity. Only that player's projection contains it; authoritative simulation remains single-instance.

### NET25-12 — Separated players
Two players occupy disjoint active regions. Each receives only authorized local/known projection; neither transport causes a player-owned reality bubble.

### NET25-13 — Replaceable-state coalescing
Queue position projections 101, 102, 103, 104 for a stalled client with no intervening causal event requiring intermediate positions. Egress may retain only the latest semantic state while preserving required reliable events.

### NET25-14 — Reliable event cannot be dropped
Saturate egress around an inventory transaction result. Replaceable/disposable data may coalesce/drop, but the reliable transaction result is either delivered in order or the client is disconnected according to backpressure policy; it is never silently discarded.

### NET25-15 — Slow-client boundedness
A client stops consuming. Queue/buffer usage reaches configured bounds but does not grow unbounded; server simulation continues; eventual disconnect does not delete PlayerId/CharacterId.

### NET25-16 — Pre-auth cheapness
Open many unauthenticated connections within admission limits. None receives active-region leases, world projection caches or controlled entities.

### NET25-17 — Graceful disconnect
After an accepted reliable result is queued, request disconnect. The defined already-queued reliable prefix may drain within the finite host-time deadline; new nonessential output is not accumulated.

### NET25-18 — Abrupt disconnect persistence
Drop a socket without drain. PlayerId and CharacterId/world state persist according to Spec 20; transport buffers are discarded.

### NET25-19 — Reconnect
Reconnect the same authorized player through a new ConnectionId. Bind to the existing PlayerId/current CharacterId, issue a fresh current projection, and do not rewind canonical time.

### NET25-20 — No socket state in save
Save/load a world with a connected player. Loaded state restores durable player/world identity and simulation state but no socket, parser offset, transport sequence or byte buffer.

### NET25-21 — UI does not pause
Open a local inventory/targeting UI while another player and AI continue. Canonical time advances; stale confirmation is server-revalidated.

### NET25-22 — Godot object identity rejected
Submit a gameplay request referring only to a client scene-node/list index. Protocol validation rejects it as lacking a stable domain reference.

### NET25-23 — Protocol major mismatch
Client presents incompatible major version. Reject during negotiation before authentication/world allocation.

### NET25-24 — Minor capability negotiation
Client/server minor versions differ but required features are declared compatible. Session proceeds using negotiated capabilities; unsupported optional capability is not used.

### NET25-25 — Hostile request rate
Flood cheap syntactically valid requests. Infrastructure throttles/rejects before expensive gameplay work, within bounded CPU/queue budget.

### NET25-26 — Network RNG isolation
Vary connection IDs, fragmentation, retry timing and transport jitter under the same accepted gameplay sequence. Authoritative RNG stream state and results remain identical.

### NET25-27 — AI/common command path
A player request and an AI decision that select the same movement intent reach the same Cataclysm movement resolution code; only source/admission metadata differs.

### NET25-28 — Projection privacy under reconnect
After reconnect, a fresh projection contains only the player's currently authorized knowledge; stale pre-disconnect hidden data is not re-sent merely because it existed in an old client cache.

### NET25-29 — Save barrier with in-flight network data
At a save boundary, one request is already admitted and another remains only in a transport queue. Persist only authoritative state according to deterministic simulation admission; raw transport queue is excluded.

### NET25-30 — One-player-server equivalence
Run a representative new-game-to-movement flow with exactly one player. The same session/projection/authority code paths used by multiplayer are exercised; no privileged-avatar server shortcut exists.

## 22. Acceptance mapping for #90

- transport/session/simulation/projection layering: sections 2, 4, 12;
- async callbacks never mutate ECS: sections 2, 7 and NET25-08;
- connection/player/entity identity: section 8;
- lifecycle/parser states: sections 5 and 8;
- bounded queues/backpressure: section 10;
- outbound semantic classes: section 6;
- pre-auth resource limits: sections 5 and 8;
- structural parser limits: section 5;
- layered rate limiting: section 11;
- graceful disconnect: section 8;
- reconnect/rebind: section 9;
- deterministic intake/equal-tick ordering: section 7;
- visibility/interest boundary: section 12;
- framing/versioning: section 5;
- initial socket backend: section 5.2;
- observability: section 18;
- lifecycle/network conformance: section 21;
- ModernUO licensing constraint: independent patterns only; no GPL implementation copying.

## 23. Implementation sequencing

1. Define transport-neutral protocol DTOs/envelopes and stable reason/type IDs.
2. Implement authoritative server ingress/egress interfaces and in-process backend.
3. Bind session -> PlayerId -> CharacterId using existing persistence/world contracts.
4. Route one movement request through deterministic command intake and player-specific projection.
5. Add bounded queues/backpressure and lifecycle metrics to in-process conformance tests.
6. Implement the initial TCP backend/framing/parser behind the same interfaces.
7. Run NET25 in-process/loopback equivalence and hostile-input tests.
8. Integrate Godot through Spec 21 without direct ECS access.
9. Resolve the #92 before public internet deployment.

No gameplay/runtime implementation is performed by this specification.

## 24. Post-spec acceptance qualifications

- **Ordering:** NET25-09/10 and AI NET25-27 require the shared decision/scenarios in [#95](https://github.com/LambdaSix/OctoGhast/issues/95), including conflicting PlayerId/CharacterId order, a bounded frozen candidate set and deferred work.
- **Outcome recovery:** §6.3's terminal-outcome promise and §16's result-history wording depend on [#96](https://github.com/LambdaSix/OctoGhast/issues/96). A committed command whose reply was lost cannot be called rejected, nor safely replayed solely because a new session has a fresh transport sequence. Record the uncertainty until the chosen reconciliation contract proves the outcome; do not silently choose a retention/persistence policy here.
- **Determinism:** network transport does not make different physical arrival histories identical. Equivalence tests control canonical admission ticks/order as well as payloads; admission decisions are traceable. No async callback may mutate the world.
- **Projection integration:** consume Spec 26's enter/update/leave/privacy semantics and Spec 27's baseline epoch, per-object revision, sample tick and motion-continuity requirements. Coalescing must not resurrect hidden state or carry interpolation across an obsolete baseline.

**NET25-AUD-01:** issue the same authorized pure query repeatedly through both transports; authoritative world state/time/RNG stay unchanged while bounded infrastructure metrics may change.

These are targeted integration amendments. No Cataclysm move cost, identity separation, queue bound, transport-neutrality or visibility requirement is relaxed.
