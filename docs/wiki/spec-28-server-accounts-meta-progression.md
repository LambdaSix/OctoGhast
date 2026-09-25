# Spec 28 — Server-scoped accounts and cross-world meta-progression

Status: architecture/specification complete  \
Tracking issue: #91  \
Related: #52, #64, #65, #68 / Spec 03, #85 / Spec 20, #90 / Spec 25, #92 / Spec 29  \
Reference rules baseline: `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Purpose

This specification defines the durable **server-scoped account** boundary for OctoGhast: identity and gameplay state that may outlive any one world, especially Cataclysm achievement-backed character-creation unlocks.

It resolves #91 without moving authentication, credentials, sockets, world state or Character state into the account store. The account layer is reusable Core/server infrastructure; Cataclysm meta-progression is a rules-profile consumer of that layer.

ModernUO is used only as architectural evidence that a mature multiplayer server can persist an account independently from its connections and independently from any one character while associating multiple characters with that account. OctoGhast does **not** copy ModernUO code, persistence formats, password/authentication implementation, fixed character-slot counts or UO-specific account fields.

## 1. Evidence and reference behavior

### 1.1 Pinned Cataclysm behavior

Spec 03 established that scenario/profession unlocks are cross-run state, not ordinary current-world Character state. The pinned baseline provides the controlling behavior:

- `scenario::can_pick()` reads `META_PROGRESS` and `past_achievements_info`;
- with `META_PROGRESS=false`, a scenario without `hard_requirement` bypasses its achievement gate;
- with a hard requirement, the required past achievement is still checked even when `META_PROGRESS=false`;
- `profession::can_pick()` applies the same policy and requires all configured achievement requirements;
- completion of the special arcade-mode achievement bypasses these gates;
- the `META_PROGRESS` option describes saved-character achievements from **any world** as the source of eligibility.

Normative pinned anchors:

- `src/scenario.cpp::scenario::can_pick`;
- `src/profession.cpp::profession::can_pick`;
- `src/options.cpp` definition of `META_PROGRESS`;
- `src/past_achievements_info.*` and the achievement/stat-tracker behavior already captured by Spec 03.

OctoGhast reproduces those eligibility semantics in the Cataclysm profile while replacing CDDA's local user-directory ownership with the server-account model below.

### 1.2 ModernUO architectural reference

ModernUO's current account subsystem demonstrates the useful pattern, not a code template:

- `Projects/UOContent/Accounting/Accounts.cs` persists a server account collection;
- `Projects/UOContent/Accounting/Account.cs` persists account-level state and associates an account with multiple character/mobile slots;
- `Projects/Server/IAccount.cs` exposes the account/character relationship separately from network state;
- login binds a connection to a durable account rather than making the connection itself the identity.

The reference is conceptual only. ModernUO is GPL-3.0; OctoGhast must independently implement this contract unless a separate licensing decision explicitly permits otherwise.

## 2. Canonical identity model

The following identities are distinct typed values:

```text
authentication/provider subject        Spec 29, replaceable
        |
        v
AccountId / ServerAccountId            Spec 28, durable and server-scoped
        |
        +-- account meta-progression
        +-- account/world memberships
        +-- future explicitly account-scoped gameplay state
        |
        +---- WorldId + PlayerId        Spec 20, durable world membership
                    |
                    +-- CharacterId(s)  authoritative world entities

ConnectionId -> SessionId              Spec 25, transient
SessionId -> AccountId                 authenticated binding
SessionId + WorldId -> PlayerId        world membership binding
SessionId -> CharacterId?              current control binding
```

### 2.1 Identity invariants

1. `ConnectionId` and `SessionId` are ephemeral and never durable account or world identity.
2. `AccountId` is stable across reconnects and across worlds hosted under the same authoritative server data root.
3. `PlayerId` remains stable **within one world** and is persisted by Spec 20.
4. For the initial model, one pair `(AccountId, WorldId)` maps to at most one durable `PlayerId`.
5. A `PlayerId` may own/control eligibility for multiple `CharacterId` values in that world.
6. Multiple simultaneous sessions for one `AccountId` may bind to the same world-local `PlayerId` and control different owned Characters.
7. Two sessions do not concurrently control the same `CharacterId` unless a future explicit shared-control policy is added. Ordinary control acquisition is an exclusive server-owned lease/binding, not an account-wide session prohibition.
8. `AccountId`, `PlayerId`, `CharacterId`, `SessionId` and `ConnectionId` are never substituted for one another in persistence or DTOs.

Existing Spec 25 per-`PlayerId` ingress fairness therefore applies to the account's world membership. Multiple sessions attached to the same `PlayerId` share that player's bounded admission budget; a client cannot obtain more authoritative ingress simply by opening more sessions.

## 3. Server scope and local single-player

An account belongs to exactly one authoritative OctoGhast **server data root**. It is not a global OctoGhast identity and is not automatically portable to another server.

Local single-player is the same logical model:

- the local server creates/uses a trusted local account through Spec 29's local/synthetic authentication provider;
- all local single-player worlds under that same server/user-data root share the same `AccountId`;
- therefore Cataclysm meta unlocks earned in one local world are visible to character creation in another local world, matching the pinned cross-world intent;
- local mode does not bypass `SessionId -> AccountId -> PlayerId -> CharacterId` authority boundaries.

The user-facing word **profile** may be used for the local account UX, but the domain identity is `AccountId`. This avoids collision with OctoGhast rules/content **profiles**.

## 4. Account record and ownership

The durable account record is server-owned and versioned. A logical record contains at minimum:

```text
AccountRecord
  formatVersion
  AccountId
  revision
  createdAt
  updatedAt
  accountMetaPolicy
  metaProgression
  worldMemberships[]
  extensionData/versioned profile-owned account payloads
```

`AccountId` is an opaque stable runtime identifier with an explicit stable codec. Implementations may use a UUID or another collision-safe opaque representation; its in-memory representation is not protocol/persistence API.

### 4.1 World membership

A durable membership contains:

```text
AccountWorldMembership
  WorldId
  PlayerId
  membershipRevision/status
```

The account store owns the account-to-world-membership binding. The **world save remains authoritative** for:

- the existence and full state of Characters;
- `PlayerId -> CharacterId` ownership/control-eligibility data;
- Character inventory, position, progression and all world-local state.

The account store may maintain derived character locator/index data to populate a character-selection UI, but such an index is non-authoritative and must be revalidated against the world. It must never become a second serialized copy of Character state.

A deleted/missing world or Character may leave a stale derived locator. Stale locators are ignored/cleaned deterministically and never cause a different Character to be retargeted.

## 5. Multiple characters and server policy

Accounts may own multiple characters by default, including multiple characters in the same world.

Core has no ModernUO-style fixed slot count. Server configuration may impose a multiplayer policy such as:

- optional maximum Characters per `AccountId` per `WorldId`;
- optional server-wide account Character cap;
- creation disabled/closed policy independent of existing Character ownership.

An unset limit means no architectural slot cap. Limits are validated by the authoritative server at creation commit time. Client UI may display projected limits/counts but cannot enforce them authoritatively.

A limit change never silently deletes existing Characters. If a new limit is below current ownership, existing Characters remain valid and further creation is rejected until policy permits it.

## 6. Concurrent same-account sessions

There is no one-session-per-account restriction.

A single `AccountId` may authenticate through multiple simultaneous `SessionId` values. In one world those sessions normally share the same `PlayerId`, and each may acquire control of a different owned `CharacterId`.

Rules:

- each request is still authorized against the session's bound `AccountId`, `PlayerId` and current `CharacterId`;
- one session cannot control a Character owned by another account/world membership;
- ordinary control of one Character is exclusive to one active session at a time;
- disconnect releases the transient control binding according to Spec 25/Character policy but does not delete the Character, PlayerId or AccountId;
- another authorized session for the same account may acquire that Character after the previous control binding is released;
- session count/rate limits are Spec 25 infrastructure policy and authentication/security limits are Spec 29 policy, not Character-slot semantics.

## 7. Meta-progression model

Meta-progression is durable account state. It is not:

- ECS component state;
- Character state;
- world-save state;
- socket/session state;
- Godot/client-cache authority;
- authentication-provider credential state.

The generic account layer exposes a typed profile-owned payload/service. For the Cataclysm profile, the minimum authoritative payload records completed achievement IDs and any version/provenance needed to interpret them against the current content generation.

Achievement completion is sourced only from authoritative gameplay/stat-tracker events. Client telemetry or client-supplied unlock flags are never trusted.

### 7.1 Cataclysm eligibility

For scenario eligibility, the Cataclysm profile applies the pinned behavior:

1. if the account has the arcade-mode achievement, allow;
2. if `META_PROGRESS=false` and the scenario is not hard-required, allow without checking the soft requirement;
3. otherwise, if a requirement exists, require the account to have completed it;
4. a hard requirement with no requirement is invalid definition data.

For profession eligibility:

1. if the account has arcade-mode achievement, allow;
2. if `META_PROGRESS=false` and the profession is not hard-required, allow;
3. otherwise require **all** configured achievement requirements;
4. a hard requirement with no requirements is invalid definition data.

This logic is evaluated during authoritative creation-draft/eligibility queries and again when `CreateCharacterIntent` commits, so a stale client eligibility screen cannot bypass changed account state or server policy.

### 7.2 “Disabled meta progression” terminology

For Cataclysm parity, disabling `META_PROGRESS` means **soft gates are disabled**, not that account achievement history ceases to exist. Hard requirements still consult account achievements exactly as the pinned baseline does.

A future generic ruleset may choose no account progression at all, but that is a separate profile policy. A Cataclysm server that intentionally disables all account-achievement tracking must explicitly define what happens to hard-gated content and record that as a non-parity server policy; it must not silently treat `META_PROGRESS=false` as “all hard gates unlocked.”

## 8. Mutation, commit and world-save isolation

Account persistence has an independent transaction/revision boundary from Spec 20 world snapshots.

### 8.1 Account mutation

An account mutation:

1. resolves one `AccountId`;
2. validates the current account revision and profile/content interpretation;
3. applies an idempotent semantic mutation;
4. increments account revision;
5. atomically publishes the complete new account record or leaves the previous record authoritative.

Completed-achievement insertion is set-like/idempotent: processing the same authoritative completion more than once does not duplicate progression or create extra unlock side effects.

### 8.2 World/account ordering

A world event may produce an account mutation, but the world snapshot never embeds the account record.

Before a world save barrier publishes a snapshot whose authoritative state includes account-affecting completion events, all already-issued account persistence mutations from that cut must either:

- have committed durably; or
- cause that save publication to fail/defer with a structured persistence error.

This permits the account store to be ahead of an older world snapshot, which is intentional. It prevents a newly published world snapshot from knowingly claiming durable account-affecting completion while the corresponding account write is still known to have failed.

Loading or rolling back an older world snapshot:

- may roll back that world's `PlayerId`, Character and achievement-tracker state;
- MUST NOT decrease or overwrite the independently committed account meta state;
- MUST NOT duplicate account achievements when the same world event is encountered again.

## 9. Deletion

Account deletion is explicit and server-authoritative.

For the local single-player **profile delete** operation:

- delete the local server account record and its account-scoped meta-progression;
- remove the local authentication binding as defined by Spec 29;
- terminate/detach active sessions for that account;
- do **not** implicitly delete world save files or mutate Characters inside those saves.

Worlds formerly bound to the deleted account therefore retain their world-local state but no longer have a valid live account binding. Rebinding/recovery requires an explicit local recovery/admin/import operation; creating a new account must not silently claim those `PlayerId`/Characters merely because names match.

Dedicated-server account deletion follows the same non-cascading persistence rule unless an operator explicitly requests a separate world/Character administration action.

Deletion must not be implemented as “clear unlocks but keep the same account identity” unless the operation is explicitly a meta-reset rather than account deletion.

## 10. Authentication/provider boundary

[Spec 29](./spec-29-authentication-public-server-security.md) resolves #92 and owns credentials, provider subjects/bindings, invitations, protected transport trust, resumption secrets, authentication-specific abuse controls, account admission/bans and administrator authorization.

Spec 28 owns the durable gameplay account after authentication:

```text
provider proves subject
        -> server resolves authorized AccountId
        -> SessionId binds to AccountId
        -> world join resolves (AccountId, WorldId) -> PlayerId
        -> control request resolves an owned CharacterId
```

Changing an authentication provider must not require migrating Cataclysm unlock history into that provider. The server account is the durable gameplay owner; provider bindings are replaceable security mappings. One AccountId may have multiple explicitly linked Spec 29 provider identities, while any one provider identity binds to at most one AccountId within the server data root.

Spec 29's server-local invitation/bootstrap flow may establish a persistent individual identity without any mandatory central service. That bootstrap still resolves to the same AccountId model here.

A provider subject is not persisted into world entities as ownership identity. Security-store revocation/rollback is independent of world-save rollback.

## 11. Projection and privacy

Clients receive only the account information needed for the active workflow.

Allowed purpose-specific projections include:

- account-local display label/summary where configured;
- available world memberships;
- character-selection summaries derived/revalidated from world state;
- configured Character limits and current count;
- whether Cataclysm soft meta gates are enabled;
- eligibility result/reason for a requested scenario/profession;
- required achievement names/status only where the Cataclysm UI/rules intentionally reveal them.

Do not project by default:

- the entire achievement/history ledger;
- other accounts' memberships or Character lists;
- provider credential/token material;
- server-internal persistence revisions/paths;
- arbitrary account extension data.

Account-private information sent to one session is not thereby world/party-visible. Multiple sessions of the same account may receive the same owner-authorized account summary.

## 12. Versioning, migration and corruption

Each account record carries an explicit `formatVersion` and account `revision`.

Requirements:

- migrations are deterministic, ordered and versioned;
- a newer unsupported format fails closed with a structured compatibility error;
- corrupt/truncated records do not silently become fresh empty accounts under the same provider binding;
- the server may quarantine a bad record and surface recovery/admin diagnostics;
- migration never changes `AccountId` merely because representation changes;
- Cataclysm achievement IDs are interpreted against compatible profile/content-generation metadata where necessary; obsolete/remapped IDs require explicit migration/profile policy rather than silent reinterpretation;
- unknown optional future extension data may only be preserved/skipped when its owning schema explicitly allows that behavior.

Account-store errors are server persistence errors, not client validation failures. Clients get bounded non-sensitive failure results.

## 13. Determinism and test fixtures

Account eligibility is deterministic for fixed:

- Account record/meta set and revision;
- Cataclysm profile/content generation;
- `META_PROGRESS` policy;
- requested scenario/profession definition.

Eligibility consumes no gameplay RNG.

Tests must be able to instantiate an in-memory/fixture `IAccountStore` and fixed `AccountId`/membership/meta payload without requiring a real password/authentication provider. In-process and loopback transport tests use the same semantic account fixture and must produce identical eligibility/creation outcomes.

## 14. Portability and export

Cross-server account/meta export is a supported future evolution seam, **not an M0 requirement**.

Future export/import must be:

- explicit and operator/user initiated;
- versioned;
- bounded and validated before allocation/mutation;
- checked against profile/content/schema compatibility;
- non-authoritative merely because a client supplies a file;
- explicit about whether the target server mints a new local `AccountId`, restores a same-server identity, or merges only allowed meta state.

M0 does not require an export format, account federation, roaming identity or automatic progression sync between servers.

## 15. Core versus Cataclysm ownership

| Area | Generic Core/server | Cataclysm profile |
|---|---|---|
| Durable cross-world identity | `AccountId`, store, membership, revisions | no CDDA filesystem/account schema |
| World membership | `(AccountId, WorldId) -> PlayerId` | uses it for player-owned runs/Characters |
| Characters | world-owned stable entities, many per membership | Cataclysm creation/progression rules |
| Meta payload | versioned profile-owned account extension | completed achievement IDs / unlock interpretation |
| Eligibility query | authoritative pure account/profile query | exact `META_PROGRESS`, hard-requirement and arcade bypass behavior |
| Networking | session/account/player/control bindings | no Cataclysm socket behavior |
| Authentication | replaceable Spec 29 provider/security boundary | no gameplay rule owns credentials |
| Persistence | independent atomic account record | achievement/meta serialization adapter |
| Export | future capability seam | future Cataclysm-compatible account-meta payload if needed |

No Core API may assume fixed UO-style Character slots, CDDA achievement semantics, a global cloud identity, one session per account, or one Character per world.

## 16. Conformance scenarios

**ACC28-01 — identity separation.** Authenticate a session, join a world and control a Character. Assert distinct `ConnectionId`, `SessionId`, `AccountId`, `PlayerId` and `CharacterId`; reconnect changes connection/session IDs only.

**ACC28-02 — local cross-world unlock.** In local single-player account A, complete achievement X in world W1, commit account state, create/open W2 under the same server data root and assert X-backed eligibility uses A's account history.

**ACC28-03 — server isolation.** Create account A on server S1 and an independently named/equivalent account on S2. Unlock X on S1. Assert S2 receives no unlock without explicit future import.

**ACC28-04 — world rollback isolation.** Commit X to account A, then load a W1 snapshot from before X. Assert A still owns X while W1 world-local state rolls back.

**ACC28-05 — multiplayer account isolation.** Accounts A and B share a world. A completes X. Assert B's eligibility remains unchanged and no B projection reveals A's account history.

**ACC28-06 — multiple Characters in one world.** Account A creates Characters C1 and C2 in W. Both remain owned by W's same `PlayerId`; neither creation overwrites the other.

**ACC28-07 — concurrent same-account sessions.** Sessions S1 and S2 authenticate as A, join W, bind to the same `PlayerId`, and control distinct C1/C2 simultaneously. Assert both use normal request validation and share the per-`PlayerId` ingress budget.

**ACC28-08 — duplicate Character control.** S1 controls C1. S2 attempts to control C1 concurrently. Assert the exclusive control binding rejects/defers S2 without forbidding S2 from controlling another owned Character.

**ACC28-09 — configured Character limit.** Set per-world limit 2, create C1/C2, reject C3 atomically. Lower the limit to 1 and assert C1/C2 remain; new creation stays rejected.

**ACC28-10 — Cataclysm soft gate enabled.** With `META_PROGRESS=true`, request a scenario/profession requiring X before and after account X completion; assert rejection then success.

**ACC28-11 — Cataclysm soft gate disabled.** With `META_PROGRESS=false` and a non-hard requirement X, assert eligibility succeeds even without X.

**ACC28-12 — hard requirement while soft gates disabled.** With `META_PROGRESS=false` and `hard_requirement=true`, assert missing X rejects and completed X succeeds. For professions with X+Y, require both.

**ACC28-13 — arcade bypass.** Complete the pinned arcade-mode achievement and assert Cataclysm scenario/profession achievement gates allow according to baseline behavior.

**ACC28-14 — stale eligibility.** Query eligible, then change account/server policy before `CreateCharacterIntent`. Assert commit revalidates and does not trust the stale client result.

**ACC28-15 — account deletion.** Delete a local profile/account. Assert account meta and binding are gone, active sessions detach, world files/Characters are not implicitly deleted, and a newly created account does not auto-claim the orphaned world membership.

**ACC28-16 — migration.** Load an older supported account schema and migrate deterministically without changing `AccountId`, membership or completed achievements.

**ACC28-17 — corrupt account.** Corrupt the durable account record. Assert structured failure/quarantine rather than silently creating an empty replacement that would lose unlocks.

**ACC28-18 — save barrier/account commit ordering.** Complete X, request a world save while the account persistence write is pending, and inject account-store failure. Assert the world snapshot is not published as a successful post-X save until the account mutation has committed or the save reports failure/defer.

**ACC28-19 — transport equivalence.** Run the same account eligibility and creation sequence through in-process and loopback transports with identical account/world fixtures. Assert identical authoritative account/world results and no gameplay RNG difference.

**ACC28-20 — privacy.** Ask for character-creation eligibility and account summary. Assert only purpose-required account/eligibility fields are projected; raw history, other accounts and provider secrets are absent.

**ACC28-21 — provider replacement.** Change the Spec 29 authentication binding/provider for an existing account without changing `AccountId`. Assert the same memberships/meta remain and world saves need no rewrite.

**ACC28-22 — export is not M0.** M0 conformance does not require an export file/API. Any later export/import implementation must pass explicit version/compatibility/trust tests before being considered supported.

## 17. Implementation boundaries

Suggested logical interfaces, without prescribing storage technology:

```text
IAccountStore
  Load(AccountId)
  Create(...)
  Commit(expectedRevision, AccountRecord)
  Delete(AccountId)

IAccountBindingService
  ResolveOrCreateWorldMembership(AccountId, WorldId) -> PlayerId
  AuthorizeCharacter(AccountId, WorldId, PlayerId, CharacterId)

IAccountMetaProgression
  HasCompleted(AccountId, AchievementId)
  RecordCompletion(AccountId, AchievementId)
  QueryCreationEligibility(AccountId, DefinitionId, policy)
```

Implementations may use files, a database or another durable backend, but must preserve the identity, atomicity, migration, rollback-isolation and privacy behavior above. Filesystem paths are deployment policy from Spec 24, not Core API.

## 18. Resolution

This specification resolves #91's architecture decision.

The settled OctoGhast model is:

- **server-scoped accounts**;
- one local meta account shared by local single-player worlds under one server data root;
- account deletion clears that account/meta data without implicitly deleting worlds;
- multiple Characters per account and per world by default, with optional server-side multiplayer limits;
- multiple concurrent sessions for one account are allowed and may control distinct Characters;
- `AccountId` replaces player-identity use of `ProfileId`;
- account meta state is server-owned and independent of world snapshots and authentication providers;
- Cataclysm `META_PROGRESS` and hard requirements retain their pinned behavior;
- cross-server export is a future, post-M0 capability.

No runtime implementation is included in this specification.


## 19. Spec 29 authentication/security follow-through — 2026-09-25

Spec 29 resolves the security half of the account boundary assumed by this specification.

- `AccountId` remains the durable server-scoped gameplay identity; provider subjects are replaceable security bindings.
- Local single-player's synthetic provider maps into the existing local AccountId rather than creating a privileged identity path.
- Friend-server invitation bootstrap may create/bind a persistent server-local provider identity, but the resulting account/world/meta ownership remains entirely Spec 28 state.
- External provider replacement, session revocation or security-store rollback rules never move Cataclysm meta-progression into the provider.
- Server-local account denial can reject login even if an external provider still validates the subject.
- Account deletion detaches/revokes authentication bindings and sessions while preserving the existing rule that world files are not implicitly deleted.

ACC28-21 should be run with AUTH92-08/09/17, and ACC28-15 with AUTH92-24, so account identity and security binding lifecycle are proven together without duplicating ownership.
