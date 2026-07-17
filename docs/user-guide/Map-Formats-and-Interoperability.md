# Map Formats and Interoperability

## Text `.map`

Text maps contain:

1. tileset identifier;
2. `<height> <width>`;
3. `height` rows with `width` tile indices.

Example:

```text
01020304
2 3
0 1 2
3 4 5
```

In memory, map arrays use `[x, y]` order even though files are written row by row.

## Binary `.mar`

MAR stores row-major signed 16-bit values as:

```text
encoded value = tile index * 32
```

MAR does **not** contain:

- width;
- height;
- tileset identifier.

Every reader must receive all three from the user, a CLI option, a JSON spec, or a sidecar. Never guess them from file size or nearby assets.

## `.tmx`

TMX maps use orthogonal 16x16 tiles. FE Map Creator reads explicit tile elements, CSV data, and uncompressed/gzip/zlib base64 data.

TMX GIDs are converted relative to `firstgid`. Transform flags are rejected rather than silently changing tile orientation.

When saving or converting TMX, FE Map Creator rebases the image source relative to the destination file. The PNG itself is not copied, so keep the referenced asset available when moving the TMX to another project.

FE Map Creator is a single-layer map editor: it reads the first tileset and first tile layer, then writes a new orthogonal TMX containing one tileset and one tile layer. Additional tilesets, extra tile/object/image layers, custom map/layer properties, and other Tiled project content are not preserved. Never overwrite a richer Tiled project file directly; use **Save As** to a copy and verify the result in Tiled before replacing anything.

## Conversion guidance

- Save a copy before converting formats.
- Use a copy for TMX files that may contain more than one tileset/layer or custom Tiled content.
- Confirm the selected tileset after conversion.
- Supply MAR metadata explicitly.
- Validate generated/converted maps with the CLI when using automation.
- Test TMX output in the target Tiled/project workflow before replacing source assets.
