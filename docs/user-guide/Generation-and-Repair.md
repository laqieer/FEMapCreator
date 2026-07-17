# Generation and Repair

## Generate versus repair

- **Generate** fills every open cell in the current map.
- **Repair** refills holes and incompatible regions while preserving drawn/locked context where possible.

On the desktop GUI, use **Cancel** to cooperatively stop a long-running operation. A canceled or failed desktop operation is rolled back instead of being committed as an editor change.

Browser generation/repair currently runs on the browser UI thread and cannot be interrupted in-app after it starts. Use conservative search limits/restarts, save first, and keep the page open until the operation finishes.

## Algorithms

### Constraint solver

The default whole-map solver propagates adjacency and terrain constraints, backtracks when needed, and reports unresolved cells when its search budget is exhausted.

### Hybrid legacy + constraint

The hybrid solver runs the historical frontier generator first, groups unresolved cells, and applies the constraint solver only to adaptive regions around those cells.

### Legacy frontier

The historical generator is fast and useful for comparison, but it can leave unresolved cells when greedy choices reach a dead end.

## Settings

| Setting | Meaning |
| --- | --- |
| Depth | Legacy lookahead depth (1 or 2) |
| Repair radius | Area around repair holes included in repair |
| Seed | Reproduce a run; blank selects a random seed |
| Search node limit | Maximum complete-search nodes |
| Restarts | Deterministic complete-search attempts |
| Nogood limit | Maximum retained learned conflicts |
| Conflict learning | Enables conflict-directed learning/backjumping |
| Branch arc consistency | Applies stronger propagation after complete-search assignments |
| Hybrid initial/max halo | Starting and maximum hybrid repair-region expansion |

## Reproducibility

Record the seed shown after generation or repair. Reusing the same map state, tileset data, algorithm, settings, and seed should reproduce the same result.

## Incomplete results

An incomplete result means the selected search budget ended with unresolved cells. It is not automatically a corrupt file:

1. review unresolved/open cells;
2. retry with a larger node limit or more restarts;
3. try hybrid or legacy for comparison;
4. simplify conflicting locks or terrain constraints;
5. save under a new name while evaluating alternatives.

Technical methodology and benchmark reports remain in the repository's [`docs/`](https://github.com/laqieer/FEMapCreator/tree/main/docs) directory.
