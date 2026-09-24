# Spec 22 — Graphics, tilesets, audio and localization

## 1. Purpose and compatibility decision

This specification defines the implementation contract for OctoGhast non-gameplay presentation assets and localization: tileset/sprite lookup, overmap graphics, animation-facing semantic state, soundpack/music/SFX mapping, translated strings, Unicode/font handling, and presentation-pack discovery.

The behavioural reference is the pinned Cataclysm:DDA baseline:

`LambdaSix/Cataclysm-DDA@e262adb299a7613b4aedc5f12c08fe0413c56a84`.

This is a **presentation/content compatibility specification**, not a requirement to reproduce CDDA's SDL renderer, curses frontend, mixer backend, file APIs, render timing, or exact pixel rasterization. Reference parity means that pinned-compatible presentation packs and translation data can be interpreted with the documented ID, lookup, fallback and language semantics closely enough to present the same gameplay meaning.

The architecture is split into four layers:

1. **Generic Core/domain contracts** — stable semantic IDs, immutable content-generation references, authoritative visibility/audibility facts, deterministic presentation-event ordering, and transport-neutral projections.
2. **Cataclysm profile compatibility** — pinned CDDA tile ID conventions, `looks_like` fallback, tileset/soundpack metadata, sound ID/variant conventions, gettext-style context/plural semantics and relevant data schemas.
3. **Client presentation runtime** — Godot 2D asset loading, sprite/audio/font resources, interpolation, animation, local language selection and accessibility rendering.
4. **Future profile seam** — other rulesets may use different sprite/audio/localization schemas while reusing the same semantic projection and asset-provider boundaries.

The server owns simulation truth. Presentation packs are client/product-surface resources and MUST NOT become authoritative ECS/world state. A client missing an optional sprite or sound MUST remain able to participate in the simulation using defined fallbacks.

## 2. Reference evidence

All references in this section are to the pinned baseline above.

### 2.1 Tileset format and lookup

Authoritative/reference evidence:

- [`doc/TILESET.md`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/TILESET.md) defines tileset terminology, tile entries, hardcoded/complex IDs, gender/season/transparent/item variants, rotations, weighted alternatives, multitiles, connection groups, `tile_info`, expansion tiles and legacy package structure.
- [`src/cata_tiles.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/cata_tiles.cpp) implements runtime tile lookup, `looks_like` recursion, seasonal lookup, category-aware fallback and drawing-facing ID conventions.
- [`src/tileset_loader.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/tileset_loader.cpp) loads tile configuration and diagnoses a missing `unknown` / `unknown_terrain` tile.
- [`gfx/ASCIITileset/tileset.txt`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/gfx/ASCIITileset/tileset.txt) and [`tile_config.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/gfx/ASCIITileset/tile_config.json) are concrete package fixtures.
- [`gfx/Larwick_Overmap/tileset.txt`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/gfx/Larwick_Overmap/tileset.txt) is the pinned overmap-pack fixture.
- [`src/options.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/options.cpp) discovers user/bundled tilesets and soundpacks and ignores duplicate package names after the earlier discovery wins.
- [`src/mod_tileset.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/mod_tileset.cpp) records mod tileset expansions and their explicit compatible main-tileset IDs.

Important pinned lookup behavior:

1. For a requested semantic ID and variant, CDDA tries the variant form first.
2. If the variant is absent, it tries the base ID.
3. If the base ID is absent and the category supports definitions with `looks_like`, it follows that definition's `looks_like` chain with a bounded jump count.
4. Seasonal lookup is applied to tile lookup.
5. Field intensity, transparent, gender, item/mutation variant, vehicle-part and other category conventions can alter the candidate ID before final fallback.
6. If no concrete tile is found, CDDA attempts a category-derived ASCII/symbol fallback where possible, then `unknown_<category>_<subcategory>`, then `unknown_<category>`, then `unknown`.
7. A tileset lacking its required unknown tile is diagnosed rather than silently treated as complete.

For non-vehicle item-style variants the pinned candidate form is `<id>_var_<variant>`. For ordinary suffix variants beginning with `_`, the suffix is appended directly. Vehicle-part variants use their own progressively shortened suffix convention.

### 2.2 Overmap graphics

The pinned SDL frontend uses a dedicated overmap tileset context selected by `OVERMAP_TILES`; isometric IDs are filtered out of the overmap option list. The bundled default is `Larwick Overmap`. Overmap rendering consumes overmap terrain, vision-level, weather, map-extra, note and vehicle-derived semantic IDs.

OctoGhast MUST preserve these **semantic lookup inputs** for Cataclysm-profile compatibility, but the Godot client is not required to reproduce CDDA's SDL window structure or overmap drawing loop.

### 2.3 Soundpack format and hooks

Reference evidence:

- [`doc/SOUNDPACKS.md`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/SOUNDPACKS.md) documents the sound ID/variant vocabulary and precedence for many gameplay hooks.
- [`data/sound/Menu_Sound_Test/soundpack.txt`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/sound/Menu_Sound_Test/soundpack.txt), [`soundset.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/sound/Menu_Sound_Test/soundset.json) and [`musicset.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/sound/Menu_Sound_Test/musicset.json) are concrete package fixtures.
- [`src/sdlsound.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/sdlsound.cpp) loads sound effects/playlists and implements ID/variant fallback.
- [`src/sounds.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/sounds.cpp) maps authoritative gameplay sound occurrences to presentation SFX hooks.

A `sound_effect` entry has:

- required `id`;
- optional `variant`, string or array, defaulting to `default`;
- optional `season`;
- optional `is_indoors` and `is_night` selectors;
- optional `volume`, default 100;
- one or more relative asset `files`.

Multiple matching files form an alternative set. The pinned runtime randomly chooses one matching effect. Normal playback first resolves the requested ID/variant/qualifiers, then falls back to the same ID with `default` variant and unconstrained qualifiers. If neither exists, playback is silent/no-op. Some documented hook families have extra explicit precedence rules; for example `fire_gun` may take precedence over `fire_ammo`, and `plmove <terrain>` precedes generic movement variants.

Music data defines named playlists with a `shuffle` flag and file/volume entries.

### 2.4 Localization model

Reference evidence:

- [`src/translation.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/translation.cpp) defines translatable values carrying singular text, optional plural text and optional context.
- [`src/translations.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/translations.cpp) selects language, loads catalogues, invalidates translation caches after language changes, and falls back to English when localization is disabled.
- [`tests/translation_system_test.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/translation_system_test.cpp) proves untranslated-string identity, context disambiguation and language-specific plural-rule evaluation.
- [`tests/translations_test.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/tests/translations_test.cpp) proves untranslated strings remain usable in the English/no-catalogue case.
- [`lang/string_extractor`](https://github.com/LambdaSix/Cataclysm-DDA/tree/e262adb299a7613b4aedc5f12c08fe0413c56a84/lang/string_extractor) and [`lang/extract_json_strings.py`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/lang/extract_json_strings.py) are extraction-pipeline evidence.

Pinned semantics:

- untranslated text falls back to its source string;
- context distinguishes otherwise identical source strings;
- plural choice is driven by the active catalogue's plural rules, not by a universal English singular/plural boolean;
- context + plural uses the equivalent of `npgettext`;
- changing language invalidates cached translated values;
- JSON translation objects support explicit singular/plural forms and validation of unsupported or unnecessary plural members.

### 2.5 Fonts and Unicode

Reference evidence:

- [`doc/user-guides/FONT_OPTIONS.md`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/doc/user-guides/FONT_OPTIONS.md) defines four font categories and ordered fallback.
- [`data/fontdata.json`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/data/fontdata.json) supplies the bundled defaults.
- [`src/font_loader.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/font_loader.cpp) accepts a string, object or array per category and ensures Unifont is present as last-resort fallback.
- [`src/unicode.cpp`](https://github.com/LambdaSix/Cataclysm-DDA/blob/e262adb299a7613b4aedc5f12c08fe0413c56a84/src/unicode.cpp) contains pinned width/classification assumptions including CJK/emoji ranges.

The four pinned categories are `typeface`, `gui_typeface`, `map_typeface`, and `overmap_typeface`. OctoGhast may map these to Godot font stacks rather than FreeType/ImGui flags, but MUST preserve ordered glyph fallback and Unicode-capable rendering.

## 3. Architectural ownership and data model

### 3.1 Immutable definition/template data

Immutable within a loaded client presentation generation:

- `PresentationPackDefinition`
  - stable pack ID/name;
  - kind: tile, overmap tile, sound, localization, font;
  - provenance/provider;
  - declared compatibility/profile;
  - metadata and resource manifest.
- `TileSetDefinition`
  - semantic tile IDs;
  - sprite references;
  - rotations/multitile/connectivity metadata;
  - weighted variants;
  - seasonal/transparent/intensity variants;
  - dimensions/offsets/pixel scale/isometric metadata;
  - expansion-pack compatibility.
- `SoundPackDefinition`
  - sound effect ID + selector key;
  - file alternatives and gain;
  - music playlists.
- `LocalizationCatalogueDefinition`
  - language tag;
  - source/context/plural mappings;
  - plural rule;
  - catalogue provenance.
- `FontStackDefinition`
  - logical font category;
  - ordered resources and local rendering options.

These objects are presentation resources. They do not belong in authoritative world saves.

### 3.2 Mutable authoritative runtime state

The server owns only gameplay state that can cause presentation:

- current world/spatial state and player-specific visibility;
- authoritative sound occurrences and their gameplay propagation/audibility;
- messages/events/results;
- weather/time/season and other domain facts already owned by their specifications;
- stable definition/runtime references required to describe projected entities.

The server MUST NOT own Godot textures, audio streams, font resources, animation players, client camera transforms or current language.

### 3.3 Mutable client presentation state

Examples:

- selected tile/sound/font/language packs;
- loaded resource handles and caches;
- current local language;
- animation/interpolation state;
- cosmetic random-variation cache;
- audio channel/playback state;
- local volume/mute preferences;
- font/glyph cache;
- local fallback diagnostics.

This state may be discarded and rebuilt from explicit projections plus local configuration.

## 4. Pack discovery, identity and precedence

### 4.1 Pinned CDDA reference behavior

CDDA recursively discovers `tileset.txt` under user and bundled graphics roots and `soundpack.txt` under user and bundled sound roots. It reads at least `NAME` and `VIEW`; duplicate discovered names are diagnosed and the earlier stored definition is retained. The pinned search order checks user roots before bundled roots.

Tileset metadata may also identify JSON and tilesheet resources such as:

```text
NAME: ASCIITiles
VIEW: ASCIITiles
JSON: tile_config.json
TILESET: ASCIITiles.png
```

### 4.2 OctoGhast adaptation

Generic Core MUST NOT depend on filesystem paths. Spec 19's logical content-provider/resource model is reused for presentation resources. The default desktop client MAY expose filesystem providers that emulate pinned CDDA discovery rules for compatibility.

A discovered pack receives a stable logical `PresentationPackId`. Local absolute paths are implementation detail and MUST NOT cross the network boundary.

### 4.3 Implementation contract

For Cataclysm-compatible legacy packs:

1. discover eligible roots in configured deterministic provider order;
2. find metadata files recursively;
3. parse `NAME` and `VIEW`, plus pack-kind fields;
4. reject/diagnose missing stable name;
5. on duplicate `NAME`, retain the earlier definition and diagnose the ignored duplicate to match pinned effective behavior;
6. validate referenced JSON/assets before activation;
7. activation is atomic: a failed replacement MUST NOT leave a half-loaded active pack;
8. missing optional presentation packs do not invalidate the authoritative world.

User-selected pack IDs are durable client/profile preferences, not world state.

## 5. Tile/sprite identity and fallback resolution

### 5.1 Semantic tile request

The renderer consumes a toolkit-neutral request:

```text
TileVisualKey
  semantic_id
  category
  subcategory?
  variant?
  intensity?
  season?
  gender?
  orientation/subtile?
  presentation_flags
```

The server or client projection layer supplies semantic facts. It does **not** supply a texture object or sprite-sheet index.

### 5.2 Cataclysm-profile resolution order

For a normal semantic tile request, resolve in this order where applicable:

1. category-specific derived candidate such as transparent/intensity/gender/season/item variant;
2. requested variant form;
3. requested base ID;
4. definition `looks_like` chain, bounded and cycle-safe;
5. category-specific symbolic/ASCII fallback derived from the definition when available;
6. `unknown_<category>_<subcategory>`;
7. `unknown_<category>`;
8. `unknown`;
9. client-native emergency placeholder if even `unknown` is absent.

An emergency placeholder is an OctoGhast hardening adaptation. Its use MUST emit a diagnostic because a Cataclysm-compatible tileset is expected to provide an unknown fallback.

The `looks_like` chain uses stable definition IDs from the matching frozen Cataclysm content generation. It MUST NOT dereference arbitrary client objects or recurse without a limit/cycle guard.

### 5.3 Variants and seasonal forms

Support the pinned ID forms required by baseline packs, including:

- `_var_<variant>`;
- `_season_spring|summer|autumn|winter`;
- `_transparent`;
- field `_intN`;
- gendered overlays;
- overlay/worn/wielded/corpse/mutation/bionic and vehicle-part conventions documented by `TILESET.md`.

The exact prefix/suffix vocabulary is Cataclysm-profile policy, not generic Core API.

### 5.4 Rotations, multitiles and weighted alternatives

The client tileset adapter MUST understand pinned rotation arrays, `rotates`, `multitile` and `additional_tiles` conventions required by baseline packs.

Weighted sprite alternatives are presentation-only. Selection MUST NOT consume authoritative simulation RNG. OctoGhast SHOULD derive cosmetic variation from a stable presentation hash of semantic ID + authoritative stable location/entity reference + relevant visual generation, so redraws/reconnects do not flicker and observer timing does not change the choice.

No gameplay rule may depend on which cosmetic weighted sprite was selected.

### 5.5 Rendering-facing domain state

The renderer may require projected semantic facts such as:

- stable definition ID and runtime entity/item reference;
- world position and orientation;
- terrain/furniture connection mask;
- variant ID;
- field intensity;
- season;
- visible/remembered/hidden state;
- damage/broken/open state where represented visually;
- character gender/body presentation tags where allowed;
- equipment overlay IDs;
- overmap terrain/knowledge/vision-level IDs;
- weather visual ID;
- transient presentation-event ID.

These facts come from owning domain/projection specs. The renderer MUST NOT inspect ECS components or world registries directly.

## 6. Overmap presentation contract

The overmap client consumes the player-specific knowledge projection defined by Specs 12, 13 and 21. It MUST NOT render unrevealed server overmap state simply because the server knows it.

Cataclysm-profile tile lookup uses overmap semantic categories compatible with pinned IDs. Notes and mission markers are derived only from that player's permitted state.

The client may use a separate overmap tileset, fonts, zoom policy and animation without altering authoritative coordinates or knowledge.

Multiple players standing in the same world area may legitimately receive different overmap visuals because their remembered/revealed knowledge differs.

## 7. Animation and transient visual effects

### 7.1 Pinned reference behavior

CDDA contains hardcoded presentation IDs such as running directions, bash effects, explosions, bullet/hit/weather effects, cursor/highlight and other special overlays.

### 7.2 OctoGhast adaptation

Server/domain systems emit semantic result events; the client maps eligible events to local animation cues. Animation duration does not delay authoritative resolution unless the owning gameplay spec defines a simulation activity.

Example:

```text
authoritative ranged result
  -> ProjectileResolved / HitResult
  -> viewer-specific event projection
  -> local "animation_bullet_normal" / impact animation
```

A client that disables or lacks the animation remains simulation-equivalent.

Transient effect events MUST be audience-filtered. A hidden explosion may still produce an audible cue if the authoritative sound system says it is heard, but the client MUST NOT receive a hidden visual entity solely to animate it.

## 8. Sound domain versus soundpack presentation

### 8.1 Pinned CDDA reference behavior

Pinned CDDA gameplay sound and presentation SFX are coupled in one executable: gameplay emits a sound occurrence and the SDL sound layer maps IDs/variants to audio files.

### 8.2 OctoGhast adaptation

Split this into:

```text
authoritative SoundOccurrence
  -> gameplay propagation/hearing
  -> PlayerAudibleSoundProjection
  -> client AudioCue
  -> selected SoundPackDefinition
  -> local audio backend
```

The server owns whether a player can hear something, the semantic source/category, authoritative volume/range facts required by gameplay, and ordering. The client owns asset selection, final device gain, stereo/panning/HRTF policy and playback.

### 8.3 Audio cue contract

A projected cue contains only player-authorized information:

```text
AudioCue
  event_id
  sound_id
  variant
  authoritative_source_ref?   // only if viewer is entitled
  perceived_direction_or_position?
  perceived_volume
  season?
  indoors?
  night?
  semantic_category?
```

A cue MUST NOT expose an exact hidden entity ID/location when the player's gameplay perception provides only approximate direction/volume.

### 8.4 Soundpack resolution

For Cataclysm profile:

1. resolve requested `id + variant + season/indoors/night`;
2. use the pinned selector fallback rules;
3. if absent, attempt `id + default` with unconstrained optional qualifiers;
4. apply hook-specific documented precedence where the gameplay-to-SFX adapter defines it;
5. if absent, produce no audio and continue normally.

Missing audio is therefore a **silent presentation fallback**, not a gameplay failure.

### 8.5 Audio randomness and determinism

Alternative-file choice and playlist shuffle are cosmetic and MUST NOT consume server simulation RNG.

For tests, the client audio mapper MUST support an injected deterministic cosmetic chooser so the selected asset can be asserted. Production may use a local presentation RNG because asset choice has no simulation effect, but authoritative event ordering and cue identity remain deterministic.

### 8.6 Continuous time and disconnects

Audio playback never pauses or owns simulation time. Opening menus, muting sound or losing an audio device does not pause the server.

After reconnect, the server does not replay arbitrary historical one-shot effects. It rebuilds current projections and may send explicitly durable/looping ambience state where the contract requires it. Connection/session identity is inherited from #90; sound channels are local client state and are never persisted as authoritative state.

## 9. Music and ambience

Music playlists are presentation definitions. Server gameplay may project high-level semantic context such as menu/world state or explicitly authorized danger/ambient state, but the exact track is local.

Pinned soundpack playlist IDs and shuffle/file-volume data SHOULD be accepted for compatibility.

Danger music and similar context derived from visible hostiles MUST be computed from the player's permitted projection or from a server-projected semantic context. It MUST NOT become a side channel revealing hidden entities.

Music position, currently playing track and crossfade state are local preferences/runtime state and are excluded from world saves.

## 10. Translation string model

### 10.1 Logical value

Use a toolkit-neutral `LocalizedText` value:

```text
LocalizedText
  source_singular
  source_plural?
  context?
  count?
  named/typed format arguments
  translation_domain/profile
```

The source text is the required fallback.

### 10.2 Pinned Cataclysm semantics

The Cataclysm adapter MUST preserve:

- singular source text;
- explicit plural source text where present;
- context;
- language-catalogue plural rule evaluation;
- untranslated fallback to source text;
- language-change cache invalidation;
- JSON extraction of marked translatable members required by implemented content types.

It MUST NOT reduce plural handling to `count == 1` because pinned tests demonstrate languages with one, two, four and six plural forms.

### 10.3 Client/server boundary

Where practical, server-generated user-facing output SHOULD cross the protocol as semantic/localizable data plus typed arguments rather than already localized final strings. This keeps each client free to select its language.

The server may still project immutable source/fallback text from mod content when there is no stable catalogue key beyond gettext's source/context pair.

Formatting MUST occur after translation when the target language requires it. Clients MUST NOT reinterpret gameplay quantities or hidden state to create localization arguments.

### 10.4 Security and privacy

Localization tokens carry only information already permitted by the owning projection/event. Localization MUST NOT become a route for shipping arbitrary server registry state, hidden names, hidden item IDs or filesystem paths to clients.

## 11. Extraction and catalogue loading

For Cataclysm-compatible content:

- the build/content toolchain identifies translatable JSON members using the pinned schema/extractor mapping;
- source strings, optional context and plural forms are extracted into catalogue input;
- runtime catalogue loading is per language/domain;
- malformed catalogue data is diagnosed and isolated;
- untranslated entries fall back to source;
- changing language invalidates translated-text caches but does not mutate authoritative world state.

OctoGhast does not require Python or GNU gettext as permanent implementation dependencies. It requires **behaviourally compatible catalogue semantics**. A different compiled catalogue format is acceptable if extraction/loading round-trips pinned source/context/plural behavior and compatibility tooling can import pinned catalogues.

## 12. Unicode, fonts and display width

### 12.1 Encoding

All protocol/localization/UI text contracts use Unicode scalar text encoded as UTF-8 at serialization boundaries unless the selected protocol specifies an equivalent Unicode encoding.

Invalid encoded input from external content is a validation error with provenance; it MUST NOT become malformed runtime text.

### 12.2 Font fallback

The Godot client MUST support ordered font fallback. For Cataclysm-compatible font configuration, map the four pinned logical categories:

- general/legacy UI `typeface`;
- rich GUI `gui_typeface`;
- local map `map_typeface`;
- overmap `overmap_typeface`.

A general Unicode fallback equivalent in coverage purpose to the pinned Unifont fallback MUST be available in supported distributions, without requiring that exact font file or rasterizer.

### 12.3 Width and layout

Do not assume byte length equals glyph count or display width. UI layout/truncation/wrapping MUST use the actual client text-shaping/layout engine while preserving semantic focus and accessibility behavior from Spec 21.

Cataclysm differential fixtures SHOULD include ASCII, accented Latin, Cyrillic, CJK and emoji/symbol text to catch width/fallback regressions.

Core simulation MUST NOT depend on rendered glyph width.

### 12.4 Missing glyphs

A missing glyph uses the client's visible replacement-glyph/fallback behavior and emits diagnostic telemetry in test/developer modes. It does not change simulation or translation identity.

## 13. Stable identity, persistence and generations

### 13.1 Stable references

Use:

- server content: stable typed Cataclysm definition IDs and Spec 18/19 content-generation identity;
- presentation packs: client-local stable `PresentationPackId`;
- transient presentation events: stable event/cue ID scoped sufficiently for de-duplication;
- resources inside packs: logical pack-relative resource IDs/paths, never process object identity.

Sprite indices inside an atlas and Godot resource instance IDs are cache-local only.

### 13.2 Persistence

World saves persist no renderer/audio/font resources.

Durable client/profile preferences may persist:

- selected tile/overmap/sound/font/language pack IDs;
- volumes/mute;
- presentation/accessibility options.

If a selected pack is missing on next launch, fall back to the configured default/emergency pack, diagnose the missing preference, and keep the preference recoverable if practical.

Spec 20 world rollback MUST NOT roll back unrelated client presentation preferences.

### 13.3 Content-generation mismatch

A server may project Cataclysm definition IDs not represented by the client's chosen tileset. This is expected and resolves through `looks_like` / category / unknown fallback. It is not equivalent to an authoritative content-generation mismatch.

Protocol/session compatibility of server gameplay definitions remains governed by Specs 18/19/#90. A presentation pack can be incomplete without granting the client a different gameplay schema.

## 14. Concurrency and multiplayer

Presentation assets are client-local and therefore do not contend in the authoritative simulation.

Shared authoritative causes do contend according to their owning domain specs. Example: two players triggering the same world object may produce one accepted mutation and viewer-specific result events. Each client independently maps those events to local graphics/audio.

Observer count MUST NOT:

- change server RNG;
- change weighted gameplay outcomes;
- cause effects to execute twice;
- change tile/sound fallback results for another client;
- expose another player's private/hidden projection.

Two clients may deliberately choose different tilesets, soundpacks, languages and font stacks while observing the same authoritative state.

## 15. Validation and failure behavior

### 15.1 Tilesets

Diagnose/reject activation for structurally invalid required metadata, malformed config, invalid referenced sheet/resource, impossible sprite indexes or incompatible expansion metadata.

An incomplete but structurally valid tileset may activate if an `unknown` fallback exists; missing ordinary IDs resolve through fallback.

A missing `unknown` tile is compatibility-invalid for normal Cataclysm tileset activation. OctoGhast may use an emergency client-native placeholder so the client remains operable, but MUST mark the pack degraded.

### 15.2 Soundpacks

Malformed JSON or invalid required members fail that definition/load with diagnostics. Missing optional sound IDs/files yield silence for those cues. Audio-device/backend failure degrades to silent mode without affecting server simulation.

### 15.3 Localization

Malformed catalogue/plural rules fail catalogue activation or isolate the bad catalogue according to loader policy; source strings remain the final safe fallback.

### 15.4 Fonts

Invalid font entries are diagnosed and skipped/fail according to whether a usable fallback stack remains. Client startup MUST preserve an emergency readable UI font path in supported distributions.

## 16. Dependencies and contracts

- **Spec 12 / #77:** authoritative `WorldPosition`, `SpatialCell`, visibility-space semantics; presentation transforms remain client-only.
- **Spec 13 / #78:** overmap IDs, player knowledge and world-generation definitions.
- **Spec 14 / #79:** authoritative weather/field/environment state and player-specific environmental projections.
- **Spec 15 / #80:** vehicle part IDs/orientation/state used by tile/audio adapters.
- **Spec 18 / #83:** stable typed IDs, immutable frozen gameplay content generations and definition lookup.
- **Spec 19 / #84:** logical content providers, package provenance and presentation-pack separation.
- **Spec 20 / #85:** authoritative world persistence excludes renderer/audio/client preference state.
- **Spec 21 / #86:** player-specific UI projections, messages, accessibility, semantic focus, local options and Godot presentation boundary.
- **#90:** connection/session/projection transport; wire data carries semantic IDs/events, never Godot/SDL resource identity.
- **#91:** cross-world profile persistence; if language/presentation preferences later become account-synced, #91 owns that policy rather than world saves.

## 17. Pinned CDDA behavior versus OctoGhast adaptation

| Area | Pinned CDDA reference behavior | OctoGhast adaptation | Implementation contract |
|---|---|---|---|
| Tile rendering | SDL tiles context resolves CDDA IDs | Godot 2D renderer | Preserve semantic tile IDs and fallback; backend is replaceable |
| `looks_like` | renderer follows definition chain | client adapter resolves against matching content definitions/projection metadata | bounded, cycle-safe Cataclysm fallback |
| Unknown tile | category/ASCII/unknown fallback | emergency client placeholder additionally allowed | missing ordinary art never mutates gameplay |
| Weighted sprites | presentation choice inside tile renderer | cosmetic client choice | never consume authoritative RNG |
| Overmap | separate SDL overmap tile context | player-specific Godot overmap view | preserve permitted semantic IDs, not SDL window mechanics |
| Gameplay sounds | gameplay and SDL SFX live in same process | server audibility -> client cue -> local pack | server controls information/audibility; client controls assets |
| Missing SFX | no matching effect -> no playback | same | silence is non-fatal |
| Localization | runtime gettext-like manager | client-local language/catalogue | preserve source/context/plural semantics |
| Fonts | configured FreeType stacks + Unifont fallback | Godot font fallback | preserve Unicode coverage/order, not rasterizer |
| Pause/timing | presentation executes in turn-gated client process | world continues on canonical server time | assets/UI never own simulation clock |

## 18. Black-box, conformance and parity scenarios

### GFX22-01 Direct tile ID
Given a tileset containing `mon_cat`, requesting the Cataclysm monster visual for `mon_cat` resolves that direct tile without consulting `looks_like`.

### GFX22-02 Item variant before base
Given `item1_var_orange` and `item1`, requesting `item1` with variant `orange` selects the variant; an unknown variant falls back to `item1`.

### GFX22-03 Seasonal variant
Given a winter-specific tile plus base tile, winter resolves the seasonal form and another season resolves the base when its seasonal form is absent.

### GFX22-04 `looks_like` chain
Given A missing from the tileset, A `looks_like` B, and B present, A renders B's tile while authoritative ID remains A.

### GFX22-05 `looks_like` cycle/limit
Given an invalid cycle A -> B -> A, lookup terminates deterministically and proceeds to normal unknown fallback without recursion failure.

### GFX22-06 Category fallback
Given no direct/looks-like tile but an available category fallback, the category fallback is chosen before generic `unknown`.

### GFX22-07 Generic unknown
Given no direct, variant, looks-like, symbol or category tile, `unknown` is selected.

### GFX22-08 Missing unknown
Given a structurally loadable pack with no required `unknown`, activation is diagnosed as degraded/invalid and the emergency OctoGhast placeholder keeps the client operable.

### GFX22-09 Weighted sprite observer invariance
Two clients and repeated redraws of one unchanged map object do not consume server RNG or alter simulation. A deterministic presentation test chooser yields stable expected artwork.

### GFX22-10 Rotation/multitile
A wall/door fixture with pinned connection neighbors resolves the expected subtile/orientation from the same semantic connection mask.

### GFX22-11 Transparent/intensity IDs
A transparent terrain request and a field intensity request attempt pinned derived IDs before their base IDs.

### GFX22-12 Overmap knowledge isolation
Player A knows a town and Player B has not discovered it. The clients may use the same overmap tileset, but B receives no hidden town semantic state to render.

### GFX22-13 Separate overmap pack
Changing only the overmap pack alters overmap assets while authoritative overmap coordinates/knowledge remain byte-for-byte equivalent.

### GFX22-14 Duplicate pack identity
Two discovered legacy packs declare the same `NAME`. Deterministic provider/search precedence retains the earlier one and emits a duplicate diagnostic.

### GFX22-15 Mod tileset compatibility
A MOD_TILESET expansion declaring compatibility with tileset X is applied when X is active and not applied to incompatible tileset Y.

### AUD22-01 Exact sound variant
A cue matching an exact ID/variant/qualifier entry selects from that entry's file alternatives.

### AUD22-02 Default variant fallback
A requested variant absent from the pack falls back to the same sound ID's `default` variant.

### AUD22-03 Missing sound
A sound ID absent from the pack produces no playback and no gameplay error.

### AUD22-04 Hook precedence
A pinned firearm fixture with both applicable specific and generic hooks follows the documented Cataclysm sound-hook precedence.

### AUD22-05 Hidden source privacy
A player hears an unseen source. The server projects only permitted perceived direction/volume/category; the audio cue does not reveal an exact hidden entity reference/location.

### AUD22-06 Two players, different packs
Two players hear the same authoritative event while using different soundpacks. Their chosen files differ; gameplay and authoritative event identity are identical.

### AUD22-07 Muted client
Muting or losing the audio device produces no server command, no pause and no simulation difference.

### AUD22-08 Reconnect one-shot behavior
After disconnect/reconnect, stale one-shot SFX are not replayed merely because they happened while disconnected; durable ambience is rebuilt only from current state.

### AUD22-09 Cosmetic RNG isolation
Changing the local alternative-file RNG seed changes only selected audio assets, not authoritative state, event ordering or save output.

### L10N22-01 Untranslated fallback
An unknown source string resolves exactly to its source text.

### L10N22-02 Context disambiguation
The same source `pike` with contexts `weapon` and `fish` resolves to different catalogue entries; an unknown context falls back to source.

### L10N22-03 English plural
With no non-English catalogue, count 1 uses singular and counts 0/2 use plural for an English fixture.

### L10N22-04 Russian plural
The pinned Russian plural fixture selects the expected multiple forms for representative counts.

### L10N22-05 Arabic/CJK plural evaluator
Plural rule fixtures cover six-form Arabic and one-form CJK behavior rather than a two-form assumption.

### L10N22-06 Language hot change
Changing client language invalidates translated-text caches and redraws local UI without mutating authoritative state.

### L10N22-07 Two players, different languages
Two clients receive the same semantic result and render it in different selected languages without requiring different server simulation outcomes.

### L10N22-08 Extraction round-trip
Representative Cataclysm JSON with source/context/plural fields is extracted, compiled/imported, loaded and resolves to the expected translations.

### TXT22-01 Unicode fallback
ASCII, accented Latin, Cyrillic, CJK and emoji/symbol fixtures render through the configured font stack without corrupting UTF-8 identity.

### TXT22-02 Width independence
Changing font, shaping or display scale may change pixel width but does not change focus identity, command targets, authoritative positions or protocol values.

### TXT22-03 Missing glyph
A missing glyph produces a visible replacement/fallback plus diagnostic in test mode, not a crash or gameplay mutation.

### NET22-01 In-process/network equivalence
Given identical semantic projections/events, in-process single-player and loopback network clients resolve the same tile/audio/localization semantic keys; serialization does not expose server filesystem paths or resource-object identities.

### NET22-02 Projection does not expose ECS
A graphics/audio/localization client trace contains only explicit projected facts and stable semantic IDs; no arbitrary ECS component dump, Godot node reference or server registry object identity appears.

### SAVE22-01 World save exclusion
Changing selected tileset, soundpack, language or volume does not change authoritative world-save contents.

### SAVE22-02 Missing preference pack
A persisted client preference references a removed pack. Startup selects a safe default/emergency pack, reports the issue and loads the world unchanged.

### CORE22-01 Alternative rules profile
A non-Cataclysm profile supplies different visual/audio ID adapters without implementing CDDA suffix/`looks_like` conventions; generic presentation projections and pack-provider contracts remain reusable.

## 19. Representative compatibility fixtures

Minimum automated fixture set:

- bundled `ASCIITileset` metadata/config;
- bundled `Larwick Overmap` metadata/config;
- synthetic tile fixture covering direct ID, `looks_like`, variant, season, intensity, unknown category and generic unknown;
- synthetic MOD_TILESET compatibility fixture;
- bundled `Menu_Sound_Test` soundpack;
- synthetic SFX fixture with exact/default/season/indoors/night selectors and multiple alternative files;
- playlist fixture with shuffle true/false;
- pinned TEST_DATA Russian MO catalogue;
- synthetic context/plural catalogues including English, Russian, Arabic and CJK rules;
- Unicode text corpus covering ASCII, Latin diacritics, Cyrillic, CJK and emoji/symbols;
- two-client fixture with different language/tile/sound preferences over identical authoritative projections.

## 20. Implementation sequence

1. Define toolkit-neutral presentation DTOs/keys: tile visual key, animation cue, audio cue and localized-text value.
2. Implement logical presentation-pack provider/discovery abstraction; add Cataclysm legacy metadata adapter.
3. Implement Cataclysm tile-config parser and lookup resolver independently of Godot texture loading.
4. Implement Godot sprite/atlas resource adapter and emergency fallback asset.
5. Implement overmap renderer against Spec 21 player-knowledge projection.
6. Implement soundpack parser/resolver separately from the audio backend.
7. Connect authoritative audible-sound projection to local audio cues.
8. Implement localization catalogue abstraction, Cataclysm import/extraction compatibility and language switching.
9. Implement Unicode-capable font stacks/fallback.
10. Add the GFX22/AUD22/L10N22/TXT22/NET22/SAVE22/CORE22 conformance suite.
11. Add representative pinned packs/catalogues to the parity matrix owned by Spec 23/#88.

## 21. Explicit non-goals

This specification does not:

- implement gameplay FOV, sound propagation, weather, combat, item state or overmap knowledge;
- require SDL, SDL_mixer, curses, ImGui, GNU gettext or CDDA's exact renderer architecture;
- require pixel-perfect reproduction of every third-party tileset;
- make presentation-pack contents authoritative;
- make audio playback timing part of simulation scheduling;
- synchronize client volume, current track, animation frame or loaded GPU resource state as world state;
- expose server files or ECS internals to satisfy renderer lookup;
- define authentication/account synchronization of presentation preferences.

## 22. Definition of done

Spec 22 is implementation-ready when:

- tile/sprite identity and Cataclysm fallback resolution are explicit;
- asset-pack discovery, identity, precedence, validation and missing-asset behavior are explicit;
- rendering-facing semantic state is separated from renderer/backend implementation;
- overmap presentation consumes player-specific knowledge only;
- sound event IDs/hooks, soundpack mapping, fallback and absent-audio behavior are explicit;
- translation source/context/plural and extraction/loading requirements are explicit;
- Unicode/font/fallback/display-width assumptions are explicit;
- existing pinned CDDA tileset/soundpack/translation compatibility targets are bounded and testable;
- ownership, persistence, stable identity and RNG isolation are defined;
- continuous-time, co-op, privacy, disconnect/reconnect and one-player-server behavior are explicit adaptations rather than attributed to upstream CDDA;
- representative black-box/parity fixtures are enumerated;
- no unresolved cross-cutting architecture decision remains hidden in this feature spec.
