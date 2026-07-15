using System;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using FE_Map_Creator.Cli.Execution;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>Builds the <c>generate</c> command: options, cross-option validation, and executor dispatch.</summary>
internal static class Generate_Command
{
  internal static Command build(ICli_Executor executor, Cli_Output output)
  {
    Option<int?> width_option = Common_Options.width();
    Option<int?> height_option = Common_Options.height();
    Option<string> tileset_option = Common_Options.tileset();
    Option<string> output_option = new Option<string>("--output", "-o")
    {
      Description = "Output map file path. Required unless --count/--output-dir batch generation is used.",
    };
    Option<string> format_option = new Option<string>("--format")
    {
      Description = "Output map format. Inferred from --output's extension when omitted.",
    }.AcceptOnlyFromAmong("map", "mar", "tmx");
    Option<string> template_option = new Option<string>("--template")
    {
      Description = "Path to a template map providing initial drawn/locked tile values.",
    };
    Option<string> spec_option = Common_Options.spec();
    Option<string> algorithm_option = Common_Options.algorithm();
    Option<int> experimental_search_node_limit_option = Common_Options.experimental_search_node_limit();
    Option<int> depth_option = Common_Options.depth();
    Option<int?> seed_option = Common_Options.seed();
    Option<string> assets_dir_option = Common_Options.assets_dir();
    Option<string> tileset_image_option = Common_Options.tileset_image();
    Option<string> generation_data_option = Common_Options.generation_data();
    Option<int?> count_option = new Option<int?>("--count")
    {
      Description = "Number of maps to generate into --output-dir instead of a single --output file.",
    };
    Option<string> output_dir_option = new Option<string>("--output-dir")
    {
      Description = "Destination directory for --count homogeneous multi-map generation.",
    };
    Option<string> name_template_option = new Option<string>("--name-template")
    {
      Description = "Output file name pattern for --count generation. \"{index}\" is replaced with the 1-based job index.",
      DefaultValueFactory = _ => "map-{index}",
    };
    Option<bool> force_option = Common_Options.force();
    Option<bool> allow_incomplete_option = Common_Options.allow_incomplete();
    Option<bool> require_complete_option = Common_Options.require_complete();

    Command command = new Command("generate", "Generate a new map.")
    {
      width_option,
      height_option,
      tileset_option,
      output_option,
      format_option,
      template_option,
      spec_option,
      algorithm_option,
      experimental_search_node_limit_option,
      depth_option,
      seed_option,
      assets_dir_option,
      tileset_image_option,
      generation_data_option,
      count_option,
      output_dir_option,
      name_template_option,
      force_option,
      allow_incomplete_option,
      require_complete_option,
    };

    command.Validators.Add(result =>
    {
      int? width = result.GetValue(width_option);
      int? height = result.GetValue(height_option);
      int? count = result.GetValue(count_option);
      string spec = result.GetValue(spec_option);
      string output_value = result.GetValue(output_option);
      string output_dir = result.GetValue(output_dir_option);
      bool allow_incomplete = result.GetValue(allow_incomplete_option);
      bool require_complete = result.GetValue(require_complete_option);
      int depth = Cli_Validation.bound_value(result, depth_option);
      int experimental_search_node_limit = Cli_Validation.bound_value(result, experimental_search_node_limit_option);

      Cli_Validation.positive(result, width, "--width");
      Cli_Validation.positive(result, height, "--height");
      Cli_Validation.positive(result, count, "--count");
      Cli_Validation.valid_depth(result, depth);
      Cli_Validation.positive(result, experimental_search_node_limit, "--experimental-search-node-limit");
      Cli_Validation.mutually_exclusive_flags(
        result, allow_incomplete, "--allow-incomplete", require_complete, "--require-complete");

      if (count.HasValue && string.IsNullOrWhiteSpace(output_dir))
        result.AddError("--output-dir is required when --count is used.");
      if (!count.HasValue && string.IsNullOrWhiteSpace(output_value) && string.IsNullOrWhiteSpace(spec))
        result.AddError("--output is required unless --spec supplies it or --count/--output-dir batch generation is used.");
      if (count.HasValue && !string.IsNullOrWhiteSpace(output_value))
        result.AddError("--output cannot be combined with --count; use --output-dir and --name-template instead.");
      if (string.IsNullOrWhiteSpace(spec))
      {
        if (!width.HasValue)
          result.AddError("--width is required unless --spec supplies it.");
        if (!height.HasValue)
          result.AddError("--height is required unless --spec supplies it.");
        if (string.IsNullOrWhiteSpace(result.GetValue(tileset_option)))
          result.AddError("--tileset is required unless --spec supplies it.");
      }
    });

    command.SetAction((ParseResult parse_result, CancellationToken cancellation_token) =>
    {
      Generate_Request request = new Generate_Request
      {
        Width = parse_result.GetValue(width_option),
        Height = parse_result.GetValue(height_option),
        Tileset = parse_result.GetValue(tileset_option),
        Output = parse_result.GetValue(output_option),
        Format = Cli_Validation.parse_format(parse_result.GetValue(format_option)),
        Template = parse_result.GetValue(template_option),
        Spec = parse_result.GetValue(spec_option),
        Algorithm = Cli_Binding.explicit_value_or_null(parse_result, algorithm_option),
        Experimental_Search_Node_Limit = Cli_Binding.explicit_value_or_null(parse_result, experimental_search_node_limit_option),
        Depth = Cli_Binding.explicit_value_or_null(parse_result, depth_option),
        Seed = parse_result.GetValue(seed_option),
        Assets_Dir = parse_result.GetValue(assets_dir_option),
        Tileset_Image = parse_result.GetValue(tileset_image_option),
        Generation_Data = parse_result.GetValue(generation_data_option),
        Count = parse_result.GetValue(count_option),
        Output_Dir = parse_result.GetValue(output_dir_option),
        Name_Template = parse_result.GetValue(name_template_option),
        Force = parse_result.GetValue(force_option),
        Allow_Incomplete = parse_result.GetValue(allow_incomplete_option),
        Require_Complete = parse_result.GetValue(require_complete_option),
      };
      return Cli_Command_Runner.run(
        () => executor.generate_async(request, output, cancellation_token), output, cancellation_token);
    });

    return command;
  }
}
