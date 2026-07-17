using System.CommandLine;
using System.Threading;
using FE_Map_Creator.Cli.Execution;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>Builds the <c>map</c> editing and inspection command group.</summary>
internal static class Map_Command
{
  internal static Command build(ICli_Executor executor, Cli_Output output)
  {
    Command map = new Command("map", "Create, edit, convert, and inspect map files.");
    map.Subcommands.Add(build_edit(executor, output));
    map.Subcommands.Add(build_inspect(executor, output));
    return map;
  }

  private static Command build_edit(ICli_Executor executor, Cli_Output output)
  {
    Option<string> input_option = new Option<string>("--input", "-i")
    {
      Description = "Optional input .map, .mar, or .tmx file. Omit to create a new map.",
    };
    Option<string> output_option = new Option<string>("--output", "-o")
    {
      Description = "Output map path. Required unless --in-place or --spec supplies it.",
    };
    Option<bool> in_place_option = new Option<bool>("--in-place")
    {
      Description = "Atomically replace --input after every edit has validated.",
      DefaultValueFactory = _ => false,
    };
    Option<string> spec_option = Common_Options.spec();
    Option<string> format_option = new Option<string>("--format")
    {
      Description = "Output format: map, mar, or tmx. Inferred from --output when omitted.",
    }.AcceptOnlyFromAmong("map", "mar", "tmx");
    Option<int?> width_option = Common_Options.width();
    Option<int?> height_option = Common_Options.height();
    Option<string> tileset_option = Common_Options.tileset();
    Option<string> assets_dir_option = Common_Options.assets_dir();
    Option<string> tileset_image_option = Common_Options.tileset_image();
    Option<bool> force_option = Common_Options.force();

    Command command = new Command(
      "edit",
      "Create or load a map, apply ordered edits from a versioned JSON spec, resize/convert it, and save atomically.")
    {
      input_option,
      output_option,
      in_place_option,
      spec_option,
      format_option,
      width_option,
      height_option,
      tileset_option,
      assets_dir_option,
      tileset_image_option,
      force_option,
    };

    command.Validators.Add(result =>
    {
      string input = result.GetValue(input_option);
      string output_value = result.GetValue(output_option);
      string spec = result.GetValue(spec_option);
      bool in_place = result.GetValue(in_place_option);
      int? width = result.GetValue(width_option);
      int? height = result.GetValue(height_option);

      Cli_Validation.positive(result, width, "--width");
      Cli_Validation.positive(result, height, "--height");
      Cli_Validation.mutually_exclusive_flags(
        result, in_place, "--in-place", !string.IsNullOrWhiteSpace(output_value), "--output");
      if (in_place && string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(spec))
        result.AddError("--in-place requires --input unless --spec supplies it.");
      if (!in_place && string.IsNullOrWhiteSpace(output_value) && string.IsNullOrWhiteSpace(spec))
        result.AddError("--output is required unless --in-place or --spec supplies it.");
      if (string.IsNullOrWhiteSpace(input) && string.IsNullOrWhiteSpace(spec) &&
          (!width.HasValue || !height.HasValue))
      {
        result.AddError("New maps require --width and --height unless --spec supplies them.");
      }
    });

    command.SetAction((ParseResult parse_result, CancellationToken cancellation_token) =>
    {
      Map_Edit_Request request = new Map_Edit_Request
      {
        Input = parse_result.GetValue(input_option),
        Output = parse_result.GetValue(output_option),
        In_Place = parse_result.GetValue(in_place_option),
        Spec = parse_result.GetValue(spec_option),
        Format = Cli_Validation.parse_format(parse_result.GetValue(format_option)),
        Width = parse_result.GetValue(width_option),
        Height = parse_result.GetValue(height_option),
        Tileset = parse_result.GetValue(tileset_option),
        Assets_Dir = parse_result.GetValue(assets_dir_option),
        Tileset_Image = parse_result.GetValue(tileset_image_option),
        Force = parse_result.GetValue(force_option),
      };
      return Cli_Command_Runner.run(
        () => executor.map_edit_async(request, output, cancellation_token), output, cancellation_token);
    });

    return command;
  }

  private static Command build_inspect(ICli_Executor executor, Cli_Output output)
  {
    Option<string> input_option = new Option<string>("--input", "-i")
    {
      Description = "Input .map, .mar, or .tmx file.",
      Required = true,
    };
    Option<int?> width_option = Common_Options.width();
    Option<int?> height_option = Common_Options.height();
    Option<string> tileset_option = Common_Options.tileset();
    Option<bool> json_option = new Option<bool>("--json")
    {
      Description = "Write stable machine-readable JSON instead of human-readable metadata.",
      DefaultValueFactory = _ => false,
    };

    Command command = new Command("inspect", "Inspect map metadata and row-major tile values.")
    {
      input_option,
      width_option,
      height_option,
      tileset_option,
      json_option,
    };
    command.Validators.Add(result =>
    {
      if (string.IsNullOrWhiteSpace(result.GetValue(input_option)))
        result.AddError("--input is required.");
      Cli_Validation.positive(result, result.GetValue(width_option), "--width");
      Cli_Validation.positive(result, result.GetValue(height_option), "--height");
    });
    command.SetAction((ParseResult parse_result, CancellationToken cancellation_token) =>
    {
      Map_Inspect_Request request = new Map_Inspect_Request
      {
        Input = parse_result.GetValue(input_option),
        Width = parse_result.GetValue(width_option),
        Height = parse_result.GetValue(height_option),
        Tileset = parse_result.GetValue(tileset_option),
        Json = parse_result.GetValue(json_option),
      };
      return Cli_Command_Runner.run(
        () => executor.map_inspect_async(request, output, cancellation_token), output, cancellation_token);
    });
    return command;
  }
}
