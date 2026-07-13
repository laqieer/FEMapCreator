using System.CommandLine;
using FE_Map_Creator.Cli.Execution;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>Assembles the top-level <see cref="RootCommand"/> from the individual command builders.</summary>
internal static class Root_Command_Factory
{
  internal static RootCommand build(ICli_Executor executor, Cli_Output output)
  {
    RootCommand root = new RootCommand("FE Map Creator command-line tool for generating and repairing maps.");
    root.Subcommands.Add(Generate_Command.build(executor, output));
    root.Subcommands.Add(Repair_Command.build(executor, output));
    root.Subcommands.Add(Batch_Command.build(executor, output));
    root.Subcommands.Add(Tilesets_Command.build(executor, output));
    return root;
  }
}
