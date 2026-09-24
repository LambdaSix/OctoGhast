# Production networking core — superseded prospective design

Status: **superseded by implementation-ready specification**

The prospective design recorded here was completed through #90 and promoted to:

- [Spec 25 — Authoritative server, transport, session and projection boundary](./spec-25-authoritative-server-networking-core.md)

Spec 25 resolves transport/session/simulation/projection layering, deterministic intake, identity separation, lifecycle state, framing/versioning, bounded queues, backpressure, initial TCP transport, projection security, in-process/loopback equivalence, reconnect and persistence exclusions.

The one deliberately separate cross-cutting concern is production authentication/public-server security, now owned by #92. Cross-world profile/meta-progression identity remains owned by #91.

This file is retained only so existing links to the former prospective design continue to resolve.
