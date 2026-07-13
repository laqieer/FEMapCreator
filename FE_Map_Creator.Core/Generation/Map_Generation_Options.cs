#nullable disable
namespace FE_Map_Creator.Generation;

/// <summary>
/// Options controlling a fresh map generation pass (<see cref="Map_Generation_Engine.generate"/>).
/// </summary>
public sealed class Map_Generation_Options
{
  /// <summary>
  /// Lookahead depth for the neighbor-search backtracking algorithm. Must be 1 or 2
  /// (matches the GUI's DEPTH/MAX_DEPTH constants).
  /// </summary>
  public int Depth { get; init; } = 1;

  /// <summary>
  /// Random seed to use. When null, the engine generates one and reports it back via
  /// <see cref="Map_Generation_Result.Seed"/> so a run can be reproduced later.
  /// </summary>
  public int? Seed { get; init; }
}
