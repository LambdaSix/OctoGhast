# Spec 18 — Data loading, typed IDs, registries, JSON inheritance and validation

Status: investigation complete / implementation specification  
Parent: [#65](https://github.com/LambdaSix/OctoGhast/issues/65)  
Ticket: [#83](https://github.com/LambdaSix/OctoGhast/issues/83)  
Reference implementation: `LambdaSix/Cataclysm-DDA`  
Pinned parity baseline: `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Purpose

This page records both the pinned Cataclysm:DDA loader/reference semantics and the OctoGhast architecture that hosts them. It is intentionally about externally observable data semantics, not a requirement to reproduce CDDA's C++ class layout.

The pinned compatibility evidence covers JSON type dispatch, typed IDs, registries, `copy-from`, abstract definitions, mutation operators, load ordering, deferred resolution, finalization, consistency checks, duplicate/override behavior, diagnostics, migrations, and mod precedence. The re-evaluated platform contract below deliberately keeps CDDA-specific vocabulary in the Cataclysm profile rather than making it universal Core behaviour.

## Authoritative evidence at the pinned baseline

The main evidence inspected for this specification is:

- `src/init.cpp`: `DynamicDataLoader`, type dispatch, path loading, deferred loading, ordered finalization and consistency verification.
- `src/generic_factory.h`: registry behavior, inheritance, abstract definitions, duplicate replacement, string/int ID conversion, cache invalidation, mandatory/optional member loading, and mutation operators.
- `src/string_id.h` and `src/int_id.h`: stable string identity versus session-local integer identity.
- `src/type_id.h`: concrete typed-ID aliases used across game domains.
- `src/mod_manager.cpp`: mod metadata, dependencies, mod migrations/removals and active mod order.
- `tests/generic_factory_test.cpp`: registry validity, overwrite behavior, reset invalidation, null IDs and cached ID behavior.
- `tests/json_test.cpp`: JSON deserialization and diagnostic behavior.

All source links below are pinned to the baseline commit so later upstream changes do not silently alter this contract.


## Foundational re-evaluation — Core infrastructure versus Cataclysm compatibility

This specification was re-evaluated on 2026-09-24 against #52, #57, #58, #64, #65, #84 and completed dependent architecture. The original pinned-CDDA investigation remains authoritative evidence; no contradiction requiring renewed upstream loader/source investigation was found.

The correction is one of **semantic ownership**. CDDA remains the reference profile and completeness benchmark, but its loader vocabulary is not the permanent definition of generic Core.

### A. Pinned CDDA loader/reference behaviour

The evidence sections below document what `LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84` does: JSON object/type dispatch, `string_id<T>` / `int_id<T>`, generic-factory replacement, `abstract`, `copy-from`, deferred inheritance, `relative`, `proportional`, `extend`, `delete`, ordered finalization, strict/unconsumed-member diagnostics, and core/mod load ordering.

These facts remain parity evidence. They do not imply that another OctoGhast profile must use JSON, inheritance, CDDA mutation operators, or CDDA duplicate precedence.

### B. Cataclysm-profile data compatibility contract

The Cataclysm profile owns the compatibility semantics required by the pinned milestone:

- CDDA JSON top-level object/array handling and `type` dispatch;
- CDDA schema shapes and member readers;
- `abstract` and exact `copy-from` lookup/deferred-resolution behaviour;
- `relative`, `proportional`, `extend`, `delete` and their pinned precedence/error rules;
- CDDA-specific duplicate-definition replacement and source-order policy;
- CDDA finalization sequence/quirks and strict-member compatibility;
- migration content records and other CDDA-specific content handlers;
- the pinned core/mod content-pack expectations defined jointly with Spec 19 / #84.

Full Cataclysm compatibility remains required for the pinned reference milestone. Moving these rules out of generic Core does not weaken their conformance requirement.

### C. Generic Core definition/registry/ID infrastructure

Generic Core owns reusable capabilities rather than Cataclysm vocabulary:

- stable typed/domain identifiers with durable representations independent of ephemeral registry indexes;
- definition catalogues/registries keyed by stable IDs;
- immutable frozen content generations used by a running authoritative world;
- content-generation identity suitable for save compatibility, protocol negotiation and diagnostics;
- source provenance expressed as logical source/package/resource identity, not mandatory filesystem paths;
- deterministic loading/build phases and dependency ordering;
- dependency-aware finalization and typed cross-reference resolution;
- validation/diagnostics with source-aware context;
- schema/profile/version extension points;
- deterministic override/merge hooks where a profile deliberately supplies such policy;
- invalidation of generation-local lookup caches/handles when a catalogue generation changes.

Core MUST NOT hard-code `copy-from`, CDDA mutation keywords, `MOD_INFO`, a global JSON `type` table, "later definition wins", filesystem traversal, or any particular CDDA finalization list as universal rules.

A profile may reject duplicates, merge definitions, use inheritance, use composition, or obtain definitions from JSON, YAML, binary, generated, networked or in-memory sources so long as it satisfies Core's deterministic catalogue/finalization/validation contract.

### D. Future schema/content-system seams

The platform must permit a non-CDDA definition profile to reuse Core identity/registry infrastructure without emulating CDDA inheritance:

- parsing/decoding is a profile/content-provider concern that produces typed definition candidates plus provenance;
- package/resource discovery is supplied by content providers; generic domain APIs consume logical resource identities/streams/documents rather than assuming host filesystem paths;
- override/merge policy is a profile-owned deterministic strategy;
- finalization dependencies are named/typed contracts rather than one hard-coded Cataclysm order;
- schema/version metadata may be interpreted by future profiles without changing stable-ID or registry primitives;
- immutable definition generations may be built from different source technologies while runtime systems consume the same frozen catalogue interfaces.

### Immutable definitions versus mutable runtime state

Definitions/templates and their provenance belong to a frozen content generation. Runtime ECS/world state references definitions by stable typed ID and may use generation-local resolved handles internally, but gameplay MUST NOT mutate definition objects as runtime state.

Mutable values such as item damage, actor HP, mission progress, map state or activity progress belong to runtime/persistence models. Their durable definition references are stable typed IDs plus whatever world/profile content-generation identity is required to interpret those IDs correctly.

### World/profile content-generation identity and save compatibility

A world save MUST identify the rules/profile and frozen content generation against which durable definition IDs are interpreted. The exact fingerprint/manifest encoding is a shared persistence/content-package concern with Spec 19 (#84) and Spec 20 (#85); it is intentionally not invented locally here. The invariant is:

> A save may not silently reinterpret a stable definition ID against materially different content and call that the same continuation.

Load may proceed only when the selected profile declares the saved generation compatible, or after an explicit migration/compatibility decision. Session-local compact registry indexes are never sufficient save identity.

Transport projections likewise use stable typed IDs, plus profile/content-generation metadata where the protocol requires it, never server-process registry object identity or compact indexes.


## 1. Loader architecture

### 1.1 Input shape

A content file may contain either:

1. one JSON object, or
2. an array of JSON objects.

Any other top-level JSON value is an error.

Every content object handled by the dynamic loader has a string `type`. The loader looks that type up in a registered handler table. An unknown type is a JSON error at the `type` member.

For pinned compatibility, the Cataclysm profile MUST expose dispatch equivalent to:

```text
type string -> content loader
```

Within the Cataclysm profile, registration of a second handler for the same CDDA type MUST be diagnosed. Implementations may reject duplicate handler registration outright; silently replacing a handler is not parity-compatible. Generic Core is not required to expose this string-type dispatch model.

### 1.2 Loading phases

The observable phase model is:

```text
register loaders
    ↓
load core/content paths
    ↓
load active mods in resolved order
    ↓
load applicable mod-interaction content
    ↓
resolve deferred definitions during registry finalization
    ↓
ordered finalization / cross-link construction
    ↓
global consistency verification
    ↓
runtime use
```

Once global finalization has completed, additional content loading is not permitted without unloading/resetting the data model first.

OctoGhast SHOULD model this as an explicit loader state machine rather than a loose set of booleans:

```text
Empty -> Loading -> Finalizing -> Validated
  ^                                |
  +------------- Reset ------------+
```

Calls that violate the state machine MUST fail deterministically.

### 1.3 File ordering versus semantic ordering

CDDA recursively enumerates JSON files and dispatches objects as encountered, but the semantic contract must not depend on arbitrary filesystem enumeration for references that are designed to be deferred.

A loaded content folder is expected to be internally consistent with itself and all previously loaded folders. Later mods may depend on earlier content; earlier content must not depend on a later mod.

The Cataclysm profile MUST preserve **content-source order** (core, then active mods in dependency/load order), while allowing same-registry `copy-from` dependencies to be deferred until their base exists. Generic Core instead requires deterministic source/dependency ordering supplied by the active profile.

## 2. Typed IDs and identity

### 2.1 String IDs are the durable identity

A `string_id<T>` is a string identifier parameterized by domain type. IDs from different domains are not interchangeable even when their text is identical.

Required OctoGhast properties:

- equality is type-safe;
- the serialized/persistent identity is the string form;
- string IDs may survive reload/reset cycles;
- lookup of a missing ID is distinguishable from a valid ID;
- lookup failure is diagnosable;
- IDs are suitable for cross-reference fields before finalization.

A practical C# shape is a strongly typed value object or generic wrapper rather than passing raw strings throughout the runtime.

### 2.2 Integer IDs are session-local indexes

CDDA's `int_id<T>` is an efficient index into a finalized registry. Its meaning depends on the currently loaded content/mod set and therefore cannot safely persist across reloads.

OctoGhast MUST NOT serialize registry indexes as durable identity.

If OctoGhast introduces compact numeric IDs, they are caches only. They MUST be invalidated when a registry is reset or structurally mutated.

### 2.3 Registry generations

CDDA's generic factory maintains a modification/version count. A `string_id` can cache its resolved integer index together with that version. Any insertion or reset advances the version, invalidating previous cached hits **and cached misses**.

Parity requirement: a lookup that was missing before an insertion must be able to become valid immediately after insertion. Negative-result caching must therefore participate in registry generation invalidation.

### 2.4 Invalid lookup

A missing string ID:

- reports invalid from validity checks;
- converts to a caller-provided null integer ID when requested;
- emits a diagnostic when object access/conversion is requested in warning mode;
- must not accidentally resolve to another object.

OctoGhast MAY use exceptions, diagnostics plus a sentinel, or a Result type internally, but conformance tests must verify the same observable distinction between valid, null and missing references.

## 3. Registry semantics

A registry stores one runtime definition per typed string ID and supports:

- insert/load;
- string ID lookup;
- optional compact-ID lookup;
- validity checks;
- iteration over all real definitions;
- reset;
- finalization;
- consistency checking.

### 3.1 Duplicate IDs and override behavior — Cataclysm profile

In `generic_factory::insert`, loading an object whose ID already exists replaces the existing object **in the same registry slot**. It does not append a second definition.

Before replacement CDDA calls its duplicate-entry tracking logic, so source provenance can decide whether a diagnostic is appropriate.

Cataclysm-profile compatibility contract:

- the latest permitted CDDA definition for an ID is the effective definition;
- overriding must preserve one logical registry identity;
- provenance (source/core/mod/file where practical) MUST be retained;
- suspicious duplicates MUST be diagnosable;
- replacement MUST invalidate cached resolutions.

This replacement behavior is what makes later Cataclysm content/mod layers able to override earlier definitions. Generic Core exposes deterministic conflict/override policy hooks but does not universally select replacement or "later wins" semantics.

### 3.2 Abstract definitions

A generic-factory JSON definition may use `abstract` instead of `id`.

Abstract definitions:

- can be targets of `copy-from`;
- are held separately from real runtime definitions;
- are not returned as ordinary real registry entries;
- are cleared after factory finalization;
- cannot specify both `abstract` and the registry's real ID member.

Specifying both is an error.

### 3.3 Multiple IDs

A generic factory can accept an ID member that is either a string or an array. With an array, the definition is loaded once per listed ID into distinct real registry identities.

## 4. `copy-from` inheritance — pinned CDDA / Cataclysm profile

### 4.1 Base lookup

When a definition contains:

```json
{ "id": "child", "copy-from": "base" }
```

the loader resolves `base` first against real definitions, then against abstract definitions in the same registry/domain.

If found, the base object becomes the initial state of the child before the child's members and mutation operators are applied.

Types may provide specialized inheritance handling; otherwise ordinary value copying is used.

### 4.2 Deferred inheritance

If the requested base does not yet exist, the object is deferred rather than immediately rejected.

During factory finalization, deferred JSON is retried. Retry continues while progress is made.

If a complete retry pass resolves nothing, the remaining objects form an unresolved/circular dependency set. Each is diagnosed as a circular dependency and discarded.

The Cataclysm profile MUST implement equivalent fixed-point behavior:

```text
pending = unresolved definitions
repeat:
    resolved_this_pass = 0
    retry every pending definition
    remove successful definitions
until pending empty OR resolved_this_pass == 0
if pending remains:
    diagnose every remaining definition as unresolved/circular
```

The diagnostic SHOULD distinguish "base never existed" from a true cycle where possible, but both must fail deterministically.

## 5. Member loading and inheritance mutation — pinned CDDA / Cataclysm profile

CDDA's generic member loaders use a `was_loaded` concept: after `copy-from`, the child starts with inherited values. Missing members therefore retain inherited values rather than receiving new defaults.

### 5.1 Direct member value has precedence

For an optional member, the effective precedence is:

1. if the normal member is present, read it as a replacement;
2. otherwise, if supported and present, apply `proportional`;
3. otherwise, if supported and present, apply `relative`;
4. if this is not inherited and none of the above supplied a value, apply the declared/default value;
5. after that, apply `extend`;
6. after that, apply `delete`.

Thus a direct member suppresses `relative` and `proportional` for that member, while `extend` and `delete` are still processed afterwards by the generic optional loader.

### 5.2 `relative`

Example:

```json
{
  "id": "child",
  "copy-from": "base",
  "relative": { "weight": 5 }
}
```

For supported values, the relative value is added to the inherited value.

Conceptually:

```text
child.weight = base.weight + 5
```

Relative mutation is only valid for member types that support the required additive operation or a specialized reader. Unsupported use is diagnosed.

### 5.3 `proportional`

Example:

```json
{
  "id": "child",
  "copy-from": "base",
  "proportional": { "weight": 1.5 }
}
```

For ordinary supported numeric/unit-like values:

```text
child.weight = base.weight * 1.5
```

The generic implementation requires a numeric scalar greater than zero and rejects `1` as an invalid/no-op scalar. Types may provide specialized proportional handling.

Unsupported proportional use is diagnosed.

### 5.4 `extend`

`extend` mutates an inherited container/member after ordinary loading. Generic typed readers add the specified values to the inherited collection; specialized types may provide their own extension behavior.

Example:

```json
{
  "id": "child",
  "copy-from": "base",
  "extend": { "flags": [ "EXTRA_FLAG" ] }
}
```

The resulting collection contains the inherited entries plus the extension, subject to the collection's duplicate rules.

### 5.5 `delete`

`delete` is processed after `extend` and removes requested entries from the resulting collection/member.

Example:

```json
{
  "id": "child",
  "copy-from": "base",
  "delete": { "flags": [ "OLD_FLAG" ] }
}
```

Attempting to delete a value that is not present is diagnosable in the generic handlers.

### 5.6 Mutation without inheritance

For generic optional members, using `relative`, `proportional`, `extend` or `delete` when the object has no inherited base is diagnosed as "no copy-from" usage. OctoGhast SHOULD reject or warn consistently rather than silently treating these as ordinary assignment syntax.

### 5.7 Mandatory members

Generic mandatory members do not accept the four inheritance mutation features. Missing mandatory data is an error for a fresh object. For an inherited object, a missing mandatory member is permitted because the base has already supplied it.

## 6. Mod and core precedence — Cataclysm profile / Spec 19

The world stores an explicit active mod order. Mod metadata includes dependencies and conflicts, and the dependency tree is built from declared dependencies.

The Cataclysm-profile content loading rule to preserve is:

- core/base content is loaded before dependent mod content;
- active mods are loaded in resolved world order;
- later definitions can replace earlier registry definitions with the same ID;
- a mod may depend on content from earlier/core sources;
- earlier sources must not depend on later mods;
- mod-interaction files are conditional on the associated mod being active.

For Cataclysm compatibility, provenance MUST expose at least:

```text
source/package id (core/mod)
logical resource identity (and file path when the provider is filesystem-backed)
load-order position
```

Generic Core requires logical provenance but does not require a filesystem. Cataclysm's content provider may additionally retain normalized paths for parity diagnostics and Spec 19 package handling.

## 7. Finalization and cross-reference resolution

Parsing is not the end of loading. CDDA has an explicit ordered finalization list spanning flags, body parts, items, requirements, vehicle parts, terrain, overmap data, recipes, monsters, factions, professions, mutations and many other domains.

Important consequences:

1. a definition may be syntactically loaded before all referenced definitions are finalized;
2. derived caches and compact IDs are constructed during finalization;
3. finalization order is a dependency contract;
4. consistency checking occurs after finalization unless verification is explicitly skipped.

OctoGhast SHOULD represent finalizers as named dependency-aware stages rather than one monolithic method. The minimum API should support:

```text
Load -> ResolveDeferred -> Finalize(stage order) -> Validate
```

Cross-registry references SHOULD remain typed string IDs during parse and resolve to runtime handles/indexes only when the referenced registry is ready.

A missing cross-reference discovered during finalization/validation MUST produce a source-aware diagnostic.

## 8. Validation and diagnostics — Core mechanism, profile policy

### 8.1 JSON structure diagnostics

Required failures include:

- top-level value is neither object nor array;
- content object has no recognized `type`;
- required member missing;
- member exists but has an invalid JSON type/value;
- real ID and `abstract` both supplied;
- unsupported inheritance mutation;
- invalid proportional scalar;
- unresolved/circular `copy-from`;
- invalid cross-reference discovered during finalization/checking.

Diagnostics SHOULD carry:

- source/mod ID;
- file/resource path;
- JSON member where available;
- content type;
- content ID/abstract ID where known;
- actionable message.

### 8.2 Unconsumed members

CDDA's JSON object machinery tracks visited members and reports unvisited members unless the loader explicitly permits omissions. This catches misspelled/unsupported properties.

The Cataclysm profile SHOULD provide equivalent strict-member validation. Generic Core supplies structured validation/diagnostic plumbing but does not require every future schema to use CDDA's visited-member model.

### 8.3 Duplicate type-handler registration

Within the Cataclysm profile, registering a second handler for the same CDDA `type` is a diagnostic. Generic Core does not require a universal string `type` dispatch table.

### 8.4 Consistency pass

After ordered finalization, a global consistency pass validates domain-specific invariants and references. OctoGhast MUST retain a distinct validation phase so "parsed successfully" never implies "content graph is valid."

## 9. Migration and obsoletion — profile/domain policy

Migration is not one universal registry feature in the baseline; multiple domains register explicit migration content types, including examples such as item `MIGRATION`, traits, bionics, proficiencies, fields, terrain/furniture, traps, vehicle parts, effects, spells, overmap terrain and mods.

The Cataclysm adapter must support migration definitions as first-class CDDA content handlers, while migration semantics remain domain-owned. Generic Core needs migration/compatibility extension points, not knowledge of CDDA migration object types.

For mods specifically:

- a `mod_migration` maps an old mod ID to a new ID, or records a removal reason;
- when a world's active mod list contains a missing ID, the migration may replace it;
- removed/missing mods require an explicit resolution path rather than silently disappearing.

OctoGhast design rule: the loader provides the mechanism (typed handler, source order, diagnostics); each domain owns the meaning and application of its migration records.

## 10. Recommended Core/profile seams

The following is a behavioural shape, not a required class layout.

```text
// Generic Core
DefinitionId<TDomain>              // stable durable identity
DefinitionGenerationId            // frozen content-generation identity

IDefinitionCatalogue<TDefinition, TId>
  TryGet(TId)
  IsValid(TId)
  Generation
  EnumerateDefinitions()

IDefinitionBuildPipeline
  AddSource(IContentSource)
  Build(profile)
    -> DecodeCandidates
    -> ApplyProfileConflictMergePolicy
    -> ResolveDependencies
    -> Finalize
    -> Validate
    -> Freeze

IContentSource
  SourceIdentity
  EnumerateLogicalResources()

DefinitionProvenance
  SourceIdentity
  LogicalResourceIdentity
  PackageIdentity?
  ProfileMetadata?

IDefinitionProfile
  ProfileIdentity
  SchemaVersion?
  Decode(resource, diagnostics)
  ConflictMergePolicy
  FinalizationPlan
  Validate(...)
  CompatibilityPolicy(...)

// Cataclysm profile adapter
CataclysmJsonProfile : IDefinitionProfile
  JSON type dispatch
  abstract/copy-from
  relative/proportional/extend/delete
  pinned duplicate/override policy
  CDDA finalization/validation compatibility
```

Strongly typed IDs should be cheap value types. Registry indexes/handles are generation-local implementation details and must never cross save, reload or transport boundaries as durable identity. Generic Core APIs should accept logical source/resource abstractions; a filesystem-backed provider is one implementation rather than a domain assumption.

## 11. Conformance fixture suite

The following original black-box fixtures verify the pinned Cataclysm compatibility layer; the BND scenarios below verify that these semantics do not leak into universal Core.

| ID | Fixture | Expected result |
|---|---|---|
| DL-01 | top-level object with registered type | handler invoked once |
| DL-02 | top-level array with two objects | handlers invoked in array order |
| DL-03 | scalar top-level JSON | load error |
| DL-04 | unknown `type` | member-localized load error |
| DL-05 | duplicate type-handler registration | diagnostic/rejection |
| ID-01 | same text in two typed ID domains | IDs are not interchangeable |
| ID-02 | missing string ID | invalid; no accidental object |
| ID-03 | cached miss then insert same ID | subsequent lookup succeeds |
| ID-04 | compact ID then registry reset | old compact ID is invalid |
| REG-01 | duplicate real ID later in load order | later definition is effective, one logical entry |
| REG-02 | reset registry | all prior IDs/handles invalidated |
| INH-01 | child copies earlier real base | child begins with base values |
| INH-02 | child copies abstract base | child resolves; abstract is not a real entry |
| INH-03 | object has both `id` and `abstract` | load error |
| INH-04 | child appears before base in same load set | deferred child resolves at finalization |
| INH-05 | A copies B and B copies A | both diagnosed/discarded |
| INH-06 | child copies nonexistent base | unresolved definition fails deterministically |
| MUT-01 | inherited scalar + `relative` | inherited value plus delta |
| MUT-02 | inherited scalar + `proportional` | inherited value multiplied by scalar |
| MUT-03 | proportional scalar <= 0 | diagnostic/no mutation |
| MUT-04 | proportional scalar == 1 | diagnostic/no mutation |
| MUT-05 | inherited collection + `extend` | entries added |
| MUT-06 | inherited collection + `delete` | entries removed |
| MUT-07 | same member in `extend` and `delete` | extension occurs first, deletion second |
| MUT-08 | direct member plus `relative` | direct member wins; relative not applied |
| MUT-09 | direct member plus `extend` | direct replacement then extension |
| MUT-10 | mutation syntax without `copy-from` | diagnostic |
| VAL-01 | missing mandatory fresh member | load error |
| VAL-02 | inherited object omits mandatory member | inherited value retained |
| VAL-03 | unknown/unconsumed property | strict-member diagnostic |
| FIN-01 | cross-reference becomes valid before validation | validation succeeds |
| FIN-02 | cross-reference remains missing | source-aware validation error |
| MOD-01 | core ID then mod overrides same ID | mod definition effective |
| MOD-02 | mod A depends on core/A-earlier content | references resolve |
| MOD-03 | earlier source references later mod-only content | validation fails |
| MIG-01 | registered migration content type | migration handler receives record |


### Core/profile boundary scenarios added by #83 re-evaluation

| ID | Fixture | Expected result |
|---|---|---|
| BND-01 | Cataclysm profile loads a base/child fixture using `copy-from`, `relative`, `extend` and `delete` | Effective result matches pinned CDDA semantics; no generic Core API needs knowledge of those keywords |
| BND-02 | Minimal non-CDDA profile supplies two typed definitions directly with no inheritance vocabulary | Core catalogue builds, finalizes, validates and freezes them successfully without `copy-from`, `abstract` or mutation operators |
| BND-03 | Persist runtime object referencing definition `item/test`, reload same compatible content generation, then project over transport | Stable typed ID round-trips unchanged; no compact registry index or server object identity appears in durable/wire identity |
| BND-04 | Attempt to mutate a definition after its content generation is frozen | Mutation is rejected/not observable; runtime state changes occur only on runtime instances |
| BND-05 | Build the same candidate set/provenance/dependency graph twice with identical profile configuration | Final catalogue, diagnostics ordering and finalization outcome are deterministic |
| BND-06 | Two profiles receive duplicate ID candidates: Cataclysm profile and a strict non-CDDA profile | Cataclysm applies pinned later-permitted replacement policy; strict profile deterministically rejects duplicates; Core supports both without hard-coded universal precedence |
| BND-07 | Filesystem-backed and in-memory providers emit equivalent logical resources | Domain registry/finalization behaviour is identical apart from provenance details; Core domain APIs do not require filesystem paths |
| BND-08 | Save declares content generation A but server attempts load against materially different generation B with no declared compatibility/migration | Load produces an explicit compatibility decision/failure; IDs are not silently reinterpreted |
| BND-09 | Cataclysm world manifest from Spec 19 resolves core + mods in pinned order | Resulting candidate precedence follows Cataclysm profile/package policy, not a universal Core "mods later win" rule |
| BND-10 | Cross-reference target is supplied by another registry finalized in a declared dependency stage | Deterministic finalization resolves it; an undeclared/missing dependency produces a source-aware validation error |

### Golden inheritance fixture

A compact fixture should exercise precedence in one definition:

```json
[
  {
    "type": "TEST_DEF",
    "abstract": "base",
    "power": 10,
    "flags": [ "A", "B" ]
  },
  {
    "type": "TEST_DEF",
    "id": "child",
    "copy-from": "base",
    "relative": { "power": 5 },
    "extend": { "flags": [ "C" ] },
    "delete": { "flags": [ "B" ] }
  }
]
```

Expected effective child:

```json
{
  "id": "child",
  "power": 15,
  "flags": [ "A", "C" ]
}
```

The test should assert semantics, not collection iteration order unless the domain itself promises ordering.

## 12. Implementation slices

A dependency-friendly implementation sequence that preserves the boundary is:

**Generic Core first**

1. stable typed/domain ID primitives and explicit missing-reference results;
2. catalogue/registry storage plus generation-local cache invalidation;
3. logical source/provenance abstractions with no required filesystem path;
4. deterministic build lifecycle and named dependency/finalization stages;
5. immutable/frozen content-generation identity;
6. typed cross-registry reference validation and structured diagnostics;
7. profile extension points for decoding, conflict/merge policy, schema/version and compatibility;
8. save/transport contracts that expose stable IDs rather than compact handles.

**Cataclysm profile on top**

9. JSON parser facade with pinned strict-member behaviour;
10. CDDA `type` dispatch;
11. abstract + `copy-from` inheritance and deferred fixed-point resolution;
12. optional/mandatory CDDA member helpers plus `relative`, `proportional`, `extend`, `delete`;
13. CDDA duplicate/override and finalization-order compatibility;
14. Spec 19 package/mod load-plan integration and migration handler plumbing;
15. pinned fixture/golden conformance suite plus BND boundary scenarios above.

This sequence is architectural decomposition, not an instruction to implement the loader in #83.

## 13. Acceptance criteria mapping for #83

- **Loading phases and ordering guarantees explicit:** sections 1, 6 and 7.
- **ID type, registry, lookup, invalid/missing ID and lifetime semantics:** sections 2 and 3.
- **JSON inheritance/mutation operations with precedence examples:** sections 4 and 5.
- **Deferred resolution/finalization and cross-reference validation:** sections 4.2 and 7.
- **Duplicate/override/error behavior and diagnostics:** sections 3.1 and 8.
- **Obsoletion/migration behavior mapped:** section 9.
- **Mod/core precedence and dependency inputs:** section 6.
- **Schema/fixture conformance suite:** section 11.

## 14. Findings that constrain later specs

Later feature specs should consume the following shared rules without rediscovering them.

### Generic Core rules

- persistent and protocol-visible definition references use stable typed/domain IDs, never session-local registry indexes;
- a running authoritative world consumes an immutable frozen content generation;
- definition generation identity participates in save/profile compatibility;
- finalization and validation are distinct from decoding/parsing;
- provenance is logical source/resource/package context; filesystem paths are optional provider metadata;
- deterministic dependency ordering, finalization and diagnostics are platform requirements;
- duplicate/merge/override behaviour is supplied by a profile policy rather than universally fixed by Core.

### Cataclysm-profile compatibility rules

- same-registry `copy-from` may resolve a base loaded later, provided deferred resolution succeeds before final validation;
- `abstract` definitions are Cataclysm inheritance templates, not runtime entities;
- later permitted CDDA definitions replace earlier definitions with the same ID according to the pinned source/package order;
- direct member assignment takes precedence over `relative`/`proportional`;
- `extend` is applied before `delete`;
- CDDA strict-member/type-dispatch and migration behaviour remain compatibility requirements.

### Runtime-state rule

Definitions/templates are immutable inputs. Mutable ECS/world state belongs to runtime entities/components and persists separately, carrying stable definition references where required.

## Pinned source links

- [DynamicDataLoader / init.cpp](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/init.cpp)
- [generic_factory.h](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/generic_factory.h)
- [string_id.h](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/string_id.h)
- [int_id.h](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/int_id.h)
- [type_id.h](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/type_id.h)
- [mod_manager.cpp](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/mod_manager.cpp)
- [generic_factory_test.cpp](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/generic_factory_test.cpp)
- [json_test.cpp](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/json_test.cpp)
