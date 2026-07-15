using System.CommandLine;
using System.Threading;
using FE_Map_Creator.Cli.Execution;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

internal static class Validate_Command
{
  internal static Command build(ICli_Executor executor, Cli_Output output)
  {
    Option<string> input_option = new Option<string>("--input", "-i")
    {
      Description = "Map file to validate.",
    };
    Option<string> spec_option = Common_Options.spec();
    Option<string> algorithm_option = Common_Options.algorithm();
    Option<string> tileset_option = Common_Options.tileset();
    Option<int?> width_option = Common_Options.width();
    Option<int?> height_option = Common_Options.height();
    Option<string> assets_dir_option = Common_Options.assets_dir();
    Option<string> tileset_image_option = Common_Options.tileset_image();
    Option<string> generation_data_option = Common_Options.generation_data();

    Command command = new Command(
      "validate",
      "Validate nonzero map cells against learned mutual adjacency and optional spec terrain constraints.")
    {
      input_option,
      spec_option,
      algorithm_option,
      tileset_option,
      width_option,
      height_option,
      assets_dir_option,
      tileset_image_option,
      generation_data_option,
    };
    command.Validators.Add(result =>
    {
      if (string.IsNullOrWhiteSpace(result.GetValue(input_option))
        && string.IsNullOrWhiteSpace(result.GetValue(spec_option)))
        result.AddError("--input is required unless --spec supplies it.");
      Cli_Validation.positive(result, result.GetValue(width_option), "--width");
      Cli_Validation.positive(result, result.GetValue(height_option), "--height");
    });
    command.SetAction((ParseResult parse_result, CancellationToken cancellation_token) =>
    {
      Validate_Request request = new Validate_Request
      {
        Input = parse_result.GetValue(input_option),
        Spec = parse_result.GetValue(spec_option),
        Algorithm = Cli_Binding.explicit_value_or_null(parse_result, algorithm_option),
        Tileset = parse_result.GetValue(tileset_option),
        Width = parse_result.GetValue(width_option),
        Height = parse_result.GetValue(height_option),
        Assets_Dir = parse_result.GetValue(assets_dir_option),
        Tileset_Image = parse_result.GetValue(tileset_image_option),
        Generation_Data = parse_result.GetValue(generation_data_option),
      };
      return Cli_Command_Runner.run(
        () => executor.validate_async(request, output, cancellation_token),
        output,
        cancellation_token);
    });
    return command;
  }
}
