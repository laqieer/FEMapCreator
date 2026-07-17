# Legacy Editor Guide

The legacy WinForms editor is the original Windows-only FE Map Creator interface. Use the shared Avalonia GUI for normal cross-platform editing; use the legacy editor when a workflow depends on specialized import, tileset processing, or adjacency-data tools that have not been ported.

## Build and start

Install the .NET 10 SDK on Windows, clone the repository, then run:

```powershell
dotnet run --project .\FE_Map_Creator\FE_Map_Creator.csproj -c Release
```

The application resolves runtime assets from its output directory. Keep the copied `Tilesets`, `Tileset Generation Data`, `Tileset_Data.xml`, `Terrain_Data.xml`, and related files beside the executable.

## Main workflows

- Create, open, save, generate, and repair Fire Emblem maps.
- Import image/MAR data through the legacy import dialogs.
- Process tileset images and regenerate learned adjacency data from map corpora.
- Select tiles/brushes in `Tileset_Palette_Form`.
- Assign terrain tags in `Terrain_Palette_Form`.
- Edit weighted tile adjacency in `Tile_Edit_Form`.
- Resize maps and perform focused MAR import through their dedicated dialogs.

## Tileset generation data

Generated adjacency settings are binary files under `Tileset Generation Data`. Their basename must exactly match the selected tileset PNG basename.

When learning from a corpus, use maps from the matching FE6/FE7/FE8 tileset family. Adjacency entries are weighted observations, not simple true/false flags, and reverse-direction entries must remain consistent.

## Safety and compatibility

- Tiles are always 16x16 pixels.
- Keep backups before tileset processing, import, or bulk generation-data changes.
- Do not interrupt a long-running legacy operation by killing the process; use the available cooperative cancellation path.
- `.mar` files still require externally supplied width, height, and tileset metadata.
- The shared GUI/CLI receive new general-purpose features first; confirm behavior there before assuming exact legacy parity.

## Support

When reporting a legacy-editor defect, select **Legacy WinForms GUI** in the bug report and include the operation, Windows version, tileset, dimensions, and exact error text.
