# FE Map Creator repository instructions

## Build, run, test, and publish

- This is a Windows-only C# WinForms application using SDK-style .NET 10 projects. `global.json` selects the .NET 10 SDK.
- Build the complete solution:

  ```powershell
  dotnet build .\FE_Map_Creator\FE_Map_Creator.sln -c Release
  ```

- Run the application:

  ```powershell
  dotnet run --project .\FE_Map_Creator\FE_Map_Creator.csproj -c Release
  ```

- Run all tests:

  ```powershell
  dotnet test .\FE_Map_Creator\FE_Map_Creator.sln -c Release
  ```

- Run the binary-serialization test alone:

  ```powershell
  dotnet test .\FE_Map_Creator.Tests\FE_Map_Creator.Tests.csproj -c Release --filter "FullyQualifiedName~TileDataTests.BinaryRoundTripPreservesPriorities"
  ```

- Publish a framework-dependent build:

  ```powershell
  dotnet publish .\FE_Map_Creator\FE_Map_Creator.csproj -c Release
  ```

  The publish directory is `FE_Map_Creator\bin\Release\net10.0-windows\publish\`. Tileset PNGs, generation `.dat` files, XML metadata, and `Terrain_Images.png` are copied there automatically.
- There is no separate lint command or lint configuration.

## Architecture

- `FE_Map_Creator\Program.cs` launches `FE_Map_Creator_Form`, which owns nearly all application state and workflows: map loading/saving, drawing tools, rendering, undo, image import, terrain tagging, tileset processing, map generation, and repair.
- The owned tool windows are `Tileset_Palette_Form` for tile/brush selection, `Terrain_Palette_Form` for terrain tags, and `Tile_Edit_Form` for editing adjacency weights. `Map_Resize_Form` and `Mar_Import_Form` handle focused dialogs.
- Maps are represented by parallel `[x, y]` arrays: `Map_Tiles`, `Drawn_Tiles`, `Locked_Tiles`, and `Terrain_Types`. `Drawn_Tiles` distinguishes generated/open cells from explicitly placed tiles, including tile index `0`; `Locked_Tiles` preserves cells during regeneration.
- Tileset generation has three layers:
  1. `Tile_Matching_Data` compares 16x16 tile image regions and groups tiles with matching sides or identical pixels.
  2. `Tile_Data` stores a tile priority and weighted valid neighbors; `Tileset_Generation_Data` stores these records plus canonical mappings for identical tiles.
  3. `FE_Map_Creator_Form.setup_tileset_data` learns adjacency weights from every `.map` file in a selected corpus folder, and `generate_map` uses those weights plus terrain constraints to fill open cells.
- `FEXNA_Library\FEXNA_Library.csproj` is now a small .NET 10 compatibility library containing only the `Data_Tileset` and `Data_Terrain` DTOs used by the application. The other decompiled files under `FEXNA_Library\` are excluded reference material; do not add them to compilation unless intentionally porting the full legacy XNA library.
- `App_Paths` resolves runtime files from `AppContext.BaseDirectory`. Generated settings are binary files in `Tileset Generation Data\`, and their basename must exactly match the selected tileset PNG basename.
- `Tileset_Data.xml` and `Terrain_Data.xml` provide tileset/terrain metadata. The `FE6 Maps\`, `FE7 Maps\`, and `FE8 Maps\` trees are source-map corpora grouped by tileset family for regenerating adjacency data.

## Repository-specific conventions

- Preserve the decompiled style in existing application code: file-scoped namespaces, `#nullable disable`, two-space indentation, snake_case methods/properties, and existing field names. Avoid broad formatting or cleanup unrelated to the change.
- WinForms classes are not split into `.Designer.cs` partial classes. Control fields and `InitializeComponent()` are in the same form `.cs` file, with a paired `.resx`; make UI changes there rather than assuming the usual designer layout.
- Tiles are always 16x16 pixels. A tileset tile index is `x + y * tilesetWidth`; a map cell index is `x + y * Map_Width`.
- Cardinal directions use numeric-keypad values throughout adjacency data: `2 = down`, `4 = left`, `6 = right`, `8 = up`; the opposite direction is `10 - dir`. Corner matching uses `1`, `3`, `7`, and `9`.
- `Tile_Data.Valid_Tile_Priority[dir][neighbor]` is a weight, not just a validity flag. Adjacency edits must preserve reverse-direction entries and apply through the matching/identical tile groups as `set_tile_data` does.
- Text `.map` files use line 1 for the tileset name/identifier, line 2 as `<height> <width>`, then `height` rows containing `width` tile indices. In memory the same data is stored as `[width, height]`. `.mar` stores row-major signed 16-bit values as `tileIndex * 32`; TMX GIDs are converted relative to `firstgid`.
- Binary dictionary serialization lives in `MapGenDictionaryExtension\Extensions.cs`. Keep its byte layout compatible with existing files and extend `TileDataTests` when changing it.
- Image-heavy code deliberately disposes replaced `Bitmap`, `Image`, and `Graphics` instances and pairs `LockBits` with `UnlockBits`. Preserve that ownership pattern to avoid locked asset files and GDI handle leaks.
- Generation and repair use raw `Thread` instances. Keep the `Updating` guard and marshal UI work through the existing `InvokeRequired`/`BeginInvoke` pattern. Long-lived threads must use cooperative cancellation; `Thread.Abort` is unsupported on .NET 10.
- `.vs`, `bin`, and `obj` are ignored generated outputs. Do not force-add IDE caches or build artifacts.
