# Getting Started

## Web app

Open <https://laqieer.github.io/FEMapCreator/>.

The browser build uses the browser's file picker. It cannot silently read or overwrite arbitrary local paths:

- **Open** always requires selecting a file.
- **Save As** asks for a destination or downloads a file, depending on browser support.
- Keep a local backup before replacing an important map.
- For `.mar` files, select the correct tileset and enter the map width and height before opening because MAR does not store that metadata.

Use a current Chromium-, Firefox-, or Safari-based browser. If file access fails, first retry after allowing downloads/file access for the site.

## Desktop GUI

1. Open the [latest release](https://github.com/laqieer/FEMapCreator/releases/latest).
2. Download the package for your platform:
   - Windows x64 ZIP
   - Linux x64 tarball
   - macOS x64 tarball for Intel
   - macOS arm64 tarball for Apple Silicon
3. Extract the complete archive.
4. Run `FE_Map_Creator.Gui.Desktop` (or the `.exe` on Windows).

Desktop packages are self-contained and do not require a separate .NET installation.
Each desktop package also includes the matching self-contained CLI in its `CLI` directory.

### macOS first launch

The macOS app is ad-hoc signed. If Gatekeeper blocks the first launch:

1. Control-click **FE Map Creator.app**.
2. Select **Open**.
3. Confirm the launch.

## CLI

Open the `CLI` directory inside a desktop release package, or build the CLI from source with the .NET 10 SDK:

```powershell
dotnet build ./FE_Map_Creator.Cli/FE_Map_Creator.Cli.csproj -c Release
dotnet run --project ./FE_Map_Creator.Cli/FE_Map_Creator.Cli.csproj -c Release -- --help
```

List bundled tilesets:

```powershell
# Windows PowerShell
.\FE_Map_Creator.Cli.exe tilesets list

# Linux or macOS
./FE_Map_Creator.Cli tilesets list
```

Continue with the [CLI User Guide](CLI-User-Guide.md).

## Legacy WinForms editor

The legacy editor is Windows-only and is not included in the cross-platform release archives. Build and run it from source with the .NET 10 SDK:

```powershell
dotnet run --project .\FE_Map_Creator\FE_Map_Creator.csproj -c Release
```

Use it for specialized tileset processing, image/MAR import, adjacency editing, and other workflows that have not been ported to the shared GUI. Continue with the [Legacy Editor Guide](Legacy-Editor-Guide.md).

## Verify the version

When reporting a problem, include:

- the release tag or commit;
- Web, desktop, CLI, or legacy WinForms;
- operating system and browser version when applicable.
