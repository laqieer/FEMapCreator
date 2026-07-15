#nullable disable
namespace FE_Map_Creator.Cli.Requests;

/// <summary>
/// Bound options for the <c>repair</c> command. This is a pure data-transfer object;
/// all cross-option validation happens in <see cref="Commands.Repair_Command"/> before
/// the request reaches an <see cref="Execution.ICli_Executor"/>.
/// </summary>
internal sealed class Repair_Request
{
  internal string Input { get; init; }

  internal string Output { get; init; }

  internal bool In_Place { get; init; }

  internal string Spec { get; init; }

  internal string Tileset { get; init; }

  internal int? Width { get; init; }

  internal int? Height { get; init; }

  /// <summary>
  /// Null when <c>--algorithm</c> was not explicitly typed so a spec-supplied selector
  /// can still take effect; the executor falls back to legacy.
  /// </summary>
  internal string Algorithm { get; init; }

  internal int? Experimental_Search_Node_Limit { get; init; }

  internal int? Experimental_Restart_Count { get; init; }

  internal int? Experimental_Nogood_Limit { get; init; }

  internal bool? Experimental_Enable_Conflict_Learning { get; init; }

  /// <summary>
  /// Null when <c>--repair-radius</c> was not explicitly typed on the command line, so a
  /// spec-supplied radius can still take effect; the executor falls back to 0.
  /// </summary>
  internal int? Repair_Radius { get; init; }

  /// <summary>
  /// Null when <c>--depth</c> was not explicitly typed on the command line, so a
  /// spec-supplied depth can still take effect; the executor falls back to 1.
  /// </summary>
  internal int? Depth { get; init; }

  internal int? Seed { get; init; }

  internal string Assets_Dir { get; init; }

  internal string Tileset_Image { get; init; }

  internal string Generation_Data { get; init; }

  internal bool Force { get; init; }

  internal bool Allow_Incomplete { get; init; }

  internal bool Require_Complete { get; init; }

  internal string Input_Dir { get; init; }

  internal string Output_Dir { get; init; }

  internal string Pattern { get; init; } = "*.map";

  internal bool Recursive { get; init; }

  internal bool Fail_Fast { get; init; }

  /// <summary>True when <c>--input-dir</c>/<c>--output-dir</c> homogeneous directory repair was requested.</summary>
  internal bool Is_Directory_Mode => !string.IsNullOrWhiteSpace(this.Input_Dir);
}
