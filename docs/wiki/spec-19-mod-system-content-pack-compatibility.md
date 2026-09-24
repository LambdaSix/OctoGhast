# Spec 19 — Mod system and content-pack compatibility

Status: investigation complete / implementation specification  
Parent: [#65](https://github.com/LambdaSix/OctoGhast/issues/65)  
Ticket: [#84](https://github.com/LambdaSix/OctoGhast/issues/84)  
Prerequisite: [Spec 18 / #83](spec-18-data-loading-ids-registries.md)  
Reference implementation: `LambdaSix/Cataclysm-DDA`  
Pinned parity baseline: `e262adb299a7613b4aedc5f12c08fe0413c56a84`

## Purpose

This page defines the implementation contract for discovering, describing, selecting, ordering, loading, persisting and diagnosing Cataclysm-style content packs ("mods") in OctoGhast.

The pinned Cataclysm:DDA baseline is the behavioural/content reference. OctoGhast does **not** reproduce the upstream UI or filesystem-centric ownership model literally. The authoritative server owns a world's simulation content set in both single-player and cooperative multiplayer. The Godot client may present mod-selection and diagnostics, but it cannot mutate loaded registries or world mod state directly.

This spec consumes Spec 18's loader/registry contract rather than redefining it. In particular:

- typed string IDs are durable identity;
- immutable definitions/registries are server-authoritative;
- core content loads before active mod layers;
- later permitted definitions may replace earlier definitions with the same typed ID;
- provenance is retained;
- finalization/validation is explicit;
- registry objects and compact indexes do not cross persistence or transport boundaries.

## 1. Authoritative pinned-baseline evidence

Primary source evidence inspected at the pinned commit:

- [`src/mod_manager.h`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/mod_manager.h) — `MOD_INFORMATION`, durable `mod_id`, active-list persistence API, metadata fields, core/obsolete semantics.
- [`src/mod_manager.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/mod_manager.cpp) — recursive discovery, metadata loading, duplicate IDs, default mods, world `mods.json`, missing-mod migration/removal, usable-mod filtering.
- [`src/mod_manager_ui.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/mod_manager_ui.cpp) — add/remove/reorder behaviour, conflict checks, dependency insertion, dependent removal.
- [`src/dependency_tree.h`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/dependency_tree.h) and [`src/dependency_tree.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/dependency_tree.cpp) — missing-dependency errors, transitive closure, valid dependency order, strongly-connected/cycle detection and inherited errors.
- [`src/init.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/init.cpp) — ordinary JSON loading excludes `mod_interactions`; conditional interaction files load after ordinary mod content and only for active associated mod IDs; content cannot be loaded after finalization.
- [`src/worldfactory.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/worldfactory.cpp) — world ownership of `active_mod_order`, world save integration and conflict validation on new-world creation.
- [`src/path_info.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/path_info.cpp) — bundled data/mod and user-mod roots plus default-mod configuration paths.
- [`doc/MODDING.md`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/MODDING.md) — user-facing `MOD_INFO` schema, categories, third-party locations, version being informational only, blacklist/whitelist examples and unsupported hard-coded extension caveats.
- [`doc/MOD_COMPATIBILITY.md`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/MOD_COMPATIBILITY.md) — `mod_interactions` ordering, case-sensitive associated-mod directory naming and single-associated-mod limitation.
- [`doc/IN_REPO_MODS.md`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/IN_REPO_MODS.md) — in-repository mod categories/maintenance expectations and obsoletion policy.
- [`data/core/mod_migrations.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/core/mod_migrations.json) — concrete removed-mod migration records.
- [`tools/load_all_mods.sh`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tools/load_all_mods.sh) and [`build-scripts/get_all_mods.py`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/build-scripts/get_all_mods.py) — bundled-mod load validation, dependency expansion, conflict-aware compatible sets and explicit total-conversion separation in test grouping.
- [`tests/worldfactory_test.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/worldfactory_test.cpp) — limited direct worldfactory coverage. The pinned baseline has little focused unit coverage for mod-manager edge cases, so source/docs and load-all-mod validation are important evidence.

Representative pinned data fixtures include:

- [`data/mods/dda/modinfo.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/mods/dda/modinfo.json) — core mod and `path` redirect into base JSON.
- [`data/mods/Magiclysm/modinfo.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/mods/Magiclysm/modinfo.json) — `MOD_INFO` mixed with other content objects in the same file.
- [`data/mods/aftershock_exoplanet/modinfo.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/mods/aftershock_exoplanet/modinfo.json) — total conversion, dependencies, conflicts and loading-image metadata.
- [`data/mods/No_Hope/modinfo.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/mods/No_Hope/modinfo.json) — informational `version` and conflicts.
- [`data/mods/CrazyCataclysm/modinfo.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/mods/CrazyCataclysm/modinfo.json) — `MONSTER_WHITELIST` combined with normal mod metadata.
- [`data/mods/TEST_DATA/modinfo.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/mods/TEST_DATA/modinfo.json) — obsolete metadata used for testing rather than new-world selection.
- [`data/mods/default.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/mods/default.json) — developer default list encoded as an obsolete pseudo-mod whose dependencies are the defaults.

## 2. Reference model versus OctoGhast platform model

### 2.1 Pinned CDDA reference behaviour

CDDA discovers mod metadata from bundled and user/private mod roots, builds a dependency graph, allows the player to maintain an ordered world mod list, loads content in that order, then finalizes/validates the global definition set before gameplay. The world stores the active mod IDs in `mods.json`.

The upstream application is single-process and its mod-management flow is local-UI-centric. Missing world mods can cause an interactive prompt during load. There is no multiplayer authority boundary to consider.

### 2.2 OctoGhast Cataclysm-profile adaptation

OctoGhast MUST keep the same logical mod/content semantics while changing ownership:

- the **authoritative server/world** owns the active simulation content manifest;
- single-player uses the same server-owned manifest through in-process transport;
- clients submit semantic world-configuration intents before world activation; they do not mutate registries;
- a connected client cannot independently enable/disable/reorder simulation mods;
- mod selection never creates a client-local simulation fork;
- a running world's finalized content set is immutable; changing it requires an explicit inactive-world reconfiguration/reload workflow;
- missing-mod resolution is a structured world-load/configuration result, not a modal prompt inside simulation code;
- no mod-management UI pauses canonical simulation time for an already-running multiplayer world;
- transport/session state and Godot object identity are not persisted as part of the mod set.

### 2.3 Generic Core contract

Generic Core needs a ruleset-agnostic content-package capability:

```text
ContentPackageId
ContentPackageMetadata
ContentSource / provenance
PackageDiscovery
PackageDependencyGraph
WorldContentManifest (ordered package IDs)
PackageLoadPlan
ContentFingerprint (diagnostic/negotiation aid)
```

Cataclysm-specific concepts such as `MOD_INFO` field names, CDDA categories, `mod_interactions`, `MONSTER_BLACKLIST` and the special `dda` core package belong in the Cataclysm profile.

### 2.4 Future evolution seams

Core MUST NOT permanently assume:

- packages are stored as directories of JSON;
- package metadata is named `modinfo.json`;
- one package can only interact conditionally with one other package at a time;
- package versions are informational forever;
- every future ruleset has exactly one "core" package;
- content-package changes can never be hot-applied in any future ruleset.

The Cataclysm profile, however, MUST implement the pinned behaviour in this spec until deliberately changed.

## 3. Immutable definition data versus mutable world state

### 3.1 Immutable package/definition data

After discovery and validation, the following are immutable for a loaded content generation:

- package metadata;
- resolved package source/provenance;
- dependency/conflict declarations;
- category and core/obsolete flags;
- package file/resource inventory;
- loaded definition registries produced by Spec 18;
- the resolved load plan;
- any content fingerprint/hash used for diagnostics.

The active simulation MUST NOT mutate these structures during canonical ticks.

### 3.2 Mutable persistent world configuration

Each world persistently owns:

```text
WorldContentManifest
  OrderedPackageIds : [ContentPackageId]
```

For the Cataclysm profile, the durable identity of an entry is the case-sensitive CDDA mod ID string.

OctoGhast MAY additionally persist non-authoritative diagnostic metadata such as last-seen package version/content digest, but these MUST NOT replace the mod ID as identity and MUST NOT silently become a compatibility gate. The pinned baseline persists IDs only.

### 3.3 Runtime state created by mod content

Runtime entities, variables, quests, map state, items, NPCs, EOCs and other mutable data created by definitions are **not** part of the package metadata model. They are owned/persisted by their respective specs. A package ID may be retained as provenance where useful, but a filesystem path or registry index is never runtime persistent identity.

## 4. Discovery and source precedence

### 4.1 Pinned CDDA behaviour

The mod manager recursively searches for files named `modinfo.json` in:

1. the bundled/data mod root;
2. the user/private mod root.

It then optionally loads developer/user default-mod metadata files.

At the pinned baseline:

- bundled mods are discovered before user mods;
- a duplicate mod ID is diagnosed and the later duplicate is discarded rather than shadowing the first;
- a missing discovery root is tolerated; the user mod directory is created if absent;
- metadata files may be a single object or an array;
- non-`MOD_INFO` objects in a metadata file are ignored by the metadata scan;
- ordinary mod JSON content is later loaded recursively from the package's selected content path.

### 4.2 OctoGhast contract

The Cataclysm profile MUST provide two logical package source classes:

1. **Bundled** — packaged with the selected Cataclysm content baseline.
2. **User/private** — administrator/user-installed packages.

Physical platform paths are owned by Spec 24 (#89); this spec defines only logical roots and precedence.

Discovery MUST be deterministic and independent of host filesystem enumeration order. Within a logical root, OctoGhast MUST canonicalize discovered metadata resources (for example normalized relative path ordinal ordering) before metadata registration.

Duplicate `ContentPackageId` across any roots MUST:

- produce a source-aware diagnostic naming both candidates;
- keep exactly one deterministic effective registration;
- for Cataclysm parity, preserve **first discovered source precedence**, with bundled sources searched before user/private sources;
- never silently merge two packages with the same ID.

A future package policy may support explicit replacement/overlay packages, but duplicate identity is not that mechanism.

## 5. `MOD_INFO` metadata schema

### 5.1 Required fields

A Cataclysm package metadata object is identified by:

```json
{
  "type": "MOD_INFO",
  "id": "stable_mod_id",
  "name": "Display name"
}
```

Required semantics:

- `type` MUST equal `MOD_INFO`;
- `id` is the durable, case-sensitive package identity;
- `id` MUST be unique across discovered packages;
- `id` MUST NOT contain `#` because the baseline reserves `base#associated` source notation for interaction content;
- `name` is human-facing metadata and not identity.

### 5.2 Optional fields

The pinned fields to support are:

| Field | Type | Default | Contract |
|---|---|---:|---|
| `authors` | array of strings | empty | informational |
| `maintainers` | array of strings | empty | informational/diagnostic |
| `description` | translated string | empty | informational |
| `version` | string | empty | **informational only** at the pinned baseline |
| `category` | string | empty/no category | known values map to Cataclysm UI grouping |
| `dependencies` | array of mod IDs | empty | required predecessors |
| `conflicts` | array of mod IDs | empty | incompatible packages |
| `core` | bool | false | core package, loaded before normal packages |
| `obsolete` | bool | false | usable for legacy worlds but hidden from normal new-world selection |
| `path` | relative path | metadata directory | package content root relative to metadata file |
| `loading_images` | array of filenames | empty | presentation metadata only |
| `disable_other_loading_screens` | bool | false | presentation metadata only |

Unknown `category` values fall back to "no category" semantics; category is not a load-order or compatibility rule.

### 5.3 Category values

The pinned baseline recognizes:

- `total_conversion`
- `content`
- `items`
- `creatures`
- `misc_additions`
- `buildings`
- `vehicles`
- `rebalance`
- `magical`
- `item_exclude`
- `monster_exclude`
- `graphical`
- `accessibility`
- empty/no category

Category remains descriptive/presentation metadata. The runtime MUST NOT infer arbitrary compatibility from category. The upstream load-all-mod test helper elects not to group multiple total conversions together, but the game metadata model itself does not make `total_conversion` an automatic conflict.

### 5.4 Validation

Metadata validation MUST reject/diagnose:

- missing/invalid ID;
- duplicate ID;
- ID containing `#`;
- self-dependency;
- wrong JSON type for declared fields;
- invalid `path` escape outside the permitted package/root sandbox;
- dependency cycles/missing dependencies during graph resolution.

The path-sandbox requirement is an OctoGhast safety adaptation. CDDA accepts a relative path such as `../../json` for its own bundled `dda` package. The Cataclysm content adapter therefore needs an explicitly trusted bundled-source capability for that fixture while untrusted user/private packages MUST NOT escape allowed roots.

## 6. Dependency graph, load ordering and conflicts

### 6.1 Dependencies

Pinned behaviour:

- dependencies are package IDs that must load first;
- dependencies are transitive;
- adding a package through the UI automatically adds missing dependencies;
- the dependency graph returns dependencies in a valid predecessor-first order;
- missing dependencies make the dependent package unavailable;
- cycle detection uses strongly connected components and marks members of a cycle unavailable;
- errors inherited from dependencies make downstream packages unavailable.

OctoGhast MUST construct a deterministic dependency graph from immutable package metadata.

For a requested ordered set, the resolver MUST return either:

```text
ResolvedLoadPlan
  ordered unique package IDs
```

or a structured failure including at least:

```text
MissingDependency(package, missingId)
DependencyCycle([ids])
DependencyUnavailable(package, inheritedReason)
Conflict(packageA, packageB)
InvalidCoreSet(...)
```

No simulation RNG may participate in graph ordering.

### 6.2 Explicit user order and dependency constraints

The world manifest's explicit order is meaningful because later content can override earlier definitions.

Resolver invariants:

1. every dependency appears before its dependent;
2. each ID appears at most once;
3. core package(s) appear before normal packages;
4. otherwise, the user's valid relative order is preserved;
5. a reordering request that would put a dependent before its dependency is rejected or constrained rather than silently producing invalid order.

For the Cataclysm profile, one selected core package is supported for a world. The reference `dda` package is the normal core. The upstream UI replaces an existing core when a different core is selected. Third-party custom core packages are recognized metadata but are outside the initial OctoGhast compatibility guarantee.

### 6.3 Conflicts

Conflict checking is effectively symmetric at selection time: the baseline checks both the candidate's declared conflicts and conflicts declared by already active packages.

Therefore if either A declares B or B declares A, A+B is an invalid active set.

World creation/activation MUST fail validation while an unresolved conflict remains. There is no "last loaded wins" semantics for conflicts.

### 6.4 Removal

Removing a dependency from the configured set MUST also remove active transitive dependents or reject the operation with a result that identifies those dependents. The Cataclysm-profile configuration command SHOULD match baseline UX semantics by returning the deterministic cascade set and, once confirmed by the caller, applying the whole change atomically.

## 7. Core, obsolete and default packages

### 7.1 Core

A core package supplies the base definition set. In the pinned fixture, `dda` is `core: true` and redirects its content path to `data/json`.

Cataclysm-profile rules:

- the resolved load plan has exactly one core for ordinary gameplay;
- core content loads before supplemental mods;
- normal supplemental packages may depend on the core;
- a package declaring `core` is not automatically trusted as a server plugin or executable extension.

### 7.2 Obsolete

`obsolete: true` means:

- do not expose the package as a normal choice for new worlds;
- retain the ability to resolve it for legacy worlds if still installed;
- retain metadata/migration diagnostics;
- do not silently delete it from an existing world's manifest solely because it is obsolete.

The `TEST_DATA` package and default pseudo-mod demonstrate that "obsolete" can also be used as a visibility mechanism, so the authoritative semantic is availability for legacy/explicit use versus normal new-world discoverability, not "content is invalid."

### 7.3 Defaults

The baseline supports developer defaults and user defaults, with user defaults preferred when present. Defaults are represented by pseudo-`MOD_INFO` records whose dependencies are the actual default package list.

OctoGhast SHOULD expose defaults as configuration data rather than requiring the pseudo-mod representation internally.

Defaults:

- seed new-world configuration only;
- are not retroactively applied to existing worlds;
- do not override a persisted world manifest.

## 8. Content load phases and override precedence

The Cataclysm profile MUST use this phase ordering:

```text
discover package metadata
  -> validate package graph/configuration
  -> load core ordinary content
  -> load active supplemental ordinary content in resolved world order
  -> load applicable mod_interaction content
  -> resolve deferred definitions
  -> ordered finalization
  -> global validation
  -> freeze content generation
  -> activate world simulation
```

### 8.1 Ordinary content

For each active package, ordinary `.json` files under its content root are loaded recursively, excluding its `mod_interactions` subtree.

Spec 18's duplicate/override rules apply:

- later permitted definitions with the same typed ID replace earlier effective definitions;
- there remains one logical registry entry per typed ID;
- provenance records the effective source and override chain where practical.

Therefore the active package order is authoritative simulation configuration, not presentation sorting.

### 8.2 Conditional `mod_interactions`

Pinned behaviour:

- ordinary content from all active packages loads first;
- then each package's `mod_interactions` directory is examined;
- a first-level interaction directory is loaded only if its directory name exactly/case-sensitively matches an active mod ID;
- interaction files then load recursively;
- multi-associated paths such as `mod_interactions/A/B` are not a supported "A and B active" condition;
- source provenance is represented as `baseMod#associatedMod`, hence `#` is illegal in base IDs.

OctoGhast Cataclysm profile MUST preserve those semantics.

Core SHOULD model this generically as a conditional content-source predicate rather than hard-coding directory conventions.

### 8.3 Version field

Pinned CDDA explicitly documents `version` as informational and supplies no version constraint system.

Therefore the Cataclysm profile MUST NOT interpret a dependency such as "foo >= 2" or infer semver compatibility from `version`.

A future OctoGhast package format MAY add version/range constraints, but that is a separate extension and must not silently reinterpret pinned `MOD_INFO`.

## 9. Blacklists, whitelists and exclusion mods

"Blacklist/whitelist" behaviour in CDDA is not one generic mod-manager feature. It is implemented by domain JSON handlers loaded through the same package pipeline.

Pinned examples include:

- `MONSTER_BLACKLIST`
- `MONSTER_WHITELIST`
- `ITEM_BLACKLIST`
- `TRAIT_BLACKLIST`
- `SCENARIO_BLACKLIST`
- profession/shopkeeper blacklist/whitelist handlers
- region overlays that suppress map content

For monsters, the documented semantics include:

- blacklist by monster ID, species or category;
- a non-exclusive whitelist can exempt entries from blacklists;
- an exclusive whitelist can define the only allowed set and takes precedence over ordinary blacklists.

Implementation contract:

- the package system owns source/order/provenance only;
- each target domain owns the actual filter semantics and validation;
- order-dependent handlers MUST document their merge/precedence semantics in the owning feature spec;
- item/monster exclusion categories are UI metadata only and do not magically perform filtering.

The representative `CrazyCataclysm` whitelist fixture MUST be preserved as a compatibility test.

## 10. World persistence, migration and reload

### 10.1 Pinned CDDA persistence

The baseline stores an ordered array of mod IDs in:

```text
<world>/mods.json
```

On load:

- the prior in-memory list is cleared;
- IDs are read in stored order;
- duplicate IDs in the file are ignored after the first occurrence;
- missing IDs are checked against mod migrations/removals;
- a migration may replace an old ID with a new ID;
- a removed/missing mod can be removed from the world list through an interactive decision;
- a cancelled decision aborts world load;
- if migration/removal changes the list, the updated list is persisted.

### 10.2 OctoGhast authoritative-server adaptation

Spec 20 remains authoritative for save barriers and world persistence ownership. This spec adds the world content manifest as required persistent configuration.

World activation MUST run a deterministic pre-simulation content-resolution phase.

Automatic actions allowed without user/admin input:

- de-duplicate repeated IDs while retaining first-order position;
- apply an unambiguous registered `old_id -> new_id` migration when the replacement is not already present;
- emit diagnostics recording the migration.

Actions that require an explicit configuration decision:

- remove an unknown package with no migration;
- remove a package with a documented removal reason;
- select among conflicting replacements;
- change core package;
- change active package order.

In a headless or multiplayer host, unresolved decisions MUST fail world activation with a structured `WorldContentResolutionRequired` result. Simulation MUST NOT start in a partially loaded state.

The Godot single-player client may render a prompt for that result and resubmit an explicit configuration command; the server remains the owner of the mutation.

### 10.3 Content-set drift

The pinned baseline can load a world against changed files under the same mod IDs.

OctoGhast preserves stable ID resolution and records the frozen content-generation identity/compatibility metadata required by Specs 18/20. A diagnostic fingerprint is one possible part of that evidence, not a replacement for compatibility policy.

- Report changed generation/fingerprint in world-load diagnostics.
- A mismatch alone does not prove incompatibility: a declared profile compatibility rule may accept the change, or an explicit migration may transform saved state.
- A materially changed definition under the same ID MUST NOT be silently accepted as the same continuation. With no declared compatibility or migration decision, stop activation with a structured compatibility/resolution-required result, as Spec 18 BND-08 requires.
- This is an intentional OctoGhast reproducibility/continuation safeguard. The pinned CDDA changed-files behaviour above remains reference evidence; it does not authorize silent reinterpretation in OctoGhast.

This introduces no package-version solver and does not require exact byte equality for compatible content.

## 11. Runtime lifecycle and concurrency

### 11.1 Content-generation state machine

```text
Undiscovered
  -> Discovered
  -> Configured
  -> Loading
  -> Finalizing
  -> Validated/Frozen
  -> WorldActive
```

A reset/unload returns to a pre-load state only when the world is not actively simulating.

### 11.2 No live simulation-mod mutation

While `WorldActive`:

- enable/disable/reorder simulation package commands MUST be rejected with a deterministic "content set locked" result;
- no client, including the local single-player client, may mutate registries;
- disconnect/reconnect does not change the world content manifest;
- the mod manager does not consume canonical ticks, action budget or gameplay RNG.

This is an OctoGhast authority/concurrency rule consistent with the baseline's "cannot load after finalization" invariant.

### 11.3 Multiple clients

All connected players share the same authoritative world content set.

Player A opening a mod/configuration UI:

- does not pause the world;
- does not alter Player B's simulation;
- cannot stage a private server-side definition graph.

Administrative workflows for changing the manifest MUST require the world to transition out of active simulation under server policy.

## 12. Client/server and projection contract

The network layer (#90) owns framing/session/backpressure. This spec defines only package-domain messages/queries required by that layer.

Minimum semantic operations:

```text
QueryAvailableContentPackages
QueryWorldContentManifest(worldId)
ValidateWorldContentManifest(candidateOrderedIds)
ConfigureInactiveWorldContentManifest(worldId, candidateOrderedIds)
QueryContentDiagnostics(worldId/contentGeneration)
```

Minimum projected data:

```text
ContentPackageSummary
  id
  displayName
  description
  category
  informationalVersion
  core
  obsolete
  dependencyIds
  conflictIds

WorldContentManifestProjection
  orderedPackageIds
  contentGeneration/fingerprint (diagnostic)
```

Do not project:

- absolute server filesystem paths;
- registry object references;
- compact registry indexes as durable IDs;
- arbitrary package file contents merely because a client is connected.

Clients do **not** participate in authoritative dependency resolution.

Presentation packs/assets are a separate concern for Specs 21/22 (#86/#87). A Godot client may render a fallback for an unknown mod-defined visual asset while still consuming authoritative projected game state.

## 13. Compatibility envelope

### 13.1 Core data compatibility target

**Target: pinned-baseline Cataclysm core data compatibility.**

For the reference-parity milestone, the Cataclysm profile must be able to load/finalize/validate the pinned core data set using the data handlers covered by the feature-spec programme. Unsupported content types remain explicit parity gaps until their owning feature lands.

"Core compatible" means semantic compatibility with the pinned definitions and loader rules, not binary compatibility with CDDA C++ objects.

### 13.2 Bundled CDDA mod target

**Target: all bundled, non-obsolete mods present at the pinned baseline are in-scope compatibility fixtures.**

The pinned tree contains 46 `data/mods/*/modinfo.json` packages. The programme SHOULD mirror CDDA's `load_all_mods.sh` strategy: test compatible sets that collectively cover every bundled non-obsolete mod plus their interaction directories.

A bundled mod may initially be reported as blocked by a known unimplemented feature-domain handler, but the final reference-parity milestone requires every bundled in-scope package to load and its exercised mechanics to conform to the relevant feature specs.

Obsolete bundled packages are migration/legacy fixtures, not required new-world choices.

### 13.3 Third-party/private mod target

**Target: data-only third-party mods written against the pinned Cataclysm JSON/package contract are supported to the extent they use content types/extension points implemented by the Cataclysm profile.**

Guaranteed package-level compatibility includes:

- `MOD_INFO` discovery/metadata;
- dependencies/conflicts;
- ordinary content layering/override;
- supported `mod_interactions`;
- world mod-list persistence/migration;
- source-aware diagnostics.

Not guaranteed:

- private C++ patches or hard-coded source changes;
- undocumented dependence on undefined filesystem enumeration order;
- custom executable/native code injection;
- a custom `core: true` replacement profile in the initial milestone;
- JSON content types not yet implemented by the relevant OctoGhast feature spec;
- newer upstream schema/features introduced after the pinned commit.

An unsupported extension MUST fail or diagnose explicitly. It must never be silently ignored when doing so could change gameplay semantics.

### 13.4 Existing-save compatibility

Direct loading of upstream CDDA save files is **not** established by this spec. Spec 20 owns save-format migration policy.

This spec requires only that OctoGhast worlds persist their ordered mod IDs and can apply Cataclysm-style mod-ID migrations/removals during OctoGhast world reload.

## 14. Unsupported extension and failure behaviour

Required structured diagnostics include:

| Condition | Result |
|---|---|
| duplicate package ID | diagnostic; deterministic first registration retained |
| illegal `#` in Cataclysm mod ID | package metadata invalid |
| metadata parse/type error | package invalid with source/member diagnostic |
| missing dependency | package unavailable; dependent unavailable |
| dependency cycle | all cycle members unavailable; dependents inherit unavailability |
| conflict in active set | manifest invalid; world cannot activate |
| self-dependency | metadata error |
| unsupported content JSON type | load/finalization failure naming package/resource/type |
| invalid cross-reference after override | validation failure via Spec 18 |
| unknown persisted mod ID | migration lookup, else resolution-required failure |
| removed persisted mod | resolution-required diagnostic containing removal reason |
| obsolete mod in existing world | allowed to resolve if installed |
| obsolete mod for new world | hidden/not normally selectable |
| invalid package content path | package rejected; no sandbox escape |
| attempt to change mods while active | deterministic content-set-locked rejection |
| malformed client configure request | reject request; no registry/world mutation |

Diagnostics SHOULD include package ID, source class, logical resource path, load-order position and underlying Spec 18 content diagnostic.

## 15. Determinism and RNG

The package subsystem MUST be deterministic for a fixed:

- discovered package set;
- metadata contents;
- ordered world manifest;
- Cataclysm profile/version;
- platform-independent logical path normalization.

Dependency resolution, conflict validation, override precedence, migration and final content fingerprint MUST NOT use gameplay RNG.

Any random choice among loading-screen images is presentation-only and MUST use client/presentation randomness, never authoritative simulation RNG.

Content definitions may themselves configure gameplay systems that use RNG; those systems own their stream/order contracts.

## 16. Ownership and stable identity summary

| Concept | Owner | Stable identity | Persisted? | Client authority? |
|---|---|---|---|---|
| package metadata | server content subsystem | `ContentPackageId` / Cataclysm mod ID | package install, not world mutable state | no |
| package source | server host/config | logical source + package ID | optional config | no |
| world content manifest | authoritative world | ordered package IDs | **yes** | request only while world inactive |
| definition registry | authoritative server content generation | typed string IDs | regenerated from content; references persist by string ID | no |
| registry compact index | server process generation | generation-local integer | no | no |
| content-generation identity / compatibility metadata | server content generation | versioned identity interpreted by profile policy | required in world save per Specs 18/20; a diagnostic fingerprint may supplement it | no |
| presentation asset pack | client/product surface | asset-pack ID | client config | local presentation only |

Filesystem paths, sockets, connection IDs, ECS entity IDs and Godot node IDs are never package identity.

## 17. Dependencies and contracts with other OctoGhast systems

- **Spec 18 / #83** — owns JSON dispatch, typed IDs, inheritance, override insertion, finalization, validation and source provenance.
- **Spec 20 / #85** — owns atomic/quiescent world persistence. The ordered world content manifest is part of server-owned world configuration.
- **Spec 17 / #82** and gameplay feature specs — own semantics of mod-defined EOCs/content types once loaded.
- **Spec 12 / #77** — active regions do not affect package selection; content is world-global definition state, not per-reality-bubble state.
- **#90** — transport/session design must carry semantic package queries/configuration results without exposing filesystems or allowing async callbacks to mutate content/world state.
- **#86** — owns mod-selection/diagnostic UI flows; UI consumes server queries and sends configuration intents.
- **#87** — owns tileset/sound/localization asset-pack compatibility and fallback presentation.
- **#88** — parity harness should implement the mod-set matrix and differential/load-all-mod fixtures defined below.
- **#89** — owns platform-specific physical resource/user-data paths and packaging layout.

## 18. Black-box/conformance scenarios

These are implementation acceptance scenarios, not suggestions.

### Discovery and metadata

1. **MOD-01 — bundled discovery:** place two valid `modinfo.json` files under the bundled logical root; both are discovered with stable IDs.
2. **MOD-02 — user discovery:** a valid user/private package is discovered without changing bundled package registration.
3. **MOD-03 — missing user root:** discovery succeeds with no packages from the absent root and creates/initializes the user location according to platform policy.
4. **MOD-04 — duplicate ID across roots:** bundled and user packages declare the same ID; a diagnostic identifies both, bundled/first registration remains effective.
5. **MOD-05 — mixed metadata file:** load the pinned Magiclysm `modinfo.json`; metadata discovery reads `MOD_INFO` and ignores non-`MOD_INFO` objects during metadata scan, while normal content loading later sees supported objects.
6. **MOD-06 — illegal ID:** an ID containing `#` is rejected.
7. **MOD-07 — path redirect:** the pinned `dda` metadata resolves its trusted bundled `../../json` content path to core JSON while an equivalent escape from an untrusted user package is rejected.
8. **MOD-08 — unknown category:** package remains valid and receives no-category presentation semantics.
9. **MOD-09 — informational version:** changing only `version` does not alter dependency resolution or reject an existing world.

### Dependencies, order and conflicts

10. **DEP-01 — transitive insertion:** A depends on B, B depends on core; selecting A resolves `core, B, A`.
11. **DEP-02 — missing dependency:** A depends on absent B; A is unavailable and world activation fails if A is configured.
12. **DEP-03 — cycle:** A depends B and B depends A; both are unavailable and diagnostics identify the cycle.
13. **DEP-04 — inherited failure:** C depends on A from DEP-03; C is unavailable because its dependency is unavailable.
14. **DEP-05 — no duplicates:** selecting two packages that share a dependency produces one dependency entry.
15. **DEP-06 — constrained reorder:** user attempts to move dependent before dependency; command is rejected/constrained and effective order remains valid.
16. **DEP-07 — symmetric conflict:** only A declares B as conflict; configuring B then A or A then B fails equivalently.
17. **DEP-08 — dependent removal:** removing B from `core,B,A` reports/removes A in the same atomic reconfiguration.
18. **DEP-09 — core uniqueness:** configuring two core packages is invalid for the Cataclysm profile.

### Loading and overrides

19. **LOAD-01 — ordered override:** core defines typed ID X=v1, mod A defines X=v2, mod B defines X=v3; manifest `core,A,B` yields one X with v3 and provenance chain core->A->B.
20. **LOAD-02 — order swap:** valid manifest `core,B,A` yields A's X, proving explicit mod order is semantic.
21. **LOAD-03 — finalization freeze:** after validated/frozen state, an attempt to load another content source fails without partial registry mutation.
22. **LOAD-04 — unsupported JSON type:** package metadata is valid but contains unknown gameplay content type; world activation fails with source/type diagnostic.
23. **LOAD-05 — strict final validation:** mod override leaves an invalid cross-reference; load fails during finalization/validation, before simulation starts.

### Conditional interactions and filters

24. **INT-01 — inactive interaction:** base package has `mod_interactions/other/foo.json`; `other` inactive => file not loaded.
25. **INT-02 — active interaction:** same fixture with `other` active => file loads after all ordinary package content.
26. **INT-03 — case sensitive:** active ID `other` does not activate directory `Other`.
27. **INT-04 — source provenance:** interaction definition provenance is represented logically as `base#other`.
28. **INT-05 — single-association limitation:** nested/multi-mod condition is not interpreted as an "all active" expression.
29. **FILTER-01 — whitelist fixture:** pinned Crazy Cataclysm `MONSTER_WHITELIST` is loaded through the monster-domain filter handler and can exempt listed monsters according to that domain's contract.

### Persistence, migration and reload

30. **SAVE-01 — round trip:** save a world with ordered `[dda, magiclysm, crazy_cataclysm]`; reload preserves the same ordered unique IDs.
31. **SAVE-02 — duplicate persisted IDs:** persisted duplicate is de-duplicated retaining first occurrence and diagnosed/normalized.
32. **SAVE-03 — mod migration:** old ID with valid `new_id` migration is replaced deterministically and persisted.
33. **SAVE-04 — already-present migration target:** old ID maps to an ID already active; resolver does not create a duplicate and reports required normalization.
34. **SAVE-05 — removed mod:** persisted removed ID produces a structured resolution-required result with removal reason; no world tick occurs.
35. **SAVE-06 — unknown missing mod:** unresolved ID produces a structured resolution-required result; headless host does not silently delete it.
36. **SAVE-07 — obsolete legacy package:** existing world can load an installed obsolete package; new-world package query marks it non-selectable.
37. **SAVE-08 — defaults:** changing configured default packages affects a new world but not an existing world's persisted manifest.
38. **SAVE-09 — content-generation drift:** same IDs with changed package bytes retain stable ID lookup and emit mismatch diagnostics. A declared compatible change may load; a material definition change without compatibility/migration stops activation before any world tick. Verify both branches against Spec 18 BND-08 and Spec 20 P20-39.

### Authoritative server/co-op

39. **NET-01 — single-player equivalence:** in-process client and loopback/network client receive the same available-package/world-manifest query semantics.
40. **NET-02 — no client mutation:** client submits a candidate manifest; server validates and commits only while world inactive. Client-side object mutation has no effect.
41. **NET-03 — active-world lock:** with world simulation active, two clients concurrently request different mod changes; both receive content-set-locked (or server-policy equivalent) and registries remain unchanged.
42. **NET-04 — reconnect:** disconnect/reconnect of any player leaves world manifest/content generation unchanged.
43. **NET-05 — projection privacy:** package summaries expose logical metadata but no absolute server filesystem paths.
44. **NET-06 — UI non-pause:** one player opens package/config UI while a multiplayer world is running; canonical time continues and no content change occurs.

### Bundled compatibility matrix

45. **BUNDLE-01 — core fixture:** pinned `dda` package loads/finalizes as the selected core.
46. **BUNDLE-02 — dependency/conflict fixture:** pinned Aftershock: Exoplanet metadata resolves its dependencies and rejects a declared conflicting combination.
47. **BUNDLE-03 — informational-version fixture:** pinned No Hope metadata loads without treating `3.5` as a solver constraint.
48. **BUNDLE-04 — obsolete fixture:** pinned TEST_DATA is hidden from ordinary new-world selection but remains explicitly loadable for test/legacy purposes.
49. **BUNDLE-05 — all-mod coverage:** generate conflict-compatible package sets analogous to pinned `get_all_mods.py`; collectively cover every non-obsolete bundled mod and active `mod_interactions` pair, load/finalize/validate each set.
50. **BUNDLE-06 — third-party data-only fixture:** install a synthetic private mod using only pinned-supported JSON types; it discovers, resolves dependencies, overrides a definition, survives world reload and does not require server code changes.

## 19. Implementation checklist

A #84 implementation is not complete until all of the following are true:

- package discovery is deterministic and source-aware;
- `MOD_INFO` metadata and Cataclysm categories/defaults/core/obsolete semantics are represented;
- dependency graph and conflict validation are deterministic and structured;
- world manifest order drives content-source precedence;
- conditional `mod_interactions` semantics match the pinned baseline;
- world mod IDs persist and migration/removal resolution is explicit;
- definitions freeze before active simulation;
- server authority prevents client/local UI from mutating content state;
- diagnostics identify package/resource/load-order provenance;
- unsupported content/extension points fail explicitly;
- the compatibility envelope in section 13 is enforced;
- scenarios MOD-01 through BUNDLE-06 are automated at the appropriate unit/contract/scenario layers;
- #88's parity matrix can record per-bundled-mod load/feature status against this pinned baseline.

## 20. Deliberate non-goals

This specification does not:

- implement gameplay/runtime code;
- define a new mod scripting language;
- add native/plugin code execution;
- require direct compatibility with future CDDA HEAD;
- define tileset/soundpack/localization compatibility (Spec 22 / #87);
- define platform packaging/search paths in OS-specific detail (Spec 24 / #89);
- define direct upstream CDDA save-file compatibility (Spec 20 / #85);
- turn CDDA's informational `version` field into a hidden semver contract.

No unresolved cross-cutting architecture decision was found during this investigation. The server-owned frozen content-generation model follows #52/#57/#58/#65 and Spec 18/20 directly; future package-version negotiation or executable plugin support would be an explicit platform extension rather than a requirement for Cataclysm reference parity.

