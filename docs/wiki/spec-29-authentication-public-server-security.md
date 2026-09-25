# Spec 29 — Authentication, trust and public-server security

Status: architecture/specification complete  
Tracking issue: #92  
Related: #52, #60, #62, #64, #65, #90 / Spec 25, #91 / Spec 28, #96  
Reference architecture: Spec 25 networking/session boundary and Spec 28 server-scoped account identity

## Purpose

This specification defines OctoGhast's authentication and server-security contract for local single-player, LAN/friend servers and public dedicated servers.

It deliberately does **not** make authentication part of Cataclysm gameplay, ECS state or world persistence. Authentication establishes a verified external/server-local identity and binds it to the durable server-scoped `AccountId` defined by Spec 28. Spec 25 then owns the live `ConnectionId` / `SessionId` lifecycle, while world membership and control continue through `PlayerId` and `CharacterId`.

The design must remain fully self-hostable and offline-capable. No OctoGhast-operated identity, matchmaking or account service is required.

External systems and games are architectural references only. Provider-ticket, linked-identity and server-local invitation patterns may inform implementations, but OctoGhast does not copy third-party protocol or GPL implementation code.

## 1. Security invariants

The following are normative:

1. Successful authentication proves/binds identity; it never bypasses authoritative gameplay validation.
2. `ConnectionId`, `SessionId`, provider identity, `AccountId`, `PlayerId` and `CharacterId` are distinct typed identities.
3. Raw credentials, provider tickets, invitation secrets, resumption secrets and private keys are never ECS components, gameplay DTOs or world-save data.
4. Remote authentication and session secrets are never sent over plaintext transport.
5. An external provider continuing to validate a subject does not override server-local bans, revoked capabilities or session revocation.
6. A shared server password, if a deployment adds one as an admission gate, is not an individual player identity.
7. Authentication/provider replacement must not require rewriting world saves or changing an existing `AccountId` when the replacement binding is securely established.
8. Network callback timing and cryptographic randomness do not consume or perturb authoritative simulation RNG.
9. Authentication failure is fail-closed: no world membership, controlled entity, active-region lease or private projection is granted before the required trust/authentication steps succeed.
10. Single-player uses the same logical `SessionId -> AccountId -> PlayerId -> CharacterId` authority chain even though its local synthetic provider does not need remote credentials.

## 2. Canonical identity chain

```text
authentication proof
        |
        v
VerifiedProviderIdentity
(ProviderId, Issuer, Subject)
        |
        v
AuthBindingStore
        |
        v
AccountId                       Spec 28, durable/server-scoped
        |
        +-- WorldId -> PlayerId  Spec 28 + Spec 20
                    |
                    +-- CharacterId(s)

ConnectionId -> SessionId       Spec 25, transient
SessionId -> authenticated AccountId
Session/world selection -> PlayerId
control lease -> CharacterId
```

### 2.1 Verified provider identity

A successful provider verification produces a normalized immutable result containing at least:

- stable `ProviderId`;
- stable issuer/authority identifier;
- provider subject identifier;
- proof/authentication time where supplied;
- proof expiry/freshness information where supplied;
- provider assurance/capability metadata required by policy;
- no raw bearer credential.

The canonical binding key is `(ProviderId, Issuer, Subject)`. Display names, email addresses, platform nicknames and socket addresses are not stable identity keys.

### 2.2 Binding cardinality

- One `AccountId` may have multiple explicitly linked provider identities.
- One provider identity may bind to at most one `AccountId` within one authoritative server data root.
- Binding conflicts reject; they never silently merge accounts.
- Linking requires an authenticated account plus proof of the new identity, or an explicit privileged local/operator recovery path.
- Unlinking must not silently strand an account with no usable authentication method. Operator override may deliberately do so, but must be explicit and audited.
- Provider replacement preserves `AccountId`, account meta state, world memberships and Character ownership.

## 3. Deployment security profiles

### 3.1 Local single-player

Local single-player uses a trusted local/synthetic provider.

Requirements:

- the provider subject is stable within the local server/user-data root;
- it resolves to the normal Spec 28 local `AccountId`;
- the resulting session follows normal authorization, world membership and gameplay validation;
- the in-process transport does not require TLS because no remote trust boundary exists;
- local mode must not add a direct Godot-to-ECS or client-to-`PlayerId` shortcut.

Deleting the local account/profile follows Spec 28. Authentication binding cleanup is part of the account deletion transaction, but world files are not implicitly deleted.

### 3.2 LAN/friend server

LAN/friend operation supports either:

1. a configured external authentication provider; or
2. a server-local invitation/bootstrap flow that establishes a persistent individual server-local identity.

A deployment may additionally use a shared join password as a cheap admission gate. Such a password cannot create or identify an `AccountId` by itself.

Remote traffic carrying authentication/session secrets uses TLS or an equivalent protected channel.

### 3.3 Public dedicated server

Public Internet deployment requires:

- protected transport;
- authenticated individual identity through a production-capable provider conforming to this specification;
- explicit server identity validation;
- bounded pre-auth resources and layered abuse controls;
- server-local account admission/ban/authorization policy;
- security observability and redaction.

Traditional OctoGhast-hosted username/password accounts are **not** an M0/M1 requirement. They may be added later as another provider implementation. A future password provider must separately define password hashing parameters, credential reset/recovery, credential migration, breach/rotation policy and enumeration-resistant failure behavior.

## 4. Authentication-provider boundary

The generic server boundary is conceptually:

```text
IAuthenticationProvider
    Begin(context) -> provider challenge/requirements
    Verify(proof, connection challenge, context)
        -> VerifiedProviderIdentity | AuthFailure

IAuthBindingStore
    Resolve(VerifiedProviderIdentity) -> AccountId?
    Bind(AccountId, VerifiedProviderIdentity)
    Unbind(AccountId, ProviderBindingId)
    EnumerateBindings(AccountId)
```

Exact APIs are implementation choices; the semantic separation is required.

Provider adapters own provider-specific token formats, SDKs, validation endpoints and proof rules. Gameplay systems, ECS systems and the durable account/meta store do not.

Provider verification may be local or remote. A provider outage/time-out must return a bounded authentication failure/defer result and must not grant authority from stale unverified input.

## 5. Server-local invitation/bootstrap identity

To preserve self-hosted/offline friend servers without requiring an external identity provider, OctoGhast supports a server-local invitation bootstrap.

### 5.1 Invitation object

A bootstrap invitation contains or resolves to:

- an opaque random invitation secret generated from cryptographic randomness;
- a non-secret invitation identifier;
- target server identity/fingerprint information;
- issue time and bounded expiry;
- single-use status;
- optional operator-specified account/admission policy metadata;
- protocol/security-format version.

Raw invitation secrets are shown only to the inviter/recipient and are not logged. The server persists only material sufficient to validate the invitation without storing the raw secret where feasible.

Initial bootstrap invitations are single-use. If an operator wants several invitees, create separate invitations rather than making one identity-bootstrap secret multi-use.

### 5.2 Bootstrap flow

```text
operator creates invitation
        |
client receives server address + expected server identity + invite secret
        |
TLS connection validates/pins expected server identity
        |
client presents invite + new persistent server-local identity proof
        |
server validates invite and proof
        |
atomically consume invitation
        |
bind persistent provider subject -> AccountId
        |
create authenticated SessionId
```

The server-local provider should use a persistent client-held cryptographic identity rather than treating the invitation secret itself as the long-term identity. The specific signature/key algorithm is versioned security-provider policy, not a Core gameplay invariant.

Invitation consumption and provider binding are atomic from the externally observable perspective: a crash/failure must not both leave the invitation reusable and partially create the binding.

### 5.3 Invitation replay/failure

- expired, revoked or already-consumed invitations reject;
- replay after successful use cannot create another binding;
- presenting the same invitation concurrently resolves at most one successful consumption;
- a server-identity mismatch aborts before the invitation secret is submitted;
- invalid invitations do not allocate world/player/Character state.

## 6. Transport protection and server identity

### 6.1 Remote secret transport

Provider proofs, bearer tickets, invitation secrets, session/resumption secrets and privileged-authentication material must travel only over TLS or an equivalently strong authenticated encrypted channel.

Certificate/trust failure must not silently downgrade to plaintext or encryption without authenticated server identity.

### 6.2 LAN/friend trust

A LAN/friend invitation may carry or pin the expected server certificate/public-key fingerprint. Explicit trust-on-first-use is also permitted when the user is clearly shown and confirms the server identity.

A later unexpected server-identity change fails closed until explicitly re-trusted or re-invited.

### 6.3 Public-server trust

Public dedicated servers use an explicit configured trust mechanism, such as:

- conventional CA/server-name validation;
- trusted discovery carrying a pinned server identity;
- an operator-managed trust root/pin.

The architecture does not require an OctoGhast-operated CA or discovery service.

## 7. Session establishment and reconnect

### 7.1 Session binding

After authentication:

```text
ConnectionId
   -> authenticated SessionId
   -> AccountId
   -> selected WorldId
   -> PlayerId
   -> optional CharacterId control lease
```

The client never gains authority by supplying a remembered `PlayerId` or `CharacterId`. The server resolves and revalidates bindings from the authenticated `AccountId`.

Provider credentials/tickets are used only where needed for authentication/re-authentication. Ordinary gameplay messages do not repeatedly carry external provider bearer credentials.

### 7.2 Resumption credential

A deployment may issue an OctoGhast resumption credential after full authentication.

The initial contract uses an opaque high-entropy bearer secret whose server-side representation is independently revocable. It is:

- scoped to one authoritative server and one account/session purpose;
- short-lived and never indefinite;
- stored/transmitted as secret material, never logged;
- accepted only over protected transport;
- single-use on successful reconnect and rotated immediately;
- invalid after expiry, explicit revocation, account denial or successful prior consumption.

The server should store a cryptographic digest/verification representation rather than the raw token where practical.

Reusing a successfully consumed token rejects and emits a safe security event. A failed attempt must not disclose whether the account exists beyond the coarse failure required by the protocol.

### 7.3 Reconnect

Successful reconnect creates a new `ConnectionId` and live session attachment. It:

1. authenticates through a valid provider proof or valid resumption credential;
2. resolves the same `AccountId`;
3. revalidates server-local account admission;
4. resolves the existing `PlayerId` for the selected world;
5. revalidates any `CharacterId` control lease;
6. creates a fresh Spec 25 projection baseline;
7. does not rewind canonical simulation time.

Multiple simultaneous sessions for one `AccountId` remain permitted by Spec 28. Each session is independently identifiable and revocable.

Spec #96, not authentication, decides retry/outcome recovery for gameplay requests whose prior authoritative result may be unknown after disconnect.

## 8. Replay and session-hijack resistance

Required defenses include:

- protected transport against passive credential theft;
- connection/server challenges or provider-native freshness checks where the provider supports them;
- provider proof expiry/audience validation where available;
- one-time invitation consumption;
- one-time rotation of successful resumption credentials;
- server-side session revocation;
- no bearer secrets in URLs, logs, telemetry or gameplay state;
- no trust in client-declared prior session/player/character identity without authenticated rebinding.

Authentication/security randomness is cryptographic randomness isolated from deterministic simulation RNG.

## 9. Authorization model

Authentication answers **who is this account?** Authorization answers **what may it do?**

### 9.1 Ordinary gameplay

An authenticated account receives ordinary player capabilities only after server/world admission. All gameplay requests still use Spec 25 and domain authorization/precondition validation.

### 9.2 Moderator/operator/admin capabilities

Privileged authority is explicit server-side policy keyed to `AccountId` or a local host/console principal.

It is not derived automatically from:

- `PlayerId`;
- controlled `CharacterId`;
- display name;
- provider nickname;
- arbitrary provider claims unless the operator explicitly configures such a mapping.

Authorization should be capability-oriented even if the operator-facing configuration groups capabilities into roles.

A local host/console principal may administer the server without controlling a gameplay Character.

Sensitive remote privileged operations are auditable and may require explicit elevation/re-authentication according to server policy.

### 9.3 Revocation domains

The server can independently revoke:

- one live `SessionId`;
- all live/resumable sessions for an `AccountId`;
- one provider binding;
- account admission entirely (ban/deny);
- one or more moderator/operator capabilities;
- an invitation.

Revoking a provider binding does not implicitly delete the account or world state. Deleting the account follows Spec 28 and revokes/detaches all authentication bindings and sessions as part of deletion handling.

## 10. Abuse controls and pre-auth cost

Authentication uses layered bounded controls in addition to Spec 25 transport limits.

At minimum maintain independent bounded budgets for:

- source/network-origin connection/auth attempts;
- claimed provider subject/account identifier where known;
- invitation identifier/use attempts;
- provider-specific expensive verification work;
- server-wide authentication workload.

Cheap structural, source and invitation checks happen before expensive remote-provider calls or cryptographic work where possible.

No single unauthenticated peer may force unbounded:

- password/provider hashing or signature checks;
- outbound provider requests;
- allocations;
- world/account scans;
- active-region activation;
- ECS queries.

Rate-limit and lockout policy must avoid turning one spoofed source into an easy permanent denial of service against an account. Limits therefore combine source, identity and global/provider budgets rather than relying on a single account lockout counter.

## 11. Failure behavior and information disclosure

Authentication failures return stable coarse reason classes appropriate to clients/operations, for example:

- unsupported provider;
- invalid/expired proof;
- invitation invalid/expired/consumed;
- server trust failure;
- authentication temporarily unavailable;
- rate limited;
- account denied;
- provider-binding conflict;
- insufficient privilege.

Remote ordinary clients must not receive enough detail to enumerate whether a claimed account/provider subject exists when that information is unnecessary.

Internal security logs may retain more precise non-secret diagnostic reason codes, subject to access controls.

Authentication failure does not mutate gameplay/world state.

## 12. Security persistence and secret storage

Security state is server-owned infrastructure persistence, not world-save data and not Cataclysm ECS state.

Durable security state may include:

- provider binding records containing non-secret normalized identifiers;
- server-local public-key identity records;
- account admission/ban state;
- moderator/operator authorization assignments;
- server TLS/private identity material in an appropriate protected host store;
- outstanding invitation identifiers, expiry, state and secret-verification digests;
- revocation state/epochs;
- resumable-session verification records until expiry.

Do not persist in world saves:

- raw provider credentials or tickets;
- raw invitation secrets;
- raw resumption secrets;
- TLS private keys;
- provider SDK session objects;
- `ConnectionId` or live `SessionId` transport state.

Security-store and account-store commits are logically separate from world snapshots. A world rollback cannot restore a revoked credential or deleted provider binding.

Host filesystem/resource placement follows Spec 24.

## 13. Privacy, logging and observability

### 13.1 Never log

Normal logs, metrics, traces and crash diagnostics must redact or omit:

- passwords;
- provider bearer/access/refresh tickets;
- invitation secrets;
- resumption secrets;
- private keys;
- full authorization headers or equivalent credential envelopes.

### 13.2 Safe observability

Security observability should expose:

- lifecycle stage and provider ID;
- coarse outcome/reason ID;
- rate-limit bucket hits;
- invitation created/consumed/revoked events without secret;
- provider-binding link/unlink events;
- session/account revocation events;
- privileged capability changes/actions;
- server trust/certificate failure category;
- provider latency/outage metrics without credential payloads.

Use stable internal IDs such as `AccountId` only where operationally necessary and access-controlled; user-visible/client logs should prefer purpose-limited references.

## 14. Versioning, migration and provider replacement

Persisted security records include a schema/version and provider-binding format version.

Migration rules:

- supported old formats migrate deterministically while preserving `AccountId` bindings;
- malformed/unknown security records fail closed or quarantine; do not silently create a replacement account;
- provider adapter removal leaves the account intact but that binding unavailable;
- adding a replacement binding does not alter `AccountId`, world membership or meta-progression;
- cryptographic suite/key migrations are explicit security-store migrations and may require re-authentication/re-enrollment;
- no migration copies raw provider credentials into the durable account/meta store.

## 15. Determinism and RNG

Authentication is infrastructure outside authoritative simulation determinism.

- invitation/session token generation uses a CSPRNG, never gameplay RNG;
- TLS/provider cryptography uses security RNG, never gameplay RNG;
- changing authentication nonces, connection IDs or TLS handshakes does not alter authoritative simulation RNG state;
- once the same authenticated account produces the same canonical admitted gameplay request trace, authentication implementation details do not change gameplay results.

Security records and timestamps used for expiry are based on host/security time policy, not Cataclysm move currency. They must not be exposed as authoritative gameplay chronology.

## 16. Cross-spec contracts

### Spec 25 / #90 — networking and sessions

Spec 25 owns TCP/in-process transport, parser/session lifecycle, bounded queues, deterministic gameplay admission and projection. Spec 29 owns authentication proof, protected remote trust, session/account security binding and auth-specific abuse controls.

Pre-auth promotion now requires successful Spec 29 authentication and server-local authorization.

### Spec 28 / #91 — accounts and meta-progression

Spec 28 owns the durable server-scoped `AccountId`, world memberships and cross-world gameplay/meta state. Spec 29 owns provider-to-account bindings and credentials/security state.

Providers never own Cataclysm unlock history.

### Spec 20 / #85 — world persistence

World saves persist `PlayerId`, Character/world state and canonical simulation data. They do not persist security credentials, sessions or provider bindings.

### #96 — request outcome recovery

Authentication/reconnect can prove which account reattached. It cannot by itself determine whether a previously submitted non-idempotent gameplay command committed. #96 owns that result/deduplication contract.

### Cataclysm profile

Cataclysm gameplay code receives authenticated/authorized player commands through the generic authoritative pipeline. It does not depend on provider SDKs, TLS, invitation types or authentication token formats.

## 17. Conformance scenarios

**AUTH92-01 — local authority path.** Start local single-player with the synthetic provider. Assert session resolves `AccountId -> PlayerId -> CharacterId` through normal server authority and no Godot/client shortcut mutates ECS.

**AUTH92-02 — remote plaintext rejected.** Attempt to send a provider/invitation/session secret over an unprotected production remote transport. Assert authentication does not proceed.

**AUTH92-03 — pinned friend-server identity.** Connect using an invitation containing expected server identity. A matching server proceeds; a mismatch fails before the invitation secret is submitted.

**AUTH92-04 — invitation bootstrap.** Use a valid unexpired invitation with a new server-local persistent identity. Assert one AccountId binding is created and the invitation becomes consumed.

**AUTH92-05 — invitation replay.** Replay a consumed invitation, including concurrently. Assert at most one successful binding/session bootstrap.

**AUTH92-06 — invalid invitation cheapness.** Flood invalid invitation attempts within transport bounds. Assert no world allocation, no ECS query, bounded security work and rate limiting.

**AUTH92-07 — provider binding uniqueness.** Bind provider subject S to account A, then attempt to bind S to account B. Assert conflict; no automatic account merge.

**AUTH92-08 — multiple linked providers.** Authenticated account A explicitly links provider subjects P1 and P2. Either valid provider resolves to the same AccountId and therefore the same existing memberships/meta state.

**AUTH92-09 — provider migration.** Replace/remove P1 while P2 remains. Assert AccountId, PlayerId mappings, Characters and meta-progression are unchanged and no world rewrite occurs.

**AUTH92-10 — provider outage.** Provider verification times out/fails. Assert bounded failure/defer, no authority grant and no corruption/change to the existing account.

**AUTH92-11 — fresh session binding.** Authenticate and join world. Assert a fresh ConnectionId/SessionId binds the server-resolved AccountId and PlayerId; client-supplied alternative PlayerId is rejected.

**AUTH92-12 — resumption rotation.** Reconnect with a valid resumption credential. Assert it is consumed/rotated and the old credential cannot successfully resume again.

**AUTH92-13 — expired/revoked resumption.** Present expired or revoked resumption material. Assert failure with no world/control mutation.

**AUTH92-14 — same-account concurrent sessions.** Establish two allowed sessions for one AccountId controlling distinct owned Characters. Assert separate SessionIds and independent revocation while Spec 28 shared PlayerId admission semantics remain intact.

**AUTH92-15 — revoke one session.** Revoke session S1. S1 loses authority/resumption while S2 for the same AccountId remains valid if policy allows; account/world state persists.

**AUTH92-16 — account denial overrides provider.** Provider still validates the subject but the server-local AccountId is banned/denied. Assert new session/world admission fails.

**AUTH92-17 — provider-binding revocation.** Revoke P1 from account A while P2 remains. P1 can no longer authenticate A; P2 can; account/meta/world state is unchanged.

**AUTH92-18 — admin isolation.** Ordinary authenticated account invokes an operator-only action. Assert authorization failure. Grant the explicit capability and reattempt; assert the privileged audit event is recorded.

**AUTH92-19 — host console independence.** Perform an authorized local host/console administrative action without controlling a gameplay Character. Assert no CharacterId privilege shortcut is required.

**AUTH92-20 — layered brute-force bounds.** Exercise many failed auth attempts from one source, against one claimed subject and across many subjects. Assert source, subject/provider and server-wide/provider-cost budgets bound work before expensive world operations.

**AUTH92-21 — anti-enumeration.** Compare invalid-subject and invalid-proof remote failure surfaces where disclosure is unnecessary. Assert responses do not reveal account existence while internal safe diagnostics remain distinguishable.

**AUTH92-22 — secret redaction.** Generate auth failures, invitation use, reconnect and provider errors. Assert raw credential/ticket/invite/resumption/private-key material appears in none of logs, metrics, traces or diagnostics.

**AUTH92-23 — world rollback isolation.** Revoke a provider binding/session, then restore an older world snapshot. Assert the security revocation remains revoked.

**AUTH92-24 — account deletion.** Delete an account per Spec 28. Assert its sessions and auth bindings are detached/revoked; world files are not implicitly deleted and the old provider subject does not silently claim orphaned world membership through a new account.

**AUTH92-25 — security RNG isolation.** Change TLS handshakes, invitation/token nonces and provider challenge values while keeping the same canonical gameplay admission trace. Assert authoritative simulation RNG state/results are unchanged.

**AUTH92-26 — public pre-auth confinement.** Open authenticated-looking but incomplete public connections. Assert no active-region lease, world snapshot cache, controlled entity or private projection before authentication + authorization + world join succeed.

## 18. Acceptance mapping for #92

- identity/trust model for local, LAN/friend and public dedicated: §§2–3;
- credential/token lifecycle and replay/revocation: §§5–8;
- transport protection/server trust: §6;
- provider identity -> AccountId -> PlayerId -> CharacterId: §§2, 7 and 16;
- admin/player authorization: §9;
- rate limits, failure behavior, privacy/logging: §§10–13;
- reconnect/rebind: §7;
- migration/provider replacement: §14;
- Spec 25/28 integration without duplicated ownership: §16;
- headless security/session scenarios: §17.

## 19. Implementation sequencing

1. Define normalized provider identity and auth failure/reason DTOs outside gameplay namespaces.
2. Define the security store for provider bindings, revocation/admission policy and authorization assignments.
3. Implement the trusted local/synthetic provider and prove AUTH92-01.
4. Add TLS/server-identity handling to the remote transport integration without changing Spec 25 gameplay framing semantics.
5. Implement one server-local invitation/bootstrap provider with persistent individual identity.
6. Implement opaque resumption credentials with expiry, independent revocation and rotate-on-success semantics.
7. Add external-provider adapter support behind `IAuthenticationProvider`; do not make one vendor mandatory.
8. Integrate session -> AccountId -> PlayerId/Character binding with Specs 25/28.
9. Add layered abuse controls, logging/redaction and privileged authorization.
10. Run all AUTH92 conformance scenarios before declaring public Internet deployment supported.

A traditional username/password provider is later optional work, not part of this implementation sequence.

## 20. Resolution

The settled OctoGhast authentication/security model is:

- fully self-hostable/offline-capable with no mandatory central OctoGhast identity service;
- trusted synthetic authentication for local single-player through the normal session/account path;
- external provider or one-time invitation/bootstrap identity for LAN/friend operation;
- mandatory individual authentication and protected transport for public dedicated servers;
- provider identity bound to durable server-scoped `AccountId`, never directly to gameplay authority;
- TLS/equivalent protection for every remote credential/session secret, with invitation pinning/TOFU available for friend servers and explicit trusted server identity for public servers;
- multiple provider bindings and multiple same-account sessions are supported;
- replay-resistant one-time invitations and rotated/revocable resumption credentials;
- server-local bans and capability authorization override provider validity;
- security persistence is separate from world saves and account gameplay/meta state;
- local-password authentication is a future optional provider rather than an M0/M1 architectural dependency.

No runtime implementation is included in this specification.
