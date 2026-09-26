# Issue 52 integration status

This branch is an implementation checkpoint for #52, not completion of the umbrella ticket. It integrates the independently reviewed foundations below on top of `experimental` at `363094c`.

## Integrated

- Removed zero-responsibility OO actor shells and dead commented movement/presentation hooks. The compiled `Framework.Mobile` hierarchy remains transitional because live item/activity APIs still consume it.
- Documented and guarded the current dependency graph while preserving the target Core / Cataclysm / server / client direction.
- Added durable `EntityId`, typed immutable definition references, a frozen definition catalogue, and Capsicum identity/reference components.
- Added rules-neutral `WorldPosition` / `SpatialCell` separation and a world-owned spatial index with atomic conditional spawn, move, and despawn operations.
- Added generic command lifecycle, bounded rotating player admission, synchronous queries, ordered bounded events, durable activity records, canonical fixed-step time, profile chronology/action-budget mappings, execution lanes, and deterministic scheduled work.
- Added a headless movement adapter slice: authoritative terrain/occupancy resolution, explicit bump requests, action-cost/scheduler seams, common AI/player command resolution, bounded in-process request/response queues, and visibility-filtered projection DTOs.

## Orchestrator review corrections

- Unified movement on the durable ECS `EntityId`, generic `StableSimulationId`, command lifecycle, and shared `WorldPosition`; no parallel identity or position model remains.
- Updated dependency allowlists after Capsicum/spatial foundations became explicit project references.
- Preserved host pacing fractions across time-scale changes and added stable actor/work execution keys from the final #56/#57 review.
- Made repeated deferral legal so a bounded command may remain pending across several intake steps.
- Added missing spatial/identity imports and `System.Numerics` references that would otherwise prevent compilation.
- Reject default/invalid player and entity identities at the movement transport boundary.

## Still required before #52 can close

- Replace the remaining compiled `Framework.Mobile` consumers and remove `Player`, `BaseCreature`, `BaseNpc`, and `Mobile<T>`.
- Extract the legacy hybrid `OctoGhast` assembly into enforceable headless Core, Cataclysm profile, server host, and client/protocol projects. The current project layout is still transitional.
- Add production Capsicum adapters connecting entity components to `WorldSpatialIndex`, including active-region activation/publication rules.
- Add the production scheduler/action-budget adapter and the complete pinned-CDDA movement formula/policies (diagonal movement, terrain/mode/effect/encumbrance modifiers, blocked attempts, and resolved bump action charging).
- Implement the production request/outcome persistence and reconnect recovery contract from #96.
- Implement and exercise the Spec 25 loopback transport: framing, fragmented/coalesced input, parser guards, lifecycle, malformed/oversized input, slow-client backpressure, and authoritative equivalence with in-process transport.
- Add the Godot 2D projection/interpolation proof without client ownership of simulation state.
- Run the full build and NUnit suite in an environment with the legacy .NET Framework/Mono toolchain. This execution environment has no `dotnet`, MSBuild, Mono, or C# compiler.

Keep #53 and #59–#62 open until their remaining production adapters and conformance scenarios land. The foundational contracts for #54–#58 and #63 are present in this checkpoint but should be closed only after CI/build verification succeeds.
