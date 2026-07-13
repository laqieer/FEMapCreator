# FE Map Creator repository instructions

## Build, run, and validation

- This is a Windows-only C# WinForms application. The two non-SDK projects target .NET Framework 4.0 and came from a dotPeek decompilation.
- Run the checked-in application from the repository root:

  ```powershell
  .\FE_Map_Creator.exe
  ```

  Keep the repository root as the working directory. The application loads `Tileset_Data.xml` and `Terrain_Data.xml` beside the executable, while `Terrain_Images.png` and `Tileset Generation Data\` are accessed through relative paths.
- The source-build diagnostic command is:

  ```powershell
  msbuild .\FE_Map_Creator\FE_Map_Creator.sln /t:Rebuild /p:Configuration=Release /p:LangVersion=latest /p:PlatformTarget=x86
  ```

  It requires a modern Visual Studio/MSBuild installation plus the .NET Framework 4.0 targeting pack. The decompiled source is not currently cleanly rebuildable: the application project contains decompiler-generated assembly metadata that triggers CS1112, and the `FEXNA_Library` source contains invalid obfuscated identifiers.
- `FE_Map_Creator\FE_Map_Creator.csproj` references the checked-in `FE_Map_Creator\lib\FEXNA_Library.dll`, not `FEXNA_Library\FEXNA_Library.csproj`. Editing the library source does not affect the application unless a compatible DLL is rebuilt and deliberately replaced.
- There is no automated test suite, single-test command, or lint configuration. Do not count launching the unchanged checked-in executable as validation of source edits.

## Architecture

- `FE_Map_Creator\Program.cs` launches `FE_Map_Creator_Form`, which owns nearly all application state and workflows: map loading/saving, drawing tools, rendering, undo, image import, terrain tagging, tileset processing, map generation, and repair.
- The owned tool windows are `Tileset_Palette_Form` for tile/brush selection, `Terrain_Palette_Form` for terrain tags, and `Tile_Edit_Form` for editing adjacency weights. `Map_Resize_Form` and `Mar_Import_Form` handle focused dialogs.
- Maps are represented by parallel `[x, y]` arrays: `Map_Tiles`, `Drawn_Tiles`, `Locked_Tiles`, and `Terrain_Types`. `Drawn_Tiles` distinguishes generated/open cells from explicitly placed tiles, including tile index `0`; `Locked_Tiles` preserves cells during regeneration.
- Tileset generation has three layers:
  1. `Tile_Matching_Data` compares 16x16 tile image regions and groups tiles with matching sides or identical pixels.
  2. `Tile_Data` stores a tile priority and weighted valid neighbors; `Tileset_Generation_Data` stores these records plus canonical mappings for identical tiles.
  3. `FE_Map_Creator_Form.setup_tileset_data` learns adjacency weights from every `.map` file in a selected corpus folder, and `generate_map` uses those weights plus terrain constraints to fill open cells.
- Generated settings are binary files in `Tileset Generation Data\`. Their basename must exactly match the selected tileset PNG basename. The checked-in `Tilesets\` PNGs and generation-data files are paired this way.
- `Tileset_Data.xml` and `Terrain_Data.xml` provide FEXNA tileset/terrain metadata. The `FE6 Maps\`, `FE7 Maps\`, and `FE8 Maps\` trees are source-map corpora grouped by tileset family for regenerating adjacency data.

## Repository-specific conventions

- Preserve the decompiled style in touched code: file-scoped namespaces, `#nullable disable`, two-space indentation, snake_case methods/properties, and existing field names. Avoid broad formatting or cleanup of decompiler artifacts.
- WinForms classes are not split into `.Designer.cs` partial classes. Control fields and `InitializeComponent()` are in the same form `.cs` file, with a paired `.resx`; make UI changes there rather than assuming the usual designer layout.
- Tiles are always 16x16 pixels. A tileset tile index is `x + y * tilesetWidth`; a map cell index is `x + y * Map_Width`.
- Cardinal directions use numeric-keypad values throughout adjacency data: `2 = down`, `4 = left`, `6 = right`, `8 = up`; the opposite direction is `10 - dir`. Corner matching uses `1`, `3`, `7`, and `9`.
- `Tile_Data.Valid_Tile_Priority[dir][neighbor]` is a weight, not just a validity flag. Adjacency edits must preserve reverse-direction entries and apply through the matching/identical tile groups as `set_tile_data` does.
- Text `.map` files use line 1 for the tileset name/identifier, line 2 as `<height> <width>`, then `height` rows containing `width` tile indices. In memory the same data is stored as `[width, height]`. `.mar` stores row-major signed 16-bit values as `tileIndex * 32`; TMX GIDs are converted relative to `firstgid`.
- Image-heavy code deliberately disposes replaced `Bitmap`, `Image`, and `Graphics` instances and pairs `LockBits` with `UnlockBits`. Preserve that ownership pattern to avoid locked asset files and GDI handle leaks.
- Generation and repair use raw `Thread` instances. Keep the `Updating` guard and marshal UI work through the existing `InvokeRequired`/`BeginInvoke` pattern when changing background workflows.
- Build probes can touch tracked `.vs`/`obj` cache files, while `bin` outputs are untracked. Review the worktree after builds and do not include cache/output churn in focused changes.
