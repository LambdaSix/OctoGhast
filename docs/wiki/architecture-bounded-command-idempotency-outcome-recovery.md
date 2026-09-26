# Architecture — bounded command idempotency and outcome recovery

## Status

**Adopted 2026-09-26.** This page is the normative resolution of [#96](https://github.com/LambdaSix/OctoGhast/issues/96).

It resolves the post-spec gap between:

- transport/session retry and reconnect behaviour;
- deterministic authoritative command admission/execution;
- persistence/save/restart semantics;
- client-visible terminal outcomes for non-idempotent operations.

It does **not** reopen completed pinned-CDDA investigations. CDDA supplies feature rules, costs, RNG behaviour and state transitions. This page defines an OctoGhast Core/server adaptation required by authoritative real-time/co-op operation.

## 1. Decision

OctoGhast adopts a **hybrid command retry model**.

Every gameplay-affecting command type MUST explicitly declare one retry/outcome policy. There is no silent default:

1. **StateReconciled**
   - for high-frequency or replaceable intents such as movement/steering/input-like requests;
   - ambiguous delivery is resolved from fresh authoritative projection/state;
   - the client MUST NOT blindly replay an old ambiguous request as though it were guaranteed not to have committed.

2. **IntrinsicIdempotent**
   - for operations whose domain identity/preconditions make legitimate repetition observably harmless under the owning feature contract;
   - the feature specification MUST state why repetition is safe;
   - no generic durable result ledger is required merely because a correlation ID exists.

3. **DurableOutcome**
   - for client-requested operations that are non-idempotent, irreversible, resource-consuming, RNG-bearing, or otherwise unsafe to repeat after a lost response;
   - retry/result-query across reconnect and committed save/restart MUST associate with the original terminal semantic outcome and MUST NOT repeat authoritative mutation, action cost, resource consumption or RNG;
   - character creation, consequential transaction-style inventory/trade/crafting operations, consequential dialogue choices, and similar “execute at most once for this logical operation” actions are expected consumers where their owning feature chooses reconnect/restart-safe retry.

Command registration/spec validation MUST fail when a gameplay-affecting command has no declared policy.

This is **bounded idempotency**, not unlimited exactly-once delivery.

## 2. Identity model

### 2.1 Operation identity is distinct

A logical durable operation key is conceptually:

```text
OperationKey
  WorldId
  WorldHistoryEpoch
  PlayerId
  OperationGeneration
  OperationId
```

Where:

- `WorldId` is the durable world identity from Spec 20.
- `WorldHistoryEpoch` identifies one accepted authoritative history/branch of that world.
- `PlayerId` is the durable world-local player identity.
- `OperationGeneration` is a server-issued bounded submission generation for that player/world/history.
- `OperationId` is an opaque client-generated high-entropy identifier unique within the current generation.

The operation namespace MUST NOT use or alias:

- `ConnectionId`;
- `SessionId`;
- transport message sequence;
- `CharacterId`;
- `AccountId`;
- projection revision/baseline;
- Godot/client object identity.

Those identities still participate in authentication, authorization, control binding and command payload validation, but they do not define operation deduplication identity.

### 2.2 Same-account sessions

Multiple simultaneous sessions bound to one `AccountId` and the same world-local `PlayerId` share that `PlayerId`'s operation namespace and bounded operation-generation budget. Opening additional sessions cannot create independent idempotency/fairness namespaces.

### 2.3 Character replacement

Changing the currently controlled `CharacterId` does not change historical operation ownership. The actor/target remains part of the canonical request payload/fingerprint. Reusing an old operation key with a changed actor is therefore either an exact retry of the original payload or a payload conflict; it cannot silently become a fresh action by the replacement Character.

## 3. Canonical payload fingerprint

Every `DurableOutcome` submission is associated with a canonical semantic fingerprint containing at least:

- command/operation type ID;
- command schema/semantic version;
- stable domain references and semantically relevant request parameters;
- controlled/acting Character identity where the command is actor-scoped;
- profile-owned parameters that determine the intended attempt.

The fingerprint is derived from canonical semantic data, **not serialized packet bytes**. Serializer choice, field order, transport framing and in-process versus socket transport MUST NOT change it.

Reusing one `OperationKey` with a different canonical payload MUST produce a terminal `OperationIdConflict` (or equivalently specific named error) and MUST NOT execute either payload as a fresh operation.

## 4. Lifecycle and outcome states

Transport receipt and gameplay success are separate concepts.

A logical operation may move through:

```text
Received
  -> Validated/Reserved
  -> Admitted
  -> Queued
  -> Executing
  -> Committed | Rejected
  -> OutcomeAvailable
  -> zero or more delivery attempts
```

Normative rules:

- **Received** means only that the server parsed/accepted the message envelope.
- **Admitted** means the request entered the authoritative bounded admission/execution pipeline; it is not a promise of gameplay success.
- A deterministic gameplay rejection after admission is a terminal outcome for that operation attempt.
- Retrying a terminal rejected durable operation returns/associates with the same rejection; later world changes do not turn that retry into a new attempt.
- Result delivery/acknowledgement is not authoritative mutation state.
- Transport loss after commit does not undo the committed operation.
- Queries and projection refresh do not create an additional gameplay attempt or RNG draw.

The #95 canonical admission/execution architecture remains authoritative for ordering. Operation identity does not reorder work and does not replace the persisted origin/admission keys required by #95.

## 5. Bounded generations and retention

### 5.1 Generation states

Each world/player/history has one **current** `OperationGeneration` accepting previously unseen durable IDs.

Older generations transition to **closed/retry-only**, then eventually **expired**.

```text
Current
  - known ID: return/associate with existing state/outcome
  - unknown ID: may admit as a new durable operation

Closed / retry-only
  - known retained ID: return/associate with existing state/outcome
  - unknown ID: HistoryExpired / UnknownHistoricalOperation
  - NEVER execute as fresh

Expired
  - any ID from this generation: HistoryExpired
  - NEVER execute as fresh
```

This is the key boundedness invariant: deleting an old result MUST NOT make an old key eligible to execute again.

### 5.2 Rotation

Generation rotation occurs according to bounded configured limits such as:

- maximum operation records;
- maximum durable bytes;
- explicit maintenance/checkpoint policy;
- optional deployment retention age in addition to count/byte limits.

Correctness MUST NOT depend solely on wall-clock TTL. The authoritative server may be paused/offline and OctoGhast remains self-hostable.

Configuration may choose concrete limits, but compiled/server policy MUST always impose finite upper bounds.

### 5.3 Hostile duplicate/query load

Known duplicate lookup, unknown closed-generation rejection and result queries MUST have bounded CPU, memory and disk impact. Authentication/rate limits from Specs 25/29 continue to apply. A client cannot force unbounded retention by repeatedly touching old records.

## 6. Restart and crash recovery

### 6.1 Clean committed save/restart

A committed world snapshot containing a `DurableOutcome` effect MUST contain the corresponding durable operation outcome metadata in the same logical snapshot history.

After restart from the latest committed snapshot:

- known retained operation key -> return the stored semantic outcome;
- no extra mutation, cost, resource consumption or RNG;
- current submission generation is **rotated** before accepting new durable operations.

### 6.2 Crash after in-memory commit but before save

An authoritative operation can commit in memory, lose its response, then the server can fail before that operation appears in the latest committed world snapshot.

After loading the older committed snapshot, the pre-crash submission generation is no longer current. Therefore:

- known operation records that survived the snapshot remain queryable/retryable;
- an unknown ID from the prior generation is **indeterminate historical work** and MUST NOT automatically execute;
- the server returns a terminal recovery classification such as `IndeterminateAfterRestart` / `HistoryExpired` appropriate to what it can prove;
- the client reconciles from authoritative state and obtains fresh user intent before submitting a new operation in the new generation.

This avoids pretending that a snapshot can prove whether a missing post-snapshot operation committed before a crash.

## 7. World rollback and branching

Spec 20 snapshots identify durable world history. Deliberately loading an older snapshot or explicitly branching history creates a new `WorldHistoryEpoch` before gameplay submissions resume.

Consequences:

- operation keys from the abandoned/later history do not apply to the newly selected history;
- retry/result-query using an old epoch returns `HistoryMismatch` (or equivalent) and MUST NOT mutate the restored world;
- a normal restart of the latest committed history does **not** create a new history epoch;
- administrative repair/branch tools must make epoch changes explicit and durable.

This prevents automatic client retry from silently replaying a command into a deliberately rolled-back world.

## 8. Persistence schema and ownership

The durable operation ledger is generic Core/server **world persistence**, adjacent to authoritative command/admission and snapshot coordination. It is not:

- an ECS component on Player/Character entities;
- socket/session/parser state;
- a client projection cache;
- account/provider credential state;
- raw serialized request/response packets.

A logical durable record contains at least:

```text
DurableOperationRecord
  formatVersion
  OperationKey
  commandType
  commandSemanticVersion
  canonicalPayloadDigest
  lifecycle/terminalState
  authoritative admission/origin metadata where needed
  commit canonical coordinate / stable domain refs where needed
  semantic outcome
```

Implementation may compact records or store outcome-specific data by typed schema. Raw transport DTO bytes are not the persistence contract.

### 8.1 Atomic linkage

Within a world save history, committed gameplay effects and their durable dedup/outcome record MUST be atomically linked by Spec 20's snapshot transaction:

- previous committed snapshot: contains neither the new durable effect nor its new outcome record;
- new committed snapshot: contains both;
- interrupted save: loads the previous valid manifest and therefore neither appears as durably committed in that history.

A save barrier MUST NOT split a synchronous atomic command between gameplay commit and durable outcome-record creation.

### 8.2 Account-store operations

If a future durable operation mutates only the independent Spec 28 account store, its operation record belongs to that account-store transaction.

No generic cross-world/account distributed transaction is introduced by this decision. A future operation that truly must atomically mutate both account and world stores requires an explicit cross-store protocol/specification.

## 9. Projection, authorization and privacy

Persist **semantic outcomes**, not previously rendered client DTOs.

On retry/result query:

1. authenticate/authorize the current session;
2. resolve `AccountId -> PlayerId` and current control rights;
3. resolve the durable operation by `OperationKey`;
4. re-project only the portions of the semantic outcome authorized for the current viewer/context.

A current session may legitimately learn only:

- `KnownSuccess`;
- `KnownRejected`;
- `Indeterminate`;
- `HistoryExpired`;
- `HistoryMismatch`;

without receiving hidden domain details no longer visible/authorized.

Changing character control, visibility, party membership or other projection context cannot disclose stale hidden inventory/NPC/world data merely because a prior session once received it.

## 10. UI/client contract

Clients distinguish at least:

- **Pending** — operation is known submitted but no terminal outcome is currently known to the client.
- **KnownSuccess** — authoritative terminal success is known.
- **KnownRejected** — authoritative terminal rejection/failure is known.
- **Indeterminate** — the server cannot prove an old ambiguous attempt's terminal outcome after the applicable recovery/history cut.
- **HistoryExpired / HistoryMismatch** — the supplied historical key is outside the accepted retry history.

For `DurableOutcome`, clients MAY retry/query the same operation key while the server declares it recoverable.

For `StateReconciled` or `Indeterminate`, clients MUST NOT silently synthesize a new operation as though it were the same attempt. They first reconcile authoritative state; any replacement attempt uses a fresh current-generation operation ID and represents fresh user intent/policy.

## 11. RNG, cost and feature semantics

Feature specifications retain their Cataclysm costs, state changes and RNG semantics.

For one logical `DurableOutcome` operation:

- duplicate receipt before admission is not an extra attempt;
- duplicate while queued/executing associates with the same operation;
- duplicate after terminal outcome returns that outcome;
- retry after reconnect/save/restart does not consume another cost/resource/RNG draw;
- changed-payload reuse is rejected before gameplay execution;
- projection/result queries never reroll gameplay.

No generic operation-ledger bookkeeping consumes authoritative gameplay RNG.

## 12. In-process/socket equivalence

The same logical operation-policy, identity, fingerprint, duplicate, retention and recovery rules apply through:

- in-process single-player transport;
- loopback/network cooperative transport.

In-process calls MUST NOT bypass operation classification/deduplication merely because no bytes were serialized.

## 13. Required conformance scenarios

### OP96-01 lost response after commit
Commit a `DurableOutcome` inventory/craft-style operation; disconnect before response; reconnect and retry. Assert exactly one authoritative effect/cost/RNG attempt and recovery of the same terminal outcome.

### OP96-02 committed save/restart
Commit and save the operation/outcome; restart from the committed snapshot; retry. Assert the original outcome and no mutation/RNG.

### OP96-03 crash before save
Commit in memory, lose response, crash before a save containing the operation. Restart the older snapshot; retry the old-generation unknown ID. Assert indeterminate/expired historical response and no automatic execution.

### OP96-04 duplicate phases
Send duplicates before admission, while queued, while executing and after commit. Assert one logical operation/effect.

### OP96-05 admitted rejection remains rejection
Lose a deterministic contention/stale rejection response; later change the world so the command could now succeed; retry the same operation key. Assert the original rejection, not a new attempt.

### OP96-06 changed payload
Reuse one operation key with a semantically different actor/target/quantity/choice. Assert `OperationIdConflict`, no mutation and no RNG.

### OP96-07 generation rollover
Rotate retention generation. Known retained old ID returns prior outcome; unknown ID in the closed generation is rejected and never executed; unseen ID in current generation can be admitted.

### OP96-08 two players same local ID
Two different `PlayerId` values use the same `OperationId` in their own current generation. Assert no collision.

### OP96-09 same account multiple sessions
Two sessions for the same account/world `PlayerId` submit the same durable key. Assert one operation and shared result.

### OP96-10 replacement character
Retry a historical operation after the same PlayerId controls another Character. Assert exact-payload retry semantics or payload conflict; never execute it as a new action by the replacement Character.

### OP96-11 older snapshot / branch
Commit an operation in history A, deliberately restore an older snapshot into history epoch B, then retry the A key. Assert `HistoryMismatch`, no mutation and no RNG.

### OP96-12 privacy
Resolve a prior operation from a changed control/visibility context. Assert only currently authorized semantic result fields are projected.

### OP96-13 serializer/transport equivalence
Submit semantically identical operation through in-process and loopback transports/serializer representations. Assert canonical fingerprint/behaviour is transport-independent.

### OP96-14 hostile duplicate/result-query load
Flood known duplicates, closed-generation unknown IDs and result queries. Assert bounded memory/disk/CPU, rate limiting and no retention extension by touch.

### OP96-15 save barrier atomicity
Fault-inject before/after world mutation, operation-record creation, unit flush and manifest commit. Assert every loadable snapshot contains either both effect+outcome or neither for that durable history.

## 14. Amendments consumed by existing specs

- Spec 04: command retry policy declaration; `OperationId` distinct from activity identity.
- Spec 06: inventory retry guarantee is conditional on `DurableOutcome`, with this page owning bounded retention/restart rules.
- Spec 07: consequential craft/disassembly attempts must declare retry policy; durable retries never grant extra RNG/resource attempts.
- Spec 11: consequential dialogue/mission choices use durable outcome semantics where reconnect/restart-safe no-reroll is promised.
- Spec 20: operation ledger/history epoch/generation become durable Core world state while sockets/sessions/acks remain excluded.
- Spec 21: UI terminal/indeterminate/history states and fresh-intent rule.
- Spec 23: fault-injection/restart/rollback/hostile duplicate conformance matrix.
- Spec 25: transport correlation ID is distinct from `OperationKey`; terminal result/retry guarantees consume this architecture.
- #90 stays historically specification-complete; #96 owns this later cross-cutting integration decision.
- #64/#65 readiness may treat reconnect/restart-safe non-idempotent retry as architecturally specified once all amendments below are published.

## 15. Non-goals

This decision does not:

- promise infinite exactly-once delivery;
- persist every movement/input packet;
- make transport ACKs part of world state;
- introduce a global account service;
- put account/provider/session state into ECS;
- make all commands durable transactions;
- require a database, WAL or specific serializer;
- require cross-store distributed transactions;
- change Cataclysm feature costs/RNG/state-transition rules;
- implement runtime code.

