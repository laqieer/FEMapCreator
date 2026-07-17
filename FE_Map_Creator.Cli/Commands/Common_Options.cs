using System.CommandLine;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>Factory helpers for options shared between the <c>generate</c> and <c>repair</c> commands.</summary>
internal static class Common_Options
{
  internal static Option<string> tileset()
  {
    return new Option<string>("--tileset", "-t")
    {
      Description = "Tileset selector: exact bundled name, short identifier, or explicit override paths. Required for MAR input because MAR stores no tileset identifier.",
    };
  }

  internal static Option<string> assets_dir()
  {
    return new Option<string>("--assets-dir")
    {
      Description = "Root directory containing bundled Tilesets/, Tileset Generation Data/, and metadata XML. Defaults to the CLI install directory.",
    };
  }

  internal static Option<string> tileset_image()
  {
    return new Option<string>("--tileset-image")
    {
      Description = "Explicit override path for the tileset PNG image.",
    };
  }

  internal static Option<string> generation_data()
  {
    return new Option<string>("--generation-data")
    {
      Description = "Explicit override path for the tileset's binary generation-data (.dat) file.",
    };
  }

  internal static Option<int> depth()
  {
    return new Option<int>("--depth")
    {
      Description = "Neighbor lookahead depth used by the generation algorithm. Must be 1 or 2.",
      DefaultValueFactory = _ => 1,
    };
  }

  internal static Option<string> algorithm()
  {
    return new Option<string>("--algorithm")
    {
      Description = "Map solver to use: experimental (default), legacy, or hybrid.",
      DefaultValueFactory = _ => "experimental",
    }.AcceptOnlyFromAmong("legacy", "experimental", "hybrid");
  }

  internal static Option<int> experimental_search_node_limit()
  {
    return new Option<int>("--experimental-search-node-limit")
    {
      Description = "Maximum experimental backtracking nodes after the greedy incumbent. Must be positive.",
      DefaultValueFactory = _ => 10000,
    };
  }

  internal static Option<int> experimental_restarts()
  {
    return new Option<int>("--experimental-restarts")
    {
      Description = "Number of deterministic experimental search restarts. Must be positive.",
      DefaultValueFactory = _ => 4,
    };
  }

  internal static Option<int> experimental_nogood_limit()
  {
    return new Option<int>("--experimental-nogood-limit")
    {
      Description = "Maximum exact nogoods retained per experimental component. Zero disables caching.",
      DefaultValueFactory = _ => 4096,
    };
  }

  internal static Option<bool> no_experimental_conflict_learning()
  {
    return new Option<bool>("--no-experimental-conflict-learning")
    {
      Description = "Disable experimental conflict-directed backjumping and nogood reuse.",
      DefaultValueFactory = _ => false,
    };
  }

  internal static Option<bool> experimental_branch_arc_consistency()
  {
    return new Option<bool>("--experimental-branch-arc-consistency")
    {
      Description = "Enable fixed-point arc consistency during experimental complete-search branches.",
      DefaultValueFactory = _ => false,
    };
  }

  internal static Option<int> hybrid_initial_halo()
  {
    return new Option<int>("--hybrid-initial-halo")
    {
      Description = "Initial Manhattan halo around legacy unresolved cells for hybrid solving.",
      DefaultValueFactory = _ => 1,
    };
  }

  internal static Option<int> hybrid_max_halo()
  {
    return new Option<int>("--hybrid-max-halo")
    {
      Description = "Maximum adaptive Manhattan halo used by hybrid solving.",
      DefaultValueFactory = _ => 3,
    };
  }

  internal static Option<int?> seed()
  {
    return new Option<int?>("--seed")
    {
      Description = "Random seed. When omitted, a seed is generated and reported so the run can be reproduced.",
    };
  }

  internal static Option<bool> force()
  {
    return new Option<bool>("--force")
    {
      Description = "Overwrite an existing output file.",
      DefaultValueFactory = _ => false,
    };
  }

  internal static Option<bool> allow_incomplete()
  {
    return new Option<bool>("--allow-incomplete")
    {
      Description = "Exit successfully even if some cells could not be resolved.",
      DefaultValueFactory = _ => false,
    };
  }

  internal static Option<bool> require_complete()
  {
    return new Option<bool>("--require-complete")
    {
      Description = "Suppress writing the output if any cells remain unresolved.",
      DefaultValueFactory = _ => false,
    };
  }

  internal static Option<string> spec()
  {
    return new Option<string>("--spec")
    {
      Description = "Path to a versioned JSON job spec. Direct command options override it; edit/repair specs may supply required MAR metadata.",
    };
  }

  internal static Option<int?> width()
  {
    return new Option<int?>("--width")
    {
      Description = "Map width in tiles. Must be positive and is required when reading MAR input.",
    };
  }

  internal static Option<int?> height()
  {
    return new Option<int?>("--height")
    {
      Description = "Map height in tiles. Must be positive and is required when reading MAR input.",
    };
  }

  internal static Option<bool> fail_fast()
  {
    return new Option<bool>("--fail-fast")
    {
      Description = "Stop at the first failed or incomplete job instead of continuing and summarizing at the end.",
      DefaultValueFactory = _ => false,
    };
  }
}
