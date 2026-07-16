using FEXNA_Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#nullable disable
namespace FE_Map_Creator.Generation;

internal sealed class Hybrid_Map_Generation_Solver
{
  private readonly Tileset_Generation_Data _generation_data;
  private readonly Data_Tileset _terrain_tileset;

  internal Hybrid_Map_Generation_Solver(
    Tileset_Generation_Data generation_data,
    Data_Tileset terrain_tileset)
  {
    this._generation_data = generation_data;
    this._terrain_tileset = terrain_tileset;
  }

  internal Map_Generation_Result generate(
    Map_State state,
    Map_Generation_Options options,
    int seed,
    CancellationToken cancellation_token,
    Tile_Drawn_Callback tile_drawn,
    IProgress<int> progress)
  {
    Map_State legacy_state = clone_state(state);
    Map_Generation_Result legacy_result = new Map_Generation_Engine(
      this._generation_data,
      this._terrain_tileset).generate(
        legacy_state,
        new Map_Generation_Options()
        {
          Algorithm = Map_Generation_Algorithm.Legacy,
          Depth = options.Depth,
          Seed = seed
        },
        cancellation_token);
    return this.improve_and_publish(
      state,
      legacy_state,
      legacy_result,
      options.Depth,
      options.Experimental_Search_Node_Limit,
      options.Experimental_Restart_Count,
      options.Experimental_Nogood_Limit,
      options.Experimental_Enable_Conflict_Learning,
      options.Experimental_Enable_Branch_Arc_Consistency,
      options.Hybrid_Initial_Halo,
      options.Hybrid_Max_Halo,
      seed,
      cancellation_token,
      tile_drawn,
      progress);
  }

  internal Map_Generation_Result repair(
    Map_State state,
    Map_Repair_Options options,
    int seed,
    CancellationToken cancellation_token,
    Tile_Drawn_Callback tile_drawn,
    IProgress<int> progress)
  {
    Map_State legacy_state = clone_state(state);
    Map_Generation_Result legacy_result = new Map_Generation_Engine(
      this._generation_data,
      this._terrain_tileset).repair(
        legacy_state,
        new Map_Repair_Options()
        {
          Algorithm = Map_Generation_Algorithm.Legacy,
          Depth = options.Depth,
          Radius = options.Radius,
          Seed = seed
        },
        cancellation_token);
    return this.improve_and_publish(
      state,
      legacy_state,
      legacy_result,
      options.Depth,
      options.Experimental_Search_Node_Limit,
      options.Experimental_Restart_Count,
      options.Experimental_Nogood_Limit,
      options.Experimental_Enable_Conflict_Learning,
      options.Experimental_Enable_Branch_Arc_Consistency,
      options.Hybrid_Initial_Halo,
      options.Hybrid_Max_Halo,
      seed,
      cancellation_token,
      tile_drawn,
      progress);
  }

  private Map_Generation_Result improve_and_publish(
    Map_State destination,
    Map_State legacy_state,
    Map_Generation_Result legacy_result,
    int depth,
    int search_node_limit,
    int restart_count,
    int nogood_limit,
    bool enable_conflict_learning,
    bool enable_branch_arc_consistency,
    int initial_halo,
    int max_halo,
    int seed,
    CancellationToken cancellation_token,
    Tile_Drawn_Callback tile_drawn,
    IProgress<int> progress)
  {
    Map_State working_state = legacy_state;
    List<Cell[]> regions = collect_regions(
      legacy_result.Unresolved_Cells,
      legacy_state.Width,
      legacy_state.Height,
      max_halo,
      cancellation_token);
    Dictionary<int, Cell> final_unresolved = new Dictionary<int, Cell>();
    foreach (Cell cell in legacy_result.Unresolved_Cells)
      final_unresolved[cell.X + cell.Y * legacy_state.Width] = cell;
    List<Map_Generation_Attempt_Result> attempt_results =
      new List<Map_Generation_Attempt_Result>();
    List<Map_Generation_Component_Result> all_components =
      new List<Map_Generation_Component_Result>();
    int best_halo = -1;
    int total_nodes = 0;
    int total_propagation_removals = 0;
    int remaining_nodes = search_node_limit;
    bool improved = false;
    bool work_skipped_for_budget = false;

    for (int region_index = 0; region_index < regions.Count; ++region_index)
    {
      if (remaining_nodes <= 0)
      {
        work_skipped_for_budget = true;
        break;
      }
      Cell[] region = regions[region_index];
      int region_best_unresolved = region.Length;
      Map_State region_best_state = null;
      Map_Generation_Result region_best_result = null;
      int region_best_halo = -1;
      for (int halo = initial_halo;
        region_best_unresolved > 0 && halo <= max_halo;
        ++halo)
      {
        if (remaining_nodes <= 0)
        {
          work_skipped_for_budget = true;
          break;
        }
        cancellation_token.ThrowIfCancellationRequested();
        Map_State candidate_state = clone_state(working_state);
        bool[,] active_region = reopen_halo(candidate_state, region, halo);
        isolate_active_region(candidate_state, active_region);
        Map_Generation_Result candidate_result = new Map_Generation_Engine(
          this._generation_data,
          this._terrain_tileset).generate(
            candidate_state,
            new Map_Generation_Options()
            {
              Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
              Depth = depth,
              Experimental_Search_Node_Limit = remaining_nodes,
              Experimental_Restart_Count = restart_count,
              Experimental_Nogood_Limit = nogood_limit,
              Experimental_Enable_Conflict_Learning = enable_conflict_learning,
              Experimental_Enable_Branch_Arc_Consistency = enable_branch_arc_consistency,
              Seed = derive_halo_seed(seed, region_index, halo)
            },
            cancellation_token);
        Array.Copy(
          working_state.Locked,
          candidate_state.Locked,
          working_state.Locked.Length);
        attempt_results.Add(new Map_Generation_Attempt_Result(
          region_index,
          halo,
          candidate_result.Unresolved_Tile_Count,
          candidate_result.Search_Node_Count,
          candidate_result.Search_Budget_Exhausted,
          candidate_result.Components));
        all_components.AddRange(candidate_result.Components);
        total_nodes += candidate_result.Search_Node_Count;
        total_propagation_removals += candidate_result.Propagation_Removal_Count;
        remaining_nodes -= candidate_result.Search_Node_Count;
        if (candidate_result.Unresolved_Tile_Count < region_best_unresolved)
        {
          region_best_unresolved = candidate_result.Unresolved_Tile_Count;
          region_best_state = candidate_state;
          region_best_result = candidate_result;
          region_best_halo = halo;
        }
      }
      if (region_best_state == null)
        continue;
      improved = true;
      working_state = region_best_state;
      best_halo = Math.Max(best_halo, region_best_halo);
      foreach (Cell original in region)
        final_unresolved.Remove(original.X + original.Y * legacy_state.Width);
      foreach (Cell unresolved in region_best_result.Unresolved_Cells)
        final_unresolved[unresolved.X + unresolved.Y * legacy_state.Width] = unresolved;
      if (region_best_unresolved == 0)
      {
        foreach (Cell original in region)
          final_unresolved.Remove(original.X + original.Y * legacy_state.Width);
      }
    }

    cancellation_token.ThrowIfCancellationRequested();
    publish(destination, working_state, tile_drawn);
    progress?.Report(destination.Width * destination.Height);
    List<Cell> unresolved_cells = final_unresolved.Values
      .OrderBy(cell => cell.X + cell.Y * legacy_state.Width)
      .ToList();
    bool exhausted = unresolved_cells.Count > 0
      && (work_skipped_for_budget
        || attempt_results.Any(attempt => attempt.Search_Budget_Exhausted));
    return new Map_Generation_Result(
      unresolved_cells.Count,
      seed,
      Map_Generation_Algorithm.Experimental_Hybrid,
      unresolved_cells,
      exhausted,
      total_nodes,
      total_propagation_removals,
      all_components,
      legacy_result.Unresolved_Tile_Count,
      best_halo,
      attempt_results.Count,
      improved,
      attempt_results);
  }

  private static List<Cell[]> collect_regions(
    IReadOnlyList<Cell> unresolved_cells,
    int width,
    int height,
    int max_halo,
    CancellationToken cancellation_token)
  {
    Cell[] cells = unresolved_cells
      .GroupBy(cell => cell.X + cell.Y * width)
      .Select(group => group.First())
      .OrderBy(cell => cell.X + cell.Y * width)
      .ToArray();
    bool[] visited = new bool[cells.Length];
    int connection_distance = checked(max_halo * 2 + 1);
    Dictionary<int, int> cell_indices = new Dictionary<int, int>();
    for (int index = 0; index < cells.Length; ++index)
      cell_indices.Add(cells[index].X + cells[index].Y * width, index);
    List<Cell[]> regions = new List<Cell[]>();
    for (int start = 0; start < cells.Length; ++start)
    {
      if (visited[start])
        continue;
      List<Cell> region = new List<Cell>();
      Queue<int> queue = new Queue<int>();
      queue.Enqueue(start);
      visited[start] = true;
      while (queue.Count > 0)
      {
        cancellation_token.ThrowIfCancellationRequested();
        int index = queue.Dequeue();
        Cell cell = cells[index];
        region.Add(cell);
        for (int dy = -connection_distance; dy <= connection_distance; ++dy)
        {
          cancellation_token.ThrowIfCancellationRequested();
          int max_dx = connection_distance - Math.Abs(dy);
          for (int dx = -max_dx; dx <= max_dx; ++dx)
          {
            int x = cell.X + dx;
            int y = cell.Y + dy;
            if (x < 0 || x >= width || y < 0 || y >= height)
              continue;
            int key = x + y * width;
            if (!cell_indices.TryGetValue(key, out int candidate) || visited[candidate])
              continue;
            visited[candidate] = true;
            queue.Enqueue(candidate);
          }
        }
      }
      regions.Add(region
        .OrderBy(cell => cell.X + cell.Y * width)
        .ToArray());
    }
    return regions;
  }

  private static bool[,] reopen_halo(
    Map_State state,
    IReadOnlyList<Cell> unresolved_cells,
    int halo)
  {
    bool[,] reopen = new bool[state.Width, state.Height];
    foreach (Cell unresolved in unresolved_cells)
    {
      for (int dy = -halo; dy <= halo; ++dy)
      {
        int max_dx = halo - Math.Abs(dy);
        for (int dx = -max_dx; dx <= max_dx; ++dx)
        {
          int x = unresolved.X + dx;
          int y = unresolved.Y + dy;
          if (!state.is_off_map(x, y) && !state.Locked[x, y])
            reopen[x, y] = true;
        }
      }
    }
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (!reopen[x, y])
          continue;
        state.Tiles[x, y] = 0;
        state.Drawn[x, y] = false;
      }
    }
    return reopen;
  }

  private static void isolate_active_region(Map_State state, bool[,] active_region)
  {
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (!active_region[x, y] && !state.Drawn[x, y])
          state.Locked[x, y] = true;
      }
    }
  }

  private static void publish(
    Map_State destination,
    Map_State source,
    Tile_Drawn_Callback tile_drawn)
  {
    for (int y = 0; y < destination.Height; ++y)
    {
      for (int x = 0; x < destination.Width; ++x)
      {
        bool changed = destination.Tiles[x, y] != source.Tiles[x, y]
          || destination.Drawn[x, y] != source.Drawn[x, y];
        destination.Tiles[x, y] = source.Tiles[x, y];
        destination.Drawn[x, y] = source.Drawn[x, y];
        if (changed)
          tile_drawn?.Invoke(x, y, source.Tiles[x, y]);
      }
    }
  }

  private static int derive_halo_seed(int seed, int region, int halo)
  {
    ulong stream = (ulong) (region + 1) << 32 | (uint) (halo + 1);
    ulong value = (uint) seed + 0x9E3779B97F4A7C15UL * stream;
    value ^= value >> 30;
    value *= 0xBF58476D1CE4E5B9UL;
    value ^= value >> 27;
    value *= 0x94D049BB133111EBUL;
    value ^= value >> 31;
    return unchecked((int) (uint) value);
  }

  private static Map_State clone_state(Map_State state)
  {
    return new Map_State(
      (int[,]) state.Tiles.Clone(),
      (bool[,]) state.Drawn.Clone(),
      (bool[,]) state.Locked.Clone(),
      (int[,]) state.Terrain.Clone());
  }
}
