# CLI User Guide

The CLI is designed for repeatable generation, repair, validation, and batch automation.

Examples below use Windows PowerShell and `.\FE_Map_Creator.Cli.exe`. On Linux or macOS, run `./FE_Map_Creator.Cli` instead; replace PowerShell backtick line continuations with `\` or place the command on one line.

## Discover assets

```powershell
.\FE_Map_Creator.Cli.exe tilesets list
.\FE_Map_Creator.Cli.exe tilesets list --json
.\FE_Map_Creator.Cli.exe tilesets terrain --tileset "FE6 - Fields - 01020304" --json
```

Use `--assets-dir` when assets are stored outside the published CLI directory.

## Create, edit, inspect, and convert maps

`map edit` creates or loads one map, applies an ordered versioned edit spec, and writes once:

```powershell
.\FE_Map_Creator.Cli.exe map edit --spec edit.json
.\FE_Map_Creator.Cli.exe map edit --input chapter.map --output chapter.tmx --assets-dir .
.\FE_Map_Creator.Cli.exe map edit --input chapter.map --spec edits.json --in-place
```

Example edit spec:

```json
{
  "version": 1,
  "operation": "edit",
  "input": "chapter.map",
  "output": "chapter-edited.map",
  "edits": [
    { "action": "set-tile", "x": 3, "y": 2, "tile": 17 },
    {
      "action": "set-tile",
      "shape": "rectangle",
      "x": 5,
      "y": 4,
      "endX": 8,
      "endY": 6,
      "tile": 0
    },
    { "action": "resize", "width": 40, "height": 30 }
  ]
}
```

Supported actions are `set-tile`, `erase`, `lock`, `unlock`, `require-terrain`, `forbid-terrain`, `resize`, `clear-locks`, and `clear-terrain`. Cell actions default to `shape: "cell"`; use `rectangle` or `flood-fill` when needed. Coordinates are zero-based, rectangles are inclusive and clipped to the map, flood fill uses four-way connectivity, and resize preserves the top-left overlap.

Plain map files store tile values only. Use erase, lock, and terrain edits inside the same `generate` or `repair` spec that consumes them; standalone `map edit` rejects transient state that cannot be serialized safely.

Inspect metadata and row-major tiles:

```powershell
.\FE_Map_Creator.Cli.exe map inspect --input chapter.map
.\FE_Map_Creator.Cli.exe map inspect --input chapter.map --json
```

For MAR input, supply `--width`, `--height`, and `--tileset`.

## Generate

```powershell
.\FE_Map_Creator.Cli.exe generate `
  --width 30 `
  --height 20 `
  --tileset "FE6 - Fields - 01020304" `
  --output map.map `
  --seed 12345
```

Select an algorithm with `--algorithm experimental`, `hybrid`, or `legacy`.

## Repair

```powershell
.\FE_Map_Creator.Cli.exe repair `
  --input map.map `
  --output map.repaired.map `
  --seed 12345
```

Use `--in-place` only after keeping a backup. In-place writes are performed atomically.

For MAR input, always provide width, height, and tileset:

```powershell
.\FE_Map_Creator.Cli.exe repair `
  --input map.mar `
  --width 30 `
  --height 20 `
  --tileset "FE6 - Fields - 01020304" `
  --output map.repaired.mar
```

## Validate

```powershell
.\FE_Map_Creator.Cli.exe validate `
  --input map.map `
  --tileset "FE6 - Fields - 01020304"
```

Validation checks learned mutual adjacency and optional terrain/lock constraints supplied by a spec.

## JSON specs and batches

Single jobs can use a versioned JSON spec:

```powershell
.\FE_Map_Creator.Cli.exe map edit --spec edit.json
.\FE_Map_Creator.Cli.exe generate --spec job.json
.\FE_Map_Creator.Cli.exe repair --spec job.json
```

Direct CLI options override values from the spec. Spec-relative paths resolve relative to the spec file.

Run heterogeneous jobs with:

```powershell
.\FE_Map_Creator.Cli.exe batch --manifest manifest.json --fail-fast
```

Directory repair supports sidecars named `<basename>.mapgen.json` or `<filename>.mapgen.json`.

## Output and scripting

- Progress is written to stderr.
- Result paths, seeds, and summaries are written to stdout.
- `--json` discovery/inspection commands emit stable camel-case JSON.
- Exit code `0` means complete success, or an incomplete result explicitly accepted with `--allow-incomplete`.
- Exit code `2` means incomplete generation/repair. By default the incomplete output is written; with `--require-complete`, output is not written.
- Exit code `3` means a batch had a failed/incomplete job or stopped before every job was attempted.
- Other command errors return exit code `1`.

Run `.\FE_Map_Creator.Cli.exe <command> --help` on Windows or `./FE_Map_Creator.Cli <command> --help` on Linux/macOS for every current option. The repository [README CLI section](https://github.com/laqieer/FEMapCreator#cli-build-run-publish) contains complete command/spec examples.
