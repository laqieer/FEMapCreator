param(
  [Parameter(Mandatory = $true)]
  [string]$MapPath,
  [Parameter(Mandatory = $true)]
  [string]$TilesetImagePath,
  [Parameter(Mandatory = $true)]
  [string]$OutputPath,
  [int]$Scale = 2
)

$ErrorActionPreference = "Stop"
if ($Scale -le 0) { throw "Scale must be positive." }
Add-Type -AssemblyName System.Drawing

$lines = Get-Content -LiteralPath $MapPath
$dimensions = $lines[1].Split(" ", [StringSplitOptions]::RemoveEmptyEntries)
$height = [int]$dimensions[0]
$width = [int]$dimensions[1]
$tileSize = 16
$tileset = [Drawing.Bitmap]::FromFile([IO.Path]::GetFullPath($TilesetImagePath))
$output = New-Object Drawing.Bitmap ($width * $tileSize * $Scale), ($height * $tileSize * $Scale)
$graphics = [Drawing.Graphics]::FromImage($output)
try {
  $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
  $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
  $tilesetWidth = [int]($tileset.Width / $tileSize)
  for ($y = 0; $y -lt $height; ++$y) {
    $row = $lines[2 + $y].Split(" ", [StringSplitOptions]::RemoveEmptyEntries)
    for ($x = 0; $x -lt $width; ++$x) {
      $tile = [int]$row[$x]
      $source = New-Object Drawing.Rectangle (
        ($tile % $tilesetWidth) * $tileSize),
        ([Math]::Floor($tile / $tilesetWidth) * $tileSize),
        $tileSize,
        $tileSize
      $destination = New-Object Drawing.Rectangle (
        $x * $tileSize * $Scale),
        ($y * $tileSize * $Scale),
        ($tileSize * $Scale),
        ($tileSize * $Scale)
      $graphics.DrawImage(
        $tileset,
        $destination,
        $source,
        [Drawing.GraphicsUnit]::Pixel)
    }
  }
  $directory = Split-Path -Parent ([IO.Path]::GetFullPath($OutputPath))
  New-Item -ItemType Directory -Force -Path $directory | Out-Null
  $output.Save(
    [IO.Path]::GetFullPath($OutputPath),
    [Drawing.Imaging.ImageFormat]::Png)
}
finally {
  $graphics.Dispose()
  $output.Dispose()
  $tileset.Dispose()
}
