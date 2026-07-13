using System;
using System.Threading;
using System.Threading.Tasks;
using FE_Map_Creator.Cli.Execution;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>
/// Runs an executor call from a command action, routes the resulting summary to
/// stdout/stderr based on its exit code, and translates cancellation/unexpected
/// exceptions into a stable exit code instead of letting them escape the action.
/// </summary>
internal static class Cli_Command_Runner
{
  internal static async Task<int> run(
    Func<Task<Cli_Execution_Result>> operation,
    Cli_Output output,
    CancellationToken cancellation_token)
  {
    try
    {
      Cli_Execution_Result result = await operation().ConfigureAwait(false);
      var writer = result.Exit_Code == Cli_Exit_Codes.Error ? output.Error : output.Out;
      if (!string.IsNullOrEmpty(result.Summary))
        writer.WriteLine(result.Summary);
      return result.Exit_Code;
    }
    catch (OperationCanceledException) when (cancellation_token.IsCancellationRequested)
    {
      output.Error.WriteLine("Operation canceled.");
      return Cli_Exit_Codes.Error;
    }
    catch (Exception ex)
    {
      output.Error.WriteLine($"Error: {ex.Message}");
      return Cli_Exit_Codes.Error;
    }
  }
}
