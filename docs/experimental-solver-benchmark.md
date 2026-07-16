# Solver Benchmark and Promotion Gates

**Date:** 2026-07-16

**Build:** Release, .NET 10, Windows x64

**Harness:** `scripts\benchmark-solvers.ps1`

The benchmark compares `legacy`, `experimental`, and `hybrid` generation/repair through
the published CLI surface. Every produced map is checked by `validate` against learned
adjacency and applicable terrain constraints. Repeated runs with identical inputs are
SHA-256 compared.

## Reproduce

Focused matrix used for this report:

```powershell
.\scripts\benchmark-solvers.ps1 `
  -Quick `
  -RepeatCount 2 `
  -OutputDirectory "$env:TEMP\FEMapCreator-solver-benchmark"
```

Full matrix (adds seed `99`):

```powershell
.\scripts\benchmark-solvers.ps1 `
  -RepeatCount 2 `
  -OutputDirectory "$env:TEMP\FEMapCreator-solver-benchmark-full"
```

The harness writes `results.json`, `results.csv`, generated maps/specs, and `summary.md`
under the selected output directory. It exits nonzero for a validation failure,
nondeterministic output, or any hybrid result worse than its paired legacy result.

## Matrix

- Games: FE6, FE7, FE8.
- Families: Fields and Castle (six bundled tilesets).
- Seeds: `7` and `42`.
- Repeats: 2 per exact case.
- Algorithms: legacy, whole-map experimental, hybrid.
- Generation:
  - blank 4x3 and 20x15 maps;
  - 20x15 maps with required tag-1 cells on the left and forbidden tag-1 cells on the right;
  - 20x15 template maps split by a locked vertical wall.
- Repair:
  - one interior hole, radius 1;
  - three interior holes, radius 2.
- Experimental limits: 10,000 total search nodes, four deterministic restarts, 4,096
  retained exact nogoods.
- Timings include CLI process startup.

## Results

| Scenario | Algorithm | Runs | Complete | Median ms | Worst ms | Median unresolved | Worst unresolved | Budget cuts | Invalid |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| blank | experimental | 48 | 48 | 347.5 | 438 | 0 | 0 | 0 | 0 |
| blank | hybrid | 48 | 28 | 546 | 1647 | 0 | 27 | 20 | 0 |
| blank | legacy | 48 | 6 | 306 | 958 | 10.5 | 46 | 0 | 0 |
| disconnected | experimental | 24 | 24 | 440.5 | 990 | 0 | 0 | 0 | 0 |
| disconnected | hybrid | 24 | 0 | 908.5 | 1508 | 11 | 18 | 24 | 0 |
| disconnected | legacy | 24 | 0 | 463.5 | 1099 | 27.5 | 37 | 0 | 0 |
| repair-multi | experimental | 24 | 24 | 333 | 376 | 0 | 0 | 0 | 0 |
| repair-multi | hybrid | 24 | 24 | 389 | 514 | 0 | 0 | 0 | 0 |
| repair-multi | legacy | 24 | 8 | 260 | 325 | 1 | 6 | 0 | 0 |
| repair-single | experimental | 24 | 24 | 327 | 347 | 0 | 0 | 0 | 0 |
| repair-single | hybrid | 24 | 24 | 266.5 | 281 | 0 | 0 | 0 | 0 |
| repair-single | legacy | 24 | 24 | 260 | 279 | 0 | 0 | 0 | 0 |
| terrain | experimental | 24 | 20 | 780 | 6391 | 0 | 18 | 4 | 0 |
| terrain | hybrid | 24 | 4 | 871 | 1374 | 17.5 | 39 | 20 | 0 |
| terrain | legacy | 24 | 0 | 436 | 834 | 44 | 297 | 0 | 0 |

## Correctness gates

- Validation failures: **0**.
- Determinism failures: **0**.
- Hybrid-worse-than-legacy paired cases: **0**.
- Experimental budget cuts: **4**, all in the constrained-terrain scenario.
- Hybrid retains its promised quality floor but often exhausts the regional budget.

The whole-map experimental solver completed every blank, disconnected, and repair case,
and 20 of 24 constrained-terrain runs. Hybrid improved or matched legacy in every pair,
but remained incomplete on larger blank, disconnected, and terrain-constrained cases
because its budget is divided across regional attempts.

## Conclusion

The whole-map experimental solver is the strongest opt-in choice for map quality: it
resolved every blank, disconnected, and repair case and most constrained-terrain cases,
with no validation or determinism failures. Hybrid is a safe regional fallback because
it never produced more unresolved cells than legacy, but its divided budget left more
large and constrained maps incomplete.

Legacy remains the default. The experimental solver exceeded the 2x worst-runtime limit
and exhausted its node budget in four terrain runs, while this report covers only one
focused matrix rather than the required three consecutive full matrices. Promotion
should be reconsidered only after those runtime, budget, and repetition gates pass.

## Default-promotion criteria

Changing the default away from legacy requires all of the following:

1. Three consecutive **full** matrix runs on documented hardware.
2. Zero validation and determinism failures.
3. Zero candidate-algorithm cases worse than legacy in unresolved count.
4. Candidate median unresolved count no worse than legacy in every scenario.
5. Candidate worst runtime no more than 2x legacy in every scenario.
6. No search-budget exhaustion for the candidate default.
7. Release build and full automated tests remain green.

This report is one focused matrix, not three consecutive full matrices. The whole-map
experimental solver also exceeds the 2x worst-runtime gate and exhausts its budget in
some terrain cases. Hybrid does not meet the runtime or budget-exhaustion gates either.
Legacy therefore remains the default.

## Limitations

- Only two seeds were included in the committed report; the full harness adds a third.
- Process-startup noise is included, so sub-second differences should not be overread.
- Terrain uses a positive tag-1 left half and a negative tag-1 right half, exercising
  both required and forbidden terrain filtering.
- Template coverage uses one locked-wall topology.
- Repair uses center-biased nonzero source cells and two damage patterns.
- Serialized tile `0` is skipped by `validate` because map formats cannot distinguish a
  legitimate tile-zero cell from an unresolved/hole sentinel.
