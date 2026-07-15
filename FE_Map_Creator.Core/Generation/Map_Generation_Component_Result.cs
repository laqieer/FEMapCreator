#nullable disable
namespace FE_Map_Creator.Generation;

public sealed class Map_Generation_Component_Result
{
  public Cell Origin { get; }
  public int Cell_Count { get; }
  public int Unresolved_Tile_Count { get; }
  public int Search_Node_Limit { get; }
  public int Search_Node_Count { get; }
  public bool Search_Budget_Exhausted { get; }
  public int Propagation_Removal_Count { get; }

  public Map_Generation_Component_Result(
    Cell origin,
    int cell_count,
    int unresolved_tile_count,
    int search_node_limit,
    int search_node_count,
    bool search_budget_exhausted,
    int propagation_removal_count)
  {
    this.Origin = origin;
    this.Cell_Count = cell_count;
    this.Unresolved_Tile_Count = unresolved_tile_count;
    this.Search_Node_Limit = search_node_limit;
    this.Search_Node_Count = search_node_count;
    this.Search_Budget_Exhausted = search_budget_exhausted;
    this.Propagation_Removal_Count = propagation_removal_count;
  }
}
