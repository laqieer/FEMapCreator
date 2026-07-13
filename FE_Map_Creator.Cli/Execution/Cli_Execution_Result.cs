#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Outcome of a single <see cref="ICli_Executor"/> operation. <see cref="Exit_Code"/> should
/// use the values in <see cref="Cli_Exit_Codes"/> so command actions can return it directly
/// as the process exit code.
/// </summary>
internal sealed class Cli_Execution_Result
{
  internal int Exit_Code { get; }

  /// <summary>Human-readable summary. Written to stdout on success, stderr on failure.</summary>
  internal string Summary { get; }

  internal Cli_Execution_Result(int exit_code, string summary)
  {
    this.Exit_Code = exit_code;
    this.Summary = summary;
  }

  internal static Cli_Execution_Result success(string summary)
  {
    return new Cli_Execution_Result(Cli_Exit_Codes.Success, summary);
  }

  internal static Cli_Execution_Result failure(string summary)
  {
    return new Cli_Execution_Result(Cli_Exit_Codes.Error, summary);
  }
}
