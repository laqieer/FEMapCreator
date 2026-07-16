param(
  [string]$OutputDirectory = "",
  [int]$RepeatCount = 2,
  [switch]$Quick,
  [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repo "FE_Map_Creator\FE_Map_Creator.sln"
$cliProject = Join-Path $repo "FE_Map_Creator.Cli\FE_Map_Creator.Cli.csproj"
$cliDll = Join-Path $repo "FE_Map_Creator.Cli\bin\Release\net10.0\FE_Map_Creator.Cli.dll"
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
  $OutputDirectory = Join-Path ([IO.Path]::GetTempPath()) ("FEMapCreator-benchmark-" + [Guid]::NewGuid().ToString("N"))
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
$mapsDirectory = Join-Path $OutputDirectory "maps"
Remove-Item -LiteralPath $mapsDirectory -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory "previews") -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory "results.json") -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory "results.csv") -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory "summary.md") -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $mapsDirectory | Out-Null

if (-not $SkipBuild) {
  & dotnet build $solution -c Release --nologo --verbosity quiet
  if ($LASTEXITCODE -ne 0) { throw "Release build failed." }
}
if (-not (Test-Path -LiteralPath $cliDll)) {
  throw "CLI binary not found at $cliDll."
}
if ($RepeatCount -le 0) {
  throw "RepeatCount must be positive."
}

$tilesets = @(
  [pscustomobject]@{
    Game = "FE6"; Family = "Fields"; Selector = "01020304"
    RepairSource = Join-Path $repo "FE6 Maps\0102xx04\Chapter1BreathofDestiny.map"
  },
  [pscustomobject]@{
    Game = "FE7"; Family = "Fields"; Selector = "1c1d1e1f"
    RepairSource = Join-Path $repo "FE7 Maps\1c1dxx1f\Ch9AGrimReunion.map"
  },
  [pscustomobject]@{
    Game = "FE8"; Family = "Fields"; Selector = "01000203"
    RepairSource = Join-Path $repo "FE8 Maps\0100xx03\Ch2.map"
  },
  [pscustomobject]@{
    Game = "FE6"; Family = "Castle"; Selector = "2c002d2e"
    RepairSource = Join-Path $repo "FE6 Maps\2c00xx2e\Chapter8Reunion.map"
  },
  [pscustomobject]@{
    Game = "FE7"; Family = "Castle"; Selector = "0a000b0c"
    RepairSource = Join-Path $repo "FE7 Maps\0a00xx0c\Ch2SwordofSpirits.map"
  },
  [pscustomobject]@{
    Game = "FE8"; Family = "Castle"; Selector = "1800481a"
    RepairSource = Join-Path $repo "FE8 Maps\1800xx1a\Ch8.map"
  }
)
$seeds = if ($Quick) { @(7, 42) } else { @(7, 42, 99) }
$algorithms = @("legacy", "experimental", "hybrid")
$results = [Collections.Generic.List[object]]::new()
$determinismFailures = [Collections.Generic.List[string]]::new()
$visualTileMaps = @{}

function Invoke-Cli([string[]]$Arguments) {
  $watch = [Diagnostics.Stopwatch]::StartNew()
  $text = (& dotnet $cliDll @Arguments 2>&1 | Out-String)
  $exitCode = $LASTEXITCODE
  $watch.Stop()
  $unresolved = $null
  $nodes = 0
  $budgetExhausted = $text -match "search budget exhausted"
  if ($text -match "\(seed -?\d+, (\d+) unresolved cell\(s\)") {
    $unresolved = [int]$Matches[1]
  } elseif ($text -match "produced (\d+) unresolved cell\(s\)") {
    $unresolved = [int]$Matches[1]
  }
  if ($text -match "(\d+) search node\(s\)") { $nodes = [int]$Matches[1] }
  return [pscustomobject]@{
    ExitCode = $exitCode
    Output = $text.Trim()
    Milliseconds = $watch.ElapsedMilliseconds
    Unresolved = $unresolved
    SearchNodes = $nodes
    BudgetExhausted = $budgetExhausted
  }
}

function New-Matrix([int]$Width, [int]$Height, $Value) {
  $rows = @()
  for ($y = 0; $y -lt $Height; ++$y) {
    $row = @()
    for ($x = 0; $x -lt $Width; ++$x) { $row += $Value }
    $rows += ,$row
  }
  return $rows
}

function New-TerrainMatrix([int]$Width, [int]$Height) {
  $rows = @()
  for ($y = 0; $y -lt $Height; ++$y) {
    $row = @()
    for ($x = 0; $x -lt $Width; ++$x) {
      $row += if ($x -lt [Math]::Floor($Width / 2)) { [int]1 } else { [int]-1 }
    }
    $rows += ,$row
  }
  return $rows
}

function Write-ScenarioSpec(
  [string]$Path,
  [string]$Scenario,
  [int]$Width,
  [int]$Height,
  [string]$Selector,
  [string]$Template
) {
  $spec = [ordered]@{
    version = 1
    operation = "generate"
    width = $Width
    height = $Height
    tileset = $Selector
  }
  if ($Scenario -eq "terrain") {
    $spec.terrain = New-TerrainMatrix $Width $Height
  }
  if ($Scenario -eq "disconnected") {
    $locked = New-Matrix $Width $Height $false
    $wallX = [Math]::Floor($Width / 2)
    for ($y = 0; $y -lt $Height; ++$y) { $locked[$y][$wallX] = $true }
    $spec.locked = $locked
    $spec.template = $Template
  }
  $spec | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $Path -Encoding utf8
}

function Get-MapHash([string]$Path) {
  if (-not (Test-Path -LiteralPath $Path)) { return $null }
  return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Get-VisualTileMap([string]$Selector) {
  if ($visualTileMaps.ContainsKey($Selector)) {
    return $visualTileMaps[$Selector]
  }
  Add-Type -AssemblyName System.Drawing
  $image = Get-ChildItem -LiteralPath (Join-Path $repo "Tilesets") -Filter "*$Selector.png" |
    Select-Object -First 1
  if ($null -eq $image) { throw "Tileset image for $Selector was not found." }
  $bitmap = [Drawing.Bitmap]::FromFile($image.FullName)
  $sha = [Security.Cryptography.SHA256]::Create()
  try {
    $tileSize = 16
    $tilesWide = [int]($bitmap.Width / $tileSize)
    $tilesHigh = [int]($bitmap.Height / $tileSize)
    $groups = @{}
    $map = @{}
    $nextGroup = 0
    for ($tile = 0; $tile -lt $tilesWide * $tilesHigh; ++$tile) {
      $bytes = New-Object byte[] ($tileSize * $tileSize * 4)
      $offset = 0
      $baseX = ($tile % $tilesWide) * $tileSize
      $baseY = [Math]::Floor($tile / $tilesWide) * $tileSize
      for ($y = 0; $y -lt $tileSize; ++$y) {
        for ($x = 0; $x -lt $tileSize; ++$x) {
          $color = $bitmap.GetPixel($baseX + $x, $baseY + $y)
          $bytes[$offset++] = $color.B
          $bytes[$offset++] = $color.G
          $bytes[$offset++] = $color.R
          $bytes[$offset++] = $color.A
        }
      }
      $hash = ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace("-", "")
      if (-not $groups.ContainsKey($hash)) {
        $groups[$hash] = $nextGroup++
      }
      $map[$tile] = $groups[$hash]
    }
    $visualTileMaps[$Selector] = $map
    return $map
  }
  finally {
    $sha.Dispose()
    $bitmap.Dispose()
  }
}

function Get-DiversityMetrics(
  [string]$Path,
  $DiversityMask,
  [bool]$IncludeZero,
  [string]$Selector
) {
  if (-not (Test-Path -LiteralPath $Path)) {
    return [pscustomobject]@{
      SampleCount = 0; UniqueTiles = 0; DominantShare = 1.0; EntropyBits = 0.0; SameNeighborShare = 1.0
    }
  }
  $lines = Get-Content -LiteralPath $Path
  $dimensions = $lines[1].Split(" ", [StringSplitOptions]::RemoveEmptyEntries)
  $height = [int]$dimensions[0]
  $width = [int]$dimensions[1]
  $tiles = New-Object "int[,]" $width, $height
  $visualMap = Get-VisualTileMap $Selector
  $values = [Collections.Generic.List[int]]::new()
  for ($y = 0; $y -lt $height; ++$y) {
    $row = $lines[2 + $y].Split(" ", [StringSplitOptions]::RemoveEmptyEntries)
    for ($x = 0; $x -lt $width; ++$x) {
      $tile = [int]$row[$x]
      $visualTile = if ($visualMap.ContainsKey($tile)) { $visualMap[$tile] } else { $tile }
      $tiles[$x, $y] = $visualTile
      $key = $x + $y * $width
      $cellInScope = $null -eq $DiversityMask -or $DiversityMask.ContainsKey($key)
      if (($IncludeZero -or $tile -ne 0) -and $cellInScope) {
        $values.Add($visualTile)
      }
    }
  }
  if ($values.Count -eq 0) {
    return [pscustomobject]@{
      SampleCount = 0; UniqueTiles = 0; DominantShare = 1.0; EntropyBits = 0.0; SameNeighborShare = 1.0
    }
  }
  $groups = @($values | Group-Object | Sort-Object Count -Descending)
  $entropy = 0.0
  foreach ($group in $groups) {
    $probability = $group.Count / [double]$values.Count
    $entropy -= $probability * [Math]::Log($probability, 2)
  }
  $checkedNeighbors = 0
  $sameNeighbors = 0
  for ($y = 0; $y -lt $height; ++$y) {
    for ($x = 0; $x -lt $width; ++$x) {
      $tile = $tiles[$x, $y]
      if (-not $IncludeZero -and $tile -eq 0) { continue }
      $key = $x + $y * $width
      $rightInScope = $null -eq $DiversityMask `
        -or $DiversityMask.ContainsKey($key) `
        -or $DiversityMask.ContainsKey(($x + 1) + $y * $width)
      $hasRight = $x + 1 -lt $width
      $rightUsable = $hasRight -and ($IncludeZero -or $tiles[($x + 1), $y] -ne 0)
      if ($rightUsable -and $rightInScope) {
        ++$checkedNeighbors
        if ($tile -eq $tiles[($x + 1), $y]) { ++$sameNeighbors }
      }
      $downInScope = $null -eq $DiversityMask `
        -or $DiversityMask.ContainsKey($key) `
        -or $DiversityMask.ContainsKey($x + ($y + 1) * $width)
      $hasDown = $y + 1 -lt $height
      $downUsable = $hasDown -and ($IncludeZero -or $tiles[$x, ($y + 1)] -ne 0)
      if ($downUsable -and $downInScope) {
        ++$checkedNeighbors
        if ($tile -eq $tiles[$x, ($y + 1)]) { ++$sameNeighbors }
      }
    }
  }
  return [pscustomobject]@{
    SampleCount = $values.Count
    UniqueTiles = $groups.Count
    DominantShare = $groups[0].Count / [double]$values.Count
    EntropyBits = $entropy
    SameNeighborShare = if ($checkedNeighbors -eq 0) { 0.0 } else { $sameNeighbors / [double]$checkedNeighbors }
  }
}

function Validate-Output(
  [string]$Path,
  [string]$Selector,
  [string]$SpecPath,
  [string]$Algorithm
) {
  if (-not (Test-Path -LiteralPath $Path)) {
    return [pscustomobject]@{ Valid = $false; Detail = "output missing" }
  }
  $arguments = @(
    "validate", "--input", $Path, "--tileset", $Selector,
    "--algorithm", $Algorithm
  )
  if (-not [string]::IsNullOrWhiteSpace($SpecPath)) {
    $arguments += @("--spec", $SpecPath)
  }
  $validation = Invoke-Cli $arguments
  return [pscustomobject]@{
    Valid = $validation.ExitCode -eq 0
    Detail = $validation.Output
  }
}

function Add-Run(
  [string]$Scenario,
  $Tileset,
  [string]$Size,
  [int]$Seed,
  [string]$Algorithm,
  [int]$Repeat,
  [string[]]$Arguments,
  [string]$OutputPath,
  [string]$SpecPath,
  $DiversityMask = $null
) {
  Remove-Item -LiteralPath $OutputPath -Force -ErrorAction SilentlyContinue
  $run = Invoke-Cli $Arguments
  $expected_exit = $run.ExitCode -eq 0 -or $run.ExitCode -eq 2
  $produced_output = Test-Path -LiteralPath $OutputPath
  $has_unresolved_metric = $null -ne $run.Unresolved
  $validation = if ($expected_exit -and $produced_output -and $has_unresolved_metric) {
    Validate-Output $OutputPath $Tileset.Selector $SpecPath $Algorithm
  } else {
    [pscustomobject]@{
      Valid = $false
      Detail = "unexpected exit/output state: exit=$($run.ExitCode), output=$produced_output, unresolved=$($run.Unresolved)"
    }
  }
  $diversity = Get-DiversityMetrics `
    $OutputPath `
    $DiversityMask `
    ($run.Unresolved -eq 0) `
    $Tileset.Selector
  $results.Add([pscustomobject]@{
    Scenario = $Scenario
    Game = $Tileset.Game
    Family = $Tileset.Family
    Selector = $Tileset.Selector
    Size = $Size
    Seed = $Seed
    Algorithm = $Algorithm
    Repeat = $Repeat
    ExitCode = $run.ExitCode
    Unresolved = $run.Unresolved
    SearchNodes = $run.SearchNodes
    BudgetExhausted = $run.BudgetExhausted
    Milliseconds = $run.Milliseconds
    Valid = $expected_exit -and $produced_output -and $has_unresolved_metric -and $validation.Valid
    Hash = Get-MapHash $OutputPath
    DiversitySampleCount = $diversity.SampleCount
    UniqueTiles = $diversity.UniqueTiles
    DominantShare = $diversity.DominantShare
    EntropyBits = $diversity.EntropyBits
    SameNeighborShare = $diversity.SameNeighborShare
    Output = $run.Output
    Validation = $validation.Detail
  })
}

function Ensure-Template($Tileset, [int]$Width, [int]$Height, [int]$Seed) {
  $path = Join-Path $mapsDirectory ("template-{0}-{1}-{2}x{3}-{4}.map" -f
    $Tileset.Game, $Tileset.Family, $Width, $Height, $Seed)
  if (Test-Path -LiteralPath $path) { return $path }
  $run = Invoke-Cli @(
    "generate", "--width", [string]$Width, "--height", [string]$Height,
    "--tileset", $Tileset.Selector, "--output", $path,
    "--algorithm", "experimental", "--seed", [string]$Seed,
    "--experimental-search-node-limit", "10000",
    "--experimental-restarts", "4", "--force"
  )
  if ($run.ExitCode -ne 0) {
    throw "Could not create disconnected template for $($Tileset.Game) $($Tileset.Family): $($run.Output)"
  }
  return $path
}

function Invoke-GenerationMatrix {
  foreach ($tileset in $tilesets) {
    foreach ($seed in $seeds) {
      foreach ($dimensions in @(@(4, 3), @(20, 15))) {
        $width = $dimensions[0]
        $height = $dimensions[1]
        foreach ($scenario in @("blank", "terrain", "disconnected")) {
          if ($scenario -ne "blank" -and ($width -ne 20 -or $height -ne 15)) { continue }
          $specPath = ""
          $template = ""
          if ($scenario -eq "disconnected") {
            $template = Ensure-Template $tileset $width $height $seed
          }
          if ($scenario -ne "blank") {
            $specPath = Join-Path $mapsDirectory ("spec-{0}-{1}-{2}-{3}.json" -f
              $tileset.Game, $tileset.Family, $scenario, $seed)
            Write-ScenarioSpec $specPath $scenario $width $height $tileset.Selector $template
          }
          foreach ($algorithm in $algorithms) {
            $hashes = [Collections.Generic.List[string]]::new()
            for ($repeat = 1; $repeat -le $RepeatCount; ++$repeat) {
              $name = "gen-{0}-{1}-{2}-{3}x{4}-s{5}-{6}-r{7}.map" -f
                $tileset.Game, $tileset.Family, $scenario, $width, $height, $seed, $algorithm, $repeat
              $outputPath = Join-Path $mapsDirectory $name
              $arguments = @(
                "generate", "--width", [string]$width, "--height", [string]$height,
                "--tileset", $tileset.Selector, "--output", $outputPath,
                "--algorithm", $algorithm, "--depth", "1", "--seed", [string]$seed,
                "--experimental-search-node-limit", "10000",
                "--experimental-restarts", "4",
                "--experimental-nogood-limit", "4096",
                "--hybrid-initial-halo", "1", "--hybrid-max-halo", "3", "--force"
              )
              if ($scenario -ne "blank") { $arguments += @("--spec", $specPath) }
              if ($scenario -eq "disconnected") { $arguments += @("--template", $template) }
              Add-Run $scenario $tileset ("{0}x{1}" -f $width, $height) $seed $algorithm $repeat `
                $arguments $outputPath $specPath
              $hashes.Add((Get-MapHash $outputPath))
            }
            $nonNullHashes = @($hashes | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
            $uniqueHashCount = @($nonNullHashes | Select-Object -Unique).Count
            if (($nonNullHashes.Count -ne $RepeatCount) -or ($uniqueHashCount -ne 1)) {
              $determinismFailures.Add(
                "$($tileset.Game) $($tileset.Family) $scenario ${width}x${height} seed $seed $algorithm")
            }
          }
        }
      }
    }
  }
}

function New-DamagedMap(
  [string]$Source,
  [string]$Destination,
  [int]$HoleCount,
  [int]$Radius
) {
  $lines = [Collections.Generic.List[string]](Get-Content -LiteralPath $Source)
  $dimensions = $lines[1].Split(" ", [StringSplitOptions]::RemoveEmptyEntries)
  $height = [int]$dimensions[0]
  $width = [int]$dimensions[1]
  $candidates = [Collections.Generic.List[object]]::new()
  for ($y = 1; $y -lt $height - 1; ++$y) {
    $row = $lines[2 + $y].Split(" ", [StringSplitOptions]::RemoveEmptyEntries)
    for ($x = 1; $x -lt $width - 1; ++$x) {
      if ([int]$row[$x] -ne 0) {
        $distance = [Math]::Abs($x - [Math]::Floor($width / 2)) +
          [Math]::Abs($y - [Math]::Floor($height / 2))
        $candidates.Add([pscustomobject]@{ X = $x; Y = $y; Distance = $distance })
      }
    }
  }
  $selected = $candidates | Sort-Object Distance, Y, X | Select-Object -First $HoleCount
  foreach ($cell in $selected) {
    $row = $lines[2 + $cell.Y].Split(" ", [StringSplitOptions]::RemoveEmptyEntries)
    $row[$cell.X] = "0"
    $lines[2 + $cell.Y] = $row -join " "
  }
  Set-Content -LiteralPath $Destination -Value $lines -Encoding utf8
  $mask = @{}
  foreach ($cell in $selected) {
    for ($dy = -$Radius; $dy -le $Radius; ++$dy) {
      $maxDx = $Radius - [Math]::Abs($dy)
      for ($dx = -$maxDx; $dx -le $maxDx; ++$dx) {
        $x = $cell.X + $dx
        $y = $cell.Y + $dy
        if ($x -ge 0 -and $x -lt $width -and $y -ge 0 -and $y -lt $height) {
          $mask[$x + $y * $width] = $true
        }
      }
    }
  }
  return [pscustomobject]@{
    Size = "{0}x{1}" -f $width, $height
    DiversityMask = $mask
  }
}

function Invoke-RepairMatrix {
  foreach ($tileset in $tilesets) {
    foreach ($seed in $seeds) {
      foreach ($repairCase in @(
        [pscustomobject]@{ Name = "repair-single"; Holes = 1; Radius = 1 },
        [pscustomobject]@{ Name = "repair-multi"; Holes = 3; Radius = 2 }
      )) {
        $damaged = Join-Path $mapsDirectory ("damaged-{0}-{1}-{2}-s{3}.map" -f
          $tileset.Game, $tileset.Family, $repairCase.Name, $seed)
        $damage = New-DamagedMap `
          $tileset.RepairSource `
          $damaged `
          $repairCase.Holes `
          ($repairCase.Radius + 3)
        foreach ($algorithm in $algorithms) {
          $hashes = [Collections.Generic.List[string]]::new()
          for ($repeat = 1; $repeat -le $RepeatCount; ++$repeat) {
            $outputPath = Join-Path $mapsDirectory ("{0}-{1}-{2}-s{3}-{4}-r{5}.map" -f
              $tileset.Game, $tileset.Family, $repairCase.Name, $seed, $algorithm, $repeat)
            $arguments = @(
              "repair", "--input", $damaged, "--output", $outputPath,
              "--tileset", $tileset.Selector, "--algorithm", $algorithm,
              "--repair-radius", [string]$repairCase.Radius,
              "--depth", "1", "--seed", [string]$seed,
              "--experimental-search-node-limit", "10000",
              "--experimental-restarts", "4",
              "--experimental-nogood-limit", "4096",
              "--hybrid-initial-halo", "1", "--hybrid-max-halo", "3", "--force"
            )
            Add-Run $repairCase.Name $tileset $damage.Size $seed $algorithm $repeat `
              $arguments $outputPath "" $damage.DiversityMask
            $hashes.Add((Get-MapHash $outputPath))
          }
          $nonNullHashes = @($hashes | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
          $uniqueHashCount = @($nonNullHashes | Select-Object -Unique).Count
          if (($nonNullHashes.Count -ne $RepeatCount) -or ($uniqueHashCount -ne 1)) {
            $determinismFailures.Add(
              "$($tileset.Game) $($tileset.Family) $($repairCase.Name) $($damage.Size) seed $seed $algorithm")
          }
        }
      }
    }
  }
}

function Write-Previews {
  $previewDirectory = Join-Path $OutputDirectory "previews"
  New-Item -ItemType Directory -Force -Path $previewDirectory | Out-Null
  $tileset = $tilesets | Where-Object { $_.Game -eq "FE8" -and $_.Family -eq "Fields" } |
    Select-Object -First 1
  $seed = if ($seeds -contains 42) { 42 } else { $seeds[0] }
  $image = Get-ChildItem -LiteralPath (Join-Path $repo "Tilesets") -Filter "*$($tileset.Selector).png" |
    Select-Object -First 1
  $paths = [Collections.Generic.List[string]]::new()
  foreach ($algorithm in $algorithms) {
    $map = Join-Path $mapsDirectory ("gen-FE8-Fields-blank-20x15-s{0}-{1}-r1.map" -f $seed, $algorithm)
    if (-not (Test-Path -LiteralPath $map)) { continue }
    $preview = Join-Path $previewDirectory ("fe8-fields-{0}.png" -f $algorithm)
    & (Join-Path $PSScriptRoot "render-map-preview.ps1") `
      -MapPath $map `
      -TilesetImagePath $image.FullName `
      -OutputPath $preview `
      -Scale 2
    $paths.Add($preview)
  }
  return $paths
}

function Get-Median([double[]]$Values) {
  if ($Values.Count -eq 0) { return 0 }
  $sorted = $Values | Sort-Object
  $middle = [Math]::Floor($sorted.Count / 2)
  if ($sorted.Count % 2 -eq 1) { return [double]$sorted[$middle] }
  return ([double]$sorted[$middle - 1] + [double]$sorted[$middle]) / 2
}

function Write-Reports([string[]]$PreviewPaths) {
  $jsonPath = Join-Path $OutputDirectory "results.json"
  $csvPath = Join-Path $OutputDirectory "results.csv"
  $markdownPath = Join-Path $OutputDirectory "summary.md"
  $results | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
  $results | Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding utf8

  $summary = [Collections.Generic.List[string]]::new()
  $summary.Add("# Solver Benchmark Summary")
  $summary.Add("")
  $summary.Add("Generated: $(Get-Date -Format o)")
  $summary.Add("")
  $summary.Add("Repeat count: $RepeatCount; seeds: $($seeds -join ', '); node limit: 10000; restarts: 4.")
  $summary.Add("")
  $summary.Add("| Scenario | Algorithm | Runs | Complete | Median ms | Worst ms | Median unresolved | Worst unresolved | Median entropy | Worst dominant | Worst repeat | Budget cuts | Invalid |")
  $summary.Add("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|")
  foreach ($group in $results | Group-Object Scenario, Algorithm | Sort-Object Name) {
    $rows = @($group.Group)
    $scenario = $rows[0].Scenario
    $algorithm = $rows[0].Algorithm
    $complete = @($rows | Where-Object ExitCode -eq 0).Count
    $medianMs = [Math]::Round((Get-Median @($rows.Milliseconds)), 1)
    $worstMs = ($rows.Milliseconds | Measure-Object -Maximum).Maximum
    $unresolvedValues = @($rows | Where-Object { $null -ne $_.Unresolved } | ForEach-Object { $_.Unresolved })
    $medianUnresolved = [Math]::Round((Get-Median $unresolvedValues), 1)
    $worstUnresolved = ($unresolvedValues | Measure-Object -Maximum).Maximum
    $budgetCuts = @($rows | Where-Object BudgetExhausted).Count
    $invalid = @($rows | Where-Object { -not $_.Valid }).Count
    $medianEntropy = [Math]::Round((Get-Median @($rows.EntropyBits)), 2)
    $worstDominant = [Math]::Round(($rows.DominantShare | Measure-Object -Maximum).Maximum, 3)
    $worstRepeat = [Math]::Round(($rows.SameNeighborShare | Measure-Object -Maximum).Maximum, 3)
    $summary.Add("| $scenario | $algorithm | $($rows.Count) | $complete | $medianMs | $worstMs | $medianUnresolved | $worstUnresolved | $medianEntropy | $worstDominant | $worstRepeat | $budgetCuts | $invalid |")
  }

  $pairedViolations = [Collections.Generic.List[string]]::new()
  $experimentalQualityRegressions = [Collections.Generic.List[string]]::new()
  $diversityFailures = [Collections.Generic.List[string]]::new()
  foreach ($group in $results | Group-Object Scenario, Game, Family, Size, Seed, Repeat) {
    $legacy = $group.Group | Where-Object Algorithm -eq "legacy" | Select-Object -First 1
    $hybrid = $group.Group | Where-Object Algorithm -eq "hybrid" | Select-Object -First 1
    if (($null -ne $legacy) -and
      ($null -ne $hybrid) -and
      ($null -ne $legacy.Unresolved) -and
      ($null -ne $hybrid.Unresolved) -and
      ($hybrid.Unresolved -gt $legacy.Unresolved)) {
      $pairedViolations.Add($group.Name)
    }
    $experimental = $group.Group | Where-Object Algorithm -eq "experimental" | Select-Object -First 1
    $experimentalWorse = ($null -ne $legacy) `
      -and ($null -ne $experimental) `
      -and ($null -ne $legacy.Unresolved) `
      -and ($null -ne $experimental.Unresolved) `
      -and ($experimental.Unresolved -gt $legacy.Unresolved)
    if ($experimentalWorse) {
      $experimentalQualityRegressions.Add($group.Name)
    }
    foreach ($candidate in @($group.Group | Where-Object { $_.Algorithm -ne "legacy" })) {
      if ($null -eq $legacy -or $null -eq $candidate) { continue }
      $dominanceLimit = [double]$legacy.DominantShare + 0.10
      $repetitionLimit = [double]$legacy.SameNeighborShare + 0.10
      $dominanceRegression = [double]$candidate.DominantShare -gt $dominanceLimit
      $entropyRegression = [double]$candidate.EntropyBits + 0.5 -lt [double]$legacy.EntropyBits
      $repetitionRegression = [double]$candidate.SameNeighborShare -gt $repetitionLimit
      $comparableCompleteness = [Math]::Abs(
        [int]$candidate.Unresolved - [int]$legacy.Unresolved) -le 5
      $relativeDiversityRegression =
        $dominanceRegression -or $entropyRegression -or $repetitionRegression
      if ($comparableCompleteness -and $relativeDiversityRegression) {
        $diversityFailures.Add("$($group.Name), $($candidate.Algorithm)")
      }
      $cellCount = [int]$candidate.DiversitySampleCount
      $absoluteCollapse = [double]$candidate.DominantShare -gt 0.40 `
        -or [double]$candidate.EntropyBits -lt 3.0 `
        -or [double]$candidate.SameNeighborShare -gt 0.45
      if ($cellCount -ge 100 -and [int]$candidate.ExitCode -eq 0 -and $absoluteCollapse) {
        $diversityFailures.Add("$($group.Name), $($candidate.Algorithm), absolute collapse")
      }
    }
  }
  $invalidRuns = @($results | Where-Object { -not $_.Valid })
  $summary.Add("")
  $summary.Add("## Promotion gates")
  $summary.Add("")
  $summary.Add("- Validation failures: $($invalidRuns.Count)")
  $summary.Add("- Determinism failures: $($determinismFailures.Count)")
  $summary.Add("- Hybrid-worse-than-legacy pairs: $($pairedViolations.Count)")
  $summary.Add("- Experimental-worse-than-legacy unresolved pairs: $($experimentalQualityRegressions.Count)")
  $summary.Add("- Diversity regressions/collapses: $($diversityFailures.Count)")
  $summary.Add("- Default promotion additionally requires three consecutive full-matrix runs on documented hardware, zero correctness failures, zero hybrid regressions, and candidate median unresolved no worse than legacy with worst runtime no more than 2x legacy in every scenario.")
  $summary.Add("")
  $summary.Add("Legacy remains the default; this run alone does not promote another algorithm.")
  if ($PreviewPaths.Count -gt 0) {
    $summary.Add("")
    $summary.Add("## Human-review previews")
    $summary.Add("")
    foreach ($preview in $PreviewPaths) {
      $filename = Split-Path -Leaf $preview
      $label = [IO.Path]::GetFileNameWithoutExtension($filename).Replace("fe8-fields-", "").Replace("-", " ")
      $summary.Add("### $label")
      $summary.Add("")
      $summary.Add("![$label](previews/$filename)")
      $summary.Add("")
    }
  }
  if ($determinismFailures.Count -gt 0) {
    $summary.Add("")
    $summary.Add("Determinism failures: $($determinismFailures -join '; ')")
  }
  if ($pairedViolations.Count -gt 0) {
    $summary.Add("")
    $summary.Add("Hybrid regressions: $($pairedViolations -join '; ')")
  }
  if ($experimentalQualityRegressions.Count -gt 0) {
    $summary.Add("")
    $summary.Add("Experimental unresolved regressions: $($experimentalQualityRegressions -join '; ')")
  }
  if ($diversityFailures.Count -gt 0) {
    $summary.Add("")
    $summary.Add("Diversity failures: $($diversityFailures -join '; ')")
  }
  $summary | Set-Content -LiteralPath $markdownPath -Encoding utf8
  return [pscustomobject]@{
    Json = $jsonPath
    Csv = $csvPath
    Markdown = $markdownPath
    Invalid = $invalidRuns.Count
    DeterminismFailures = $determinismFailures.Count
    HybridRegressions = $pairedViolations.Count
    ExperimentalQualityRegressions = $experimentalQualityRegressions.Count
    DiversityFailures = $diversityFailures.Count
  }
}

Invoke-GenerationMatrix
Invoke-RepairMatrix
$previewPaths = @(Write-Previews)
$report = Write-Reports $previewPaths
Write-Output "JSON=$($report.Json)"
Write-Output "CSV=$($report.Csv)"
Write-Output "MARKDOWN=$($report.Markdown)"
$failed = $report.Invalid -gt 0 `
  -or $report.DeterminismFailures -gt 0 `
  -or $report.HybridRegressions -gt 0 `
  -or $report.ExperimentalQualityRegressions -gt 0 `
  -or $report.DiversityFailures -gt 0
if ($failed) {
  exit 1
}
