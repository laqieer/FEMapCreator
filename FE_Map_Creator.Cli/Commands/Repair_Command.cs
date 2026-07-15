using System.CommandLine;
using System.Threading;
using FE_Map_Creator.Cli.Execution;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>Builds the <c>repair</c> command: options, cross-option validation, and executor dispatch.</summary>
internal static class Repair_Command
{
  internal static Command build(ICli_Executor executor, Cli_Output output)
  {
    Option<string> input_option = new Option<string>("--input", "-i")
    {
      Description = "Input map file to repair. MAR files require width, height, and tileset metadata from options or JSON.",
    };
    Option<string> output_option = new Option<string>("--output", "-o")
    {
      Description = "Output map file path. Required unless --in-place or directory mode is used.",
    };
    Option<bool> in_place_option = new Option<bool>("--in-place")
    {
      Description = "Write the repaired map back to --input (via an atomic temp-file replace).",
      DefaultValueFactory = _ => false,
    };
    Option<string> spec_option = Common_Options.spec();
    Option<string> algorithm_option = Common_Options.algorithm();
    Option<int> experimental_search_node_limit_option = Common_Options.experimental_search_node_limit();
    Option<string> tileset_option = Common_Options.tileset();
    Option<int?> width_option = Common_Options.width();
    Option<int?> height_option = Common_Options.height();
    Option<int> repair_radius_option = new Option<int>("--repair-radius")
    {
      Description = "Manhattan-distance radius around repair holes/incompatible cells to reopen. Zero or greater.",
      DefaultValueFactory = _ => 0,
    };
    Option<int> depth_option = Common_Options.depth();
    Option<int?> seed_option = Common_Options.seed();
    Option<string> assets_dir_option = Common_Options.assets_dir();
    Option<string> tileset_image_option = Common_Options.tileset_image();
    Option<string> generation_data_option = Common_Options.generation_data();
    Option<bool> force_option = Common_Options.force();
    Option<bool> allow_incomplete_option = Common_Options.allow_incomplete();
    Option<bool> require_complete_option = Common_Options.require_complete();
    Option<string> input_dir_option = new Option<string>("--input-dir")
    {
      Description = "Source directory for homogeneous repair. MAR files may use per-file .mapgen.json sidecars for width, height, and tileset.",
    };
    Option<string> output_dir_option = new Option<string>("--output-dir")
    {
      Description = "Destination directory for --input-dir directory repair. Relative paths are preserved.",
    };
    Option<string> pattern_option = new Option<string>("--pattern")
    {
      Description = "Glob pattern used to select files under --input-dir.",
      DefaultValueFactory = _ => "*.map",
    };
    Option<bool> recursive_option = new Option<bool>("--recursive")
    {
      Description = "Recurse into subdirectories under --input-dir.",
      DefaultValueFactory = _ => false,
    };
    Option<bool> fail_fast_option = Common_Options.fail_fast();

    Command command = new Command(
      "repair",
      "Repair an existing map. MAR files do not contain dimensions or a tileset identifier; supply them with options, JSON spec/manifest fields, or a .mapgen.json sidecar.")
    {
      input_option,
      output_option,
      in_place_option,
      spec_option,
      algorithm_option,
      experimental_search_node_limit_option,
      tileset_option,
      width_option,
      height_option,
      repair_radius_option,
      depth_option,
      seed_option,
      assets_dir_option,
      tileset_image_option,
      generation_data_option,
      force_option,
      allow_incomplete_option,
      require_complete_option,
      input_dir_option,
      output_dir_option,
      pattern_option,
      recursive_option,
      fail_fast_option,
    };

    command.Validators.Add(result =>
    {
      string input = result.GetValue(input_option);
      string output_value = result.GetValue(output_option);
      bool in_place = result.GetValue(in_place_option);
      string spec = result.GetValue(spec_option);
      string input_dir = result.GetValue(input_dir_option);
      string output_dir = result.GetValue(output_dir_option);
      int? width = result.GetValue(width_option);
      int? height = result.GetValue(height_option);
      int repair_radius = Cli_Validation.bound_value(result, repair_radius_option);
      int depth = Cli_Validation.bound_value(result, depth_option);
      int experimental_search_node_limit = Cli_Validation.bound_value(result, experimental_search_node_limit_option);
      bool allow_incomplete = result.GetValue(allow_incomplete_option);
      bool require_complete = result.GetValue(require_complete_option);

      Cli_Validation.positive(result, width, "--width");
      Cli_Validation.positive(result, height, "--height");
      Cli_Validation.non_negative(result, repair_radius, "--repair-radius");
      Cli_Validation.valid_depth(result, depth);
      Cli_Validation.positive(result, experimental_search_node_limit, "--experimental-search-node-limit");
      Cli_Validation.mutually_exclusive_flags(
        result, allow_incomplete, "--allow-incomplete", require_complete, "--require-complete");

      bool is_directory_mode = !string.IsNullOrWhiteSpace(input_dir);
      if (is_directory_mode)
      {
        if (!string.IsNullOrWhiteSpace(input))
          result.AddError("--input cannot be combined with --input-dir.");
        if (string.IsNullOrWhiteSpace(output_dir))
          result.AddError("--output-dir is required when --input-dir is used.");
        if (!string.IsNullOrWhiteSpace(output_value))
          result.AddError("--output cannot be combined with --input-dir; use --output-dir instead.");
        if (in_place)
          result.AddError("--in-place cannot be combined with --input-dir; use --output-dir instead.");
      }
      else
      {
        if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(spec))
          result.AddError("Exactly one of --input or --input-dir is required (or --spec must supply an input).");
        Cli_Validation.mutually_exclusive_flags(
          result, in_place, "--in-place", !string.IsNullOrWhiteSpace(output_value), "--output");
        if (!in_place && string.IsNullOrWhiteSpace(output_value) && string.IsNullOrWhiteSpace(spec))
          result.AddError("--output is required unless --in-place or --spec supplies an output.");
      }
    });

    command.SetAction((ParseResult parse_result, CancellationToken cancellation_token) =>
    {
      Repair_Request request = new Repair_Request
      {
        Input = parse_result.GetValue(input_option),
        Output = parse_result.GetValue(output_option),
        In_Place = parse_result.GetValue(in_place_option),
        Spec = parse_result.GetValue(spec_option),
        Algorithm = Cli_Binding.explicit_value_or_null(parse_result, algorithm_option),
        Experimental_Search_Node_Limit = Cli_Binding.explicit_value_or_null(parse_result, experimental_search_node_limit_option),
        Tileset = parse_result.GetValue(tileset_option),
        Width = parse_result.GetValue(width_option),
        Height = parse_result.GetValue(height_option),
        Repair_Radius = Cli_Binding.explicit_value_or_null(parse_result, repair_radius_option),
        Depth = Cli_Binding.explicit_value_or_null(parse_result, depth_option),
        Seed = parse_result.GetValue(seed_option),
        Assets_Dir = parse_result.GetValue(assets_dir_option),
        Tileset_Image = parse_result.GetValue(tileset_image_option),
        Generation_Data = parse_result.GetValue(generation_data_option),
        Force = parse_result.GetValue(force_option),
        Allow_Incomplete = parse_result.GetValue(allow_incomplete_option),
        Require_Complete = parse_result.GetValue(require_complete_option),
        Input_Dir = parse_result.GetValue(input_dir_option),
        Output_Dir = parse_result.GetValue(output_dir_option),
        Pattern = parse_result.GetValue(pattern_option),
        Recursive = parse_result.GetValue(recursive_option),
        Fail_Fast = parse_result.GetValue(fail_fast_option),
      };
      return Cli_Command_Runner.run(
        () => executor.repair_async(request, output, cancellation_token), output, cancellation_token);
    });

    return command;
  }
}
