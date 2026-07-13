using System.CommandLine;
using System.Threading.Tasks;
using FE_Map_Creator.Cli.Commands;
using FE_Map_Creator.Cli.Execution;

#nullable disable
namespace FE_Map_Creator.Cli;

internal static class Program
{
  private static async Task<int> Main(string[] args)
  {
    Cli_Output output = Cli_Output.console();
    ICli_Executor executor = new Core_Cli_Executor();
    RootCommand root = Root_Command_Factory.build(executor, output);
    ParseResult parse_result = root.Parse(args);
    return await parse_result.InvokeAsync();
  }
}
