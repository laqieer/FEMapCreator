using System;
using FE_Map_Creator.Cli;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Shared "write the result and pick an exit code" logic for <c>generate</c>/<c>repair</c>,
/// implementing the incomplete-result rules from plan.md: writing by default (exit 2 if
/// unresolved cells remain), <c>--allow-incomplete</c> permitting exit 0, and
/// <c>--require-complete</c> suppressing the write entirely (exit 2) if any cell is
/// unresolved. Every write goes through <see cref="Safe_Output.write"/> so overwrite
/// protection and atomic in-place replacement stay centralized.
/// </summary>
internal static class Incomplete_Result_Writer
{
  internal static Cli_Execution_Result write(
    string output_path,
    bool force,
    bool allow_incomplete,
    bool require_complete,
    int unresolved_count,
    int seed,
    string action_label,
    Action<string> write_temporary)
  {
    if (require_complete && unresolved_count > 0)
    {
      return new Cli_Execution_Result(
        Cli_Exit_Codes.Incomplete,
        $"{action_label} produced {unresolved_count} unresolved cell(s) (seed {seed}); " +
        $"output was not written because --require-complete was specified.");
    }

    Safe_Output.write(output_path, force, write_temporary);

    string summary = $"{action_label} \"{output_path}\" (seed {seed}, {unresolved_count} unresolved cell(s)).";
    if (unresolved_count > 0 && !allow_incomplete)
      return new Cli_Execution_Result(Cli_Exit_Codes.Incomplete, summary);
    return Cli_Execution_Result.success(summary);
  }
}
