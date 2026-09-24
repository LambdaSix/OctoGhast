# #95 phase, causality, pause and continuation review

> Supporting proposal, not an adopted contract. The [synthesis](./synthesis.md) controls where reviews disagree.

## Recommendation

Adopt a layered contract. Core should expose a versioned, deterministic phase-plan capability; the active rules profile supplies the phase graph, source-class ranks, and causal hooks. Networking assigns an admission sequence to a bounded, frozen candidate set, but admission order must not by itself decide contention. Execution uses `(phase, due coordinate, work-class rank, stable actor/entity ID, admission sequence)`. Equal-due scheduler queues retain their persisted enqueue sequence before actor identity, as required by Spec 17. This combines transport-neutral admission with Cataclysm causality without making a CDDA phase list a permanent Core API.

Persist the phase-plan identifier, profile/rules version, rate mapping, and order keys. A save under another plan needs an explicit migration; this is separate from save schema version and #96's outcome policy.

## Evidence and classification

| Contract | Status and evidence | Review consequence |
|---|---|---|
| Integer world chronology, 100 moves/second, signed move debt, timed events after the clock increment, activity progress, RNG/order persistence | Pinned CDDA/reference contract (Spec 01 §§3–5, lines 404–411; §5, lines 185–190 and 193–206). | Preserve in the Cataclysm profile; Core supplies fixed-step/budget primitives. Negative budgets remain legal. |
| Avatar action/activity, then environment, then monsters/NPCs, then avatar replenishment; synchronous completion/cancellation hooks | Pinned causal evidence (Spec 01 lines 122–166; Spec 04 lines 130–157; Spec 17 lines 109–115). | Keep the observable local hook order while replacing the privileged avatar with actor opportunities. `PREVENT_DEATH` remains synchronous; EOC effects remain sequential. |
| 10 TPS, continuous server clock, independent actors, active-region union, no unilateral multiplayer pause | Intentional OctoGhast adaptation (Spec 01 lines 415–432 and 487–497; Spec 12 lines 161–189). | Do not present 10 TPS or avatar ownership as Core invariants. |
| Spec 01's scheduler → activities → player/AI → environment outline | Incomplete illustrative plan: expressly pending #95 (Spec 01 lines 444–460) and conflicting with Spec 14's environment-before-later-actor constraint (lines 249–257). | It cannot settle winners, lifecycle, activation, save cut, or EOC reentrancy. |
| Timed-event insertion versus recurring EOC scheduling | Pinned source evidence shows a live insertion-ordered list; appended events are visited in the same pass (see the [narrow evidence note](./timed-event-evidence.md)). | Keep this timed-event lane distinct from Spec 17's due-time/persisted-sequence EOC queue. |
| PlayerId/session sequence intake key versus actor/entity execution key | Explicit cross-spec contradiction (Spec 25 lines 225–245; audit lines 34–38). | Freeze candidates and assign admission metadata first; use the profile execution key to resolve mutations. |
| Activation load/catch-up before query and exactly-once active processing | Established invariant, but phase integration is open (Spec 12 lines 149–173, 181–189; Spec 26 lines 269–334). | Require activation preflight or explicit deferral; source position/index stays unchanged on failure. |

## Canonical plan to amend into the specs

Recommended Cataclysm role-band order (layered actor keys within each band):

| Band | Required partial order and budget rule |
|---|---|
| Begin | Establish tick/chronology; bounded control; freeze external candidates and assign admission keys; preflight required activation. |
| Global | Process global timed events by the profile live-list rule, then EOCs by due time/persisted schedule sequence; accrue each eligible actor's signed budget exactly once after those effects and identify eligibility. |
| Player-controlled actors | For each actor in the profile actor key: `do_turn_eoc`, activity progress, immediate action, and any newly-started activity's remaining-budget loop. These consume only the budget accrued at tick start plus carried balance. |
| Environment | Settle weather/fields/items/vehicles/explosions once, then apply effects and synchronous lifecycle hooks. |
| Monsters, then NPCs | Process each band in stable actor order; no monster/NPC lane can spend a second accrual. Derived AI work remains eligible within its parent opportunity under its declared local key. |
| Close | Run post-turn physiology/replenishment bookkeeping without a second budget accrual; build projections/events, close the save/intake boundary. |

Freeze only external candidates at intake; same-opportunity activity transitions, synchronous EOCs, lifecycle hooks, and derived work remain eligible. This preserves Spec 01/04/09 actor keys and fair ingress. Moving avatar accrual from late `u.process_turn()` to tick start, and replacing one avatar lane with player bands, are explicit adaptations. Newly earned budget can be spent only in that actor's band; late bookkeeping cannot create budget until the next tick.

Version these ranks. Any moved avatar or monster→NPC ordering is an explicit adaptation with a named parity consequence, never a Core rule. `do_turn_eoc` remains immediately before its handler; completion EOC precedes finish; death hooks run at the host death point. An EOC enqueued while its drain is open follows that queue's rule; a closed phase cannot reopen. Timed-event append retains separate same-pass live-list behavior.

Admission must define the frozen set, budgets, source fairness, and replay-visible deferrals; sorting after race-selected truncation is insufficient. PlayerId may assign admission order, but CharacterId, AI/system, or socket identity cannot be an undocumented execution tie-breaker. Exact quotas remain coupled to the intake-ordering sibling review.

Activation should be a transaction before a command needing new state, or an explicit deferred phase. Load, fixups, catch-up, index rebuild, and scheduler enrollment complete before queries/mutation; failure leaves source position/index intact. Define one processed-through interval: catch-up reaches `T`, and active work begins strictly after it, so a deadline yields one effect. The activation sibling owns the exact policy.

Pause needs a bounded control lane with reserved capacity and fairness. While paused it may change clock-policy/session metadata only; it cannot mutate ECS/world state, consume RNG/budgets, execute callbacks, or advance time. Gameplay queues for the next normal boundary. Resume takes effect at the service boundary; the next tick starts normally, so gameplay cannot starve control (Spec 01 lines 487–493).

## Worked traces and tests

- **Hazard/action boundary:** establish tick, drain due global work, then accrue budgets once; execute an eligible player action; settle environment fields/weather once; apply the resulting hazard to each authoritative creature; invoke synchronous `PREVENT_DEATH` if needed; then process later monster/NPC opportunities. Repeat with reversed transport callback order and assert the same trace/RNG.
- **Activity versus due work:** drain an already-due EOC by persisted sequence; run actor `do_turn_eoc`, progress, completion EOC, then finish/chain. Separately, have a timed event append during `actualize` and assert same-pass live-list visitation. Neither nested case may disappear or silently leap ahead.
- **Pause/resume:** pause after tick N commit, flood gameplay input, submit resume through the control lane, and assert no tick, budget, EOC, callback mutation, or RNG change before resume. The first post-resume tick admits the declared frozen gameplay set.
- **Activation:** teleport into an unloaded region with both success and failure. Failure preserves source position/index; success catches up through exactly one interval before active processing. Test activation exactly at a periodic deadline.
- **Save cut:** request save at a boundary T. Commands admitted through T appear exactly once; later/unadmitted transport data remains outside the snapshot. Persist tick/rate/phase-plan version, budget remainders, scheduler sequence, admitted/deferred work keys, activation timestamps, and RNG. Save must not run hooks or consume RNG (Spec 20 lines 159–176, 522–526). Request-outcome retention/replay remains #96's decision.

## Precise amendments and remaining decisions

Amend Spec 01 §§5/8/9; Specs 04/09 execution keys/hooks; Spec 12 and Spec 26 §7.1 activation/defer and interval; Spec 14 §8/§18 environment/catch-up; Spec 17 §11 equal-due/nested semantics; Spec 20 save metadata; Spec 23 §9.4 trace fields; and Spec 25 §7 candidate selection. Update #52/#57/#58/#90 and #64/#65 only after agreement.

Using PlayerId order as execution order is simple but breaks causality; preserving one admission order through execution complicates AI/due work; a profile-only plan loses a clear transport contract. The layered plan best balances these. Remaining decisions are intake quotas/fairness, activation preflight/defer, and #96 outcome semantics; none should be silently selected.
