<#
.SYNOPSIS
Benchmarks the legacy, experimental, and hybrid map-generation solvers.

.PARAMETER OutputDirectory
Directory for generated maps and benchmark results. Defaults to a unique system temp directory.

.PARAMETER RepeatCount
Number of repeated runs for each exact tileset/scenario/seed/variant case.

.PARAMETER Quick
Uses seeds 7 and 42 instead of the broader 7, 42, and 99 seed set.

.PARAMETER SkipBuild
Uses the existing Release CLI build instead of building the solution first.

.PARAMETER BranchArcComparison
Runs paired experimental flag-off and flag-on branch arc consistency variants instead of the
normal legacy/experimental/hybrid algorithm matrix.

.PARAMETER Focused
Restricts the matrix to FE6 Castle 2c002d2e and FE8 Fields 01000203, 20x15 generation stress
cases, and radius-1/radius-2 repair. Normal benchmark behavior is unchanged when omitted.
#>
param(
  [string]$OutputDirectory = "",
  [int]$RepeatCount = 2,
  [switch]$Quick,
  [switch]$SkipBuild,
  [switch]$BranchArcComparison,
  [switch]$Focused
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
Remove-Item -LiteralPath (Join-Path $OutputDirectory "determinism.json") -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory "determinism.csv") -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory "pairs.json") -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory "pairs.csv") -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory "paired-summary.json") -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $OutputDirectory "paired-summary.csv") -Force -ErrorAction SilentlyContinue
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
if ($Focused) {
  $tilesets = @($tilesets | Where-Object Selector -in @("2c002d2e", "01000203"))
}
$seeds = if ($Quick) { @(7, 42) } else { @(7, 42, 99) }
$searchNodeLimit = 10000
$restartCount = 4
$nogoodLimit = 4096
$depth = 1
$algorithmDefinitions = if ($BranchArcComparison) {
  @(
    [pscustomobject]@{
      Name = "experimental-baseline"
      CliAlgorithm = "experimental"
      BranchArcConsistency = $false
      ConflictLearning = $true
      ExtraArguments = [string[]]@()
    },
    [pscustomobject]@{
      Name = "experimental-branch-ac"
      CliAlgorithm = "experimental"
      BranchArcConsistency = $true
      ConflictLearning = $true
      ExtraArguments = [string[]]@("--experimental-branch-arc-consistency")
    }
  )
} else {
  @(
    [pscustomobject]@{
      Name = "legacy"
      CliAlgorithm = "legacy"
      BranchArcConsistency = $false
      ConflictLearning = $null
      ExtraArguments = [string[]]@()
    },
    [pscustomobject]@{
      Name = "experimental"
      CliAlgorithm = "experimental"
      BranchArcConsistency = $false
      ConflictLearning = $true
      ExtraArguments = [string[]]@()
    },
    [pscustomobject]@{
      Name = "hybrid"
      CliAlgorithm = "hybrid"
      BranchArcConsistency = $false
      ConflictLearning = $true
      ExtraArguments = [string[]]@()
    }
  )
}
$algorithms = @($algorithmDefinitions | ForEach-Object Name)
$results = [Collections.Generic.List[object]]::new()
$determinismFailures = [Collections.Generic.List[string]]::new()
$determinismChecks = [Collections.Generic.List[object]]::new()
$visualTileMaps = @{}
$pairSequence = 0

function Invoke-Cli([string[]]$Arguments) {
  $watch = [Diagnostics.Stopwatch]::StartNew()
  $text = (& dotnet $cliDll @Arguments 2>&1 | Out-String)
  $exitCode = $LASTEXITCODE
  $watch.Stop()
  $unresolved = $null
  $nodes = 0
  $searchRestarts = $null
  $nogoodsLearned = $null
  $nogoodHits = $null
  $backjumps = $null
  $budgetExhausted = $text -match "search budget exhausted"
  if ($text -match "\(seed -?\d+, (\d+) unresolved cell\(s\)") {
    $unresolved = [int]$Matches[1]
  } elseif ($text -match "produced (\d+) unresolved cell\(s\)") {
    $unresolved = [int]$Matches[1]
  }
  if ($text -match "(\d+) search node\(s\)") { $nodes = [int]$Matches[1] }
  if ($text -match
    "(\d+) search node\(s\), (\d+) restart\(s\), (\d+) nogood\(s\) learned, (\d+) reused, (\d+) backjump\(s\)") {
    $nodes = [int]$Matches[1]
    $searchRestarts = [int]$Matches[2]
    $nogoodsLearned = [int]$Matches[3]
    $nogoodHits = [int]$Matches[4]
    $backjumps = [int]$Matches[5]
  }
  return [pscustomobject]@{
    ExitCode = $exitCode
    Output = $text.Trim()
    Milliseconds = $watch.ElapsedMilliseconds
    Unresolved = $unresolved
    SearchNodes = $nodes
    SearchRestarts = $searchRestarts
    NogoodsLearned = $nogoodsLearned
    NogoodHits = $nogoodHits
    Backjumps = $backjumps
    BudgetExhausted = $budgetExhausted
  }
}

function New-RunPlan([string]$CaseKey) {
  $plan = [Collections.Generic.List[object]]::new()
  if ($BranchArcComparison) {
    for ($repeat = 1; $repeat -le $RepeatCount; ++$repeat) {
      ++$script:pairSequence
      $pairId = "$CaseKey|repeat=$repeat"
      $orderedVariants = if ($script:pairSequence % 2 -eq 1) {
        @($algorithmDefinitions[0], $algorithmDefinitions[1])
      } else {
        @($algorithmDefinitions[1], $algorithmDefinitions[0])
      }
      for ($order = 0; $order -lt $orderedVariants.Count; ++$order) {
        $plan.Add([pscustomobject]@{
          Variant = $orderedVariants[$order]
          Repeat = $repeat
          PairId = $pairId
          ExecutionOrder = $order + 1
        })
      }
    }
  } else {
    foreach ($variant in $algorithmDefinitions) {
      for ($repeat = 1; $repeat -le $RepeatCount; ++$repeat) {
        $plan.Add([pscustomobject]@{
          Variant = $variant
          Repeat = $repeat
          PairId = ""
          ExecutionOrder = 0
        })
      }
    }
  }
  return $plan
}

function Add-DeterminismCheck(
  [string]$Scenario,
  $Tileset,
  [string]$Size,
  [int]$Seed,
  [string]$Algorithm,
  [Collections.Generic.List[string]]$Hashes
) {
  $nonNullHashes = @($Hashes | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
  $uniqueHashes = @($nonNullHashes | Select-Object -Unique)
  $deterministic = $nonNullHashes.Count -eq $RepeatCount -and $uniqueHashes.Count -eq 1
  $check = [pscustomobject]@{
    Scenario = $Scenario
    Game = $Tileset.Game
    Family = $Tileset.Family
    Selector = $Tileset.Selector
    Size = $Size
    Seed = $Seed
    Algorithm = $Algorithm
    RepeatCount = $RepeatCount
    Hashes = @($Hashes)
    NonNullHashCount = $nonNullHashes.Count
    UniqueHashCount = $uniqueHashes.Count
    Deterministic = $deterministic
  }
  $determinismChecks.Add($check)
  foreach ($result in $results | Where-Object {
    $_.Scenario -eq $Scenario -and
    $_.Game -eq $Tileset.Game -and
    $_.Family -eq $Tileset.Family -and
    $_.Size -eq $Size -and
    $_.Seed -eq $Seed -and
    $_.Algorithm -eq $Algorithm
  }) {
    $result | Add-Member -NotePropertyName Deterministic -NotePropertyValue $deterministic -Force
  }
  if (-not $deterministic) {
    $determinismFailures.Add(
      "$($Tileset.Game) $($Tileset.Family) $Scenario $Size seed $Seed $Algorithm")
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
  [string]$CliAlgorithm
) {
  if (-not (Test-Path -LiteralPath $Path)) {
    return [pscustomobject]@{ Valid = $false; Detail = "output missing" }
  }
  $arguments = @(
    "validate", "--input", $Path, "--tileset", $Selector,
    "--algorithm", $CliAlgorithm
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
  $Variant,
  [int]$Repeat,
  [string[]]$Arguments,
  [string]$OutputPath,
  [string]$SpecPath,
  $DiversityMask = $null,
  [string]$PairId = "",
  [int]$ExecutionOrder = 0
) {
  Remove-Item -LiteralPath $OutputPath -Force -ErrorAction SilentlyContinue
  $run = Invoke-Cli $Arguments
  $expected_exit = $run.ExitCode -eq 0 -or $run.ExitCode -eq 2
  $produced_output = Test-Path -LiteralPath $OutputPath
  $has_unresolved_metric = $null -ne $run.Unresolved
  $validation = if ($expected_exit -and $produced_output -and $has_unresolved_metric) {
    Validate-Output $OutputPath $Tileset.Selector $SpecPath $Variant.CliAlgorithm
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
    Algorithm = $Variant.Name
    CliAlgorithm = $Variant.CliAlgorithm
    BranchArcConsistency = [bool]$Variant.BranchArcConsistency
    ConflictLearning = $Variant.ConflictLearning
    Repeat = $Repeat
    PairId = $PairId
    ExecutionOrder = $ExecutionOrder
    Depth = $depth
    SearchNodeLimit = $searchNodeLimit
    RestartLimit = $restartCount
    NogoodLimit = $nogoodLimit
    ExitCode = $run.ExitCode
    Unresolved = $run.Unresolved
    Complete = $run.Unresolved -eq 0
    SearchNodes = $run.SearchNodes
    SearchRestarts = $run.SearchRestarts
    NogoodsLearned = $run.NogoodsLearned
    NogoodHits = $run.NogoodHits
    Backjumps = $run.Backjumps
    BudgetExhausted = $run.BudgetExhausted
    Milliseconds = $run.Milliseconds
    Valid = $expected_exit -and $produced_output -and $has_unresolved_metric -and $validation.Valid
    Hash = Get-MapHash $OutputPath
    DiversitySampleCount = $diversity.SampleCount
    UniqueTiles = $diversity.UniqueTiles
    DominantShare = $diversity.DominantShare
    EntropyBits = $diversity.EntropyBits
    SameNeighborShare = $diversity.SameNeighborShare
    CommandArguments = $Arguments -join " "
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
    "--experimental-search-node-limit", [string]$searchNodeLimit,
    "--experimental-restarts", [string]$restartCount,
    "--experimental-nogood-limit", [string]$nogoodLimit, "--force"
  )
  if ($run.ExitCode -ne 0) {
    throw "Could not create disconnected template for $($Tileset.Game) $($Tileset.Family): $($run.Output)"
  }
  return $path
}

function Invoke-GenerationMatrix {
  if ($Focused) {
    $dimensionMatrix = ,@(20, 15)
  } else {
    $dimensionMatrix = @(@(4, 3), @(20, 15))
  }
  foreach ($tileset in $tilesets) {
    foreach ($seed in $seeds) {
      foreach ($dimensions in $dimensionMatrix) {
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
          $size = "{0}x{1}" -f $width, $height
          $caseKey = "generate|$($tileset.Game)|$($tileset.Family)|$scenario|$size|seed=$seed"
          $hashesByVariant = @{}
          foreach ($variant in $algorithmDefinitions) {
            $hashesByVariant[$variant.Name] = [Collections.Generic.List[string]]::new()
          }
          foreach ($planItem in @(New-RunPlan $caseKey)) {
            $variant = $planItem.Variant
            $repeat = $planItem.Repeat
            $name = "gen-{0}-{1}-{2}-{3}x{4}-s{5}-{6}-r{7}.map" -f
              $tileset.Game, $tileset.Family, $scenario, $width, $height, $seed, $variant.Name, $repeat
            $outputPath = Join-Path $mapsDirectory $name
            $arguments = @(
              "generate", "--width", [string]$width, "--height", [string]$height,
              "--tileset", $tileset.Selector, "--output", $outputPath,
              "--algorithm", $variant.CliAlgorithm, "--depth", [string]$depth, "--seed", [string]$seed,
              "--experimental-search-node-limit", [string]$searchNodeLimit,
              "--experimental-restarts", [string]$restartCount,
              "--experimental-nogood-limit", [string]$nogoodLimit,
              "--hybrid-initial-halo", "1", "--hybrid-max-halo", "3", "--force"
            )
            $arguments += @($variant.ExtraArguments)
            if ($scenario -ne "blank") { $arguments += @("--spec", $specPath) }
            if ($scenario -eq "disconnected") { $arguments += @("--template", $template) }
            Add-Run $scenario $tileset $size $seed $variant $repeat `
              $arguments $outputPath $specPath $null $planItem.PairId $planItem.ExecutionOrder
            $hashesByVariant[$variant.Name].Add((Get-MapHash $outputPath))
          }
          foreach ($variant in $algorithmDefinitions) {
            Add-DeterminismCheck `
              $scenario `
              $tileset `
              $size `
              $seed `
              $variant.Name `
              $hashesByVariant[$variant.Name]
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
        $caseKey = "repair|$($tileset.Game)|$($tileset.Family)|$($repairCase.Name)|$($damage.Size)|seed=$seed"
        $hashesByVariant = @{}
        foreach ($variant in $algorithmDefinitions) {
          $hashesByVariant[$variant.Name] = [Collections.Generic.List[string]]::new()
        }
        foreach ($planItem in @(New-RunPlan $caseKey)) {
          $variant = $planItem.Variant
          $repeat = $planItem.Repeat
          $outputPath = Join-Path $mapsDirectory ("{0}-{1}-{2}-s{3}-{4}-r{5}.map" -f
            $tileset.Game, $tileset.Family, $repairCase.Name, $seed, $variant.Name, $repeat)
          $arguments = @(
            "repair", "--input", $damaged, "--output", $outputPath,
            "--tileset", $tileset.Selector, "--algorithm", $variant.CliAlgorithm,
            "--repair-radius", [string]$repairCase.Radius,
            "--depth", [string]$depth, "--seed", [string]$seed,
            "--experimental-search-node-limit", [string]$searchNodeLimit,
            "--experimental-restarts", [string]$restartCount,
            "--experimental-nogood-limit", [string]$nogoodLimit,
            "--hybrid-initial-halo", "1", "--hybrid-max-halo", "3", "--force"
          )
          $arguments += @($variant.ExtraArguments)
          Add-Run $repairCase.Name $tileset $damage.Size $seed $variant $repeat `
            $arguments $outputPath "" $damage.DiversityMask $planItem.PairId $planItem.ExecutionOrder
          $hashesByVariant[$variant.Name].Add((Get-MapHash $outputPath))
        }
        foreach ($variant in $algorithmDefinitions) {
          Add-DeterminismCheck `
            $repairCase.Name `
            $tileset `
            $damage.Size `
            $seed `
            $variant.Name `
            $hashesByVariant[$variant.Name]
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

function Get-GeometricMean([double[]]$Values) {
  $positive = @($Values | Where-Object { $_ -gt 0 })
  if ($positive.Count -eq 0) { return 0 }
  $logSum = 0.0
  foreach ($value in $positive) {
    $logSum += [Math]::Log([double]$value)
  }
  return [Math]::Exp($logSum / $positive.Count)
}

function Get-NullableDelta($Candidate, $Baseline, [string]$Property) {
  if ($null -eq $Candidate.$Property -or $null -eq $Baseline.$Property) {
    return $null
  }
  return [double]$Candidate.$Property - [double]$Baseline.$Property
}

function New-PairedData {
  $pairs = [Collections.Generic.List[object]]::new()
  $failures = [Collections.Generic.List[string]]::new()
  foreach ($group in $results |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_.PairId) } |
    Group-Object PairId) {
    $baseline = $group.Group |
      Where-Object Algorithm -eq "experimental-baseline" |
      Select-Object -First 1
    $candidate = $group.Group |
      Where-Object Algorithm -eq "experimental-branch-ac" |
      Select-Object -First 1
    if ($null -eq $baseline -or $null -eq $candidate) {
      $failures.Add($group.Name)
      continue
    }
    $first = $group.Group | Sort-Object ExecutionOrder | Select-Object -First 1
    $pairs.Add([pscustomobject]@{
      PairId = $group.Name
      Scenario = $baseline.Scenario
      Game = $baseline.Game
      Family = $baseline.Family
      Selector = $baseline.Selector
      Size = $baseline.Size
      Seed = $baseline.Seed
      Repeat = $baseline.Repeat
      FirstVariant = $first.Algorithm
      BaselineMilliseconds = $baseline.Milliseconds
      CandidateMilliseconds = $candidate.Milliseconds
      SpeedupBaselineOverCandidate = if ($candidate.Milliseconds -gt 0) {
        $baseline.Milliseconds / [double]$candidate.Milliseconds
      } else {
        $null
      }
      BaselineUnresolved = $baseline.Unresolved
      CandidateUnresolved = $candidate.Unresolved
      UnresolvedDelta = Get-NullableDelta $candidate $baseline "Unresolved"
      BaselineComplete = $baseline.Complete
      CandidateComplete = $candidate.Complete
      BaselineSearchNodes = $baseline.SearchNodes
      CandidateSearchNodes = $candidate.SearchNodes
      SearchNodeDelta = Get-NullableDelta $candidate $baseline "SearchNodes"
      BaselineSearchRestarts = $baseline.SearchRestarts
      CandidateSearchRestarts = $candidate.SearchRestarts
      SearchRestartDelta = Get-NullableDelta $candidate $baseline "SearchRestarts"
      BaselineNogoodsLearned = $baseline.NogoodsLearned
      CandidateNogoodsLearned = $candidate.NogoodsLearned
      NogoodsLearnedDelta = Get-NullableDelta $candidate $baseline "NogoodsLearned"
      BaselineNogoodHits = $baseline.NogoodHits
      CandidateNogoodHits = $candidate.NogoodHits
      NogoodHitDelta = Get-NullableDelta $candidate $baseline "NogoodHits"
      BaselineBackjumps = $baseline.Backjumps
      CandidateBackjumps = $candidate.Backjumps
      BackjumpDelta = Get-NullableDelta $candidate $baseline "Backjumps"
      BaselineBudgetExhausted = $baseline.BudgetExhausted
      CandidateBudgetExhausted = $candidate.BudgetExhausted
      BaselineValid = $baseline.Valid
      CandidateValid = $candidate.Valid
      BaselineDeterministic = $baseline.Deterministic
      CandidateDeterministic = $candidate.Deterministic
      BaselineHash = $baseline.Hash
      CandidateHash = $candidate.Hash
      BaselineEntropyBits = $baseline.EntropyBits
      CandidateEntropyBits = $candidate.EntropyBits
      EntropyDelta = Get-NullableDelta $candidate $baseline "EntropyBits"
      BaselineDominantShare = $baseline.DominantShare
      CandidateDominantShare = $candidate.DominantShare
      DominantShareDelta = Get-NullableDelta $candidate $baseline "DominantShare"
      BaselineSameNeighborShare = $baseline.SameNeighborShare
      CandidateSameNeighborShare = $candidate.SameNeighborShare
      SameNeighborShareDelta = Get-NullableDelta $candidate $baseline "SameNeighborShare"
    })
  }
  return [pscustomobject]@{
    Pairs = $pairs
    Failures = $failures
  }
}

function New-PairedAggregate(
  [string]$Scope,
  [string]$Game,
  [string]$Family,
  [string]$Selector,
  [string]$Scenario,
  [string]$Size,
  [object[]]$Rows
) {
  $speedups = @($Rows |
    Where-Object { $null -ne $_.SpeedupBaselineOverCandidate } |
    ForEach-Object { [double]$_.SpeedupBaselineOverCandidate })
  $baselineDeterminismFailures = @($Rows |
    Where-Object { -not $_.BaselineDeterministic } |
    ForEach-Object { "$($_.Game)|$($_.Family)|$($_.Scenario)|$($_.Size)|$($_.Seed)" } |
    Select-Object -Unique).Count
  $candidateDeterminismFailures = @($Rows |
    Where-Object { -not $_.CandidateDeterministic } |
    ForEach-Object { "$($_.Game)|$($_.Family)|$($_.Scenario)|$($_.Size)|$($_.Seed)" } |
    Select-Object -Unique).Count
  return [pscustomobject]@{
    Scope = $Scope
    Game = $Game
    Family = $Family
    Selector = $Selector
    Scenario = $Scenario
    Size = $Size
    PairCount = $Rows.Count
    BaselineMedianMilliseconds = Get-Median @($Rows | ForEach-Object { [double]$_.BaselineMilliseconds })
    CandidateMedianMilliseconds = Get-Median @($Rows | ForEach-Object { [double]$_.CandidateMilliseconds })
    GeometricMeanSpeedup = Get-GeometricMean $speedups
    BaselineCompleteCount = @($Rows | Where-Object BaselineComplete).Count
    CandidateCompleteCount = @($Rows | Where-Object CandidateComplete).Count
    MedianUnresolvedDelta = Get-Median @($Rows | ForEach-Object { [double]$_.UnresolvedDelta })
    MedianSearchNodeDelta = Get-Median @($Rows | ForEach-Object { [double]$_.SearchNodeDelta })
    MedianSearchRestartDelta = Get-Median @($Rows |
      Where-Object { $null -ne $_.SearchRestartDelta } |
      ForEach-Object { [double]$_.SearchRestartDelta })
    MedianNogoodsLearnedDelta = Get-Median @($Rows |
      Where-Object { $null -ne $_.NogoodsLearnedDelta } |
      ForEach-Object { [double]$_.NogoodsLearnedDelta })
    MedianNogoodHitDelta = Get-Median @($Rows |
      Where-Object { $null -ne $_.NogoodHitDelta } |
      ForEach-Object { [double]$_.NogoodHitDelta })
    MedianBackjumpDelta = Get-Median @($Rows |
      Where-Object { $null -ne $_.BackjumpDelta } |
      ForEach-Object { [double]$_.BackjumpDelta })
    BaselineBudgetCutCount = @($Rows | Where-Object BaselineBudgetExhausted).Count
    CandidateBudgetCutCount = @($Rows | Where-Object CandidateBudgetExhausted).Count
    BaselineValidationFailureCount = @($Rows | Where-Object { -not $_.BaselineValid }).Count
    CandidateValidationFailureCount = @($Rows | Where-Object { -not $_.CandidateValid }).Count
    BaselineDeterminismFailureCount = $baselineDeterminismFailures
    CandidateDeterminismFailureCount = $candidateDeterminismFailures
    MedianEntropyDelta = Get-Median @($Rows | ForEach-Object { [double]$_.EntropyDelta })
    MedianDominantShareDelta = Get-Median @($Rows | ForEach-Object { [double]$_.DominantShareDelta })
    MedianSameNeighborShareDelta = Get-Median @($Rows | ForEach-Object { [double]$_.SameNeighborShareDelta })
  }
}

function Write-Reports([string[]]$PreviewPaths) {
  $jsonPath = Join-Path $OutputDirectory "results.json"
  $csvPath = Join-Path $OutputDirectory "results.csv"
  $determinismJsonPath = Join-Path $OutputDirectory "determinism.json"
  $determinismCsvPath = Join-Path $OutputDirectory "determinism.csv"
  $pairsJsonPath = Join-Path $OutputDirectory "pairs.json"
  $pairsCsvPath = Join-Path $OutputDirectory "pairs.csv"
  $pairedSummaryJsonPath = Join-Path $OutputDirectory "paired-summary.json"
  $pairedSummaryCsvPath = Join-Path $OutputDirectory "paired-summary.csv"
  $markdownPath = Join-Path $OutputDirectory "summary.md"
  $results | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8
  $results | Export-Csv -LiteralPath $csvPath -NoTypeInformation -Encoding utf8
  $determinismChecks |
    ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath $determinismJsonPath -Encoding utf8
  $determinismChecks |
    Select-Object Scenario, Game, Family, Selector, Size, Seed, Algorithm, RepeatCount,
      NonNullHashCount, UniqueHashCount, Deterministic,
      @{ Name = "Hashes"; Expression = { $_.Hashes -join ";" } } |
    Export-Csv -LiteralPath $determinismCsvPath -NoTypeInformation -Encoding utf8

  $pairs = @()
  $pairedAggregates = [Collections.Generic.List[object]]::new()
  $pairingFailures = [Collections.Generic.List[string]]::new()
  if ($BranchArcComparison) {
    $pairedData = New-PairedData
    $pairs = @($pairedData.Pairs)
    foreach ($failure in $pairedData.Failures) { $pairingFailures.Add($failure) }
    if ($pairs.Count -gt 0) {
      $pairedAggregates.Add((New-PairedAggregate `
        -Scope "overall" `
        -Game "all" `
        -Family "all" `
        -Selector "all" `
        -Scenario "all" `
        -Size "all" `
        -Rows $pairs))
      foreach ($group in $pairs |
        Group-Object Game, Family, Selector, Scenario, Size |
        Sort-Object Name) {
        $rows = @($group.Group)
        $pairedAggregates.Add((New-PairedAggregate `
          -Scope "case" `
          -Game $rows[0].Game `
          -Family $rows[0].Family `
          -Selector $rows[0].Selector `
          -Scenario $rows[0].Scenario `
          -Size $rows[0].Size `
          -Rows $rows))
      }
    }
    $pairs | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $pairsJsonPath -Encoding utf8
    $pairs | Export-Csv -LiteralPath $pairsCsvPath -NoTypeInformation -Encoding utf8
    $pairedAggregates |
      ConvertTo-Json -Depth 8 |
      Set-Content -LiteralPath $pairedSummaryJsonPath -Encoding utf8
    $pairedAggregates |
      Export-Csv -LiteralPath $pairedSummaryCsvPath -NoTypeInformation -Encoding utf8
  }

  $summary = [Collections.Generic.List[string]]::new()
  $summary.Add("# Solver Benchmark Summary")
  $summary.Add("")
  $summary.Add("Generated: $(Get-Date -Format o)")
  $summary.Add("")
  $mode = if ($BranchArcComparison) { "paired branch arc consistency comparison" } else { "legacy/experimental/hybrid matrix" }
  $scope = if ($Focused) { "focused FE6 Castle and FE8 Fields stress matrix" } else { "normal matrix" }
  $summary.Add("Mode: $mode; scope: $scope.")
  $summary.Add("")
  $summary.Add(
    "Repeat count: $RepeatCount; seeds: $($seeds -join ', '); depth: $depth; " +
    "node limit: $searchNodeLimit; restarts: $restartCount; nogood limit: $nogoodLimit.")
  $summary.Add("")
  $summary.Add("| Scenario | Algorithm | Runs | Complete | Median ms | Worst ms | Median unresolved | Worst unresolved | Median entropy | Worst dominant | Worst repeat | Budget cuts | Invalid |")
  $summary.Add("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|")
  foreach ($group in $results | Group-Object Scenario, Algorithm | Sort-Object Name) {
    $rows = @($group.Group)
    $scenario = $rows[0].Scenario
    $algorithm = $rows[0].Algorithm
    $complete = @($rows | Where-Object Complete).Count
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
  if (-not $BranchArcComparison) {
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
  }
  $invalidRuns = @($results | Where-Object { -not $_.Valid })
  if ($BranchArcComparison) {
    $summary.Add("")
    $summary.Add("## Paired branch arc consistency summary")
    $summary.Add("")
    $summary.Add(
      "Speedup is baseline milliseconds divided by branch-AC milliseconds; values above 1 mean branch AC was faster. " +
      "All deltas are branch AC minus baseline.")
    $summary.Add("")
    $summary.Add("| Scope | Pairs | Baseline median ms | Branch AC median ms | Geomean speedup | Median node delta | Median unresolved delta | Complete B/C | Budget B/C | Invalid B/C | Determinism B/C | Entropy delta | Dominant delta | Neighbor delta |")
    $summary.Add("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|")
    foreach ($aggregate in $pairedAggregates) {
      $label = if ($aggregate.Scope -eq "overall") {
        "Overall"
      } else {
        "$($aggregate.Game) $($aggregate.Family) $($aggregate.Scenario) $($aggregate.Size)"
      }
      $baselineMedian = [Math]::Round($aggregate.BaselineMedianMilliseconds, 1)
      $candidateMedian = [Math]::Round($aggregate.CandidateMedianMilliseconds, 1)
      $speedup = [Math]::Round($aggregate.GeometricMeanSpeedup, 3)
      $nodeDelta = [Math]::Round($aggregate.MedianSearchNodeDelta, 1)
      $unresolvedDelta = [Math]::Round($aggregate.MedianUnresolvedDelta, 1)
      $entropyDelta = [Math]::Round($aggregate.MedianEntropyDelta, 3)
      $dominantDelta = [Math]::Round($aggregate.MedianDominantShareDelta, 4)
      $neighborDelta = [Math]::Round($aggregate.MedianSameNeighborShareDelta, 4)
      $summary.Add(
        "| $label | $($aggregate.PairCount) | $baselineMedian | $candidateMedian | $speedup | " +
        "$nodeDelta | $unresolvedDelta | $($aggregate.BaselineCompleteCount)/$($aggregate.CandidateCompleteCount) | " +
        "$($aggregate.BaselineBudgetCutCount)/$($aggregate.CandidateBudgetCutCount) | " +
        "$($aggregate.BaselineValidationFailureCount)/$($aggregate.CandidateValidationFailureCount) | " +
        "$($aggregate.BaselineDeterminismFailureCount)/$($aggregate.CandidateDeterminismFailureCount) | " +
        "$entropyDelta | $dominantDelta | $neighborDelta |")
    }
    $summary.Add("")
    $summary.Add("## Search diagnostics")
    $summary.Add("")
    $summary.Add("| Variant | Median nodes | Median restarts | Median nogoods learned | Median nogood hits | Median backjumps |")
    $summary.Add("|---|---:|---:|---:|---:|---:|")
    foreach ($algorithm in $algorithms) {
      $rows = @($results | Where-Object Algorithm -eq $algorithm)
      $summary.Add(
        "| $algorithm | " +
        "$([Math]::Round((Get-Median @($rows.SearchNodes)), 1)) | " +
        "$([Math]::Round((Get-Median @($rows | Where-Object { $null -ne $_.SearchRestarts } | ForEach-Object { [double]$_.SearchRestarts })), 1)) | " +
        "$([Math]::Round((Get-Median @($rows | Where-Object { $null -ne $_.NogoodsLearned } | ForEach-Object { [double]$_.NogoodsLearned })), 1)) | " +
        "$([Math]::Round((Get-Median @($rows | Where-Object { $null -ne $_.NogoodHits } | ForEach-Object { [double]$_.NogoodHits })), 1)) | " +
        "$([Math]::Round((Get-Median @($rows | Where-Object { $null -ne $_.Backjumps } | ForEach-Object { [double]$_.Backjumps })), 1)) |")
    }
    $baselineOnlyComplete = @($pairs | Where-Object {
      $_.BaselineComplete -and -not $_.CandidateComplete
    }).Count
    $candidateOnlyComplete = @($pairs | Where-Object {
      $_.CandidateComplete -and -not $_.BaselineComplete
    }).Count
    $budgetIntroduced = @($pairs | Where-Object {
      -not $_.BaselineBudgetExhausted -and $_.CandidateBudgetExhausted
    }).Count
    $budgetAvoided = @($pairs | Where-Object {
      $_.BaselineBudgetExhausted -and -not $_.CandidateBudgetExhausted
    }).Count
    $summary.Add("")
    $summary.Add("## Correctness and paired gates")
    $summary.Add("")
    $summary.Add("- Validation failures: $($invalidRuns.Count)")
    $summary.Add("- Determinism failures: $($determinismFailures.Count)")
    $summary.Add("- Missing/malformed pairs: $($pairingFailures.Count)")
    $summary.Add("- Baseline-only complete pairs: $baselineOnlyComplete")
    $summary.Add("- Branch-AC-only complete pairs: $candidateOnlyComplete")
    $summary.Add("- Budget cuts introduced/avoided by branch AC: $budgetIntroduced/$budgetAvoided")
  } else {
    $summary.Add("")
    $summary.Add("## Promotion gates")
    $summary.Add("")
    $summary.Add("- Validation failures: $($invalidRuns.Count)")
    $summary.Add("- Determinism failures: $($determinismFailures.Count)")
    $summary.Add("- Hybrid-worse-than-legacy pairs: $($pairedViolations.Count)")
    $summary.Add("- Experimental-worse-than-legacy unresolved pairs: $($experimentalQualityRegressions.Count)")
    $summary.Add("- Diversity regressions/collapses: $($diversityFailures.Count)")
    $summary.Add("- Retaining the experimental default targets three consecutive full-matrix runs on documented hardware, zero correctness failures, zero regressions, and median unresolved no worse than legacy with worst runtime no more than 2x legacy in every scenario.")
    $summary.Add("")
    $summary.Add("Experimental is the current default by product decision; failed gates remain rollback signals.")
  }
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
  if ($pairingFailures.Count -gt 0) {
    $summary.Add("")
    $summary.Add("Pairing failures: $($pairingFailures -join '; ')")
  }
  if (-not $BranchArcComparison -and $pairedViolations.Count -gt 0) {
    $summary.Add("")
    $summary.Add("Hybrid regressions: $($pairedViolations -join '; ')")
  }
  if (-not $BranchArcComparison -and $experimentalQualityRegressions.Count -gt 0) {
    $summary.Add("")
    $summary.Add("Experimental unresolved regressions: $($experimentalQualityRegressions -join '; ')")
  }
  if (-not $BranchArcComparison -and $diversityFailures.Count -gt 0) {
    $summary.Add("")
    $summary.Add("Diversity failures: $($diversityFailures -join '; ')")
  }
  $summary | Set-Content -LiteralPath $markdownPath -Encoding utf8
  return [pscustomobject]@{
    Json = $jsonPath
    Csv = $csvPath
    DeterminismJson = $determinismJsonPath
    DeterminismCsv = $determinismCsvPath
    PairsJson = if ($BranchArcComparison) { $pairsJsonPath } else { $null }
    PairsCsv = if ($BranchArcComparison) { $pairsCsvPath } else { $null }
    PairedSummaryJson = if ($BranchArcComparison) { $pairedSummaryJsonPath } else { $null }
    PairedSummaryCsv = if ($BranchArcComparison) { $pairedSummaryCsvPath } else { $null }
    Markdown = $markdownPath
    Invalid = $invalidRuns.Count
    DeterminismFailures = $determinismFailures.Count
    PairingFailures = $pairingFailures.Count
    HybridRegressions = $pairedViolations.Count
    ExperimentalQualityRegressions = $experimentalQualityRegressions.Count
    DiversityFailures = $diversityFailures.Count
    ComparisonMode = [bool]$BranchArcComparison
  }
}

Invoke-GenerationMatrix
Invoke-RepairMatrix
$previewPaths = @(Write-Previews)
$report = Write-Reports $previewPaths
Write-Output "JSON=$($report.Json)"
Write-Output "CSV=$($report.Csv)"
Write-Output "DETERMINISM_JSON=$($report.DeterminismJson)"
Write-Output "DETERMINISM_CSV=$($report.DeterminismCsv)"
if ($report.ComparisonMode) {
  Write-Output "PAIRS_JSON=$($report.PairsJson)"
  Write-Output "PAIRS_CSV=$($report.PairsCsv)"
  Write-Output "PAIRED_SUMMARY_JSON=$($report.PairedSummaryJson)"
  Write-Output "PAIRED_SUMMARY_CSV=$($report.PairedSummaryCsv)"
}
Write-Output "MARKDOWN=$($report.Markdown)"
$failed = $report.Invalid -gt 0 `
  -or $report.DeterminismFailures -gt 0 `
  -or $report.PairingFailures -gt 0
if (-not $report.ComparisonMode) {
  $failed = $failed `
    -or $report.HybridRegressions -gt 0 `
    -or $report.ExperimentalQualityRegressions -gt 0 `
    -or $report.DiversityFailures -gt 0
}
if ($failed) {
  exit 1
}
