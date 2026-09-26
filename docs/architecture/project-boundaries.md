# Project boundaries

Status: target dependency contract, audited against `experimental` at `363094c`.

This page separates the architecture we are building toward from the legacy project graph that currently builds. The `experimental` snapshot does **not** yet contain a pure `OctoGhast.Core` assembly: the project/assembly named `OctoGhast` is the old MonoGame game host and is a transitional hybrid, not a normative definition of Core. This distinction matters; an assembly name or namespace is not evidence that its code is ruleset-neutral.

The target contract follows #52 and the boundary clarifications in Spec 24 (platform/host), Spec 25 (authoritative server/transport/projection), Spec 27 (client presentation), and the post-spec architecture audit. Those specifications supersede historical assumptions in this branch where the old project layout conflicts with them. CDDA is the first rules profile, not the definition of the platform.

## Target responsibilities

| Layer | Owns | May depend on | Must not own or depend on |
| --- | --- | --- | --- |
| Generic Core | ECS/world primitives, generic schedules and chronology, spatial indexes/primitives, command and event infrastructure, activity lifecycle, persistence mechanics, transport-neutral interfaces and DTO contracts | BCL and lower-level rules-neutral libraries; explicit adapter interfaces | Cataclysm IDs, data schemas, formulas, action costs, grid ceilings; renderer, socket implementation, or Godot objects |
| Cataclysm profile | CDDA rules, components/systems, movement/combat/interactions, terrain/item semantics, legacy CDDA JSON interpretation and profile configuration | Generic Core interfaces and rules-neutral spatial/data facilities | Client UI/rendering, socket/session internals, or Core implementation details not exposed by an interface |
| Authoritative server host | World hosting, canonical simulation intake, session/account/player binding, transport adapters, projection construction, persistence coordination | Generic Core and selected profile registration; bounded server-only adapters behind interfaces | Client presentation state; direct asynchronous callback mutation of world/ECS state |
| Client/presentation | Input mapping, UI, camera, rendering, projection cache, interpolation and local presentation preferences | Transport-neutral protocol/projection DTOs and presentation asset interfaces | Authoritative ECS/world state, profile rule resolution, server internals, or gameplay mutations |
| Server-side Godot adapter (optional) | Bounded libgodot-backed services such as physics/navigation where selected | Explicit server adapter interfaces and Godot runtime | Durable domain identity/state; Godot nodes/RIDs as save identity; client/presentation code |

The normal dependency direction is:

```text
lower-level rules-neutral libraries <- Core <- Cataclysm profile
                                        ^             ^
                                        |             |
                                 server host ---------+

protocol/projection contracts -> server transport adapters -> client presentation
```

The arrows mean “may reference/implement”; they do not mean data ownership. In particular, shared request/result/projection DTOs are not shared ECS components. Single-player still uses the same server ingress and projection contract as co-op, with an in-process transport adapter.

The server composition root is allowed to select/register a rules profile. That does not permit the networking/transport Core to interpret Cataclysm concepts. A Godot client consumes player-authorized projections only. A dedicated server may host libgodot for bounded server APIs where needed, but such use is isolated behind server-side interfaces and does not move domain ownership into Godot.

## Shared contracts and ownership seams

Place a contract at the narrowest layer that owns its semantics:

- Core owns generic entity/component identity, world/time/scheduler interfaces, command admission/result envelopes, transport-neutral messaging, generic persistence hooks, and projection delivery mechanics.
- The Cataclysm profile owns its identifiers, templates/definitions, schemas, action costs, formulas, component meaning, and JSON interpretation. Its adapter translates profile definitions/requests to generic Core contracts.
- The server owns authoritative world selection, player/session/controlled-entity binding, deterministic request intake, world mutation orchestration, persistence quiescence, and per-player projection authorization.
- The client owns device input and rendering. Client coordinates/transforms are presentation values, never authoritative spatial state.
- Optional engine integrations are adapters. Godot node/RID identity, socket identity, renderer identity, and connection state are runtime infrastructure, not durable gameplay identity.

This keeps Cataclysm loaders from becoming a Core dependency merely because several profiles could use a data-loading abstraction. Generic catalogue/registry interfaces may live in Core; CDDA field names, inheritance/override policy and legacy JSON object models remain in the profile.

## Audit of the experimental project graph

The NUnit architecture test `ProjectBoundaryTests` is the executable allowlist for direct project references on this baseline. It checks every currently scoped project, rejects unreviewed references, and specifically prevents profile/client/server dependencies being added to the legacy UI or server edges without an explicit architectural change.

| Current project | Current direct project references | Assessment |
| --- | --- | --- |
| `OctoGhast` | `Capsicum`, `InfiniMap`, `RenderLike`, `OctoGhast.DataStructures`, `OctoGhast.MapGeneration`, `OctoGhast.Spatial`, `OctoGhast.UserInterface` | Legacy game host/hybrid. Capsicum now supplies the migration ECS, but its UI and map-generation dependencies prevent treating it as the target Core assembly. |
| `OctoGhast.Cataclysm` | `InfiniMap`, `OctoGhast.Spatial`, `OctoGhast` | Profile-on-host dependency is transitional. It permits reuse of old host primitives, but the host is not a clean Core seam. |
| `OctoGhast.Server` | `OctoGhast` | Legacy shell only; it is not yet the target authoritative server host or networking core. |
| `OctoGhast.UserInterface` | `RenderLike`, `OctoGhast.DataStructures`, `OctoGhast.Spatial` | Presentation project currently avoids direct server/profile/host references. Keep it projection-facing as the client migration proceeds. |
| `OctoGhast.DataStructures` | `InfiniMap`, `OctoGhast.Spatial` | Lower-level data structures; no profile/client/server dependency. |
| `OctoGhast.MapGeneration` | `RenderLike`, `OctoGhast.DataStructures`, `OctoGhast.Spatial` | Legacy generation library. Treat its semantics as unclassified until the generation/profile split is explicit; do not make Core depend on profile policy through it. |
| `OctoGhast.Spatial` | none | Rules-neutral spatial primitives candidate. |
| `OctoGhast.Core.Tests` | `Capsicum`, `OctoGhast`, `OctoGhast.Spatial` | Tests the legacy host plus extracted rules-neutral foundations today, not proof of a separately extracted Core assembly. |
| `OctoGhast.Cataclysm.Tests` | `InfiniMap`, `OctoGhast.Cataclysm`, `OctoGhast` | Profile fixtures plus legacy host. |
| `OctoGhast.Shell.Win32` | `OctoGhast.Spatial`, `OctoGhast.UserInterface`, `OctoGhast` | Legacy graphical shell; not a server dependency. |

Other legacy projects (`CataSharp.Client`, `RenderLike`) are not target client/server/Core layers. Their dependencies remain outside the new contract until retired or explicitly migrated.

### Known domain leakage in the legacy host

At this audit point, `OctoGhast` contains CDDA-specific legacy-loader names/types in `CoreMaterials.cs`, `Framework/Material.cs`, `Framework/Data/Loading/BaseTemplateType.cs`, `Framework/Data/Loading/TemplateFactoryBase.cs`, and CDDA-specific unit/JSON conversion behavior in `UnitQuantity.cs` and `Framework/JsonDataLoader.cs`. `Chronology/Time.cs` also uses “Cataclysm” as a setting-era/calendar label. These are findings, not approved Core APIs. They must move behind Cataclysm-owned adapters/types or be replaced by generic contracts before the legacy host can be represented as Core. The guard does not pretend those migrations have happened.

There is no direct `ProjectReference` cycle in the current graph: Cataclysm references the host, while the host has no project reference to Cataclysm. However, the legacy host's source-level Cataclysm namespace usage is a domain leak and is independently called out above; project-graph acyclicity alone is insufficient proof of a clean seam.

## Enforced rules and change policy

`ProjectBoundaryTests` parses the actual `.csproj` files rather than relying on a hand-maintained diagram. It fails when a project gains an unreviewed direct project reference, when the legacy presentation project acquires server/profile/host dependencies, or when the current server project acquires presentation/profile dependencies. Intentional graph changes must update both this contract and the executable allowlist in the same change.

The test is a transition guard, not a claim that the legacy graph already satisfies the target architecture. When `OctoGhast.Core`, protocol contracts, profile composition, and the new client/server hosts are extracted, replace the transitional allowlist with the target matrix above. At that point add source/assembly-level checks that Core references no Cataclysm assemblies/namespaces and that client code cannot reach ECS mutation interfaces. Do not weaken the target rules to preserve old host convenience.

For the movement vertical slice, before implementation starts, agree these owners: the client emits a transport-neutral movement request; the server ingress validates identity/order and submits it at a deterministic simulation boundary; Cataclysm resolves grid movement and its action cost; Core commits generic position/index and schedule effects through explicit contracts; the server publishes authorized projection/result DTOs; client interpolation is cosmetic only. No layer above the profile owns the CDDA movement formula, and no client or transport adapter mutates authoritative state.
