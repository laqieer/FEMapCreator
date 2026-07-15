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
  public int Restart_Count { get; }
  public int Best_Restart { get; }
  public int Nogood_Learned_Count { get; }
  public int Nogood_Retained_Count { get; }
  public int Nogood_Hit_Count { get; }
  public int Backjump_Count { get; }

  public Map_Generation_Component_Result(
    Cell origin,
    int cell_count,
    int unresolved_tile_count,
    int search_node_limit,
    int search_node_count,
    bool search_budget_exhausted,
    int propagation_removal_count)
    : this(
      origin,
      cell_count,
      unresolved_tile_count,
      search_node_limit,
      search_node_count,
      search_budget_exhausted,
      propagation_removal_count,
      0,
      -1,
      0,
      0,
      0,
      0)
  {
  }

  public Map_Generation_Component_Result(
    Cell origin,
    int cell_count,
    int unresolved_tile_count,
    int search_node_limit,
    int search_node_count,
    bool search_budget_exhausted,
    int propagation_removal_count,
    int restart_count,
    int best_restart,
    int nogood_learned_count,
    int nogood_retained_count,
    int nogood_hit_count,
    int backjump_count)
  {
    this.Origin = origin;
    this.Cell_Count = cell_count;
    this.Unresolved_Tile_Count = unresolved_tile_count;
    this.Search_Node_Limit = search_node_limit;
    this.Search_Node_Count = search_node_count;
    this.Search_Budget_Exhausted = search_budget_exhausted;
    this.Propagation_Removal_Count = propagation_removal_count;
    this.Restart_Count = restart_count;
    this.Best_Restart = best_restart;
    this.Nogood_Learned_Count = nogood_learned_count;
    this.Nogood_Retained_Count = nogood_retained_count;
    this.Nogood_Hit_Count = nogood_hit_count;
    this.Backjump_Count = backjump_count;
  }
}
