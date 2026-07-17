using System.CommandLine;
using System.Threading;
using FE_Map_Creator.Cli.Execution;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>Builds the <c>tilesets</c> discovery command group.</summary>
internal static class Tilesets_Command
{
  internal static Command build(ICli_Executor executor, Cli_Output output)
  {
    Command tilesets = new Command("tilesets", "Inspect bundled tileset assets.");
    tilesets.Subcommands.Add(build_list(executor, output));
    tilesets.Subcommands.Add(build_terrain(executor, output));
    return tilesets;
  }

  private static Command build_list(ICli_Executor executor, Cli_Output output)
  {
    Option<string> assets_dir_option = Common_Options.assets_dir();
    Option<bool> json_option = new Option<bool>("--json")
    {
      Description = "Write stable machine-readable JSON.",
      DefaultValueFactory = _ => false,
    };

    Command command = new Command("list", "List discoverable tileset selectors, asset paths, and pairing diagnostics.")
    {
      assets_dir_option,
      json_option,
    };

    command.SetAction((ParseResult parse_result, CancellationToken cancellation_token) =>
    {
      Tilesets_List_Request request = new Tilesets_List_Request
      {
        Assets_Dir = parse_result.GetValue(assets_dir_option),
        Json = parse_result.GetValue(json_option),
      };
      return Cli_Command_Runner.run(
        () => executor.tilesets_list_async(request, output, cancellation_token), output, cancellation_token);
    });

    return command;
  }

  private static Command build_terrain(ICli_Executor executor, Cli_Output output)
  {
    Option<string> tileset_option = Common_Options.tileset();
    tileset_option.Required = true;
    Option<string> assets_dir_option = Common_Options.assets_dir();
    Option<bool> json_option = new Option<bool>("--json")
    {
      Description = "Write stable machine-readable JSON including per-tile terrain tags.",
      DefaultValueFactory = _ => false,
    };

    Command command = new Command(
      "terrain",
      "List terrain ids and names supported by a tileset without loading generation data.")
    {
      tileset_option,
      assets_dir_option,
      json_option,
    };
    command.Validators.Add(result =>
    {
      if (string.IsNullOrWhiteSpace(result.GetValue(tileset_option)))
        result.AddError("--tileset is required.");
    });
    command.SetAction((ParseResult parse_result, CancellationToken cancellation_token) =>
    {
      Tilesets_Terrain_Request request = new Tilesets_Terrain_Request
      {
        Tileset = parse_result.GetValue(tileset_option),
        Assets_Dir = parse_result.GetValue(assets_dir_option),
        Json = parse_result.GetValue(json_option),
      };
      return Cli_Command_Runner.run(
        () => executor.tilesets_terrain_async(request, output, cancellation_token),
        output,
        cancellation_token);
    });
    return command;
  }
}
