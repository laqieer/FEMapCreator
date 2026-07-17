param(
  [Parameter(Mandatory = $true)]
  [string] $OutputDirectory,

  [Parameter(Mandatory = $true)]
  [string] $ArchivePath
)

$ErrorActionPreference = "Stop"
$repository_root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repository_root "FE_Map_Creator.Gui.Browser/FE_Map_Creator.Gui.Browser.csproj"
$output_directory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$archive_path = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ArchivePath)

Remove-Item $output_directory -Recurse -Force -ErrorAction SilentlyContinue
New-Item $output_directory -ItemType Directory -Force | Out-Null
New-Item (Split-Path $archive_path -Parent) -ItemType Directory -Force | Out-Null
Remove-Item $archive_path -Force -ErrorAction SilentlyContinue

& dotnet publish $project -c Release -o $output_directory
if ($LASTEXITCODE -ne 0)
{
  throw "Web publish failed."
}

& (Join-Path $PSScriptRoot "smoke-test-web.ps1") -PublishDirectory $output_directory

$wwwroot = Join-Path $output_directory "wwwroot"
New-Item (Join-Path $wwwroot ".nojekyll") -ItemType File -Force | Out-Null

[IO.Compression.ZipFile]::CreateFromDirectory(
  $wwwroot,
  $archive_path,
  [IO.Compression.CompressionLevel]::Optimal,
  $false)

if (-not (Test-Path $archive_path -PathType Leaf))
{
  throw "Web archive was not created: $archive_path"
}

Write-Host "Packaged validated Web wwwroot at $archive_path."
