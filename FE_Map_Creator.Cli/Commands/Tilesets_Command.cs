using System.CommandLine;
using System.Threading;
using FE_Map_Creator.Cli.Execution;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>Builds the <c>tilesets</c> command group, currently containing <c>tilesets list</c>.</summary>
internal static class Tilesets_Command
{
  internal static Command build(ICli_Executor executor, Cli_Output output)
  {
    Command tilesets = new Command("tilesets", "Inspect bundled tileset assets.");
    tilesets.Subcommands.Add(build_list(executor, output));
    return tilesets;
  }

  private static Command build_list(ICli_Executor executor, Cli_Output output)
  {
    Option<string> assets_dir_option = Common_Options.assets_dir();

    Command command = new Command("list", "List discoverable tileset selectors, asset paths, and pairing diagnostics.")
    {
      assets_dir_option,
    };

    command.SetAction((ParseResult parse_result, CancellationToken cancellation_token) =>
    {
      Tilesets_List_Request request = new Tilesets_List_Request
      {
        Assets_Dir = parse_result.GetValue(assets_dir_option),
      };
      return Cli_Command_Runner.run(
        () => executor.tilesets_list_async(request, output, cancellation_token), output, cancellation_token);
    });

    return command;
  }
}
