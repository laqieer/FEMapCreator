using System;
using FE_Map_Creator.Cli;
using FE_Map_Creator.Generation;

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
    Map_Generation_Result result,
    string action_label,
    Action<string> write_temporary)
  {
    string algorithm_suffix = Algorithm_Selection.suffix(result.Algorithm);
    string search_suffix = result.Search_Budget_Exhausted
      ? $", experimental search budget exhausted after {result.Search_Node_Count} node(s)"
      : "";
    if (require_complete && result.Unresolved_Tile_Count > 0)
    {
      return new Cli_Execution_Result(
        Cli_Exit_Codes.Incomplete,
        $"{action_label}{algorithm_suffix} produced {result.Unresolved_Tile_Count} unresolved cell(s) " +
        $"(seed {result.Seed}{search_suffix}); " +
        $"output was not written because --require-complete was specified.");
    }

    Safe_Output.write(output_path, force, write_temporary);

    string summary =
      $"{action_label} \"{output_path}\"{algorithm_suffix} " +
      $"(seed {result.Seed}, {result.Unresolved_Tile_Count} unresolved cell(s){search_suffix}).";
    if (result.Unresolved_Tile_Count > 0 && !allow_incomplete)
      return new Cli_Execution_Result(Cli_Exit_Codes.Incomplete, summary);
    return Cli_Execution_Result.success(summary);
  }
}
