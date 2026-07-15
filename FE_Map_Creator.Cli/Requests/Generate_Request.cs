#nullable disable
namespace FE_Map_Creator.Cli.Requests;

/// <summary>
/// Bound options for the <c>generate</c> command. This is a pure data-transfer object;
/// all cross-option validation happens in <see cref="Commands.Generate_Command"/> before
/// the request reaches an <see cref="Execution.ICli_Executor"/>.
/// </summary>
internal sealed class Generate_Request
{
  internal int? Width { get; init; }

  internal int? Height { get; init; }

  internal string Tileset { get; init; }

  internal string Output { get; init; }

  internal Cli_Map_Format? Format { get; init; }

  internal string Template { get; init; }

  internal string Spec { get; init; }

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
  /// Null when <c>--depth</c> was not explicitly typed on the command line, so a
  /// spec-supplied depth can still take effect; the executor falls back to 1 when
  /// neither the CLI nor the spec provide a value.
  /// </summary>
  internal int? Depth { get; init; }

  internal int? Seed { get; init; }

  internal string Assets_Dir { get; init; }

  internal string Tileset_Image { get; init; }

  internal string Generation_Data { get; init; }

  internal int? Count { get; init; }

  internal string Output_Dir { get; init; }

  internal string Name_Template { get; init; } = "map-{index}";

  internal bool Force { get; init; }

  internal bool Allow_Incomplete { get; init; }

  internal bool Require_Complete { get; init; }

  /// <summary>True when <c>--count</c>/<c>--output-dir</c> homogeneous multi-map generation was requested.</summary>
  internal bool Is_Batch_Mode => this.Count.HasValue;
}
