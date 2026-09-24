# Narrow evidence check: nested timed-event insertion

Reason: Spec 01 explicitly leaves same-pass insertion during `actualize` unpinned (scenario 8 and edge-case list). This is a concrete missing detail relevant to #95, not renewed general upstream investigation.

Source: https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/timed_event.cpp

At the pinned baseline, `process()` iterates the live events list, calls `per_turn()` before testing the deadline, invokes `actualize()` when due, then continues from `events.erase(it)`. `add()` uses `emplace_back`. Therefore an event appended during processing is reached in that same traversal, including one appended by the previously last element before its erasure. Its `per_turn()` runs and it actualizes in that pass if due. This is a code-derived conclusion, not an executed test result.

The generic queue is insertion ordered, not globally sorted by due time: every element receives per-turn processing. Do not flatten this manager into the recurring-EOC queue, whose Spec 17 contract separately uses due time and persisted schedule sequence. Preserve these as profile-owned queue semantics within the canonical phase plan.
