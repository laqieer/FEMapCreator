# Experimental Solver Benchmark

**Date:** 2026-07-15

**Build:** Release, .NET 10, Windows x64

This benchmark compares the default legacy frontier solver with the opt-in experimental
constraint solver. It records one fixed-seed run per case, including process startup
time. The experimental solver used its default 10,000-node backtracking budget.

## Configuration

Generation:

```powershell
dotnet FE_Map_Creator.Cli.dll generate `
  --width <width> --height <height> `
  --tileset <id> --output <file> `
  --algorithm <legacy|experimental> `
  --depth 1 --seed 42 `
  --experimental-search-node-limit 10000
```

Repair:

```powershell
dotnet FE_Map_Creator.Cli.dll repair `
  --input <damaged-corpus-map> --output <file> `
  --tileset <id> --algorithm <legacy|experimental> `
  --repair-radius 1 --depth 1 --seed 42 `
  --experimental-search-node-limit 10000
```

Tilesets:

| Game | Selector |
|---|---|
| FE6 Fields | `01020304` |
| FE7 Fields | `1c1d1e1f` |
| FE8 Fields | `01000203` |

Exit code `0` means complete; exit code `2` means the output was written but contains
unresolved cells.

## Blank-map generation

### 4x3

| Game | Algorithm | Exit | Unresolved | Budget exhausted | Time |
|---|---:|---:|---:|---:|---:|
| FE6 | legacy | 2 | 4 | no | 369 ms |
| FE6 | experimental | 0 | 0 | no | 369 ms |
| FE7 | legacy | 0 | 0 | no | 300 ms |
| FE7 | experimental | 0 | 0 | no | 372 ms |
| FE8 | legacy | 2 | 2 | no | 306 ms |
| FE8 | experimental | 0 | 0 | no | 370 ms |

The experimental solver completed all three small maps. Legacy completed one of three.
Experimental runtime was equal to or 64-72 ms slower in these single runs.

### 20x15

| Game | Algorithm | Exit | Unresolved | Budget exhausted | Time |
|---|---:|---:|---:|---:|---:|
| FE6 | legacy | 2 | 19 | no | 427 ms |
| FE6 | experimental | 2 | 26 | yes | 705 ms |
| FE7 | legacy | 2 | 27 | no | 712 ms |
| FE7 | experimental | 2 | 31 | yes | 837 ms |
| FE8 | legacy | 2 | 28 | no | 442 ms |
| FE8 | experimental | 2 | 35 | yes | 910 ms |

At 20x15, both algorithms were incomplete. Experimental mode exhausted the 10,000-node
budget in every case and returned the best assignment found so far; it did not prove a
minimum unresolved count. With this budget and seed it was 125-468 ms slower and left
4-7 more unresolved cells than legacy.

## Corpus-map repair

One nonzero interior tile in each source map was changed to `0`, then repaired with
radius 1:

| Game | Source map | Size |
|---|---|---:|
| FE6 | `FE6 Maps\0102xx04\Chapter1BreathofDestiny.map` | 15x21 |
| FE7 | `FE7 Maps\1c1dxx1f\Ch9AGrimReunion.map` | 20x15 |
| FE8 | `FE8 Maps\0100xx03\Ch2.map` | 15x15 |

| Game | Algorithm | Exit | Unresolved | Budget exhausted | Time |
|---|---:|---:|---:|---:|---:|
| FE6 | legacy | 0 | 0 | no | 269 ms |
| FE6 | experimental | 0 | 0 | no | 333 ms |
| FE7 | legacy | 0 | 0 | no | 288 ms |
| FE7 | experimental | 0 | 0 | no | 377 ms |
| FE8 | legacy | 0 | 0 | no | 269 ms |
| FE8 | experimental | 0 | 0 | no | 329 ms |

Both algorithms repaired every sampled hole completely. Experimental repair was 60-89 ms
slower in these runs and did not exhaust its budget.

## Conclusions

- Keeping legacy as the default is appropriate.
- Experimental search is effective on the sampled small blank maps and demonstrates the
  intended global-backtracking benefit.
- The default 10,000-node budget is insufficient to improve these 20x15 blank-map cases;
  budget exhaustion is therefore surfaced in API/CLI results.
- Experimental repair is functional on the sampled real maps, but offered no completion
  advantage and had higher runtime.

## Limitations

- One seed (`42`) and one run per case; timings are not statistical benchmarks.
- Process startup is included.
- Only Fields tilesets were sampled.
- Generation used blank maps; templates, locks, and terrain constraints may change the
  comparison substantially.
- Repair used one interior hole and radius 1.
- Node-limit tradeoffs above and below 10,000 were not measured.
