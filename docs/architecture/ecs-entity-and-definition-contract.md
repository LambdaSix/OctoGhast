# ECS entity and immutable definition contract

This contract applies to the authoritative Capsicum-based simulation. Capsicum is the storage/query mechanism; it is not the domain identity, persistence, scheduling, or authority boundary.

## Entity and component conventions

- A Capsicum `Entity` is a composition of `IComponent` values. It has no gameplay inheritance tree and is not a service locator.
- Every durable authoritative entity has one `EntityIdentityComponent` containing a stable `EntityId`. `EntityId` is a non-empty Guid value serialized canonically as 32 hexadecimal digits (`N` format). It is distinct from Capsicum's pool-local `CreationIndex`, a socket/session ID, an `AccountId`/`PlayerId`, a Godot object ID, and any derived spatial index key.
- Identity is assigned by the authoritative world identity allocator, retained across save/load and component changes, and retired only when the domain object is permanently destroyed. The allocator's continuation state/order is part of world persistence; it must not consume gameplay RNG. Capsicum may release or recycle its in-memory `Entity` independently.
- Components describe domain state or capability. Empty/tag components express role or capability (`PlayerActorComponent`); state components contain a coherent domain-owned state bundle. Avoid one component per primitive field and avoid copying immutable type data into every entity.
- Simulation systems own rules and mutate authoritative components. Presentation-only state belongs to client projection/presentation code and must not be used as gameplay input or persisted as world authority.
- ECS-wide enumeration is not a substitute for a purpose-built spatial index. Spatial membership, position changes, projection/publication, and other derived indexes must be changed through the owning world mutation boundary so state and indexes remain coherent.

## Systems and services

Capsicum `EntitySystem` offers `Process(Entity)` and `CanProcess`; it does not define authoritative phase ordering or a multi-component query plan. The server/world host owns system registration, explicit query composition, phase order, stable entity order, and the authoritative update boundary. A system's eligibility predicate must be a deterministic function of authoritative state, not wall-clock time, task completion, or collection/hash iteration order.

Systems receive required services through constructor injection of narrow interfaces (for example, a spatial query interface or chronology capability). Do not resolve services from an entity, static singleton, renderer, transport connection, or a generic `World.Instance` service locator. World/server composition supplies those services. The host follows the selected rules profile's versioned phase plan; Core does not invent Cataclysm phase order.

When a system mutates a component that participates in a projection or derived index, it must use the authoritative mutation API that updates that index and publishes the change at the owning deterministic boundary. Direct component mutation is reserved for that authoritative simulation path. Client commands are requests and never mutate shared ECS instances.

## Definition references versus runtime state

`DefinitionId` is a value identity of the form `domain::value`, compared ordinally and case-sensitively. It is durable and independent of any registry array/index. Generic Core provides `IContentDefinition`, a build-time `DefinitionRegistryBuilder<T>`, and a read-only `DefinitionCatalog<T>` tied to a caller-supplied stable content-generation ID.

Registry `Add` rejects duplicates. `Replace` is explicit and only represents an override after the content profile has resolved its policy. Core does not impose CDDA `copy-from`, mod order, or later-definition-wins. A catalog freezes membership and ID-to-definition associations; definition implementations themselves must expose immutable/read-only value graphs. Rebuilds create a new content generation rather than mutating a generation already used by an authoritative world.

Authoritative entities store a `DefinitionReferenceComponent` containing the stable definition ID, not a copy of its template. For example, a zombie refers to `monster::zombie`; current health, effects, position, activity progress, and AI continuation are independent mutable runtime components. Definition values are read from the selected catalog. A definition lookup failure is explicit and diagnosable; no missing ID may silently resolve to a different definition.

Cataclysm JSON interpretation, type dispatch, inheritance, override precedence, and schema validation belong to `OctoGhast.Cataclysm`. The adapter builds typed definition candidates and calls Core registry APIs; Cataclysm concepts and JSON schema do not move into Core.

## Persistence contract

Persist stable `EntityId`, stable `DefinitionId`, and explicit mutable runtime component state through versioned domain codecs. Do not persist Capsicum object references, creation indexes, registry indexes, CLR assembly-qualified type names, static service references, presentation components, or transport state as domain identity/state. The world's versioned manifest records its selected rules/profile and compatible frozen content-generation identity. At load, resolve durable definition IDs against that generation (or run an explicit compatibility/migration decision); never reinterpret them silently against changed content.

Capsicum's bundled binary entity writer is not the authoritative world-save format: its records name components by CLR assembly-qualified name and do not carry the world/profile/content-generation or migration contract. It may be useful for local framework tests, but authoritative persistence must use explicit stable DTO/codecs.

## Compiled examples and tests

The compiled examples are `EntityIdentityComponent`, `DefinitionReferenceComponent`, and the generic `DefinitionRegistryBuilder` / `DefinitionCatalog` types. `OctoGhast.Core.Tests/DefinitionCatalogTests.cs` verifies typed stable identity, invalid IDs, explicit override, frozen catalogue lookup, and component references. Movement, control dispatch, and spatial-index behavior remain owned by their respective slices.
