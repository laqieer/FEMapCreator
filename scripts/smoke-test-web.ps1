param(
  [Parameter(Mandatory = $true)]
  [string] $PublishDirectory
)

$ErrorActionPreference = "Stop"
$root = Join-Path $PublishDirectory "wwwroot"
if (-not (Test-Path $root -PathType Container))
{
  throw "Published Web root was not found at $root."
}

$required_files = @(
  "index.html",
  "main.js",
  "app.css",
  "_framework/dotnet.js"
)
foreach ($relative in $required_files)
{
  $path = Join-Path $root $relative
  if (-not (Test-Path $path -PathType Leaf))
  {
    throw "Published Web file is missing: $relative"
  }
}

$index = Get-Content (Join-Path $root "index.html") -Raw
$required_links = @(
  "https://github.com/laqieer/FEMapCreator",
  "https://github.com/laqieer/FEMapCreator/issues",
  "https://github.com/laqieer/FEMapCreator/discussions",
  "https://github.com/laqieer/FEMapCreator/blob/main/docs/user-guide/Home.md",
  "https://github.com/laqieer/FEMapCreator/releases/latest"
)
foreach ($link in $required_links)
{
  if (-not $index.Contains($link, [StringComparison]::Ordinal))
  {
    throw "Published Web menu is missing link: $link"
  }
}

$wasm = Get-ChildItem (Join-Path $root "_framework") -File |
  Where-Object { $_.Name.EndsWith(".wasm", [StringComparison]::OrdinalIgnoreCase) }
if ($wasm.Count -eq 0)
{
  throw "Published Web application contains no WebAssembly modules."
}

Write-Host "Web smoke test passed: $($wasm.Count) WebAssembly modules and all project links are present."
