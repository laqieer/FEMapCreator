using System;
using System.Collections.Generic;

#nullable disable
namespace FE_Map_Creator.Generation;

public sealed class Map_Generation_Attempt_Result
{
  public int Region_Index { get; }
  public int Halo { get; }
  public int Unresolved_Tile_Count { get; }
  public int Search_Node_Count { get; }
  public bool Search_Budget_Exhausted { get; }
  public IReadOnlyList<Map_Generation_Component_Result> Components { get; }

  public Map_Generation_Attempt_Result(
    int region_index,
    int halo,
    int unresolved_tile_count,
    int search_node_count,
    bool search_budget_exhausted,
    IReadOnlyList<Map_Generation_Component_Result> components)
  {
    this.Region_Index = region_index;
    this.Halo = halo;
    this.Unresolved_Tile_Count = unresolved_tile_count;
    this.Search_Node_Count = search_node_count;
    this.Search_Budget_Exhausted = search_budget_exhausted;
    this.Components = components == null
      ? Array.Empty<Map_Generation_Component_Result>()
      : new List<Map_Generation_Component_Result>(components).AsReadOnly();
  }
}
