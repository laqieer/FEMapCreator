# FE Map Creator

A Fire Emblem focused map creation tool, originally published at:

https://bwdyeti.com/programs/#MapGen

The solution has two front ends over a shared `FE_Map_Creator.Core` generation engine:

- **GUI** (`FE_Map_Creator\FE_Map_Creator.csproj`) - Windows-only WinForms application.
- **CLI** (`FE_Map_Creator.Cli\FE_Map_Creator.Cli.csproj`) - cross-platform .NET 10 console tool (Windows, Linux, macOS) for scripted/headless generation, repair, and batch workflows.

## Requirements

- .NET 10 SDK for development (all projects), .NET 10 SDK or matching runtime to run the published CLI.
- Windows only for the GUI: Windows to build/run/publish it, and the .NET 10 Desktop Runtime for the framework-dependent published application.

## GUI: build, run, test, publish

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
```

Generate a blank map (every cell open, filled by the generation engine):

```powershell
FE_Map_Creator.Cli.exe generate --width 30 --height 20 --tileset "FE6 - Fields - 01020304" --output map.map --seed 12345
```

The existing frontier generator remains the default. Opt into the experimental global
constraint solver with `--algorithm experimental`:

```powershell
FE_Map_Creator.Cli.exe generate --width 30 --height 20 --tileset "FE6 - Fields - 01020304" --output map.experimental.map --algorithm experimental --depth 2 --seed 12345
```

The experimental solver globally backtracks to avoid greedy dead ends. It splits open
cells into independent components, applies arc-consistency propagation, orders
candidates by remaining neighbor support plus learned corpus weights, and uses
deterministic partial/complete restarts. Complete restarts use conflict-directed
backjumping and a bounded exact nogood cache. The defaults are 10,000 total search nodes,
4 restarts, and 4,096 retained nogoods; tune them with
`--experimental-search-node-limit`, `--experimental-restarts`, and
`--experimental-nogood-limit`. Use `--no-experimental-conflict-learning` for a
chronological comparison. A zero-unresolved result is optimal; budget exhaustion is
reported when an incomplete search is cut off. The worst case remains exponential, so
the solver stays opt-in and cancellable. Use
`--algorithm legacy` explicitly to override an experimental job spec. In WinForms, the
unchecked **Map Generation > Experimental Constraint Solver** menu item controls both
generation and repair for the current session.

See [`docs/experimental-solver-benchmark.md`](docs/experimental-solver-benchmark.md) and
`scripts\benchmark-solvers.ps1` for the reproducible legacy/experimental/hybrid matrix
and default-promotion gates.

For large maps, `--algorithm hybrid` runs the legacy solver first, groups its unresolved
cells into independent regions, and applies the constraint solver only inside adaptive
Manhattan halos. Completed regions are retained while only unresolved regions expand.
Hybrid output is guaranteed to have no more unresolved cells than its legacy baseline:

```powershell
FE_Map_Creator.Cli.exe generate --width 30 --height 20 --tileset "FE6 - Fields - 01020304" --output map.hybrid.map --algorithm hybrid --hybrid-initial-halo 1 --hybrid-max-halo 3 --seed 12345
```

WinForms exposes mutually exclusive **Experimental Constraint Solver** and
**Hybrid Legacy + Constraint Solver** menu toggles; both are unchecked by default.

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

Run a heterogeneous batch of generate/repair jobs from one manifest:

```powershell
FE_Map_Creator.Cli.exe batch --manifest manifest.json --fail-fast
```

### JSON job spec (v1)

`Map_Job_Spec` documents (`"version": 1` required) drive `--spec` for a single `generate`/`repair` job. All fields are optional except `Version`; explicit CLI options take precedence over spec values, and spec-relative paths (`Input`, `Template`, `Output`, asset overrides) resolve relative to the spec file's own directory.

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
  ]
}
```

Here `template.map`'s border tiles are fixed by `locked` (also implicitly `drawn`, since with a template and no explicit spec `drawn` matrix, `drawn` defaults to the `locked` mask), the top-left interior block requires terrain tag `3`, the bottom-right interior block forbids terrain tag `5`, and every other interior cell is left open for the experimental solver to fill. Omit `algorithm` or set it to `legacy` to retain the default frontier algorithm.

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

### Batch manifest (v1)

`batch --manifest` reads a `Map_Job_Manifest` document: `"version": 1` plus a top-level `jobs` array, where each entry is itself a `Map_Job_Spec` (same schema and validation as above, including its own `operation`, `width`/`height`, `drawn`/`locked`/`terrain`, and optional `seed`). Jobs run independently and are not otherwise reproducible from a single batch-wide seed the way `generate --count` is - give each job its own `seed` if you need every job individually reproducible.

```json
{
  "version": 1,
  "jobs": [
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

### Safe output, incomplete results, and exit codes

- Every write goes through an overwrite guard: an existing `--output` target is left untouched unless `--force` is passed. Explicit `--in-place` is itself consent to replace the input. In both cases, the new file is written to a sibling temp file first and moved into place atomically.
- By default an incomplete result (cells the generation engine could not resolve) is still written and reported, but exits with code `2`. `--allow-incomplete` treats that same result as success (exit `0`). `--require-complete` instead suppresses the write entirely (exit `2`) if any cell remains unresolved. `--allow-incomplete` and `--require-complete` are mutually exclusive.
- These incomplete-output policies are unchanged by `--algorithm`. Experimental mode tracks unresolved cells separately from legitimate tile index `0` while searching, then serializes unresolved cells using the existing map-format convention.
- Exit codes: `0` success, `1` error (bad arguments, missing files, invalid data), `2` incomplete result (see above), `3` batch-failed (`generate --count`, `repair --input-dir`, or `batch --manifest`: at least one job failed or was incomplete, or the run stopped early via `--fail-fast`/cancellation before every job was attempted).
- `generate --count` derives each job's seed deterministically from a single `--seed`/spec seed and its 1-based job index (via a splitmix64-style mixer), so the whole batch is reproducible from one seed while every job still gets an independent-looking seed; omit `--seed` and each job instead gets its own randomly chosen seed (still printed per job).
