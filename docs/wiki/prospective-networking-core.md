# Production networking core — superseded prospective design

Status: **superseded by implementation-ready specification**

The prospective design recorded here was completed through #90 and promoted to:

- [Spec 25 — Authoritative server, transport, session and projection boundary](./spec-25-authoritative-server-networking-core.md)

Spec 25 resolves transport/session/simulation/projection layering, deterministic intake, identity separation, lifecycle state, framing/versioning, bounded queues, backpressure, initial TCP transport, projection security, in-process/loopback equivalence, reconnect and persistence exclusions.

Production authentication/public-server security is now specified by [Spec 29](./spec-29-authentication-public-server-security.md) / #92. Server-scoped account identity and cross-world meta-progression are specified by [Spec 28](./spec-28-server-accounts-meta-progression.md) / #91.

This file is retained only so existing links to the former prospective design continue to resolve.
