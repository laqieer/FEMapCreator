# Experimental Branch Arc Consistency Paired Benchmark

**Date:** 2026-07-17

**Implementation commit:** `56816ad0e11fe948b642add2c84a8b24ec326a9e`
(`feat: add experimental branch arc consistency`)

**Environment:** Microsoft Windows 11 Enterprise 64-bit, build 26200; AMD EPYC
7763 (16 logical processors exposed); .NET SDK 10.0.302; Microsoft.NETCore.App
10.0.10; PowerShell 7.6.3; x64 process.

**Harness:** `scripts\benchmark-solvers.ps1`

## Hypothesis and feature definition

The default-off `--experimental-branch-arc-consistency` flag changes only the
experimental solver's complete-search path. The baseline complete search checks
assigned neighbors while branching. The flagged path additionally establishes
fixed-point cardinal arc consistency at the complete-search root and after each
complete-search assignment, with domain changes restored through the existing
trail.

The hypothesis was that the extra propagation would expose contradictions
earlier and reduce search, conflict learning, nogood reuse, backjumping, budget
cuts, or wall time on constrained cases. The expected risk was extra propagation
cost on easy cases.

The paired variants were:

| Result label | CLI options |
|---|---|
| `experimental-baseline` | `--algorithm experimental` |
| `experimental-branch-ac` | `--algorithm experimental --experimental-branch-arc-consistency` |

Depth 1, a 10,000-node limit, four restarts, a 4,096-entry nogood limit, and
conflict learning enabled were identical within every pair.

## Harness changes

The existing harness now has two optional switches:

- `-BranchArcComparison` selects the two paired variants above.
- `-Focused` selects FE6 Castle `2c002d2e` and FE8 Fields `01000203`, omits the
  4x3 case, and keeps the representative 20x15 generation and repair stresses.

Without `-BranchArcComparison`, the prior `legacy`, `experimental`, and `hybrid`
matrix, ordering, filenames, promotion gates, and default seed behavior remain.
A 60-run focused compatibility execution produced all three algorithms with
zero validation or determinism failures. It retained the prior nonzero exit
behavior because the existing unresolved/diversity promotion gates found one
known FE6 Castle terrain regression.

Comparison mode alternates which variant executes first. The broad run had
72 baseline-first and 72 branch-AC-first pairs; the focused run had 50 and 50.
Each result records command arguments, runtime, unresolved count, completion,
nodes, budget status, validation, SHA-256, diversity, ordering, restart count,
nogoods learned/reused, and backjumps. The CLI does not currently print
propagation-removal counts, so the harness does not use a brittle parser or
change CLI output to obtain them.

In addition to the existing `results.json`, `results.csv`, and `summary.md`,
comparison mode writes `pairs.json`, `pairs.csv`, `paired-summary.json`,
`paired-summary.csv`, `determinism.json`, and `determinism.csv`.

## Methodology

Every timed operation launched the Release CLI in a new `dotnet` process. Timing
therefore includes CLI startup and runtime initialization, but excludes the
separate validation subprocess that follows each operation. Template preparation
for the locked-wall case is also outside the paired timing.

Each pair used the same tileset, source/template/spec, scenario, dimensions,
seed, repeat number, depth, limits, and conflict-learning setting. Only the
output path, execution order, and branch-AC flag differed. Every produced map
was validated with the existing harness behavior. Repeats for each exact
variant/case/seed were SHA-256 checked for determinism.

The diversity definitions and visual-tile identity grouping are unchanged from
the [solver benchmark and promotion gates](experimental-solver-benchmark.md):
Shannon entropy over pixel-identity groups, dominant visual-tile share, and
same-pixel-tile cardinal-neighbor share. Serialized zero is excluded for
incomplete maps. The fixed-point terminology follows the distinction discussed
in [Map Generation Methodology in `Sunnigen/godot-wfc`](map-generation-methodology-in-godot-wfc.md).

### Broad correctness/determinism smoke

- Tilesets: FE6/FE7/FE8 Fields and Castle (all six normal selectors).
- Seeds: `7`, `42`.
- Repeats: 2.
- Cases per tileset: blank 4x3, blank 20x15, constrained terrain 20x15,
  disconnected/locked 20x15, radius-1 repair, and radius-2 repair.
- Total: 144 pairs, 288 timed runs, and 288 validations.

The 4x3 cases are smoke coverage only and are excluded from performance
conclusions.

### Focused performance comparison

- Tilesets: FE6 Castle `2c002d2e`; FE8 Fields `01000203`.
- Seeds: `7`, `42`.
- Repeats: 5.
- Cases: blank 20x15; constrained terrain 20x15; a 20x15 template split by a
  locked vertical wall; radius-1 single-hole repair; radius-2 three-hole repair.
- Repair source sizes: FE6 Castle 30x24; FE8 Fields 15x15.
- Total: 100 pairs, 200 timed runs, and 200 validations.
- Each exact tileset/scenario case therefore has 10 paired samples.

The constrained terrain spec requires tag 1 on the left half and forbids tag 1
on the right half. The disconnected case uses the same generated template for
both variants. Repair diversity uses the existing common comparison mask.

An initial focused execution exposed a harness-only PowerShell nested-array
flattening bug in the new single-dimension focused selection. The harness was
fixed and the complete focused matrix was rerun. No measurements from the
invalid execution are included below.

## Reproduce

These are the commands used for the reported data:

```powershell
dotnet build .\FE_Map_Creator\FE_Map_Creator.sln -c Release

.\scripts\benchmark-solvers.ps1 `
  -BranchArcComparison `
  -Quick `
  -RepeatCount 2 `
  -SkipBuild `
  -OutputDirectory "$env:TEMP\FEMapCreator-branch-ac-smoke-20260717"

.\scripts\benchmark-solvers.ps1 `
  -BranchArcComparison `
  -Focused `
  -Quick `
  -RepeatCount 5 `
  -SkipBuild `
  -OutputDirectory "$env:TEMP\FEMapCreator-branch-ac-focused-20260717"
```

The backward-compatibility smoke used:

```powershell
.\scripts\benchmark-solvers.ps1 `
  -Focused `
  -Quick `
  -RepeatCount 1 `
  -SkipBuild `
  -OutputDirectory "$env:TEMP\FEMapCreator-solver-compat-smoke-20260717"
```

## Correctness and determinism gates

| Matrix | Timed runs | Pairs | Determinism checks | Validations failed | Determinism failed | Equal baseline/AC hashes | Complete baseline/AC | Budget cuts baseline/AC |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Broad smoke | 288 | 144 | 144 | 0 | 0 | 144/144 | 140/140 | 4/4 |
| Focused | 200 | 100 | 40 | 0 | 0 | 100/100 | 90/90 | 10/10 |

Measured fact: every paired baseline and branch-AC output had the same SHA-256
hash. The flag caused no observed completion, unresolved-count, validation,
determinism, or output-content change in these matrices.

## Focused paired results

Speedup is paired baseline wall time divided by branch-AC wall time. Values above
1 mean branch AC was faster. Node and unresolved columns show baseline/branch AC
medians.

| Exact case | Pairs | Baseline median ms | Branch AC median ms | Geometric-mean speedup | Median nodes B/AC | Median unresolved B/AC | Complete B/AC | Budget cuts B/AC |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| **Overall** | **100** | **956.5** | **821.0** | **1.283x** | — | **0 / 0** | **90 / 90** | **10 / 10** |
| FE6 Castle blank 20x15 | 10 | 928.5 | 922.5 | 1.004x | 5,000 / 5,000 | 0 / 0 | 10 / 10 | 0 / 0 |
| FE6 Castle disconnected 20x15 | 10 | 976.0 | 970.0 | 1.006x | 7,500 / 7,500 | 0 / 0 | 10 / 10 | 0 / 0 |
| FE6 Castle radius-2 repair | 10 | 331.5 | 337.5 | 0.978x | 27.5 / 27.5 | 0 / 0 | 10 / 10 | 0 / 0 |
| FE6 Castle radius-1 repair | 10 | 328.5 | 330.5 | 1.001x | 4.5 / 4.5 | 0 / 0 | 10 / 10 | 0 / 0 |
| FE6 Castle constrained terrain | 10 | 8,731.5 | 744.0 | 11.597x | 10,000 / 10,000 | 59.5 / 59.5 | 0 / 0 | 10 / 10 |
| FE8 Fields blank 20x15 | 10 | 1,494.0 | 1,454.0 | 1.057x | 5,000 / 5,000 | 0 / 0 | 10 / 10 | 0 / 0 |
| FE8 Fields disconnected 20x15 | 10 | 1,366.5 | 1,373.0 | 0.984x | 7,500 / 7,500 | 0 / 0 | 10 / 10 | 0 / 0 |
| FE8 Fields radius-2 repair | 10 | 352.0 | 350.5 | 1.006x | 101 / 101 | 0 / 0 | 10 / 10 | 0 / 0 |
| FE8 Fields radius-1 repair | 10 | 338.0 | 338.5 | 1.000x | 0 / 0 | 0 / 0 | 10 / 10 | 0 / 0 |
| FE8 Fields constrained terrain | 10 | 1,226.5 | 1,212.0 | 1.007x | 5,000 / 5,000 | 0 / 0 | 10 / 10 | 0 / 0 |

The overall 1.283x geometric mean is dominated by FE6 Castle constrained
terrain. Excluding that exact case, the remaining 90 focused pairs have a
1.004x geometric mean: effectively neutral. In the broad smoke, the geometric
mean was 1.078x overall, 1.081x after excluding 4x3, and 0.995x after also
excluding FE6 Castle terrain.

### Representative seed subsets

Each row contains five pairs.

| Case and seed | Baseline median ms | Branch AC median ms | Geometric-mean speedup | Nodes B/AC | Unresolved B/AC |
|---|---:|---:|---:|---:|---:|
| FE6 Castle terrain, seed 7 | 8,850 | 740 | 11.90x | 10,000 / 10,000 | 53 / 53 |
| FE6 Castle terrain, seed 42 | 8,387 | 745 | 11.30x | 10,000 / 10,000 | 66 / 66 |
| FE8 Fields blank, seed 7 | 1,516 | 1,536 | 0.96x | 5,000 / 5,000 | 0 / 0 |
| FE8 Fields blank, seed 42 | 1,472 | 1,172 | 1.16x | 5,000 / 5,000 | 0 / 0 |
| FE8 Fields disconnected, seed 7 | 1,386 | 1,504 | 0.93x | 7,500 / 7,500 | 0 / 0 |
| FE8 Fields disconnected, seed 42 | 1,313 | 1,246 | 1.04x | 7,500 / 7,500 | 0 / 0 |

The direction changes between FE8 seeds illustrate why the small differences
outside the FE6 stress case should not be treated as statistically significant.
The FE6 terrain result remained large whether baseline or branch AC ran first:
11.38x for baseline-first pairs and 11.82x for branch-AC-first pairs.

## Search diagnostics

All focused nogood reuse and backjump activity occurred in FE6 Castle
constrained terrain.

| FE6 Castle terrain median per run | Baseline | Branch AC | AC minus baseline |
|---|---:|---:|---:|
| Search nodes | 10,000 | 10,000 | 0 |
| Restarts | 4 | 4 | 0 |
| Nogoods learned | 2,509 | 1 | -2,508 |
| Nogood hits | 2 | 0 | -2 |
| Backjumps | 641 | 0 | -641 |
| Unresolved cells | 59.5 | 59.5 | 0 |

Measured fact: branch AC did not reduce the reported node count, restarts,
unresolved cells, or budget cuts, but it removed almost all conflict-learning
and backjump activity in this case. Across the ten FE6 terrain runs, baseline
reported 25,090 learned nogoods, 20 hits, and 6,410 backjumps; branch AC reported
10 learned nogoods, no hits, and no backjumps.

Interpretation: the 11.597x wall-time improvement is consistent with fixed-point
propagation avoiding expensive conflict bookkeeping inside the same reported
node budget. This is not a direct propagation-cost attribution because
propagation-removal counts are not present in the stable CLI summary.

## Diversity

Every focused pair produced byte-identical map output, so entropy,
dominant-share, and same-neighbor-share deltas were exactly zero in all 100
pairs. Representative medians are shown below; the branch-AC values are
identical.

| Exact case | Median entropy (bits) | Median dominant share | Median same-neighbor share | Equal hashes |
|---|---:|---:|---:|---:|
| FE6 Castle blank | 5.75 | 0.06 | 0.04 | 10/10 |
| FE6 Castle disconnected | 5.54 | 0.06 | 0.03 | 10/10 |
| FE6 Castle radius-2 repair | 4.44 | 0.10 | 0.07 | 10/10 |
| FE6 Castle radius-1 repair | 3.88 | 0.17 | 0.12 | 10/10 |
| FE6 Castle constrained terrain | 4.54 | 0.34 | 0.45 | 10/10 |
| FE8 Fields blank | 6.89 | 0.04 | 0.01 | 10/10 |
| FE8 Fields disconnected | 6.72 | 0.04 | 0.01 | 10/10 |
| FE8 Fields radius-2 repair | 5.14 | 0.15 | 0.05 | 10/10 |
| FE8 Fields radius-1 repair | 4.33 | 0.24 | 0.07 | 10/10 |
| FE8 Fields constrained terrain | 6.96 | 0.03 | 0.02 | 10/10 |

Branch AC therefore neither improved nor regressed measured diversity. The FE6
terrain case remains incomplete and relatively repetitive; faster exhaustion of
the same search budget is not a quality improvement.

## Interpretation

### Where branch AC helps

- FE6 Castle constrained terrain is a repeatable, large wall-time win:
  8,731.5 ms to 744.0 ms median and 11.597x paired geometric-mean speedup.
- The result persisted across both seeds and both execution orders.
- It eliminated almost all reported nogood/backjump work without changing the
  output, node count, or budget status.

### Where branch AC is neutral or slightly harmful

- Excluding FE6 Castle terrain, the focused geometric mean is 1.004x and the
  broad non-tiny/non-stress geometric mean is 0.995x.
- The largest focused aggregate slowdowns were FE6 Castle radius-2 repair
  (0.978x, about 2.2% slower) and FE8 Fields disconnected (0.984x, about 1.6%
  slower). These differences are small relative to process-startup noise.
- No case gained completion, reduced unresolved cells, used fewer reported
  nodes, or avoided a budget cut.

## Limitations

- This is one Windows x64 machine, two seeds, five focused repeats, and two broad
  repeats, not evidence of statistical significance.
- CLI process startup, JIT, filesystem, antivirus, and host scheduling are part
  of each wall-clock sample. Sub-second differences should not be overread.
- Validation runs are mandatory correctness gates but are not included in the
  measured generation/repair time.
- Only depth 1, 10,000 nodes, four restarts, a 4,096-entry nogood cache, and
  conflict learning enabled were tested.
- The stable CLI summary exposes nodes, restarts, nogoods, hits, and backjumps,
  but not propagation-removal counts.
- FE6 Castle constrained terrain exhausted the budget and remained incomplete
  in both variants. The optimization improved time to the same result, not
  result quality.
- The broad 4x3 cases are correctness smoke only and are excluded from timing
  conclusions.
- The focused repair sources have different dimensions, and only one locked-wall
  topology and one terrain split were tested.
- All paired outputs were identical, so this benchmark did not sample any
  branch-AC-specific output distribution.

## Conclusion

Under the measured workloads, branch arc consistency is **beneficial but
narrowly concentrated**. It is effectively neutral on nine of the ten focused
cases, while reducing FE6 Castle constrained-terrain wall time by 11.597x with
no correctness, determinism, completion, node-count, or diversity regression.

The optimization should remain **default-off** for now. The measured benefit is
dominated by one incomplete, budget-exhausted workload and does not improve map
quality or search-node usage. Promotion would be justified after replication
across more seeds, machines, terrain layouts, and search limits confirms the
stress-case win without introducing meaningful easy-case overhead.
