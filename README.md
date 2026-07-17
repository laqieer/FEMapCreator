# FE Map Creator

A Fire Emblem focused map creation and generation tool, originally published at:

https://bwdyeti.com/programs/#MapGen

**Use the Web app:** https://laqieer.github.io/FEMapCreator/

[Repository](https://github.com/laqieer/FEMapCreator) |
[Issues](https://github.com/laqieer/FEMapCreator/issues) |
[Discussions](https://github.com/laqieer/FEMapCreator/discussions) |
[User guide](https://github.com/laqieer/FEMapCreator/blob/main/docs/user-guide/Home.md) |
[Latest release](https://github.com/laqieer/FEMapCreator/releases/latest)

All front ends share the platform-neutral `FE_Map_Creator.Core` generation engine:

- **Cross-platform GUI** (`FE_Map_Creator.Gui`) - one Avalonia UI for Windows, macOS, Linux, and WebAssembly. It includes bundled tilesets, tile painting/erasing, brush/rectangle/flood-fill tools, locks, terrain constraints, resize, undo/redo, `.map`/`.mar`/`.tmx` open/save, and generation/repair.
- **CLI** (`FE_Map_Creator.Cli`) - Windows, Linux, and macOS console workflows for map editing/conversion/inspection, generation, repair, validation, discovery, and batches.
- **Legacy GUI** (`FE_Map_Creator`) - the original Windows-only WinForms editor, retained for legacy tileset-processing and specialized import/edit workflows.

## Requirements

- .NET 10 SDK for development.
- The `wasm-tools` workload to build or publish the Web app:

  ```powershell
  dotnet workload install wasm-tools
  ```

- Release downloads are self-contained and do not require a separately installed .NET runtime.

## Cross-platform GUI

Run the desktop GUI on Windows, macOS, or Linux:

```powershell
dotnet run --project .\FE_Map_Creator.Gui.Desktop\FE_Map_Creator.Gui.Desktop.csproj -c Release
```

Build the desktop and browser hosts:

```powershell
dotnet build .\FE_Map_Creator.Gui.Desktop\FE_Map_Creator.Gui.Desktop.csproj -c Release
dotnet build .\FE_Map_Creator.Gui.Browser\FE_Map_Creator.Gui.Browser.csproj -c Release
```

Publish the static Web app and validate its required assets/navigation:

```powershell
dotnet publish .\FE_Map_Creator.Gui.Browser\FE_Map_Creator.Gui.Browser.csproj -c Release
.\scripts\smoke-test-web.ps1 -PublishDirectory .\FE_Map_Creator.Gui.Browser\bin\Release\net10.0-browser\publish
```

The static site is under the publish directory's `wwwroot\`. Pushes to `main` deploy it to GitHub Pages. Version tags publish self-contained Windows x64, Linux x64, macOS x64, macOS arm64, and Web archives to GitHub Releases. Each desktop archive includes the matching self-contained CLI under `CLI\`.

Pull requests, pushes to `main`, and manually dispatched CI runs also produce downloadable Windows x64, Linux x64, macOS x64, macOS arm64, and validated Web `wwwroot` archives. Desktop CI archives include the CLI under `CLI\`. Open the relevant run under **Actions > CI**, then download the package from its **Artifacts** section. Artifact names include the platform/RID, commit SHA, run number, and run attempt.

CI artifacts are short-lived, unsigned, non-release builds intended for testing. They are not GitHub Releases; use the [latest release](https://github.com/laqieer/FEMapCreator/releases/latest) for supported downloads.

Run the platform-neutral GUI tests:

```powershell
dotnet test .\FE_Map_Creator.Gui.Tests\FE_Map_Creator.Gui.Tests.csproj -c Release
```

## Legacy WinForms GUI: build, run, test, publish

```powershell
dotnet build .\FE_Map_Creator\FE_Map_Creator.sln -c Release
dotnet run --project .\FE_Map_Creator\FE_Map_Creator.csproj -c Release
dotnet test .\FE_Map_Creator\FE_Map_Creator.sln -c Release
dotnet publish .\FE_Map_Creator\FE_Map_Creator.csproj -c Release
```

Run the tile-data serialization test alone:

```powershell
dotnet test .\FE_Map_Creator.Tests\FE_Map_Creator.Tests.csproj -c Release --filter "FullyQualifiedName~TileDataTests.BinaryRoundTripPreservesPriorities"
```

The framework-dependent GUI application and its runtime assets are written to `FE_Map_Creator\bin\Release\net10.0-windows\publish\`.

## CLI: build, run, publish

```powershell
dotnet build .\FE_Map_Creator.Cli\FE_Map_Creator.Cli.csproj -c Release
dotnet run --project .\FE_Map_Creator.Cli\FE_Map_Creator.Cli.csproj -c Release -- tilesets list
dotnet publish .\FE_Map_Creator.Cli\FE_Map_Creator.Cli.csproj -c Release
```

Publishing copies this `README.md`, `Tileset_Data.xml`, `Terrain_Data.xml`, `Tilesets\**`, and `Tileset Generation Data\**` into `FE_Map_Creator.Cli\bin\Release\net10.0\publish\` alongside `FE_Map_Creator.Cli.exe`/`FE_Map_Creator.Cli.dll`, so the published folder is self-contained for every bundled tileset. By default the CLI resolves these bundled assets from its own install directory (`AppContext.BaseDirectory`); use `--assets-dir` to point at a different asset root instead.

### CLI commands and examples

Every `generate`/`repair` run prints its random seed even when `--seed` is omitted, so any run can be reproduced later with `--seed <printed seed>`. Per-map generation/repair progress is written to stderr; result paths, seeds, and summaries remain on stdout.

List bundled tilesets (selector names, asset paths, and any missing-pairing diagnostics):

```powershell
FE_Map_Creator.Cli.exe tilesets list
FE_Map_Creator.Cli.exe tilesets list --json
FE_Map_Creator.Cli.exe tilesets terrain --tileset "FE6 - Fields - 01020304" --json
```

`tilesets list --json` is stable machine-readable asset discovery. `tilesets terrain`
resolves the selected tileset's per-tile terrain tags from `Tileset_Data.xml` and the
corresponding id/name catalog from `Terrain_Data.xml`; it does not require a generation
data `.dat` file.

Create a map, apply ordered edits from a versioned spec, resize it, and save it:

```powershell
FE_Map_Creator.Cli.exe map edit --spec edit.json
```

Load and convert without changing tile values:

```powershell
FE_Map_Creator.Cli.exe map edit --input chapter.map --output chapter.tmx --assets-dir .
FE_Map_Creator.Cli.exe map edit --input chapter.mar --width 30 --height 20 --tileset "FE6 - Fields - 01020304" --output chapter.map
```

Edit atomically in place, or inspect metadata and the row-major tile matrix:

```powershell
FE_Map_Creator.Cli.exe map edit --input chapter.map --spec edits.json --in-place
FE_Map_Creator.Cli.exe map inspect --input chapter.map
FE_Map_Creator.Cli.exe map inspect --input chapter.map --json
```

`map edit` accepts `--input`, `--output`, `--in-place`, `--spec`, `--format`,
`--width`, `--height`, `--tileset`, `--assets-dir`, `--tileset-image`, and
`--force`. Direct options override spec values. Omit `--input` to create a map;
positive dimensions are then required. MAR input always requires width, height, and
tileset metadata. `--in-place` and `--output` are mutually exclusive, and in-place
conversion to another format is rejected so an extension never silently disagrees
with its contents.

Generate a blank map (every cell open, filled by the generation engine):

```powershell
FE_Map_Creator.Cli.exe generate --width 30 --height 20 --tileset "FE6 - Fields - 01020304" --output map.map --seed 12345
```

The whole-map constraint solver is the default for generation and repair. Select the
historical frontier generator explicitly with `--algorithm legacy`:

```powershell
FE_Map_Creator.Cli.exe generate --width 30 --height 20 --tileset "FE6 - Fields - 01020304" --output map.legacy.map --algorithm legacy --depth 2 --seed 12345
```

The experimental solver globally backtracks to avoid greedy dead ends. It splits open
cells into independent components, maintains reversible word-level domains, orders
candidates by remaining neighbor support plus learned corpus weights, and uses
deterministic partial/complete restarts. Propagated greedy completion reaches a
fixed-point arc-consistent state. Complete restarts use conflict-directed backjumping
and a bounded exact nogood cache; the default-off
`--experimental-branch-arc-consistency` option also establishes fixed-point arc
consistency at the root and after every complete-search assignment. The defaults are
10,000 total search nodes, 4 restarts, and 4,096 retained nogoods; tune them with
`--experimental-search-node-limit`, `--experimental-restarts`, and
`--experimental-nogood-limit`. Use `--no-experimental-conflict-learning` for a
chronological comparison. A zero-unresolved result is optimal; budget exhaustion is
reported when an incomplete search is cut off. The worst case remains exponential, and
the solver is cooperatively cancellable. Use `--algorithm legacy` to override the
default or an experimental job spec. In WinForms, **Map Generation > Experimental
Constraint Solver** is checked by default and controls both generation and repair;
**Experimental Branch Arc Consistency** is an unchecked setting available for the
experimental and hybrid modes.

See [`docs/experimental-solver-benchmark.md`](docs/experimental-solver-benchmark.md),
[`docs/branch-arc-consistency-benchmark.md`](docs/branch-arc-consistency-benchmark.md),
and `scripts\benchmark-solvers.ps1` for the reproducible solver matrices and
default-promotion gates. The reports include entropy, dominant-tile share,
neighbor-repetition metrics, and side-by-side rendered previews for human review.

For large maps, `--algorithm hybrid` runs the legacy solver first, groups its unresolved
cells into independent regions, and applies the constraint solver only inside adaptive
Manhattan halos. Completed regions are retained while only unresolved regions expand.
Hybrid output is guaranteed to have no more unresolved cells than its legacy baseline:

```powershell
FE_Map_Creator.Cli.exe generate --width 30 --height 20 --tileset "FE6 - Fields - 01020304" --output map.hybrid.map --algorithm hybrid --hybrid-initial-halo 1 --hybrid-max-halo 3 --seed 12345
```

WinForms exposes mutually exclusive **Experimental Constraint Solver** and
**Hybrid Legacy + Constraint Solver** menu toggles; experimental is checked by default.
The separate, unchecked **Experimental Branch Arc Consistency** setting applies to both
whole-map and hybrid regional complete searches.

Generate from a template plus JSON masks that identify which template cells are initially drawn or locked:

```powershell
FE_Map_Creator.Cli.exe generate --spec job.json --template template.map --output map.map
```

A template without a `drawn` or `locked` mask supplies dimensions and tile values, but its cells remain open and may be regenerated.

Generate from a versioned JSON job spec (`--spec`, see schema below); direct CLI options override values loaded from the spec:

```powershell
FE_Map_Creator.Cli.exe generate --spec job.json
```

Generate `map.map`, `map.mar`, or `map.tmx` - format is inferred from `--output`'s extension, or set explicitly with `--format`:

```powershell
FE_Map_Creator.Cli.exe generate --width 20 --height 15 --tileset "FE6 - Fields - 01020304" --output map.mar --format mar
```

Repair an existing map, writing to a new file or in place:

```powershell
FE_Map_Creator.Cli.exe repair --input map.map --tileset "FE6 - Fields - 01020304" --output map.repaired.map
FE_Map_Creator.Cli.exe repair --input map.map --tileset "FE6 - Fields - 01020304" --in-place
```

Validate every nonzero tile and learned adjacency in an output map (and terrain
constraints when a spec is supplied):

```powershell
FE_Map_Creator.Cli.exe validate --input map.map --tileset "FE6 - Fields - 01020304"
FE_Map_Creator.Cli.exe validate --input map.map --spec job.json
```

`--in-place` and `--output` are mutually exclusive; `--in-place` replaces `--input` atomically (write to a temp file, then move over the original).

MAR files contain only tile values, not dimensions or a tileset identifier. Supply all three values explicitly when repairing a MAR file:

```powershell
FE_Map_Creator.Cli.exe repair --input map.mar --width 30 --height 20 --tileset "FE6 - Fields - 01020304" --output map.repaired.mar
```

Generate `--count` homogeneous maps into a directory (each job gets a deterministic derived seed when `--seed` is supplied; `{index}`/`{seed}` are available in `--name-template`):

```powershell
FE_Map_Creator.Cli.exe generate --width 20 --height 15 --tileset "FE6 - Fields - 01020304" --count 10 --output-dir .\maps --name-template "map-{index}-{seed}" --seed 100 --allow-incomplete
```

Repair every map under a directory, recursively, mirroring the input tree into `--output-dir`:

```powershell
FE_Map_Creator.Cli.exe repair --input-dir .\maps --output-dir .\repaired --tileset "FE6 - Fields - 01020304" --pattern "*.map" --recursive
```

Each input file under `--input-dir` may have an optional sidecar spec, either `<basename>.mapgen.json` or `<filename>.mapgen.json` (e.g. `map-1.mapgen.json` or `map-1.map.mapgen.json` for `map-1.map`); the basename form wins if both exist, and sidecars are excluded from `--pattern` matching.

For `maps\chapter-1.mar`, a `maps\chapter-1.mapgen.json` sidecar can supply the metadata absent from MAR:

```json
{
  "version": 1,
  "operation": "repair",
  "width": 30,
  "height": 20,
  "tileset": "FE6 - Fields - 01020304"
}
```

Run a heterogeneous batch of edit/generate/repair jobs from one manifest:

```powershell
FE_Map_Creator.Cli.exe batch --manifest manifest.json --fail-fast
```

### JSON job spec (v1)

`Map_Job_Spec` documents (`"version": 1` required) drive `--spec` for a single
`map edit`, `generate`, or `repair` job. All fields are optional except `Version`;
explicit CLI options take precedence over spec values, and spec-relative paths
(`Input`, `Template`, `Output`, asset overrides) resolve relative to the spec file's
own directory. When `operation` is present it must match the command (`edit`,
`generate`, or `repair`).

```json
{
  "version": 1,
  "operation": "generate",
  "width": 8,
  "height": 6,
  "tileset": "FE6 - Fields - 01020304",
  "template": "template.map",
  "output": "map.map",
  "algorithm": "experimental",
  "experimentalSearchNodeLimit": 10000,
  "experimentalRestartCount": 4,
  "experimentalNogoodLimit": 4096,
  "experimentalEnableConflictLearning": true,
  "experimentalEnableBranchArcConsistency": false,
  "hybridInitialHalo": 1,
  "hybridMaxHalo": 3,
  "seed": 42,
  "depth": 1,
  "locked": [
    [true, true, true, true, true, true, true, true],
    [true, false, false, false, false, false, false, true],
    [true, false, false, false, false, false, false, true],
    [true, false, false, false, false, false, false, true],
    [true, false, false, false, false, false, false, true],
    [true, true, true, true, true, true, true, true]
  ],
  "terrain": [
    [0, 0, 0, 0, 0, 0, 0, 0],
    [0, 3, 3, 3, 0, 0, 0, 0],
    [0, 3, 3, 3, 0, 0, 0, 0],
    [0, 0, 0, 0, -5, -5, -5, 0],
    [0, 0, 0, 0, -5, -5, -5, 0],
    [0, 0, 0, 0, 0, 0, 0, 0]
  ],
  "edits": [
    {
      "action": "set-tile",
      "shape": "rectangle",
      "x": 2,
      "y": 2,
      "endX": 4,
      "endY": 3,
      "tile": 17
    }
  ]
}
```

Here `template.map`'s border tiles are fixed by `locked` (also implicitly `drawn`, since with a template and no explicit spec `drawn` matrix, `drawn` defaults to the `locked` mask), the top-left interior block requires terrain tag `3`, the bottom-right interior block forbids terrain tag `5`, and every other interior cell is left open for the experimental solver to fill. Omit `algorithm` to use the experimental default, or set it to `legacy` for the frontier algorithm.

A single-job MAR repair spec supplies the same non-inferable metadata:

```json
{
  "version": 1,
  "operation": "repair",
  "input": "map.mar",
  "output": "map.repaired.mar",
  "width": 30,
  "height": 20,
  "tileset": "FE6 - Fields - 01020304"
}
```

Constraint matrices (`drawn`, `locked`, `terrain`) are `[row][column]` (JSON array-of-rows), matching how the fields read naturally as `height` rows of `width` values each; every row must be the same length, and all three matrices (when present) must share the same width/height as each other and as any `width`/`height` fields. Internally the Core engine (and the GUI) instead stores maps as `[x, y]` arrays, so the spec reader transposes on load.

- `drawn` vs. tile index `0`: a cell can hold tile `0` and still be "drawn" (explicitly placed) rather than open. For `generate`, `drawn` only matters with `--template`: an explicit spec `drawn` matrix wins, otherwise it defaults to the `locked` mask; without a template every cell starts open regardless of `drawn`. `repair` always treats every input cell as drawn, with tile `0` cells taken as repair holes to reopen.
- `locked` cells are preserved as-is during generation/repair and must also be `drawn` (a locked-but-undrawn cell is a validation error); locking a cell in `generate` without `--template` is likewise a conflict, since there would be no source tile to lock.
- `terrain` values are signed per-cell tags matched against the resolved tileset's `Tileset_Data.xml` terrain metadata: a **positive** value requires that terrain tag (`terrain[x,y] == N`), a **negative** value forbids it (`terrain[x,y] != -N`), and `0` means unconstrained. A cell cannot be both `locked` and terrain-constrained (nonzero `terrain`) at once. A nonzero terrain constraint on a tileset with no resolvable terrain metadata is a hard error rather than a silently ignored constraint.

### Ordered map edits (v1)

`edits` is an ordered array. Each operation sees the result of every preceding
operation, and the whole array is transactional: validation or execution failure leaves
the input/output untouched. Coordinates are zero-based `(x, y)`, with `x` increasing
right and `y` increasing down.

```json
{
  "version": 1,
  "operation": "edit",
  "input": "chapter.map",
  "output": "chapter-edited.tmx",
  "format": "tmx",
  "tileset": "FE6 - Fields - 01020304",
  "edits": [
    { "action": "set-tile", "x": 3, "y": 2, "tile": 0 },
    {
      "action": "set-tile",
      "shape": "rectangle",
      "x": 8,
      "y": 4,
      "endX": 11,
      "endY": 7,
      "tile": 0
    },
    {
      "action": "set-tile",
      "shape": "flood-fill",
      "x": 0,
      "y": 0,
      "tile": 23
    },
    { "action": "resize", "width": 40, "height": 25 }
  ]
}
```

- Actions: `set-tile`, `erase`, `lock`, `unlock`, `require-terrain`,
  `forbid-terrain`, `resize`, `clear-locks`, and `clear-terrain`.
- Shapes default to `cell`. `rectangle` uses inclusive endpoints, accepts either
  endpoint order, and clips to the map boundary. `flood-fill` uses four-way
  connectivity and matches the starting cell's relevant tile/drawn, lock, or terrain
  state.
- Cell and flood-fill origins must be on-map. A resize must have positive dimensions,
  preserves the top-left overlap of tile/drawn/lock/terrain state, drops cells outside a
  shrink, and initializes newly added cells as tile `0`, open, unlocked, and
  unconstrained.
- `set-tile` accepts non-negative tile indices. It marks the cell drawn and clears its
  lock/terrain state. `erase` writes tile `0` and marks the cell open. Lock and terrain
  operations keep the parallel state mutually safe in the same way as the GUI editor.
- Generate applies edits transactionally before generation. Repair applies edits before
  validation/generation; experimental repair first reopens zero-valued holes imported
  from the file, then applies edits, so an explicit edited tile `0` remains drawn.
  Resizes therefore affect terrain checks, progress totals, generation, and output.

Plain `.map`, `.mar`, and `.tmx` files store tile values, not the GUI/Core `drawn`,
`locked`, or `terrain` arrays. Those state edits are meaningful when consumed by a
following generation/repair step in the **same spec execution**. Standalone `map edit`
rejects erase, lock, terrain, and clear-state actions plus
`drawn`/`locked`/`terrain` matrices rather than silently discarding them; standalone
`set-tile`, resize, and conversion operations are safe to serialize.

### Batch manifest (v1)

`batch --manifest` reads a `Map_Job_Manifest` document: `"version": 1` plus a top-level `jobs` array, where each entry is itself a `Map_Job_Spec` (same schema and validation as above,
including its own `operation`, dimensions, constraints, ordered `edits`, and optional
seed). `operation: "edit"` dispatches through the same single-job implementation as
`map edit`; nested batch operations remain unsupported. Jobs run independently and are
not otherwise reproducible from a single batch-wide seed the way `generate --count`
is - give each generation/repair job its own `seed` if needed.

```json
{
  "version": 1,
  "jobs": [
    {
      "version": 1,
      "operation": "edit",
      "width": 8,
      "height": 6,
      "tileset": "FE6 - Fields - 01020304",
      "output": "maps/layout.map",
      "edits": [
        { "action": "set-tile", "x": 2, "y": 1, "tile": 17 }
      ]
    },
    {
      "version": 1,
      "operation": "generate",
      "width": 10,
      "height": 10,
      "tileset": "FE6 - Fields - 01020304",
      "output": "maps/field-1.map",
      "seed": 1
    },
    {
      "version": 1,
      "operation": "repair",
      "input": "maps/field-2.mar",
      "output": "maps/field-2.repaired.mar",
      "width": 30,
      "height": 20,
      "tileset": "FE6 - Fields - 01020304"
    }
  ]
}
```

### Sidecar and format notes

- `--assets-dir`, `--tileset-image`, and `--generation-data` override where bundled tileset assets are read from (an explicit tileset selector is still required to pick a bundled `Tileset_Data.xml` entry and to name the output, unless both a PNG and a `.dat` override are supplied).
- MAR input requires positive width/height and a tileset identifier from `--width`/`--height`/`--tileset`, a JSON job spec or manifest entry, or a directory-repair sidecar. Every MAR job is preflighted before its output is changed; directory and manifest modes preflight all MAR jobs before writing any batch output. These values are never inferred because the MAR format does not store them. Generated MAR output already gets dimensions from the generation request. MAR encodes each non-negative tile as `tileIndex * 32` in a signed 16-bit little-endian grid, so tile indices must be between `0` and `1023`.
- TMX input supports explicit `<tile gid="...">` elements, CSV, and uncompressed, gzip-compressed, or zlib-compressed base64 layer data. zstd and other encodings/compressions are rejected with an actionable error. TMX output continues to write explicit tile elements.
- Text `.map` embeds its tileset identifier and dimensions. TMX embeds dimensions, a
  tileset name, and an image source. MAR embeds none of those values. `map edit`
  preserves same-format `.map`/`.tmx` metadata by default, resolves bundled names and
  rebases PNG paths relative to the destination TMX, uses `--tileset-image` as an
  explicit TMX image override, and emits the short trailing identifier (for example
  `01020304`) when a bundled tileset is converted to text `.map`. The image itself is
  not copied.
- `map inspect --json` emits one JSON object with stable keys: `path`, `format`,
  `width`, `height`, `tileset`, `tilesetImageSource`, and `tiles` (row-major
  `[y][x]`). MAR inspection requires `--width`, `--height`, and `--tileset`.

### Front-end parity and non-goals

| Capability | Avalonia GUI | CLI |
| --- | --- | --- |
| Paint/erase, rectangle, flood-fill, resize | Interactive | Ordered `Map_Job_Spec.edits` |
| Drawn/locks/terrain constraints | Interactive and persistent in session | Applied within one edit/generate/repair spec execution |
| `.map`/`.mar`/`.tmx` open/save/convert | Yes | `map edit` |
| Map metadata/tile inspection | Visual editor | `map inspect` / `--json` |
| Tileset/terrain discovery | Bundled pickers | `tilesets list/terrain`, including `--json` |
| Generate/repair | Yes | Direct, homogeneous batch, or manifest |
| Undo/redo | Yes | No; specs are declarative, ordered, and transactional |

The CLI is not an interactive renderer, image/tileset authoring tool, or replacement
for GUI undo history. It does not invent MAR metadata, persist lock/terrain state inside
plain map formats, edit TMX object layers/properties beyond the supported tile layer, or
silently coerce unsupported format metadata.

### Safe output, incomplete results, and exit codes

- Every write goes through an overwrite guard: an existing `--output` target is left untouched unless `--force` is passed. Explicit `--in-place` is itself consent to replace the input. In both cases, the new file is written to a sibling temp file first and moved into place atomically.
- By default an incomplete result (cells the generation engine could not resolve) is still written and reported, but exits with code `2`. `--allow-incomplete` treats that same result as success (exit `0`). `--require-complete` instead suppresses the write entirely (exit `2`) if any cell remains unresolved. `--allow-incomplete` and `--require-complete` are mutually exclusive.
- These incomplete-output policies are unchanged by `--algorithm`. Experimental mode tracks unresolved cells separately from legitimate tile index `0` while searching, then serializes unresolved cells using the existing map-format convention.
- Exit codes: `0` success, `1` error (bad arguments, missing files, invalid data), `2` incomplete result (see above), `3` batch-failed (`generate --count`, `repair --input-dir`, or `batch --manifest`: at least one job failed or was incomplete, or the run stopped early via `--fail-fast`/cancellation before every job was attempted).
- `generate --count` derives each job's seed deterministically from a single `--seed`/spec seed and its 1-based job index (via a splitmix64-style mixer), so the whole batch is reproducible from one seed while every job still gets an independent-looking seed; omit `--seed` and each job instead gets its own randomly chosen seed (still printed per job).
