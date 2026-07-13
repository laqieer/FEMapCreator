using System.CommandLine;
using System.Threading;
using FE_Map_Creator.Cli.Execution;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Commands;

/// <summary>Builds the <c>batch --manifest</c> command.</summary>
internal static class Batch_Command
{
  internal static Command build(ICli_Executor executor, Cli_Output output)
  {
    Option<string> manifest_option = new Option<string>("--manifest", "-m")
    {
      Description = "Path to a JSON manifest containing a top-level \"jobs\" array of generate/repair job specs.",
      Required = true,
    };
    Option<bool> fail_fast_option = Common_Options.fail_fast();

    Command command = new Command("batch", "Run a heterogeneous batch of generate/repair jobs from a JSON manifest.")
    {
      manifest_option,
      fail_fast_option,
    };

    command.Validators.Add(result =>
    {
      string manifest = result.GetValue(manifest_option);
      if (string.IsNullOrWhiteSpace(manifest))
        result.AddError("--manifest is required.");
    });

    command.SetAction((ParseResult parse_result, CancellationToken cancellation_token) =>
    {
      Batch_Request request = new Batch_Request
      {
        Manifest = parse_result.GetValue(manifest_option),
        Fail_Fast = parse_result.GetValue(fail_fast_option),
      };
      return Cli_Command_Runner.run(
        () => executor.batch_async(request, output, cancellation_token), output, cancellation_token);
    });

    return command;
  }
}
