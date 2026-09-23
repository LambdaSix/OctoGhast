# Prospective design: production networking core

Status: **prospective architecture design**
Related programme issues: #52, #60, #62, #64, #65
Primary external references: `LambdaSix/Ghast` and `modernuo/ModernUO`

## Purpose

OctoGhast is moving toward a continuously progressing, authoritative server simulation in which single-player is a one-player server instance and cooperative multiplayer uses the same logical client/server contract over a different transport backend.

This page records prospective networking-core requirements and lessons from two complementary reference implementations:

- **Ghast** is the architectural reference for a transport-neutral client/server boundary, headless authoritative simulation, canonical fixed-step time, explicit projections, and keeping Godot presentation outside simulation ownership.
- **ModernUO** is the operational reference for hardening a long-running C# multiplayer server under real network pressure: connection lifecycle state, asynchronous I/O handoff, bounded buffers, backpressure, parser guards, throttling, graceful disconnects, cheap pre-auth connections, and networking lifecycle tests.

These are design references, not code-reuse requirements. OctoGhast should independently implement the concepts appropriate to its protocol, deterministic ECS model, and licensing constraints.

## Core architectural rule

Network I/O may be asynchronous. **Authoritative world mutation is not.**

Socket completions, callbacks or worker threads must never mutate ECS/world state directly. Network work is decoded and validated into bounded per-connection queues, then consumed at a deterministic simulation boundary.

```text
OS/socket completion
        |
transport receives bytes
        |
frame decode + structural validation
        |
bounded per-connection inbound queue
        |
======== canonical simulation boundary ========
        |
session/player validation
        |
request -> intent/command
        |
authoritative ECS systems
        |
world mutation
```

This keeps network arrival timing from becoming an uncontrolled source of simulation ordering.

## Single-player and multiplayer topology

The simulation must expose one logical protocol regardless of transport.

```text
Single-player

Godot 2D client
      |
IClientTransport
      |
in-process transport
      |
authoritative GameServer
      |
ECS/world


Co-op

Godot client -- socket --+
Godot client -- socket --+-- authoritative GameServer
Godot client -- socket --+          |
                              ECS/world
```

The architectural test is:

> Replacing the in-process transport with a loopback/network transport must not require Cataclysm gameplay-system changes.

The in-process backend may bypass byte serialization internally, but it must preserve the same logical request/response contracts, ordering rules, validation semantics and authority boundary.

## Layering

### Transport layer

Owns:

- accept/connect/disconnect;
- socket lifecycle;
- framing;
- serialization/deserialization;
- receive/send buffers;
- backpressure;
- network-level rate limiting;
- protocol-version negotiation;
- bounded queues;
- transport errors;
- metrics.

Does **not** own:

- player identity persistence;
- controlled-entity identity;
- game rules;
- ECS mutation;
- FOV/visibility semantics;
- action-cost resolution.

### Session layer

Owns the mapping between a live connection and stable game identity.

Keep these identities distinct:

1. **Connection identity** — ephemeral socket/session instance.
2. **Player identity** — stable authenticated/persisted player identity.
3. **Controlled entity** — current authoritative world entity controlled by that player.

Disconnecting a socket must not imply deleting the player or character.

### Simulation layer

Consumes validated requests at deterministic server boundaries and converts them to commands/intents. Existing command/activity/system contracts remain authoritative.

### Projection layer

Builds client-visible state and events from authoritative state according to visibility, knowledge, ownership and interest-management rules.

The networking layer must not expose arbitrary ECS components merely because they exist.

## Explicit connection state machine

Use an explicit lifecycle rather than boolean combinations:

```text
Accepted
  -> Negotiating
  -> Authenticating
  -> JoiningWorld
  -> Active
  -> Disconnecting
  -> Closed
```

An error state/reason should be retained for diagnostics.

Parser/framing state should be separate from protocol/session state so partial frames, throttling and malformed input are handled without overloading gameplay lifecycle state.

## Pre-auth resource discipline

Unauthenticated connections must be intentionally cheap.

Before authentication/world join, a connection should not receive:

- a full world projection;
- an active-region subscription;
- control of a world entity;
- large per-client caches;
- unnecessarily large send/receive budgets.

The implementation should allow small initial buffers/resource budgets to be promoted after authentication/world entry.

This follows ModernUO's useful production pattern of preventing connection floods from consuming the same per-client resources as authenticated players.

## Framing and parser safety

Every network frame must be structurally bounded before expensive allocation or game lookup.

At minimum define limits for:

- maximum frame size;
- maximum string length;
- maximum collection element count;
- maximum nesting/decompressed size where applicable;
- maximum queued requests per connection;
- legal message type for the current connection state;
- protocol-version compatibility.

Malformed, impossible or oversized frames must fail deterministically and must not leave the parser permanently waiting for data that can never fit its receive budget.

Never trust a client-provided count/length as an allocation size without protocol-level bounds.

## Request handling

Inbound wire messages should terminate at validated request DTOs.

Preferred path:

```text
wire frame
  -> frame parser
  -> typed request DTO
  -> session validation
  -> bounded request queue
  -> simulation-step intake
  -> command/intent
  -> ECS systems
```

Avoid packet handlers that directly mutate entity state.

Typical request types may include:

- `MoveRequest`;
- `InventoryTransferRequest`;
- `ActivityStartRequest`;
- `ActivityCancelRequest`;
- interaction/targeting requests;
- chat/social messages.

## Backpressure

A slow or stalled client must have bounded impact on server memory and simulation throughput.

Each active connection should have explicit budgets for:

- inbound queued bytes/messages;
- outbound queued bytes/messages;
- request rate;
- snapshot/projection production.

Exhaustion policy must be explicit. Depending on message class, the server may:

- coalesce/replace stale state;
- drop explicitly disposable presentation updates;
- defer generation;
- reject new requests;
- disconnect a persistently non-consuming client.

Unbounded queue growth is not acceptable.

## Outbound semantic classes

Do not treat every outbound update as an undifferentiated reliable byte stream at the application layer.

At least distinguish:

### Reliable ordered facts/events

Examples:

- inventory transaction result;
- activity completion/cancellation;
- death;
- important messages;
- authentication/world-join responses.

These must not be silently replaced by later state.

### Replaceable current state

Examples:

- current transform/tile;
- current HP/stamina values;
- current weather/display state;
- current visible-entity presentation state.

If an older unsent state is superseded, it may often be coalesced into the newest state.

### Disposable presentation hints

Some visual/audio hints may be droppable if a client is badly behind, provided dropping them cannot alter gameplay understanding or authoritative state.

The exact classification is a later protocol-design decision, but the networking core must support bounded policies rather than assuming all bytes are equally valuable.

## Rate limiting and abuse resistance

Apply limits in layers:

```text
socket accept
  -> cheap IP/connection-rate gate
  -> protocol/handshake limits
  -> authentication limits
  -> active-session message-rate limits
  -> gameplay validation
```

The earliest layers must be non-blocking and inexpensive. Expensive work should not occur simply because a peer can open sockets quickly.

Gameplay-specific validation remains authoritative even after rate limiting; throttling never makes a client request trusted.

## Send-buffer/resource growth

If buffers can grow, growth must be bounded by both:

- a per-connection maximum; and
- a server/global resource budget.

The implementation should be able to refuse growth and apply a clear overload policy rather than risking process-wide memory exhaustion.

Detailed buffer sizes are implementation/profiling decisions and should not be copied from ModernUO.

## Graceful disconnect semantics

Disconnect is a lifecycle, not an instantaneous boolean.

A prospective sequence:

```text
Active
  -> DisconnectRequested
  -> stop accepting new gameplay requests
  -> optionally flush already-accepted reliable output
  -> detach connection from player session
  -> close socket
  -> Closed
```

Define whether already-queued reliable messages drain before close and apply a finite drain timeout.

New output submitted after disconnect handoff should not silently grow buffers.

Connection teardown must not by itself remove persisted player identity or character/world state.

## Reconnect

Reconnect policy belongs above transport.

A reconnecting player should authenticate to a stable player identity, then rebind to the appropriate persisted/authoritative controlled entity according to server policy.

Transient socket state, sequence counters that are transport-local, buffers and presentation interpolation state are not world-save data.

## Deterministic networking boundary

The networking core must support the authoritative-tick contract from #52/#57:

- I/O completion timing may vary;
- decoded requests are queued;
- queue intake into simulation occurs at a defined server phase;
- equal-tick request ordering has a deterministic rule;
- no asynchronous callback may perform ECS/world mutation;
- network thread count/platform must not alter game-rule results for the same accepted input sequence.

This does not require packet arrival itself to be deterministic; it requires the simulation's consumption rules to be explicit and reproducible.

## Visibility and interest management

Projection is security/authority-sensitive, not merely a rendering optimization.

A client should receive only state it is entitled to know according to:

- FOV/LOS;
- remembered-map/knowledge state;
- inventory ownership/access;
- party/shared information policy;
- active-region/interest management;
- global information deliberately exposed by the game.

Multiple players may occupy separated or overlapping active regions. Overlap must not cause duplicated authoritative simulation.

## Serialization strategy

ModernUO demonstrates the value of allocation-conscious span-oriented binary parsing/writing, but OctoGhast should not prematurely reproduce its packet implementation.

Initial requirements:

- protocol is strongly typed;
- serializer is replaceable behind the transport/protocol boundary;
- decoding is bounded;
- avoid unnecessary copy/allocation chains;
- support pooled buffers where profiling justifies them;
- protocol versioning is explicit.

Hand-written span codecs, source-generated serialization or another binary format can be evaluated later.

## Socket/backend strategy

Do not make ModernUO's `IORingGroup` or Ghast's current socket choices architectural requirements.

M0/M1 should prefer a straightforward, testable .NET transport implementation behind:

- `IClientTransport`;
- `IServerTransport`;
- `IConnection` / equivalent.

A future high-performance polling backend must be replaceable without changing simulation code.

## Observability

Expose enough networking telemetry to diagnose production behaviour:

- active/pre-auth connections;
- bytes/messages in/out;
- queue depth/high-water marks;
- dropped/coalesced updates;
- parser/protocol errors;
- throttling/rate-limit hits;
- send-buffer growth/refusal;
- disconnect reason;
- connection/session age;
- simulation intake latency where useful.

Packet payload logging must be opt-in and must account for credentials/private data.

## Test strategy

Networking tests should be treated as architecture conformance tests.

Minimum scenarios:

1. fragmented frame is reassembled correctly;
2. multiple frames in one receive are all consumed;
3. malformed/oversized declared frame is rejected without unbounded allocation;
4. incomplete frame waits for more bytes when it can legally fit;
5. request received asynchronously does not mutate ECS until simulation intake;
6. requests consumed in the same simulation tick use deterministic ordering;
7. pre-auth connection cannot allocate/promote full active-player resources;
8. slow client hits bounded outbound/backpressure policy;
9. replaceable state coalesces without losing required reliable events;
10. graceful disconnect drains the defined reliable prefix and rejects later sends;
11. disconnect/reconnect rebinds stable player identity without persisting socket state;
12. in-process and loopback-network transports produce equivalent authoritative movement outcomes;
13. hidden/out-of-interest entities are not projected;
14. transport/platform timing differences do not alter deterministic results for the same accepted request sequence;
15. hostile request rates are bounded before expensive gameplay processing.

## Relationship to Ghast and ModernUO

### Adopt from Ghast as architectural direction

- server-authoritative headless simulation;
- fixed/canonical simulation progression;
- transport abstraction;
- one-player server for single-player;
- explicit projection/client boundary;
- Godot as presentation/input rather than world owner.

### Learn from ModernUO as operational hardening

- async I/O handed back to a controlled game-loop phase;
- explicit connection/protocol/parser lifecycle;
- bounded buffers and growth budgets;
- backpressure;
- partial-frame and malformed-frame guards;
- cheap pre-auth connections with later resource promotion;
- layered throttling/rate limiting;
- graceful disconnect/drain semantics;
- extensive network lifecycle tests;
- allocation-aware serialization patterns where profiling justifies them.

### Do not copy by default

- UO packet IDs/protocol compatibility structure;
- direct packet-handler-to-game-object mutation;
- specific ModernUO buffer sizes;
- `IORingGroup` as an immediate requirement;
- Ghast's exact tick rate/socket/serializer implementation.

## Licensing note

ModernUO is GPL-3.0. This document records independently implementable architectural lessons and patterns. Direct copying of implementation code should not occur without an explicit licensing decision compatible with OctoGhast.

## Open design questions

These require focused follow-up before the first production socket backend is considered complete:

1. Which transport(s) should M1 target: TCP, QUIC, ENet-like reliable UDP, or another option?
2. What delivery/reliability classes should the OctoGhast application protocol expose?
3. What is the exact equal-tick ordering rule for requests from multiple connections?
4. What authentication model is needed for LAN/friend co-op versus public dedicated servers?
5. What are initial and maximum queue/buffer budgets?
6. Which projection messages are safely coalescible?
7. What disconnect/reconnect grace policy applies to controlled characters?
8. How is protocol compatibility/version migration handled across server/client builds?
9. Which metrics and tracing are required before internet-facing testing?

## Prospective acceptance criteria for networking-core implementation

A future implementation should not be considered architecturally complete until:

- Cataclysm systems have no socket dependencies;
- in-process and socket transports satisfy the same logical contract;
- all inbound work crosses a deterministic simulation intake boundary;
- world mutation never occurs from network callbacks;
- client/session/player/entity identities are separate;
- buffers/queues are bounded and have tested overload behaviour;
- malformed input cannot trigger unbounded allocation;
- single-player still uses the server authority boundary;
- loopback co-op proves multiple simultaneous clients;
- visibility/interest projection prevents arbitrary ECS leakage;
- disconnect/reconnect semantics are deterministic and persistence-safe;
- core networking lifecycle/backpressure tests pass headlessly.
