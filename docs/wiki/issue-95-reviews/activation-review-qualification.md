# Final resolution 2026-09-25

This qualification remains historical evidence. #95 has since adopted [same-point semantic suspension for finite dynamic activation dependencies](../architecture-canonical-ordering-admission-activation.md): preserve committed prefix/RNG state, suspend at the same semantic frontier, activate/catch up privately, publish atomically and resume exactly once. The earlier private transactional rollback/prepare-commit proposal remains rejected.

# Qualification to the activation review

The synthesis challenged the proposed private prepare/commit continuation for an EOC that has already performed effects before discovering an unloaded target. The activation reviewer confirmed that this is not an existing contract.

Spec 17 requires deterministic due processing, authoritative sequential effects and persistence of queued EOCs, but does not define transactional rollback of already-executed effects, an in-flight continuation across activation, or resumption after a global phase has closed. In particular, its runtime-error contract does not automatically roll back partial effects.

Consequently, read the activation report's dynamic-effect prepare/commit paragraph as an unaccepted option, not as the recommended implementation baseline. The synthesis retains mid-operation dynamic activation as a decision gate owned by #95. The known-footprint activation barrier, private staging versus published state, per-domain processing markers, fixed target boundaries and retention of already-committed shared activation remain separately useful recommendations.

No repository document or GitHub issue was changed during these reviews.
