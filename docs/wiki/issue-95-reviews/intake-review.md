# #95 intake, execution ordering and fairness review

> Supporting proposal, not an adopted contract. The [synthesis](./synthesis.md) controls where reviews disagree.

Basis: the post-spec audit at commit `7057611f96ec08817203880869f256a94c55870f`, with pinned Cataclysm evidence at `e262adb299a7613b4aedc5f12c08fe0413c56a84`. This review recommends an integration policy; it does not select the global actor/environment phase plan or the #96 operation-history policy.

## Recommendation

Use layered ordering: fair, bounded `PlayerId` admission for external requests, followed by the existing actor/work execution key from Specs 01/04/09. Spec 25's `PlayerId -> per-player admission sequence` decides which network requests enter the authoritative queue; it does not decide which actor wins a same-tick domain contention. This preserves the existing execution contract while making the tradeoff explicit: P10 may be admitted before P20, yet P20's lower `CharacterId` can execute first. AI, activity, scheduled EOC, lifecycle and administrative work do not pass through the player ingress cap; they retain their own authoritative scheduler/work queues.

The phase plan supplies causal bands (for example, whether environment work precedes actor work), but does not need to choose a new global winner key here. The exact execution key is `(phase_rank, due_tick, subsystem_priority, work_kind_rank, primary_work_key, origin_sequence)`, with every component an explicit integer or stable typed value from the active profile. For actor/AI/activity work, `primary_work_key` is stable `ActorId`/`EntityId`; for equal-due EOCs it is persisted scheduler sequence (Spec 17 §11–12, lines 253–285); for lifecycle/admin work it is stable operation/source ID. `origin_sequence` is the server admission sequence for a player request or the persisted authoritative enqueue sequence for non-player work. There is no hash/container/thread or undefined local tie-breaker.

Do not impose a full-cost affordability gate. Eligibility is the profile's scheduling predicate (due, active/materialized target, actor opportunity and any declared minimum budget); a legal action may debit a positive budget below zero, as preserved by Spec 01 §4 (lines 436–442). Negative balance delays subsequent eligibility; it does not reject or silently refund the already selected action.

Admission bounds are independent from execution bounds. Use per-PlayerId cap `P` and global cap `G` for external requests only; authoritative AI/activity/EOC/lifecycle work is not silently dropped to satisfy those ingress limits. If a host pump cannot finish due work from tick N, continue that same tick on a later host pump before advancing to N+1, unless the phase plan explicitly records a continuous-time adaptation. Do not call unfinished due work “deferred admission.”

The external selection algorithm is exact: maintain each PlayerId's FIFO of validated requests and a sorted ring of PlayerIds. At the intake boundary, snapshot the ring and start at persisted cursor `r`; make passes around the ring, taking at most one head per non-empty PlayerId per pass, until `G` requests are selected or every PlayerId reaches `P` selections. A player's head order is its server-assigned per-player ingress rank (monotonic across sessions) then message-type discriminator; this rank is distinct from a client/session sequence. Advance `r` to the successor of the last selected PlayerId; if none was selected, leave it unchanged. Requests left in transport ingress remain unadmitted and subject to Spec 25 overload policy. A newly received request or player cannot enter this frozen selection after the boundary.

The frozen external set does not freeze actor-internal work. A selected action may start and continue an activity within Spec 01's declared actor opportunity (scenarios 10–11); generated work follows the same tick's actor path.

## Evidence and alternatives

| Evidence | Consequence for #95 |
|---|---|
| Spec 25 §7.2 (lines 223–245) orders same-tick network requests by `PlayerId`, session sequence and type, maps reconnects to a server per-player sequence, and defers reconciliation with actor keys. | Keep PlayerId for admission; reconnect cannot reorder admitted work. |
| Spec 01 §5 (lines 444–460), Spec 04 §10 (lines 223–235), and Spec 09 §13 (lines 424–438) require explicit phase/due/subsystem/actor execution and later revalidation. | Preserve that actor/work key after admission; amend NET25-09 so it no longer implies PlayerId decides the domain winner. |
| Spec 17 §11–12 (lines 253–285) requires equal-due scheduled jobs to use persisted enqueue sequence, not connection arrival, container iteration or client identity, and persists those keys. | Preserve domain queue order inside the global execution key; never re-sort equal-due EOCs by PlayerId or CharacterId. |
| Spec 25 §10 (lines 324–366) requires per-connection and aggregate bounds; Spec 25 §15 (lines 482–489) promises invariance only for an identical canonical admission trace. | Record fair selection/defer decisions; sorting later cannot equate different arrival histories. |
| Spec 20 §6 and §15 (lines 163–172, 510–526) require a deterministic save cut and durable admitted pending work with stable IDs/order keys, while excluding transport state. | Persist semantic pending work and ordering metadata, never socket queues or transport sequence counters. |

The tradeoff is explicit: P10/C200 and P20/C100 can both be admitted, then C100/P20 wins even if P10's admission rank is earlier. A stable low `CharacterId` can repeatedly win races; rotating ingress prevents low `PlayerId` monopolization but cannot remove actor-key skew without changing Specs 01/04/09. Claim bounded admission fairness and deterministic execution, not full cross-actor fairness; amend NET25-09 and add starvation telemetry. The benefit is preserving domain actor identity and existing Cataclysm execution semantics.

## Worked traces

1. **Reversed PlayerId/CharacterId.** At tick 40, P10/C200 and P20/C100 submit transfers for the same item. The rotating ring admits both (subject to P/G), assigning P10 sequence 44 and P20 sequence 45. The execution key then compares CharacterId, so C100/P20 wins and P10 receives a stale/conflict result. This is intentional layered ordering, not an accidental hidden sort. Any phase-band difference still precedes this key.

2. **Player, AI, activity and EOC contend for one item.** The player is admitted through the ring; AI/activity/EOC are already-authoritative candidates. The phase plan chooses their bands, then the exact execution key compares subsystem/work-kind, actor or persisted EOC schedule key, and origin sequence. The first commits and the others revalidate/reject. EOC equal-due order remains schedule-sequence order.

3. **Ingress overflow versus authoritative work.** With `P=2`, `G=3`, A offers A1–A3 and B offers B1. The ring selects A1, B1, then A2; A3 remains unadmitted. An AI item X and a due EOC are not in this ingress pool and still execute under their authoritative phase/work queues. If the host cannot finish them, it continues tick N rather than advancing with silently missing due work.

4. **Newly eligible versus deferred.** A3 remained in transport ingress after the prior `G` cap; B1 arrives during current resolution. Neither can enter the frozen set until the next boundary. At that boundary the ring cursor determines whether A3 or B1 is admitted; there is no arrival-time leapfrog. An actor with negative budget waits for its eligibility predicate; it is not deleted.

5. **Save/reconnect.** A's admitted request sequence 44 is pending at save cut T. Persist PlayerId, CharacterId, canonical target tick, phase-plan version, source sequence, execution/defer metadata and the semantic payload/work identity. On reconnect, a new session sequence maps to PlayerId's next server admission sequence (45); it cannot leapfrog sequence 44. Socket/session IDs and transport buffers are absent from the save. Whether a retry is the same operation, a duplicate, or unresolved delivery remains #96.

## Proposed clauses and tests

Amend Spec 25 §7.2 to state that its key is admission-only and document the rotating-ring `P/G` algorithm. Keep the actor execution key in Spec 01 §5, Spec 04 §10 and Spec 09 §13, add the Spec 17 §11 schedule-sequence adapter, and require Spec 20 pending work to retain target tick, phase-plan version, source/order keys and admission state across save.

Add fixtures for the five traces, callback permutations with the same admission trace, reconnect reset, ingress overload, actor-key starvation, equal-due EOC save/reload, activity continuation within one tick, and same-tick host continuation. Assert identical replay only for the same admission trace; Spec 25 §15 does not equate different admission cuts.

Dependencies remain explicit: the phase reviewer must choose actor/environment/lifecycle class bands and activation preflight/defer boundaries; Specs 12/26 supply availability semantics; #96 owns operation identity, duplicate suppression, retention and lost-response outcomes; Spec 23 owns the deterministic harness. The pinned CDDA costs, hook order, signed move debt, stable IDs, authoritative simulation and profile/Core separation remain unchanged.
