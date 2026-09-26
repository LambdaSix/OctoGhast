# Entity runtime authority

This page records the entity-model boundary enforced by issue #53.

## Authority

The authoritative runtime model is composition-based: Capsicum ECS entities plus components, with systems/services owning simulation behaviour. A client/presentation object is a projection and is never the owner of world state.

The following are distinct concepts:

- durable entity identity;
- actor/character capability represented by components or relations;
- player ownership/control represented by a relation or control component;
- connection/session identity;
- presentation identity and camera state.

None of these concepts requires a CLR subtype named `GameObject`, `Mobile`, or `Player`.

## Removed legacy surface

The following files were removed from the compiled projects by issue #53:

- `OctoGhast.DataStructures/Entity/GameObject.cs`;
- `OctoGhast/Entity/Mobile.cs`;
- `OctoGhast/Entity/Player.cs`;
- `OctoGhast/Renderer/CameraExtensions.cs`.

The old player weapon/demo combat implementation was not ported. Combat and control behaviour belong to the ECS/domain systems specified by the relevant feature tickets.

## Presentation boundary

The map view consumes a projected `PlayerPosition` value. It no longer references `IPlayer`, `IMobile`, or a mutable actor object, and the camera is updated from the projected position by the presentation setup. Input handlers must eventually submit commands through the authoritative command boundary; issue #53 deliberately removes the old direct `MoveTo` path without adding replacement gameplay.

## Remaining work

The detailed ECS entity/component contracts, stable identity, lifecycle, control relations, and simulation systems remain owned by #55 and the subsequent movement/control/combat tickets. Any temporary adapter introduced during that work must name its successor and be removed before the movement vertical slice is complete.

## Guardrails

Production code must not add inheritance from or construction of the retired OO types. Systems select entities by component/capability and stable identity. Rendering and input consume projections and requests, not authoritative actor instances.
