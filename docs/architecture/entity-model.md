# Entity model migration boundary

## Direction

The intended authoritative runtime model is Capsicum entities composed from components, with simulation behaviour owned by systems/services. Entity identity and lifecycle, player control, position, combat capability, definition references, and presentation projection should be explicit domain/component contracts; actors should not participate by inheriting from `GameObject`, `Mobile`, `Player`, `BaseCreature`, or `BaseNpc`.

This is the architectural destination, not a claim that the current compiled runtime has already completed the migration. On `experimental` at the start of issue #53, the core project still compiles `Framework.Mobile.Mobile<T>`, `Player`, `BaseCreature`, and `BaseNpc`. Multiple production APIs depend on those types, so deleting the hierarchy in this cleanup would require inventing or partially implementing the successor contracts owned by #55/#56/#57/#58/#60/#63.

## Inventory and disposition

| Source/type | Status on `experimental` | Cleanup disposition |
| --- | --- | --- |
| `OctoGhast.DataStructures.Entity.GameObject` (`Entity/GameObject.cs`) | Empty namespace shell, explicitly compiled | Removed; no behavior or callers. |
| `OctoGhast.Entity.Mobile` (`Entity/Mobile.cs`) | Empty namespace shell, explicitly compiled | Removed; no behavior or callers. |
| `OctoGhast.Actor.Mobile` (`Actor/Mobile.cs`) | Empty namespace, not compiled by the main project | Removed. |
| `OctoGhast.Actor.Player`, `IPlayer`, `IWeapon`, `Dagger` (`Actor/Player.cs`) | Uncompiled prototype with console combat and a dependency on the retired `IMobile` surface | Removed after repository-wide reference audit; demo combat behavior was not ported. |
| `OctoGhast.Framework.Mobile.Mobile<T>`, `Player`, `BaseCreature`, `BaseNpc` (`Framework/Mobile/Mobile.cs`) | Compiled legacy hierarchy with live production consumers | Retained as a migration dependency; additions to this inheritance tree are blocked by an architecture test. |
| `PlayerData`, `CreatureData` (`Framework/Mobile/Mobile.cs`) | Mutable/template data coupled to the retained hierarchy | Retained until #63 and #55 define and implement replacement contracts. |
| `RLObject<T>` and template/runtime data (`Entity/Item/RLObject.cs`) | Shared item and actor data/identity machinery | Not removed or redefined here; its item consumers and #63 boundary make wholesale retirement unsafe in #53. |
| Capsicum components and activities (`Components/*`, `Activities/*`) | ECS-oriented source exists, but the reviewed legacy project files do not compile these directories into the core runtime | Not promoted to authoritative runtime by this cleanup. Their conventions and integration belong to #55 and related implementation work. |
| `CameraExtensions.BindTo(Mobile)` | Commented-out method in an otherwise empty compiled class | File and compile entry removed; client camera must follow projected presentation state. |
| Direct `MainGame` movement/player construction, old `World` actor movement/FOV, `GameMapControl` player rendering | Commented remnants only | Removed; no behavior was live. Future movement input and projection remain separate command/client work. |

## Live dependencies that prevent full retirement

The core activity manager exposes `BaseCreature` as activity owner. Item-use abstractions, delegates, Cataclysm use actions, corpse handling, creature inventory extensions and some item APIs accept or construct `Player`/`BaseCreature`. `Mobile<T>` also implements `IScheduleable`, while `SchedulingSystem` currently schedules that interface. These are live signatures/behaviours in compiled projects, not empty remnants.

Replacing those references safely requires compiled entity/component identity and lifecycle conventions (#55), command/event contracts (#56), scheduler ownership (#57), position/index mutation (#58), player-control boundaries (#60), and definition/runtime state separation (#63). Until those concrete destinations are present and adopted, the legacy hierarchy remains transitional runtime code. Do not close #53 as fully retired on the strength of shell cleanup alone.

## Guardrail

`LegacyActorInheritanceTests` reflects over the compiled Core and Cataclysm production assemblies and allows only the existing `Player`, `BaseCreature`, and `BaseNpc` descendants of `Framework.Mobile`. New production subclasses of that legacy hierarchy fail the test. It intentionally does not assert that the ECS migration is complete; the remaining allowed classes and their consumers are tracked as migration debt above.
