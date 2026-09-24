# Spec 24 — Build, platform, packaging and runtime resource layout

## 1. Purpose and compatibility decision

This specification defines the implementation contract for building, testing, packaging, installing and locating runtime resources for OctoGhast.

The behavioural/build reference is the pinned Cataclysm:DDA baseline:

`LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`.

Pinned CDDA is authoritative evidence for the resource classes that must be present, platform-path edge cases, release validation and content/presentation packaging requirements. It is **not** a requirement to reproduce CDDA's CMake/Make/MSVC/Gradle build graph, curses/SDL frontend split, exact archive formats, directory names or launcher implementation.

OctoGhast applies the programme architecture from #52, #57, #58, #64 and #65:

1. **Generic Core** is a headless, renderer-independent simulation/platform library.
2. **Cataclysm profile** supplies pinned-CDDA rules, schemas and content adapters.
3. **Authoritative server** owns world state and may host `libgodot` through 2dog where bounded Godot server APIs (for example physics, navigation or pathing) are useful.
4. **Godot integration is contained behind explicit OctoGhast service interfaces**. Godot runtime objects, RIDs, nodes, caches and server-internal state are derived/runtime state rather than durable domain identity.
5. **Godot 2D client** owns presentation/input and consumes explicit player-specific projections.
6. **Single-player** is one authoritative server connected through the same logical request/projection boundary as co-op, using an in-process transport.
7. **2dog/libgodot hosting is the normative process model from project inception**, not a later migration from a Godot-owned application process.
8. **Build/package layout must not collapse authority boundaries** merely because client, server and libgodot may coexist in one desktop process.

The current repository's legacy .NET Framework 4.6.2/MonoGame/Win32-shell projects and `dmcs` Rake build are historical migration state. They are evidence about the repository's starting point, not normative platform requirements for the ECS/server/Godot architecture.

## 2. Reference evidence

All CDDA links in this section point at the exact pinned baseline.

### 2.1 Build variants and platform dependencies

- [`CMakeLists.txt`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/CMakeLists.txt) defines independent build switches for tiles, curses, sound, localization, tests, directory policy and release layout. The pinned build makes tiles and curses mutually exclusive; tiles uses SDL3 and related libraries; sound adds SDL3_mixer.
- [`Makefile`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/Makefile), [`CMakePresets.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/CMakePresets.json) and [`msvc-full-features/`](https://github.com/LambdaSix/Cataclysm-DDA/tree/e262adb299a7613b4aedc5f12c08fe0413c56a84/msvc-full-features) demonstrate that upstream supports multiple build front ends while retaining one gameplay implementation.
- [`doc/c++/COMPILING.md`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/c%2B%2B/COMPILING.md), [`COMPILING-CMAKE.md`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/c%2B%2B/COMPILING-CMAKE.md) and [`COMPILER_SUPPORT.md`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/c%2B%2B/COMPILER_SUPPORT.md) document compiler/platform expectations.
- [`.github/workflows/release.yml`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/.github/workflows/release.yml) builds distinct Windows, Linux, macOS and Android release artifacts, with graphical/sound/terminal variants where applicable.

Important reference lesson: **feature selection and distribution shape are build concerns; they do not redefine simulation semantics.**

### 2.2 Runtime path discovery

[`src/path_info.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/path_info.cpp) is authoritative for pinned CDDA path behaviour.

Pinned defaults include:

- Windows user root: `%LOCALAPPDATA%/cataclysm-dda/`;
- macOS user root: `$HOME/Library/Application Support/Cataclysm/`;
- XDG data root: `$XDG_DATA_HOME/cataclysm-dda/`, falling back to `$HOME/.local/share/cataclysm-dda/`;
- non-XDG Unix user root: `$HOME/.cataclysm-dda/`;
- XDG config root: `$XDG_CONFIG_HOME/cataclysm-dda/`, falling back to `$HOME/.config/cataclysm-dda/`;
- save, memorial and achievement subdirectories under writable user state;
- bundled data, graphics and localization roots derived from the install/base path.

The pinned command line can override base/user/data/save/config/memorial paths. Missing required data is fatal; failure to create required writable directories is fatal.

This establishes three important compatibility principles for OctoGhast:

1. installed immutable resources and writable user state are distinct;
2. launch-time path overrides are legitimate and useful for tests/portable/server operation;
3. startup must fail explicitly when required authoritative content cannot be resolved rather than silently running against an unintended working directory.

### 2.3 Package contents

- [`data/CMakeLists.txt`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/CMakeLists.txt) installs core data and conditionally adds sound/shader resources.
- [`lang/CMakeLists.txt`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/lang/CMakeLists.txt) compiles and installs localization catalogues.
- [`gfx/`](https://github.com/LambdaSix/Cataclysm-DDA/tree/e262adb299a7613b4aedc5f12c08fe0413c56a84/gfx), [`data/`](https://github.com/LambdaSix/Cataclysm-DDA/tree/e262adb299a7613b4aedc5f12c08fe0413c56a84/data) and [`lang/`](https://github.com/LambdaSix/Cataclysm-DDA/tree/e262adb299a7613b4aedc5f12c08fe0413c56a84/lang) are separate resource classes.
- [`org.cataclysmdda.CataclysmDDA.yml`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/org.cataclysmdda.CataclysmDDA.yml), [`snapcraft.yaml`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/snapcraft.yaml), [`android/`](https://github.com/LambdaSix/Cataclysm-DDA/tree/e262adb299a7613b4aedc5f12c08fe0413c56a84/android) and [`build-data/osx/`](https://github.com/LambdaSix/Cataclysm-DDA/tree/e262adb299a7613b4aedc5f12c08fe0413c56a84/build-data/osx) show platform-specific packaging without making package format part of gameplay.
- The release workflow stages dependency licence texts alongside redistributed native libraries.

### 2.4 Version/build identity

[`src/version.cmake`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/version.cmake) derives a version from Git state and writes `VERSION.txt`. The release workflow records build type and build number/timestamp.

OctoGhast needs stronger machine-readable identity because Specs 18, 19, 20 and 23 require exact content/profile/build provenance for saves and parity results.

### 2.5 Tests and release gates

- [`tests/`](https://github.com/LambdaSix/Cataclysm-DDA/tree/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests) and [`doc/c++/TESTING.md`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/c%2B%2B/TESTING.md) establish headless automated validation as a build product.
- The pinned release workflow composes translations/tiles/shaders before final platform packaging and fails release jobs when required staged resources or dependencies are absent.

Spec 23 remains authoritative for OctoGhast test taxonomy, deterministic fixtures, differential testing and parity-matrix status. This specification only defines how those tests participate in the build/package pipeline.

## 3. OctoGhast supported platform/runtime matrix

### 3.1 Toolchain policy

The implementation SHALL use SDK-style .NET projects with a repository-pinned SDK and a repository-pinned stable Godot .NET editor/export-template version.

Initial toolchain baseline for implementation:

- **.NET 10 LTS** for Core, Cataclysm profile, server, tools and test projects.
- **Godot 4.7.2 .NET** as the initial Godot client/editor/export baseline.
- Toolchain versions MUST be pinned in repository-controlled configuration and upgraded deliberately, with CI validating the new matrix before the pin moves.
- Release builds MUST NOT resolve an arbitrary machine-global "latest" SDK or Godot version.

Godot is not presentation-only infrastructure. Authoritative server infrastructure MAY depend on `libgodot` through bounded OctoGhast adapters for selected engine services. Generic Core and durable domain state remain independent of Godot object identity and lifecycle. Dedicated/headless server operation means **no graphical/window/audio requirement**, not necessarily "no libgodot dependency."

### 3.2 Release tiers

| Tier | Platform | Architecture | Authoritative server | Godot client | CI obligation |
|---|---|---:|---:|---:|---|
| Tier 1 | Windows 11 | x64 | Required | Required | build + tests + package smoke |
| Tier 1 | Linux (mainstream glibc distribution) | x64 | Required | Required | build + tests + package smoke |
| Tier 1 | macOS 13+ | arm64 | Required | Required | build + tests + package smoke |
| Tier 2 | macOS 13+ | x64 | Required | Supported | build + targeted smoke |
| Tier 2 | Linux | arm64 | Required | Not release-gated initially | build + headless tests |
| Deferred | Windows | arm64 | Architecturally allowed | Not release-gated initially | no release promise |
| Deferred | Android/iOS | mobile native | client only, remote-server use | Deferred | no parity-release gate |
| Out of initial scope | Web | wasm/browser | No | No C# client export target | none |

"Required" means the reference-parity release cannot be declared without a passing artifact on that Tier 1 row. Tier 2 failures block a release only when the project explicitly promotes that target to release-gated status.

The server protocol and save/content formats MUST NOT vary by operating system or CPU architecture except for explicitly versioned compatibility metadata.

### 3.3 CDDA frontend mapping

Pinned CDDA has terminal and graphical build products. OctoGhast does **not** require a curses/terminal gameplay frontend for parity.

- headless server/tools provide non-graphical operation;
- Godot 2D is the supported interactive gameplay client;
- debug/test/admin command surfaces from Spec 23 may be CLI-based;
- lack of a curses frontend is an intentional product-surface adaptation, not a gameplay-rule divergence.

## 4. Build products and dependency boundaries

The solution SHALL expose independent buildable products conceptually equivalent to:

```text
OctoGhast.Core
    ^ generic ECS/time/spatial/command/persistence contracts
    |
OctoGhast.Cataclysm
    ^ pinned-CDDA rules/data adapters
    |
OctoGhast.Server
    ^ authoritative host/session/projection services
    |
    +-- transport adapters (in-process and later #90 network transport)

OctoGhast.Client.Contracts
    ^ transport-neutral request/projection DTOs
    |
OctoGhast.Client.Godot
    ^ Godot 2D input/render/UI/audio/localization

OctoGhast.Tests / OctoGhast.Tools
```

Exact project names may differ, but the dependency rules are normative:

- Core MUST NOT reference Cataclysm, Godot, socket backends or platform packaging APIs.
- Cataclysm MAY reference Core; it MUST NOT reference Godot.
- Server MAY reference Core, rules profiles, transport abstractions and explicit engine-service interfaces.
- Production implementations of selected engine-service interfaces MAY use Godot server APIs through a contained adapter assembly backed by 2dog/libgodot.
- Authoritative systems feed ECS/domain data into those services, receive results, then commit authoritative outcomes back into ECS/domain state.
- Godot `RID`s, nodes, object references, navigation/physics caches and other engine-owned runtime structures MUST NOT be the sole durable copy of gameplay-significant state and MUST be reconstructible from persisted authoritative state plus immutable definitions.
- Unit tests MAY replace engine-service adapters with deterministic stubs/fakes where appropriate; conformance/integration tests MUST exercise the real Godot-backed implementation wherever Godot semantics materially affect gameplay.
- Godot client MAY reference shared transport/projection contracts, never authoritative ECS component storage.
- In-process transport and socket transport implement the same logical request/projection contract.
- Packaging code MAY compose products but MUST NOT create new gameplay mutation paths.

The legacy MonoGame/RenderLike/Win32 shell is not a required compatibility surface.

## 5. Build configurations and feature selection

### 5.1 Configurations

At minimum:

- **Debug** — developer diagnostics, symbols and assertions; not redistributable as a supported release.
- **Release** — optimized, reproducible release candidate configuration; production capability/security defaults.
- **Test** MAY be a separate configuration or Release/Debug plus explicit test-only assemblies/content. Test-only debug mutation/fault injection MUST NOT be reachable in production packages.

### 5.2 Product selection

Build targets SHALL independently support:

- headless authoritative server, optionally hosting libgodot-backed engine services without a presentation surface;
- .NET-owned 2dog/libgodot desktop client host;
- combined single-player host containing authoritative server + in-process client boundary + Godot presentation;
- test harness;
- content/schema validation tools;
- package assembly.

A single-player distribution MAY package the server and client together. That does not permit direct client ECS access: the in-process transport remains the logical boundary.

### 5.3 No simulation-semantic build flags

A release flag MUST NOT silently change Cataclysm rules, canonical time, RNG, action costs, save semantics or authoritative command ordering.

Rules/profile selection and content generation are explicit runtime/versioned inputs. Debug instrumentation may observe additional information but must not alter authoritative outcomes unless an explicit debug command does so.

## 6. Runtime resource model

### 6.1 Resource classes

OctoGhast distinguishes:

**Immutable installed/build resources**
- executable/runtime binaries;
- Cataclysm core content and bundled content packs;
- schema/definition data;
- client presentation packs/assets shipped with the product;
- localization resources;
- protocol/schema/build manifests;
- dependency/licence notices.

**Mutable server-owned world state**
- worlds/saves;
- authoritative scheduled/activity/RNG state;
- world-local player identities;
- world mod/content manifest;
- server configuration that affects a specific hosted instance.

**Mutable cross-world/profile state**
- #91-owned player/profile/meta-progression;
- client-local preferences such as selected language/presentation pack where applicable.

**Mutable client-only state**
- input bindings and UI preferences;
- presentation caches;
- local logs;
- downloaded optional presentation packs.

Runtime resource files are not ECS entities. Absolute filesystem paths are never stable domain IDs and MUST NOT appear in authoritative network DTOs or world references.

### 6.2 Logical roots

Implementations SHALL resolve logical roots, not hard-code current working directory paths:

- `InstallRoot` — immutable application installation.
- `ContentRoot` — bundled authoritative definitions/content.
- `PresentationRoot` — bundled client visuals/audio/localization.
- `UserDataRoot` — durable writable application state.
- `ConfigRoot` — local configuration/preferences.
- `CacheRoot` — disposable derived data.
- `LogRoot` — diagnostics.
- `ServerInstanceRoot` — explicit root for a headless/dedicated server instance.
- `TestRunRoot` — isolated Spec 23 test directory.

A path resolver converts these logical roots to physical paths once during host/client bootstrap. Domain services consume logical content providers/resource handles from Specs 18/19/22 rather than arbitrary OS paths.

## 7. Default writable paths and overrides

### 7.1 Windows

Default base: `%LOCALAPPDATA%\OctoGhast\`.

Recommended layout:

```text
%LOCALAPPDATA%\OctoGhast\
  config\
  worlds\
  profiles\
  mods\
  presentation\
  logs\
  cache\
```

### 7.2 Linux

Use XDG locations:

- data: `${XDG_DATA_HOME:-$HOME/.local/share}/octoghast/`;
- config: `${XDG_CONFIG_HOME:-$HOME/.config}/octoghast/`;
- cache: `${XDG_CACHE_HOME:-$HOME/.cache}/octoghast/`;
- logs/state: `${XDG_STATE_HOME:-$HOME/.local/state}/octoghast/`.

Worlds, profiles, user mods and user presentation packs are durable data, not cache.

### 7.3 macOS

Defaults:

- durable data/config: `~/Library/Application Support/OctoGhast/`;
- cache: `~/Library/Caches/OctoGhast/`;
- logs: `~/Library/Logs/OctoGhast/`.

### 7.4 Dedicated server and tests

A dedicated server SHOULD be launched with an explicit instance root. Container/service deployments may map that root to a volume. The process MUST be capable of running with a read-only installation and writable instance directory.

Spec 23 test runs MUST always use an isolated temporary/test root and MUST NOT read or mutate the operator's ordinary worlds, profiles or settings.

### 7.5 Override contract

Startup configuration MUST support explicit root overrides sufficient for:

- portable/dev checkout runs;
- CI/test isolation;
- dedicated server service/container deployment;
- migration/import tooling.

Overrides are processed before content/world loading. Once the server has admitted players into a world, changing the active authoritative content root or content generation requires an explicit reload/restart/content-generation transition; a client command cannot mutate it ad hoc.

Required roots that are absent/unreadable produce a structured startup failure. Writable roots that cannot be created/written produce a structured startup failure before a world is mutated.

## 8. Resource discovery and precedence

### 8.1 Pinned CDDA reference behaviour

Pinned CDDA derives bundled data/gfx/lang paths from a base/prefix and writable save/config paths from a user root. Command-line overrides can replace several locations. Presentation discovery checks user and bundled roots in a defined order.

### 8.2 OctoGhast adaptation

The physical root resolver only determines **providers and locations**. It does not define semantic mod/content precedence.

For each resource class:

1. explicit test/admin/launch override provider, when permitted;
2. user-installed provider;
3. bundled installation provider.

Within a provider, filesystem enumeration MUST be normalized to a deterministic ordering before package metadata is handed to the relevant subsystem.

Then:
- Spec 19 owns mod dependency ordering, activation and content override semantics;
- Spec 18 owns definition loading/finalization and immutable content generations;
- Spec 22 owns presentation-pack identity, duplicate handling and client fallback;
- Spec 20 owns world-save content/profile compatibility checks.

The implementation MUST NOT implement "last file found wins" based on host directory enumeration order.

### 8.3 Content integrity and generation identity

At authoritative server startup:

1. resolve the selected rules profile and content-package set;
2. resolve deterministic provider/package order;
3. validate definitions through Specs 18/19;
4. construct/freeze a `ContentGenerationId`;
5. create or load a world only against that frozen generation;
6. record profile/content/build provenance required by Spec 20.

A failed new content generation MUST NOT partially replace the generation used by a running world.

## 9. Build and package metadata

Every produced artifact SHALL expose a machine-readable `BuildManifest` containing at least:

- OctoGhast product/version;
- source commit SHA;
- repository dirty flag or equivalent reproducibility marker;
- build configuration;
- target runtime identifier/architecture;
- .NET SDK/runtime baseline;
- Godot version for client artifacts;
- protocol/schema compatibility version(s);
- Cataclysm rules-profile version;
- pinned CDDA reference commit `e262adb299a7613b4aedc5f12c08fe0413c56a84`;
- bundled content-manifest/content-generation input identity;
- build pipeline/release identifier.

A human-readable `--version`/About surface SHALL expose the relevant subset.

Release artifacts MUST be built from a clean, identified commit. A dirty local build may run for development but cannot be labelled as an official release/parity-verification artifact.

Build timestamps may exist as diagnostics. They MUST NOT participate in simulation ordering, RNG or semantic save comparison.

## 10. Packaging contracts

### 10.1 Headless server package

Must contain:

- server executable/runtime;
- Core + selected rules-profile assemblies;
- authoritative bundled content required for a new Cataclysm-profile world;
- build/content manifests;
- required runtime dependencies;
- licence/notice files;
- the libgodot/2dog native/runtime dependencies required by enabled server-side Godot service adapters, when those adapters are part of the selected server build;
- no mandatory Godot editor, presentation scene pack or graphical asset pack.

It MUST start with no display/audio device and support an explicit writable instance root. A dedicated server package MAY host libgodot in headless mode for bounded engine services; headless does not imply a Godot-free process.

### 10.2 Desktop client package

Must contain:

- Godot exported client;
- shared request/projection contracts;
- bundled presentation resources required for a usable default experience;
- localization/font resources required by Specs 21/22;
- build manifest and notices.

A remote-client-only installation need not contain authoritative world saves or server-private state.

### 10.3 Single-player desktop bundle

May include both server and client products plus authoritative content in one install. It MUST still:

- create/use server-owned world storage;
- communicate through in-process request/projection contracts;
- obey the same stale-request, visibility and authority rules as network co-op;
- never let Godot object identity become authoritative entity identity.

### 10.4 Resource duplication

Packaging may deduplicate shared immutable files physically, but the logical ownership remains distinct. An optimization such as a shared content directory MUST NOT make a remote client authoritative for content or give the client arbitrary access to hidden ECS/world state.

### 10.5 Licences and third-party notices

Every redistributed dependency/resource whose licence requires attribution or accompanying terms MUST have those notices included in the platform artifact. Packaging validation treats a required missing notice as a release failure.

## 11. Persistence, disconnect/reconnect and server ownership

Spec 20 remains authoritative for save transactions. Spec 24 adds these packaging/path constraints:

- world saves live under server-owned writable state, not inside immutable install resources;
- a connected client's local save directory is not authoritative for a remote server world;
- disconnect/reconnect does not move world ownership to the client;
- a one-player server uses the same ownership rule;
- transport/socket state and transient Godot runtime state are never persisted merely because client and server are co-packaged;
- any gameplay-significant data represented inside Godot server APIs at runtime must be reconstructible from ECS/domain persistence plus immutable definitions; Godot RIDs/object identity are not persistence identity;
- save metadata records enough build/profile/content identity to validate a continuation before mutating the loaded world.

Direct compatibility with upstream CDDA save files remains a separate compatibility decision; packaging parity does not imply binary/save-format parity.

## 11.1 Authoritative Godot-service boundary

Godot/libgodot MAY participate in authoritative server execution through explicit OctoGhast interfaces such as physics, navigation, pathing or other bounded engine-service contracts.

The ownership contract is normative:

1. ECS/domain state is the durable authoritative source of truth.
2. Before a Godot-backed query/step, authoritative systems synchronize the required current domain state into the adapter/runtime representation.
3. Godot server APIs perform the bounded computation/query/step.
4. The returned result is validated/interpreted by the owning authoritative system and any resulting gameplay mutation is committed back into ECS/domain state.
5. Engine runtime state may be cached or incrementally maintained for performance, but it MUST be reconstructible after process restart/save load from persisted authoritative state plus immutable definitions.

Interfaces exist to contain Godot API exposure, provide deterministic stubs/fakes for focused unit testing, and preserve a practical replacement seam. They do **not** assert that Godot-backed implementations are perfectly substitutable or free of semantic coupling. When Godot semantics affect authoritative outcomes, the real adapter is part of the conformance surface and must be tested accordingly.

2dog/libgodot process hosting is therefore compatible with both dedicated server and graphical client roles. The relevant architectural invariant is state ownership and dependency containment, not the absence of libgodot from the server process.

## 12. RNG and determinism

Build, packaging and path discovery MUST NOT consume authoritative gameplay RNG.

Determinism requirements:

- package discovery order is normalized before semantic resolution;
- content-generation identity is independent of nondeterministic filesystem iteration order;
- changing installation path alone does not change world RNG outcomes;
- client-only presentation resources/caches cannot perturb authoritative RNG;
- release build optimization/platform differences must not intentionally introduce alternate simulation rules.

If floating-point/platform behaviour is later found to produce authoritative divergence, the owning feature specification must define normalization/fixed-point/tolerance policy; Spec 24 does not hide that divergence behind platform packaging.

## 13. CI and release gates

### 13.1 Pull-request gates

At minimum:

1. restore with pinned SDK/tool versions;
2. build Core/Cataclysm/server/shared contracts;
3. run headless unit/contract/scenario tests selected by Spec 23;
4. load/validate representative pinned Cataclysm content through Specs 18/19;
5. build the Godot C# client on at least one CI host and validate its project/export configuration;
6. enforce dependency-boundary checks so Core/domain projects do not acquire arbitrary Godot dependencies and server-side Godot access remains confined to approved adapter/interface layers;
7. run deterministic resource-discovery tests in a temporary root;
8. reject committed release output/secrets or use of operator user-state paths in tests.

The complete expensive parity suite need not run on every PR; Spec 23's affected-row policy controls that.

### 13.2 Platform matrix gates

Tier 1 targets MUST build on native CI environments. Release branches/tags additionally run:

- platform package assembly;
- clean-install/package extraction smoke;
- `--version` / manifest validation;
- headless server startup with read-only install + temporary writable instance;
- core-content load/new-world bootstrap smoke;
- Godot client launch/export smoke appropriate to CI;
- in-process one-player request/projection smoke;
- loopback/network smoke once #90 transport exists;
- save/create/reload smoke using the packaged server;
- resource completeness and licence/notice validation.

### 13.3 Release artifact gates

A release artifact fails if:

- the source/build identity is ambiguous or dirty;
- required content/resources are missing;
- a package writes into its immutable install root during normal operation;
- a headless server requires a graphical/window/audio environment;
- platform-specific packaging changes authoritative rule/profile versions silently;
- bundled content manifest does not match the manifest recorded in the artifact;
- required conformance/parity rows for the declared release milestone are not in an acceptable Spec 23 state.

## 14. Headless and test-harness requirements

All simulation, content-loading, persistence, network-contract and parity scenarios that do not explicitly test presentation MUST run without a graphical/window/audio surface. They MAY use libgodot when exercising production Godot-backed server services.

Pure unit/contract tests SHOULD use interface-level stubs/fakes where that gives tighter isolation. Tests whose result depends materially on Godot physics/navigation/pathing semantics MUST also run against the real libgodot-backed adapter so the abstraction cannot mask engine-specific behaviour.

The test harness must be able to:

- supply explicit install/content/user/config/cache/server-instance roots;
- create all writable state beneath a test-owned root;
- simulate read-only installation resources;
- use fixed build/profile/content manifests;
- inspect structured startup/resource-resolution diagnostics;
- invoke server startup/shutdown without a window;
- run in-process and, later, loopback transport variants using the same scenario inputs.

No test may depend on the developer's current working directory, HOME/Application Support/LOCALAPPDATA contents or selected local Godot editor preferences unless that path behaviour is the explicit subject of the test.

## 15. Failure and validation behaviour

Required failure classes include:

- `UnsupportedPlatform`;
- `ToolchainVersionMismatch` (build-time);
- `RequiredResourceRootMissing`;
- `RequiredResourceUnreadable`;
- `WritableRootUnavailable`;
- `InvalidBuildManifest`;
- `ContentManifestMismatch`;
- `ContentGenerationInvalid`;
- `PackageIncomplete`;
- `UnsupportedClientBuild` / protocol compatibility rejection as owned by #90.

Startup failures occur before world mutation where possible. Diagnostics include the logical root/resource ID and safe normalized path, but server responses MUST NOT leak arbitrary host filesystem layout to untrusted clients.

## 16. Black-box and conformance scenarios

### BLD24-01 — Working-directory independence

Install/package resources under one directory, start the server from an unrelated current working directory, and create a world. The same configured `ContentRoot` resolves and no implicit `./data` dependency exists.

### BLD24-02 — Missing authoritative content fails closed

Remove the required bundled Cataclysm content root. Server startup fails with `RequiredResourceRootMissing` before creating/mutating a world.

### BLD24-03 — Read-only installation

Make install/content directories read-only and provide a writable server instance root. Server starts, creates/configures a world and writes only to the instance/user roots.

### BLD24-04 — Windows default path mapping

With controlled platform/environment abstraction, resolve Windows defaults under LocalApplicationData and verify worlds/config/cache classifications without touching the real user profile.

### BLD24-05 — Linux XDG mapping

Set all XDG variables to test directories. Data/config/cache/state resolve to the correct independent roots. Unset them and verify documented home-directory fallbacks.

### BLD24-06 — macOS path mapping

Resolve Application Support/Caches/Logs roots under a test home and keep durable worlds out of the cache root.

### BLD24-07 — Explicit instance-root override

Start a dedicated server with an explicit instance root. All server-owned mutable files are created beneath it; packaged immutable resources remain unchanged.

### BLD24-08 — Test isolation

Run two Spec 23 scenarios with separate `TestRunRoot` values. Neither can observe the other's saves/config/cache, and neither touches production user state.

### BLD24-09 — Deterministic enumeration

Present the same content packages in deliberately shuffled filesystem enumeration order. Specs 18/19 receive the same deterministic provider/package inputs and produce the same `ContentGenerationId`.

### BLD24-10 — User provider does not bypass mod semantics

Place a user mod with IDs overlapping bundled content. Root discovery finds it, but dependency/override resolution follows Spec 19 rather than raw path order.

### BLD24-11 — Presentation resource precedence is client-local

Install a user presentation pack with the same legacy pack name as a bundled pack. Spec 22's duplicate/selection semantics apply; authoritative server state and world hash are unchanged.

### BLD24-12 — Headless server may host libgodot without presentation

Launch the server with the configured 2dog/libgodot server adapters but no windowing or audio surface. It loads content, initializes required Godot server APIs headlessly, advances canonical simulation and passes a headless scenario. A variant using interface stubs confirms that domain/unit tests do not require the production adapter when Godot semantics are not under test.

### BLD24-13 — One-player packaged boundary

Run packaged single-player client+server through the in-process transport. The client's Godot scene cannot directly mutate ECS state; a representative command follows the same authoritative result path as the equivalent remote/loopback request.

### BLD24-14 — Remote client has no save authority

Connect a client whose local world directory contains conflicting files. Remote authoritative world state is unchanged and server saves are written only by the server.

### BLD24-15 — Build-configuration invariance

Run a deterministic headless scenario under supported Debug and Release builds with identical profile/content/seeds. Normalized authoritative outcome matches.

### BLD24-16 — Installation-path invariance

Run identical deterministic scenarios from two different absolute install paths. World state, RNG results and semantic events match.

### BLD24-17 — Clean release identity

Build an official release candidate from a clean commit. `BuildManifest` and `--version` agree on source SHA/product/platform/profile/baseline metadata.

### BLD24-18 — Dirty development identity

Build from a dirty tree. Manifest marks the build non-release/reproducibility-dirty and the parity harness refuses to treat it as an official parity-verification artifact.

### BLD24-19 — Pinned baseline provenance

Every Cataclysm-profile release manifest records `e262adb299a7613b4aedc5f12c08fe0413c56a84` until #64 deliberately repins the programme baseline.

### BLD24-20 — Content mismatch before load mutation

Attempt to load a saved world whose required frozen content generation is materially incompatible with the selected content. Spec 20/19 compatibility handling occurs before the world becomes active; no silent ID reinterpretation occurs.

### BLD24-21 — Package completeness

Assemble each Tier 1 server/client package and validate the expected manifest-driven file/resource set. Delete one required file and assert deterministic package/startup failure rather than an incidental later exception.

### BLD24-22 — Licence/notice completeness

Mark a staged dependency as requiring an accompanying notice. Package validation fails when that notice is absent and passes when restored.

### BLD24-23 — Tier 1 package smoke

For Windows x64, Linux x64 and macOS arm64 artifacts, execute the platform-native smoke sequence: version -> content validation -> headless server bootstrap -> client/export validation.

### BLD24-24 — Linux arm64 headless gate

Build and run headless contract tests for the Tier 2 Linux arm64 target. Failure is reported against that target without changing Tier 1 simulation rules.

### BLD24-25 — No terminal-frontend requirement

A release containing Godot client + headless server but no curses UI remains presentation-conformant provided Specs 21/22 gameplay interaction/information requirements pass.

### BLD24-26 — Client-only cosmetic change

Replace only client sprite/audio resources. Authoritative content generation, save compatibility, canonical event ordering and gameplay RNG remain unchanged.

### BLD24-27 — Server authoritative content change

Change a rules/content definition that participates in the authoritative content manifest. A new content generation is produced and world-load compatibility is evaluated through Specs 18/19/20.

### BLD24-28 — Reconnect across same server build

Disconnect and reconnect a player using a compatible client build. Socket/client process identity may change; world/player/entity identity and saved server ownership remain governed by Specs 20/#90, not by package paths.

### BLD24-29 — Client/server compatibility rejection

Attempt connection with an application protocol/schema version outside the supported range. #90's handshake rejects it explicitly; no ECS/world mutation occurs and no host filesystem details are exposed.

### BLD24-30 — Release artifact from legacy build path is not normative

The historical .NET Framework/MonoGame/Rake output may still be buildable during migration, but it cannot satisfy Spec 24 release gates unless it conforms to the new manifest, dependency, authority, platform and resource contracts.

## 17. Dependencies and contracts

- **#52 / #54** — project/dependency layering; Spec 24 makes those boundaries build/package enforceable.
- **Spec 01 / #57** — canonical simulation time is independent of host wall-clock/build platform.
- **Spec 12 / #58** — authoritative world/spatial state is platform/renderer independent.
- **Spec 18 / #83** — immutable definitions, logical providers, content generations and stable IDs.
- **Spec 19 / #84** — mod discovery metadata, dependency/load ordering and content-package compatibility.
- **Spec 20 / #85** — server-owned persistence, save barriers, content/profile compatibility and reconnect identity.
- **Spec 21 / #86** — toolkit-neutral UI/input contract.
- **Spec 22 / #87** — presentation-pack discovery/identity/fallback/localization; Spec 24 supplies physical resource roots and packaging.
- **Spec 23 / #88** — headless testing, parity matrix and build/scenario provenance.
- **#90** — transport/session/protocol lifecycle; Spec 24 packages transport implementations but does not choose wire semantics.
- **#91** — cross-world profile/meta-progression storage; Spec 24 supplies writable roots but does not redefine ownership.

## 18. Explicit non-goals and future seams

This specification does not require:

- CDDA's exact CMake/Make/MSVC/Gradle implementation;
- SDL3, curses or CDDA terminal frontend compatibility;
- identical CDDA archive/package naming;
- Android/iOS parity-release support;
- a web client while the selected Godot C# stack lacks a supported web export path;
- direct upstream CDDA save-file compatibility;
- a particular #90 socket backend;
- a requirement that every authoritative subsystem use Godot where a simpler deterministic implementation is preferable;
- perfect interchangeability of Godot-backed services: interfaces contain dependency scope and enable focused testing/replacement seams, but do not imply zero-cost engine substitution;
- permanent commitment to today's .NET/Godot/2dog versions.

Toolchain and supported-platform pins may evolve deliberately. Such an upgrade does not permit silent changes to rules-profile semantics, protocol compatibility, persisted state or parity evidence.

## 19. Definition of done for implementation

A later implementation of this specification is complete when:

- the repository uses pinned modern .NET/Godot/2dog toolchains;
- .NET-owned 2dog/libgodot hosting is established as the normal application process model;
- Core/domain, server adapter and client boundaries are enforceable by project references;
- Tier 1 products build and package;
- resource roots work independently of CWD with read-only installs;
- all mutable world/profile/client state goes to its correct writable owner;
- build/content/profile identity is machine-readable;
- headless and package smoke suites pass;
- scenarios BLD24-01 through BLD24-30 are automated at the appropriate test layer;
- no gameplay/runtime implementation is required merely to complete this specification investigation.

## 20. Investigation conclusion

Pinned CDDA demonstrates mature multi-platform build/release practice, explicit installed-versus-user paths, independently packaged data/gfx/lang resources and release-time validation. OctoGhast preserves those externally important capabilities while deliberately replacing CDDA-specific build architecture with .NET-owned hosts using 2dog/libgodot from project inception. The ECS/domain model remains owner of durable authoritative state, while bounded Godot server APIs may provide runtime computation/services behind explicit adapters and the Godot 2D client consumes player-specific projections.

No new unresolved cross-cutting architecture decision was discovered. The open production-network transport details remain owned by #90 and cross-world profile storage remains owned by #91.
