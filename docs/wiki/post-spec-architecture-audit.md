# Post-spec architecture audit of the #65 corpus

Audit date: 2026-09-24. Status: **conditionally ready to begin implementation; shared integration decisions remain open**.

### Subsequent architecture resolutions

This audit remains a record of the 2026-09-24 review state. Subsequent authoritative contracts qualify its open-gate language:

- #95 was resolved on 2026-09-25 by [the canonical ordering/admission/activation architecture](./architecture-canonical-ordering-admission-activation.md).
- #91 was resolved on 2026-09-25 by [Spec 28 — Server-scoped accounts and cross-world meta-progression](./spec-28-server-accounts-meta-progression.md). Cross-world player gameplay identity is now `AccountId`, not `ProfileId`; one `(AccountId, WorldId)` resolves to a world-local `PlayerId`, account meta state is independently persisted, multiple Characters/sessions are supported, and Cataclysm `META_PROGRESS`/hard requirements consume that account state.
- #96 remains the retry/outcome-history integration gate and #92 remains the production authentication/public-server-security gate.


## Scope and evidence

Reviewed all 27 completed specification pages under `docs/wiki` at OctoGhast commit [`ea85fed2171251d325c18319e63f3f5269d1e15a`](https://github.com/LambdaSix/OctoGhast/commit/ea85fed2171251d325c18319e63f3f5269d1e15a), plus [`prospective-networking-core.md`](./prospective-networking-core.md). The latter is already a supersession pointer to Spec 25, not a competing prospective contract. Specs 26 and 27 are included because they complete the active-region and presentation parts of #65.

The completed investigations #66–#89, #90, #93 and #94 were closed at audit time. Their source/data/test evidence is accepted as authoritative for `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`. No upstream investigation was repeated and no upstream baseline was advanced. Findings below concern interpretation and integration of the existing corpus, not newly established CDDA behaviour.

Architectural/programme baseline: [#52](https://github.com/LambdaSix/OctoGhast/issues/52), [#57](https://github.com/LambdaSix/OctoGhast/issues/57), [#58](https://github.com/LambdaSix/OctoGhast/issues/58), [#64](https://github.com/LambdaSix/OctoGhast/issues/64), [#65](https://github.com/LambdaSix/OctoGhast/issues/65), [#90](https://github.com/LambdaSix/OctoGhast/issues/90), their completed specification refinements, and the existing separate decisions [#91](https://github.com/LambdaSix/OctoGhast/issues/91) and [#92](https://github.com/LambdaSix/OctoGhast/issues/92). Spec 24's later explicit allowance for bounded libgodot server adapters is read together with the settled Godot-independent Core and non-authoritative Godot **client** contracts.

The review checked reference/profile/Core/adaptation/evolution separation; authority, multiple actors and active regions; time, costs and ordering; spatial and persistence identity; RNG and continuation; command/query/event mutation ownership; client projection; implementation dependencies; and acceptance scenarios. The detailed pinned formulas and data schemas were retained as supplied evidence, rather than re-derived. This is a document audit, not a runtime conformance result or a complete implementation-status inventory of the legacy repository.

## Readiness assessment

**Implementation can begin in bounded areas. The corpus is not yet an unqualified integrated implementation baseline.** The architecture is consistent about one authoritative world, profile-owned rules, transport-neutral requests and explicit projections. Two new shared decisions must be settled before final scheduler/network/persistence integration is frozen:

- [#95 — Canonical ordering, bounded admission and activation phases](https://github.com/LambdaSix/OctoGhast/issues/95).
- [#96 — Bounded command idempotency and outcome recovery across reconnect/save](https://github.com/LambdaSix/OctoGhast/issues/96) — **resolved 2026-09-26** by [`architecture-bounded-command-idempotency-outcome-recovery.md`](./architecture-bounded-command-idempotency-outcome-recovery.md).

Stable work can proceed now: ECS/composition and dependency boundaries; typed durable identity; immutable definition generations and Cataclysm data adapters; pure rule/formula fixtures; profile-configured fixed-step primitives; position-to-cell mapping and atomic index/containment mutations; save-generation/manifest atomicity; bounded parsers/queues and transport abstraction; viewer-filtered projections; and Godot presentation over confirmed samples. These can be tested through explicit supplied ordering/policy interfaces without pretending the pending global policy is settled.

#95 and #91 have subsequently been resolved by their dedicated architecture specifications. Reconnect/restart-safe non-idempotent operation recovery now consumes the resolved #96 bounded operation-generation/history contract. Before production LAN/friend/public-server trust and especially internet deployment, resolve #92. The audit does not mark M0 or any gameplay milestone complete.

Core over-fitting found here was limited but real: remaining integer-position/geometry wording, missing explicit creature-profile classification, an OO-versus-ECS choice left apparently open, and an inventory affordability sentence that could replace signed move debt with a different economy. Those passages are corrected. Existing rate, anatomy, item taxonomy, pockets, crafting, combat, construction, EOC and loader evolution seams remain intact.

No pinned formula, schema, lifecycle hook or behavioural evidence was removed in the name of generality. The audit specifically prevents weakening negative move debt, all-item identity, content-continuation checks, reference spatial tests and real-adapter conformance. Exact cross-system causal parity remains a risk until #95 resolves the phase plan; it is not declared solved by adding a generic scheduler interface.

## Findings by severity and classification

Severity describes implementation impact: **High** can change authoritative outcomes, lose identity or misstate readiness; **Medium** can misdirect a shared interface or test boundary; **Low** is navigation/editorial drift. Classification uses the requested categories. “Resolved” means the document contract is amended, not that runtime tests have passed.

| ID | Severity / classification | Evidence and consequence | Disposition |
|---|---|---|---|
| A01 | High — **Cross-spec contradiction** | Spec 01 architecture §5 and Specs 04 §10 / 09 §13 use actor/entity execution order; Spec 25 §7.2 and NET25-09 use PlayerId/admission order for contention. PlayerId and CharacterId can sort oppositely. AI/system precedence and bounded candidate selection are incomplete. Spec 01's environment-after-player/AI outline also conflicts with its reference scenarios 12–14 and Spec 14 §8's preserved environment-before-later-actor constraint. | **Open: #95.** Marked the conflicting outlines as integration gates. No winner/order chosen locally. |
| A02 | High — **Cross-spec contradiction** | Spec 12 requires catch-up before access; Spec 26 §7.1 resolves movement before loading the resulting footprint. Ordinary buffered movement can work, but unavailable teleport/long-move destinations need preflight or deferral. Catch-up through the current tick followed by that tick's active processing lacks a shared interval convention. | **Open: #95.** Preserve atomic position/index updates and exactly-once intervals; resolve the phase plan centrally. |
| A03 | High — **New unresolved architecture decision** | Spec 06 promises retry idempotency; Spec 07 suppresses replayed craft mutations; Spec 11 prevents dialogue rerolls. Spec 25 resets session sequences and promises terminal outcomes while deferring result history to domains. Spec 20 excludes transport state but permits durable admitted work. Lost response + reconnect/save/retry has no common key lifetime, retention, rollback or outcome rule. | **Open: #96.** Options and failure scenarios recorded. No unbounded cache or implied unlimited exactly-once delivery added. |
| A04 | High — **Cross-spec contradiction** | Spec 05 §3.2 requires a persistent UID for every runtime item; Spec 20 §5 previously required it only for externally referenced items. Ordinary nested items could lose identity on save and acquire a different identity when later exposed. | **Resolved in Spec 20.** Structural ownership controls storage; every Cataclysm item retains its UID. Added P20-AUD-01. |
| A05 | High — **Cross-spec contradiction** | Spec 18 BND-08 and Spec 20 §12 prohibit silent reinterpretation against materially different content. Spec 19 §10.3/SAVE-09 treated changed bytes under the same IDs as merely diagnostic and its identity table made the fingerprint optional without distinguishing required generation metadata. | **Resolved in Spec 19.** Preserve pinned changed-file evidence; require declared compatibility/migration or stop activation. A byte mismatch alone is not proof of incompatibility. SAVE-09 covers both branches. |
| A06 | High — **Cross-spec contradiction** | Spec 06 said an actor must “pay”/have the required action budget before transfer; Spec 01 explicitly permits legal actions to leave a negative move balance. An affordability gate changes Cataclysm costs/opportunities even if the debit amount is unchanged. | **Resolved in Spec 06.** Eligibility is distinct from full-cost affordability. Added INV-AUD-01; resource/access checks remain. |
| A07 | Medium — **Architecture amendment required** | Spec 12's generic replication wording required integer positions; SpatialCoordinates grouped reference constants with generic primitives; unqualified geometry/z invariants undermined its later WorldPosition seam. | **Resolved in Spec 12.** Profile-defined position codec/mapping, Cataclysm-only constants and SPAT-AUD-01. Reference negative-coordinate/grid/z tests retained. |
| A08 | Medium — **Architecture amendment required** | Spec 10 had strong server/co-op adaptations but no clear controlling Core/profile/evolution split for monster taxonomy, senses, reproduction, cooldowns and costs. This invites a universal Monster schema/scheduler. | **Resolved in Spec 10 §23.** Explicit classification and M10-AUD-01; all pinned M10 rules, including reproduction and revival identity, remain. |
| A09 | Medium — **Cross-spec contradiction** | Spec 01 scenario 44/“Godot-free” wording and Spec 23 §18 required all scenarios without Godot; Spec 24 §§4/11.1/14 permits production headless libgodot services and requires real-adapter conformance when semantics matter. | **Resolved in Specs 01/23.** Core fixtures remain Godot-independent; headless production tests may need the real adapter. Godot client remains presentation-only. HAR23-AUD-01 prevents a fake from standing in for production proof. |
| A10 | Medium — **Architecture amendment required** | Spec 26 §12.3 asserted that adding any nonacting observer could not change RNG, even though a new player's footprint can introduce authoritative simulation work. Spec 25's equivalence wording omitted admission ticks/lease inputs. | **Resolved in Specs 25/26.** Hold admission and coverage inputs fixed for observer/transport invariance; expanded coverage is an explicit simulation input. AR26-AUD-01 distinguishes the cases. |
| A11 | Medium — **Architecture amendment required** | Spec 25 §6.1 allowed an undefined “audited query bookkeeping” gameplay exception, while Specs 03/04/14 require pure queries and no authoritative RNG consumption. | **Resolved in Spec 25.** Only infrastructure metrics/rate-limit/cache bookkeeping is exempt; generation/semantic transitions use the authoritative operation path. Added NET25-AUD-01. |
| A12 | Low — **Editorial clarification** | Spec 02 left ECS-versus-aggregate/class hierarchy apparently open despite #52; Specs 07/10/16/24 retained future/prospective dependency wording; Specs 21/23 pointed authentication at #90 instead of #92; Spec 25 had a literal escaped newline in prerequisites; Spec 21 used ItemId without identifying ItemUid. | **Resolved by targeted edits.** No feature behaviour changed. #65's stale existing-save checkbox is reconciled with Spec 20's explicit initial exclusion. |

## Complete specification review register

Each row covers all five layers: **reference evidence; Cataclysm compatibility rules; reusable Core; intentional adaptation; future evolution seam**. “No change required” means no local amendment was warranted; a page still consumes shared decisions #95/#96 where relevant. It is not a claim that its implementation exists.

| Spec / investigation | Classification and layer/ownership assessment | Action and acceptance focus |
|---|---|---|
| [01 — loop/time](./spec-01-game-loop-time-scheduling.md) / #66 | **Cross-spec contradiction.** Reference turn loop retained; Cataclysm 10 TPS/100 moves separated from Core time; continuous multi-actor/server lifecycle adaptation sound; future exact rates allowed. Global phase/order integration incomplete. | Amended precedence/headless wording and #95 gate. Preserve tests 1–60 subject to stated reference/adaptation scope; integrated contention/phase tests pending. |
| [02 — Character](./spec-02-character-model-stats-anatomy-needs.md) / #67 | **Editorial clarification.** Anatomy/stats/needs/formulas are profile rules; Core state/time/identity primitives, multi-actor physiology and alternative schemas are explicit. | Clarified implementation freedom within settled ECS/composition. Existing 21–28 boundary and physiology scenarios retained. |
| [03 — creation/progression](./spec-03-character-creation-progression.md) / #68 | **No change required at audit time.** Creation, skills, mutations and achievement rules remain profile-local; Core transaction/identity and non-grid/alternative-creation seams explicit. | The then-open #91 dependency is now resolved by Spec 28; Spec 03 has been cross-referenced without reopening its pinned behavioural investigation. |
| [04 — actions/activities](./spec-04-action-dispatch-activity-framework.md) / #69 | **Cross-spec contradiction.** Cataclysm primary activity/backlog and hook order separate from generic durable work; transport/UI adaptation and alternate lanes/rates are explicit. | Marked #95 order conflict and #96 request-vs-activity identity dependency. No lifecycle or EOC hook rewrite. |
| [05 — items](./spec-05-item-model-lifecycle.md) / #70 | **No change required locally.** Taxonomy/charges/rot/stacking remain Cataclysm; Core identity/scheduling/projection and future models explicit. | Its all-item UID rule is preserved; the inconsistent consumer Spec 20 was fixed. Identity, split/merge and lifecycle scenarios remain authoritative. |
| [06 — containment/transfer](./spec-06-inventory-pockets-containment-item-transfer.md) / #71 | **Cross-spec contradiction.** Pocket/capacity/selection/reach/cost policy is separated from generic ownership/transaction machinery. | Fixed affordability drift; added #95/#96 integration dependencies and INV-AUD-01. Conservation/stale-reference scenarios retained. |
| [07 — crafting](./spec-07-crafting-recipes-requirements-disassembly.md) / #72 | **Editorial clarification**, plus shared #96 dependency. Recipe/requirement/formula policy and Core resource/work/RNG seams already clear. | Replaced prospective #90 references with completed Spec 25; exposed retry dependency. No reservation, component consumption, failure-point or disassembly rule weakened. |
| [08 — construction](./spec-08-construction-deconstruction-world-transformation.md) / #73 | **No change required.** World ProjectId versus worker activity ownership is coherent; grid site, progress currency, staged rules and byproduct origins are profile-local. | Preserve 41–47 alternate-rate/non-grid/concurrency/save scenarios and original completion ordering. Shared #95/#96 apply through owners. |
| [09 — combat](./spec-09-combat-core-damage-melee-ranged-projectiles.md) / #74 | **Cross-spec contradiction** limited to shared ordering. Damage/armour/weakpoint/projectile rules remain Cataclysm; Core does not require a combat taxonomy or instantaneous projectile. | Marked #95 integration gate. Keep costs, RNG order and immediate-reference/projectile versus future-flight distinction. |
| [10 — creatures](./spec-10-monsters-creature-simulation.md) / #75 | **Architecture amendment required.** Existing explicit actor context, CreatureId, death/corpse and active/background adaptation remain sound; generic/profile classification was incomplete. | Added §23 and M10-AUD-01, qualified rate and corrected completed AI dependency. |
| [11 — NPC/social/mission](./spec-11-npcs-dialogue-factions-missions.md) / #76 | **No change required.** Mission single-assignee, attitude/faction/trade rules are profile-specific; Core permits different ownership/dialogue/economies. Explicit participant context avoids one avatar. | Preserve private reputation/dialogue, atomic trade and stable companion identity. Its no-reroll promise consumes #96; no local retry store invented. |
| [12 — local map](./spec-12-local-map-coordinates-spatial-simulation.md) / #77 | **Architecture amendment required** and #95 integration conflict. Server union/index authority was sound; a few earlier generic clauses still imposed reference geometry. | Qualified constants, codecs and invariants; added SPAT-AUD-01 and activation gate. |
| [13 — worldgen](./spec-13-overmap-world-generation-local-mapgen.md) / #78 | **No change required.** Strategic/tactical generation is profile policy over Core atomic materialization, durable spatial keys and deterministic jobs. | Preserve ordered-generation RNG, first-discovery deduplication and WG-32 save atomicity. A new player expanding coverage is an input, not a pure observer. |
| [14 — environment](./spec-14-environment-simulation-weather-fields-fire-scent-decay.md) / #79 | **Cross-spec contradiction** in global phase integration; otherwise explicit environment/profile/Core split and future fluid/scent/ecology seams. | Qualified 10 TPS and pointed preserved actor/environment causality to #95. Historical exposure/rot and catch-up rules retained. |
| [15 — vehicles](./spec-15-vehicles-modular-mobile-structures.md) / #80 | **No change required.** Mount graph/steering/motion economy are Cataclysm; Core supports multi-cell atomic movement and stable vehicle/part references. | Origin-owned persistence, passenger/tow fixups, power/cargo and renderer isolation are compatible. Integration consumes #95. |
| [16 — AI/pathfinding](./spec-16-ai-pathfinding-autonomous-decision-systems.md) / #81 | **Editorial clarification.** Grid path/search/target policies are Cataclysm; Core traversal interfaces and alternate geometry are explicit. | Qualified timing and linked completed Spec 11. Tactical cache versus durable goal state and AI/common action path retained. |
| [17 — EOCs/events](./spec-17-events-talkers-eoc-runtime.md) / #82 | **No change required.** EOC vocabulary, alpha/beta, operators and recurrence are profile-local; generic context/job/event/state seams are explicit. | Preserve synchronous PREVENT_DEATH, sequential effects and persisted schedule order. Global scheduling integration consumes #95, not a new local queue policy. |
| [18 — data/IDs](./spec-18-data-loading-ids-registries.md) / #83 | **No change required.** Generic typed catalogues/generations versus Cataclysm JSON/inheritance/overrides already explicit. | BND-01–10 remain controlling, especially material content drift BND-08; inconsistent Spec 19 was amended. |
| [19 — content packs](./spec-19-mod-system-content-pack-compatibility.md) / #84 | **Cross-spec contradiction.** Provider/manifest infrastructure and future formats are sound; drift acceptance conflicted with newer persistence/loading contracts. | Amended §10.3, identity table and SAVE-09; preserved pinned mod discovery/precedence and compatibility envelope. |
| [20 — persistence](./spec-20-persistence-save-load-migration.md) / #85 | **Cross-spec contradiction** on ItemUid; **New unresolved architecture decision** consumed from #96. Profile-neutral partition/position and world-local PlayerId contracts are sound. | Corrected all-item identity and codec wording; added P20-AUD-01 and outcome-history gate. Atomic manifest, fixups, RNG and transport exclusions retained. |
| [21 — UI/input](./spec-21-ui-input-gameplay-presentation-accessibility.md) / #86 | **Editorial clarification.** Toolkit-neutral interaction, asynchronous staleness, accessibility and client preferences already separate from domain rules. | Standardized ItemUid and #92/#96 ownership references; no UI-selected authority or pause added. |
| [22 — assets/audio/localization](./spec-22-graphics-tilesets-audio-localization.md) / #87 | **No change required.** CDDA fallback/translation semantics are profile adapters; client assets/RNG/preferences remain outside world authority. | Retain hidden-sound-source, pack divergence, fallback and CORE22-01 coverage. |
| [23 — harness/debug](./spec-23-debugging-developer-tools-parity-test-harness.md) / #88 | **Cross-spec contradiction** on blanket Godot exclusion; otherwise strong profile-neutral harness and explicit reference tolerances. | Aligned real-adapter headless tests with Spec 24; added HAR23-AUD-01 and integration matrix gates; corrected #92 ownership. |
| [24 — build/runtime](./spec-24-build-platform-packaging-runtime-resource-layout.md) / #89 | **Editorial clarification.** Product toolchain/2dog choices do not make Godot a Core dependency; durable state remains domain-owned even with server adapters. | Updated completed networking and save-compatibility references. Production adapter determinism remains a proof obligation, not permission to weaken replay. |
| [25 — networking](./spec-25-authoritative-server-networking-core.md) / #90 | **Cross-spec contradiction / New unresolved architecture decision**, plus query clarification. Generic transport/session rules remain profile-neutral and bounded. | Exposed #95/#96, scoped replay inputs, clarified pure queries and consumed Specs 26/27 projection semantics. Existing parser/security/queue scenarios retained. |
| [26 — active regions](./spec-26-multiplayer-active-regions-visibility-interest-management.md) / #93 | **Cross-spec contradiction** on phase integration; **Architecture amendment required** on observer-invariance scope. Reference bubble is already profile-local. | Added #95 gate and AR26-AUD-01; union, exactly-once, knowledge/privacy and non-grid seams retained. |
| [27 — presentation](./spec-27-godot-2d-presentation-boundary-interpolation.md) / #94 | **No change required.** Reference overlays versus intentional interpolation are explicit; Core sample/revision contract supports future rates/positions/projectiles. | Retain epochs, coalescing, hide/re-entry reset, cosmetic prediction and G27-01–32. Spec 25 now explicitly consumes this producer/consumer contract. |

The prospective networking pointer has subsequently been amended only to point #91 at completed Spec 28; Spec 25 remains the networking contract and #92 remains the authentication/security owner.

## Core versus Cataclysm ownership summary

| Contract | Core/platform obligation | Cataclysm/profile obligation and evolution seam |
|---|---|---|
| Authority | Server-owned ECS/world; commands request, domain systems validate/mutate; one-player and co-op share the logical path. | Concrete gameplay actions, validation, costs and outcomes; no local-avatar shortcut. |
| Time | One canonical fixed-step coordinate; deterministic scheduling/remainders; host pacing and presentation separate. | 10 ticks/world second, 100 moves, speed/cadence/calendar/activity rules. Alternative exact profile rates/currencies remain possible. |
| Ordering | Explicit deterministic intake/execution and persisted pending-work order, bounded resources. | Cataclysm causal/hook ordering is integrated by the resolved #95 architecture without hard-coding CDDA phases into Core. |
| Space | WorldPosition, deterministic derived SpatialCell membership, atomic index mutation, world-owned region union. | Grid occupancy, 12/24/132 dimensions, z limits, line/path/collision/terrain rules. Future non-grid rules need explicit geometry policy, not renderer changes. |
| Identity | World/player/entity/item/project identities independent of ECS storage, socket, session and Godot objects; typed durable codecs. | ItemUid for every Cataclysm runtime item; polymorph/revival/split/merge semantics. Definition IDs stay distinct from instance IDs. |
| Persistence | Quiescent coherent world snapshots, recoverable manifest commit, stable fixups, versioning and deterministic continuation; Spec 28 adds an independent atomic server-account store. | Profile-owned world payloads/partitions and migrations. Direct CDDA save import is not initially required. Cross-world player meta belongs to Spec 28 / #91; operation history to #96. |
| Content | Frozen generation, stable typed lookup, provenance, deterministic build/finalization and compatibility gates. | CDDA JSON/copy-from/mutation/override/MOD_INFO semantics. Other schemas/providers remain possible. |
| RNG | Authoritative deterministic state/streams, reproducible ordering and save continuation; no presentation/query RNG effects. | Concrete distributions/draw order and allowable reference tolerances. No universal requirement to copy CDDA's global PRNG. |
| Events/work | Generic context, scheduling, stable references and audience routing; owners commit state. | EOCs, alpha/beta, one-primary-activity, CDDA progress/reproduction/combat/crafting rules. Future profiles may replace those models. |
| Projection | Authorized semantic state/events; bounded transport; durable knowledge distinct from current visibility and transient baselines. | Profile-specific senses/knowledge/interaction disclosure. Client Godot interpolation never changes authority. |

No contradictory ownership was found between construction's world-owned project and actor-owned activity, item identity and containment ownership, vehicle origin persistence and multi-cell footprint, or monster death and corpse/revival identities once A04 is corrected. They describe different responsibilities, not competing owners.

Atomic external publication is also distinct from universal rollback of scripted effects. Spec 17 preserves sequential EOC effects and explicitly does not promise automatic rollback of every runtime error; Specs 08/09 require coherent publication and preserve internal hook order. Implementers must retain that distinction rather than wrapping all script execution in an invented all-or-nothing transaction or delaying synchronous PREVENT_DEATH until after death. No evidence justified changing those contracts in this audit.

## Amendments and unresolved decisions

Amended **17 existing pages**: Specs **01, 02, 04, 06, 07, 09, 10, 12, 14, 16, 19, 20, 21, 23, 24, 25, 26**. Ten specification pages and the supersession pointer are unchanged. This report is the only new wiki page. Changes are targeted clauses, ownership/dependency corrections, integration qualifications and acceptance additions; no bulk document reformatting or gameplay/runtime code is included.

Created:

- **[#95](https://github.com/LambdaSix/OctoGhast/issues/95):** conflicting assumptions, affected specs, layered/admission-preserving/profile-plan options, decision criteria, bounded intake/activation/pause/save scenarios and M0 impact.
- **[#96](https://github.com/LambdaSix/OctoGhast/issues/96):** session-only, durable-operation and hybrid alternatives; key/retention/rollback/atomicity/privacy criteria; lost-response, restart and hostile-duplicate scenarios.

At audit time **#91** remained the owner of the unresolved cross-world profile/meta-progression decision and **#92** owned authentication/public-server security. #91 is now resolved by Spec 28 with server-scoped accounts; #92 remains open. No completed pinned investigation was reopened: subsequent changes are architecture/cross-reference amendments.

The #65 update records this audit and fixes the stale existing-save compatibility tracker against Spec 20. #64 records conditional milestone readiness; #52/#57/#58/#90 receive only their directly affected integration dependencies. Completion checkboxes for the historical investigations remain intact. The programme's implementation/parity matrix is still a separate deliverable under Spec 23; this review register does not pretend to be measured runtime parity.

## Residual risks and validation limits

- #95 resolved the concurrent actor/environment integration by preserving reference-relative Cataclysm causal/budget placement while generalizing the phase-plan host. Future phase changes remain explicit profile compatibility decisions.
- #96 now settles bounded retention, operation generations and world-history/rollback semantics; conformance must test the explicit success/rejection/indeterminate/history-expired/history-mismatch outcomes rather than claim unlimited exactly-once delivery.
- Cataclysm positive idle-budget carry is documented; any future anti-burst cap is an explicit profile policy under #57, not an implementation shortcut that changes costs.
- Background catch-up remains domain-specific. Monster compressed catch-up is not a promise to simulate every unloaded tactical interaction. Item/environment equivalence requires equivalent historical inputs; systems needing intermediate simulation must retain an appropriate lease or implement the declared bounded catch-up. Test domain integration before optimizing timewarp.
- Godot-backed authoritative service results require real-adapter determinism, save/restart and supported-platform tests. CDDA differential tolerances do not automatically relax OctoGhast's own replay contract.
- Exact wire encoding, serializer, numeric representation, fingerprint encoding and internal data structures remain implementation choices constrained by the stated contracts. Future sub-cell support is an interface seam, not a claim that its collision/gameplay rules already exist.
- Reference source links, evidence sections and detailed behavioural fixtures were preserved. Document/link/scope checks validate these edits; no gameplay tests were executed because no runtime code was changed. The supplied upstream investigations were not independently re-proven.

The resulting corpus is a useful implementation foundation with explicit integration gates, a strong unchanged Cataclysm reference target, and reusable Core contracts that do not require CDDA to remain OctoGhast's final product.


## Post-audit Spec 29 security resolution — 2026-09-25

The audit's historical statements that #92 remained an authentication/public-server-security gate are now resolved by [Spec 29 — Authentication, trust and public-server security](./spec-29-authentication-public-server-security.md).

Spec 29 preserves the audit's existing authority/account boundaries while fixing the previously deferred security policy:

- no mandatory OctoGhast-operated identity service;
- trusted synthetic local authentication through the same account/session authority path;
- provider or one-time invitation/bootstrap identity for LAN/friend servers;
- mandatory protected transport and authenticated individual identity for public dedicated servers;
- replaceable provider bindings to Spec 28 `AccountId`;
- replay-resistant invitation/resumption lifecycle, independent revocation and server-local bans/capabilities;
- layered auth-specific abuse controls and secret redaction;
- security state excluded from world saves and authoritative simulation RNG.

This closes #92 as an architecture/specification gate. It does not mark public-server runtime implementation or conformance complete. #96 remains the separate retry/outcome-recovery gate.


## #96 architecture resolution — 2026-09-26

[#96](https://github.com/LambdaSix/OctoGhast/issues/96) is resolved by [Architecture — bounded command idempotency and outcome recovery](./architecture-bounded-command-idempotency-outcome-recovery.md).

The adopted hybrid policy requires every gameplay-affecting command to declare `StateReconciled`, `IntrinsicIdempotent`, or `DurableOutcome`. Durable logical operation identity is separate from connection/session/transport sequence/Character/Account identities and is scoped by `WorldId + WorldHistoryEpoch + PlayerId + OperationGeneration + OperationId`.

Bounded generations make old unknown IDs retry-only/expired rather than fresh commands; process restart rotates the submission generation; deliberate older-snapshot restore changes the world-history epoch. Persisted semantic outcomes are atomically linked to their world effects, while transport/session state remains excluded from saves. Specs 04/06/07/11/20/21/23/25 consume the shared contract.

This removes #96 as an unresolved architecture gate. Runtime implementation and OP96 conformance remain future work.
