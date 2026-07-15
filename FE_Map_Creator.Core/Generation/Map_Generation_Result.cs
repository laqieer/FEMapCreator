using System;
using System.Collections.Generic;

#nullable disable
namespace FE_Map_Creator.Generation;

/// <summary>
/// Outcome of a <see cref="Map_Generation_Engine.generate"/> or
/// <see cref="Map_Generation_Engine.repair"/> call.
/// </summary>
public sealed class Map_Generation_Result
{
  /// <summary>
  /// Algorithm that produced this result.
  /// </summary>
  public Map_Generation_Algorithm Algorithm { get; }

  /// <summary>
  /// Number of cells the selected algorithm could not resolve. Legacy leaves those cells
  /// as drawn tile 0; experimental mode leaves them open and lists their coordinates.
  /// </summary>
  public int Unresolved_Tile_Count { get; }

  /// <summary>
  /// Experimental-solver cells that remain open. Legacy results retain their historical
  /// tile-0 representation and therefore expose an empty coordinate list.
  /// </summary>
  public IReadOnlyList<Cell> Unresolved_Cells { get; }

  public bool Is_Complete => this.Unresolved_Tile_Count == 0;

  public bool Search_Budget_Exhausted { get; }

  public int Search_Node_Count { get; }

  public int Search_Component_Count => this.Components.Count;

  public int Propagation_Removal_Count { get; }

  public IReadOnlyList<Map_Generation_Component_Result> Components { get; }

  /// <summary>
  /// The random seed actually used for this run (either the one supplied in the
  /// options, or one generated automatically when none was supplied).
  /// </summary>
  public int Seed { get; }

  public Map_Generation_Result(int unresolved_tile_count, int seed)
    : this(unresolved_tile_count, seed, Map_Generation_Algorithm.Legacy, Array.Empty<Cell>(), false, 0)
  {
  }

  public Map_Generation_Result(
    int unresolved_tile_count,
    int seed,
    Map_Generation_Algorithm algorithm,
    IReadOnlyList<Cell> unresolved_cells,
    bool search_budget_exhausted = false,
    int search_node_count = 0)
    : this(
      unresolved_tile_count,
      seed,
      algorithm,
      unresolved_cells,
      search_budget_exhausted,
      search_node_count,
      0,
      null)
  {
  }

  public Map_Generation_Result(
    int unresolved_tile_count,
    int seed,
    Map_Generation_Algorithm algorithm,
    IReadOnlyList<Cell> unresolved_cells,
    bool search_budget_exhausted,
    int search_node_count,
    int propagation_removal_count,
    IReadOnlyList<Map_Generation_Component_Result> components)
  {
    this.Unresolved_Tile_Count = unresolved_tile_count;
    this.Seed = seed;
    this.Algorithm = algorithm;
    this.Unresolved_Cells = unresolved_cells == null ? Array.Empty<Cell>() : new List<Cell>(unresolved_cells).AsReadOnly();
    this.Search_Budget_Exhausted = search_budget_exhausted;
    this.Search_Node_Count = search_node_count;
    this.Propagation_Removal_Count = propagation_removal_count;
    this.Components = components == null
      ? Array.Empty<Map_Generation_Component_Result>()
      : new List<Map_Generation_Component_Result>(components).AsReadOnly();
  }
}
