# GUI User Guide

## Create a map

1. Select a bundled **Tileset**.
2. Enter **Width** and **Height** from 1 to 256.
3. Select **New map**.
4. Choose tiles from the palette and edit the map canvas.

Creating, opening, or discarding a dirty map asks for confirmation.

## Open and save

The shared GUI supports:

- `.map` text maps;
- `.mar` binary tile maps;
- `.tmx` maps for Tiled-compatible workflows.

Use **File > Open**, **Save**, or **Save As**.

For `.mar`, first select the intended tileset and set the correct width and height. MAR stores only tile values, so the editor cannot infer these values from the file.

## Drawing controls

### Brush

- **Brush** edits individual cells while dragging.
- **Rectangle** applies the selected mode to an inclusive rectangle.
- **Flood fill** follows four-way connected cells that match the starting cell's relevant state.

### Mode

- **Paint tile** places the selected tile and marks the cell as explicitly drawn.
- **Erase / reopen** clears the tile and reopens the cell for generation.
- **Lock cells** preserves selected cells during generation/repair.
- **Require terrain** requires the selected terrain tag.
- **Forbid terrain** forbids the selected terrain tag.

Terrain choices come from the selected tileset. A cell cannot remain both locked and terrain-constrained.

### Other editing actions

- **Undo / Redo** restores recent editor states.
- **Resize** applies immediately. It preserves the overlapping top-left region and adds empty cells as needed; shrinking permanently drops cells outside that region, so save a copy first.
- **Clear locks** unlocks every cell.
- **Clear terrain** removes every terrain constraint.
- **Zoom** changes display scale only.

## Suggested workflow

1. Choose a tileset and map dimensions.
2. Paint fixed structures and lock cells that must not change.
3. Erase/reopen cells that generation should fill.
4. Add required or forbidden terrain constraints.
5. Save a tile-layout copy.
6. Run **Generate** or **Repair**.
7. Review the result and save it under a new name.

Map files store tile values and format metadata only. Open/drawn state, locks, terrain constraints, undo/redo history, and dirty state remain in the current editor session and are lost when the file is reopened. For reproducible constraints across sessions, express them in a versioned CLI job spec with `drawn`, `locked`, `terrain`, and ordered `edits`.

See [Generation and Repair](Generation-and-Repair.md) for solver settings.
