#nullable disable
namespace FE_Map_Creator.Generation;

/// <summary>
/// Options controlling a map repair pass (<see cref="Map_Generation_Engine.repair"/>).
/// </summary>
public sealed class Map_Repair_Options
{
  /// <summary>
  /// Repair implementation to use. The whole-map constraint solver is the default;
  /// legacy and hybrid modes remain explicitly selectable.
  /// </summary>
  public Map_Generation_Algorithm Algorithm { get; init; } = Map_Generation_Algorithm.Experimental_Constraint;

  /// <summary>
  /// Maximum backtracking nodes explored by the experimental constraint solver after it
  /// builds a fast greedy incumbent. Ignored by the legacy algorithm.
  /// </summary>
  public int Experimental_Search_Node_Limit { get; init; } = 10000;

  public int Experimental_Restart_Count { get; init; } = 4;

  public int Experimental_Nogood_Limit { get; init; } = 4096;

  public bool Experimental_Enable_Conflict_Learning { get; init; } = true;

  /// <summary>
  /// Enables fixed-point arc consistency at the root and after each assignment in
  /// experimental complete-search restarts.
  /// </summary>
  public bool Experimental_Enable_Branch_Arc_Consistency { get; init; } = false;

  public int Hybrid_Initial_Halo { get; init; } = 1;

  public int Hybrid_Max_Halo { get; init; } = 3;

  /// <summary>
  /// Manhattan-distance radius (in tiles) around every terrain-incompatible or
  /// already-empty (tile index 0) cell that gets reopened for regeneration. Must be
  /// zero or greater; zero means only the empty cells themselves are reopened.
  /// </summary>
  public int Radius { get; init; }

  /// <summary>
  /// Lookahead depth used for the regeneration pass that follows the repair scan. Must
  /// be 1 or 2 (matches the GUI's DEPTH/MAX_DEPTH constants).
  /// </summary>
  public int Depth { get; init; } = 1;

  /// <summary>
  /// Random seed to use. When null, the engine generates one and reports it back via
  /// <see cref="Map_Generation_Result.Seed"/> so a run can be reproduced later.
  /// </summary>
  public int? Seed { get; init; }
}
