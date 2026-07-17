param(
  [Parameter(Mandatory = $true)]
  [string] $Rid,

  [Parameter(Mandatory = $true)]
  [string] $OutputDirectory,

  [Parameter(Mandatory = $true)]
  [string] $ArchivePath,

  [string] $BundleVersion = "0.0.0",

  [switch] $AdHocSignMacBundle
)

$ErrorActionPreference = "Stop"
$repository_root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repository_root "FE_Map_Creator.Gui.Desktop/FE_Map_Creator.Gui.Desktop.csproj"
$readme = Join-Path $repository_root "README.md"
$output_directory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$archive_path = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ArchivePath)
$publish_directory = Join-Path $output_directory "publish"
$normalized_bundle_version = $BundleVersion
if ($normalized_bundle_version.StartsWith("v", [StringComparison]::OrdinalIgnoreCase))
{
  $normalized_bundle_version = $normalized_bundle_version.Substring(1)
}

Remove-Item $output_directory -Recurse -Force -ErrorAction SilentlyContinue
New-Item $publish_directory -ItemType Directory -Force | Out-Null
New-Item (Split-Path $archive_path -Parent) -ItemType Directory -Force | Out-Null
Remove-Item $archive_path -Force -ErrorAction SilentlyContinue

& dotnet publish $project `
  -c Release `
  -r $Rid `
  --self-contained true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o $publish_directory
if ($LASTEXITCODE -ne 0)
{
  throw "Desktop publish failed for $Rid."
}

if ($Rid.StartsWith("osx-", [StringComparison]::Ordinal))
{
  $app = Join-Path $output_directory "FE Map Creator.app"
  $macos_directory = Join-Path $app "Contents/MacOS"
  New-Item $macos_directory -ItemType Directory -Force | Out-Null
  Copy-Item (Join-Path $publish_directory "*") $macos_directory -Recurse -Force
  Remove-Item (Join-Path $macos_directory "README.md") -Force -ErrorAction SilentlyContinue

  $executable = Join-Path $macos_directory "FE_Map_Creator.Gui.Desktop"
  & chmod +x $executable
  if ($LASTEXITCODE -ne 0)
  {
    throw "Could not make the macOS application executable."
  }

  $info_plist = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDisplayName</key><string>FE Map Creator</string>
  <key>CFBundleExecutable</key><string>FE_Map_Creator.Gui.Desktop</string>
  <key>CFBundleIdentifier</key><string>com.laqieer.FEMapCreator</string>
  <key>CFBundleName</key><string>FE Map Creator</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleShortVersionString</key><string>$normalized_bundle_version</string>
  <key>CFBundleVersion</key><string>$normalized_bundle_version</string>
  <key>LSMinimumSystemVersion</key><string>12.0</string>
  <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
"@
  Set-Content (Join-Path $app "Contents/Info.plist") $info_plist -Encoding utf8

  if ($AdHocSignMacBundle)
  {
    & codesign --force --deep --sign - $app
    if ($LASTEXITCODE -ne 0)
    {
      throw "Could not ad-hoc sign the macOS application bundle."
    }
  }

  Remove-Item $publish_directory -Recurse -Force
  New-Item $publish_directory -ItemType Directory -Force | Out-Null
  Move-Item $app $publish_directory
  Copy-Item $readme (Join-Path $publish_directory "README.md")
}

if ($archive_path.EndsWith(".zip", [StringComparison]::OrdinalIgnoreCase))
{
  Compress-Archive -Path (Join-Path $publish_directory "*") -DestinationPath $archive_path
}
elseif ($archive_path.EndsWith(".tar.gz", [StringComparison]::OrdinalIgnoreCase))
{
  & tar -czf $archive_path -C $publish_directory .
  if ($LASTEXITCODE -ne 0)
  {
    throw "Could not create desktop tarball for $Rid."
  }
}
else
{
  throw "Desktop archive must end in .zip or .tar.gz: $archive_path"
}

if (-not (Test-Path $archive_path -PathType Leaf))
{
  throw "Desktop archive was not created: $archive_path"
}

Write-Host "Packaged $Rid desktop build at $archive_path."
