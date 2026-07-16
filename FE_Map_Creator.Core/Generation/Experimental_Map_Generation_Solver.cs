using FEXNA_Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;

#nullable disable
namespace FE_Map_Creator.Generation;

internal sealed class Experimental_Map_Generation_Solver
{
  private static readonly byte[] Directions = new byte[] { 2, 4, 6, 8 };

  private readonly Data_Tileset _terrain_tileset;
  private readonly Experimental_Tile_Model _model;

  internal Experimental_Map_Generation_Solver(
    Tileset_Generation_Data generation_data,
    Data_Tileset terrain_tileset)
  {
    this._terrain_tileset = terrain_tileset;
    this._model = new Experimental_Tile_Model(generation_data, terrain_tileset);
  }

  internal Map_Generation_Result generate(
    Map_State state,
    int depth,
    int search_node_limit,
    int restart_count,
    int nogood_limit,
    bool enable_conflict_learning,
    int seed,
    CancellationToken cancellation_token,
    Tile_Drawn_Callback tile_drawn,
    IProgress<int> progress)
  {
    Map_State working = clone_state(state);
    return this.solve_and_publish(
      state,
      working,
      depth,
      search_node_limit,
      restart_count,
      nogood_limit,
      enable_conflict_learning,
      seed,
      cancellation_token,
      tile_drawn,
      progress);
  }

  internal Map_Generation_Result repair(
    Map_State state,
    int depth,
    int radius,
    int search_node_limit,
    int restart_count,
    int nogood_limit,
    bool enable_conflict_learning,
    int seed,
    CancellationToken cancellation_token,
    Tile_Drawn_Callback tile_drawn,
    IProgress<int> progress)
  {
    Map_State working = clone_state(state);
    this.resolve_terrain_incompatible_cells(working);
    reopen_cells_around_holes(working, radius, cancellation_token);
    return this.solve_and_publish(
      state,
      working,
      depth,
      search_node_limit,
      restart_count,
      nogood_limit,
      enable_conflict_learning,
      seed,
      cancellation_token,
      tile_drawn,
      progress);
  }

  private Map_Generation_Result solve_and_publish(
    Map_State destination,
    Map_State working,
    int depth,
    int search_node_limit,
    int restart_count,
    int nogood_limit,
    bool enable_conflict_learning,
    int seed,
    CancellationToken cancellation_token,
    Tile_Drawn_Callback tile_drawn,
    IProgress<int> progress)
  {
    List<Cell[]> components = collect_components(working, cancellation_token);
    long[] component_scores = this.calculate_component_scores(working, components);
    int remaining_budget = search_node_limit;
    Search_Result[] search_results = new Search_Result[components.Count];
    int[] component_node_limits = new int[components.Count];
    int[] component_node_counts = new int[components.Count];
    int[] component_propagation_removals = new int[components.Count];
    int[] component_restart_counts = new int[components.Count];
    int[] component_best_restarts = Enumerable.Repeat(-1, components.Count).ToArray();
    int[] component_nogood_counts = new int[components.Count];
    int[] component_nogood_retained = new int[components.Count];
    int[] component_nogood_hits = new int[components.Count];
    int[] component_backjump_counts = new int[components.Count];
    bool[] component_restart_exhausted = new bool[components.Count];
    List<Cell> unresolved = new List<Cell>();
    int progress_offset = 0;
    int[] solve_order = Enumerable.Range(0, components.Count)
      .OrderBy(index => component_scores[index])
      .ThenBy(index => index)
      .ToArray();
    for (int order_index = 0; order_index < solve_order.Length; ++order_index)
    {
      cancellation_token.ThrowIfCancellationRequested();
      int component_index = solve_order[order_index];
      Cell[] component = components[component_index];
      int component_budget = remaining_budget;
      component_node_limits[component_index] = component_budget;
      int component_remaining = component_budget;
      Nogood_Cache nogoods = new Nogood_Cache(nogood_limit);
      Search_Result best_result = null;
      int best_unresolved = int.MaxValue;
      double best_diversity_penalty = double.MaxValue;
      for (int restart = 0; restart < restart_count && component_remaining > 0; ++restart)
      {
        int restarts_left = restart_count - restart;
        int restart_budget;
        if (restart == 0 && restart_count > 1)
          restart_budget = Math.Min(component_remaining, Math.Max(1, component_budget / 2));
        else if (restart + 1 == restart_count)
          restart_budget = component_remaining;
        else
          restart_budget = Math.Max(1, (component_remaining + restarts_left - 1) / restarts_left);
        Search_Mode mode = restart == 0 || restart + 1 == restart_count && restart_count > 2
          ? Search_Mode.Partial
          : Search_Mode.Complete;
        Random restart_random = new Random(derive_seed(seed, component_index, restart));
        Search search = new Search(
          this._model,
          working,
          component,
          depth,
          restart_budget,
          mode,
          nogoods,
          enable_conflict_learning,
          restart_random,
          cancellation_token,
          progress == null || restart > 0 ? null : new Component_Progress(progress, progress_offset));
        Search_Result restart_result = search.solve();
        ++component_restart_counts[component_index];
        component_node_counts[component_index] += restart_result.Search_Node_Count;
        component_propagation_removals[component_index] += restart_result.Propagation_Removal_Count;
        component_nogood_counts[component_index] += restart_result.Nogood_Learned_Count;
        component_nogood_hits[component_index] += restart_result.Nogood_Hit_Count;
        component_backjump_counts[component_index] += restart_result.Backjump_Count;
        component_restart_exhausted[component_index] |= restart_result.Search_Budget_Exhausted;
        component_remaining -= restart_result.Search_Node_Count;
        remaining_budget -= restart_result.Search_Node_Count;
        int restart_unresolved = restart_result.Assignments.Count(assignment => assignment < 0);
        double restart_diversity_penalty =
          diversity_penalty(component, restart_result.Assignments, working.Width);
        if (restart_unresolved < best_unresolved
          || restart_unresolved == best_unresolved
            && restart_diversity_penalty < best_diversity_penalty)
        {
          best_result = restart_result;
          best_unresolved = restart_unresolved;
          best_diversity_penalty = restart_diversity_penalty;
          component_best_restarts[component_index] = restart;
        }
        if (restart_unresolved == 0)
          break;
      }
      if (best_result == null)
      {
        Search search = new Search(
          this._model,
          working,
          component,
          depth,
          0,
          Search_Mode.Partial,
          nogoods,
          enable_conflict_learning,
          new Random(derive_seed(seed, component_index, 0)),
          cancellation_token,
          null);
        best_result = search.solve();
        component_restart_counts[component_index] = 1;
        component_best_restarts[component_index] = 0;
        component_restart_exhausted[component_index] = best_result.Search_Budget_Exhausted;
      }
      search_results[component_index] = best_result;
      component_nogood_retained[component_index] = nogoods.Count;
      progress_offset += component.Length;
      progress?.Report(progress_offset);
    }

    Map_Generation_Component_Result[] component_results =
      new Map_Generation_Component_Result[components.Count];
    int search_node_count = component_node_counts.Sum();
    int propagation_removal_count = component_propagation_removals.Sum();
    for (int component_index = 0; component_index < components.Count; ++component_index)
    {
      Search_Result search_result = search_results[component_index];
      Cell[] component = components[component_index];
      Random alias_random = new Random(derive_seed(seed, component_index + components.Count));
      int component_unresolved = 0;
      for (int variable = 0; variable < search_result.Cells.Length; ++variable)
      {
        Cell cell = search_result.Cells[variable];
        int candidate = search_result.Assignments[variable];
        if (candidate < 0)
        {
          working.Tiles[cell.X, cell.Y] = 0;
          working.Drawn[cell.X, cell.Y] = false;
          unresolved.Add(cell);
          ++component_unresolved;
        }
        else
        {
          working.Tiles[cell.X, cell.Y] =
            this._model.pick_alias(candidate, working.Terrain[cell.X, cell.Y], alias_random);
          working.Drawn[cell.X, cell.Y] = true;
        }
      }
      bool component_exhausted = component_unresolved > 0
        && component_restart_exhausted[component_index]
        && component_node_counts[component_index] >= component_node_limits[component_index];
      component_results[component_index] = new Map_Generation_Component_Result(
        component[0],
        component.Length,
        component_unresolved,
        component_node_limits[component_index],
        component_node_counts[component_index],
        component_exhausted,
        component_propagation_removals[component_index],
        component_restart_counts[component_index],
        component_best_restarts[component_index],
        component_nogood_counts[component_index],
        component_nogood_retained[component_index],
        component_nogood_hits[component_index],
        component_backjump_counts[component_index]);
    }
    bool search_budget_exhausted =
      component_results.Any(result => result.Search_Budget_Exhausted);

    cancellation_token.ThrowIfCancellationRequested();
    for (int y = 0; y < destination.Height; ++y)
    {
      for (int x = 0; x < destination.Width; ++x)
      {
        bool changed = destination.Tiles[x, y] != working.Tiles[x, y]
          || destination.Drawn[x, y] != working.Drawn[x, y];
        destination.Tiles[x, y] = working.Tiles[x, y];
        destination.Drawn[x, y] = working.Drawn[x, y];
        if (changed)
          tile_drawn?.Invoke(x, y, working.Tiles[x, y]);
      }
    }

    return new Map_Generation_Result(
      unresolved.Count,
      seed,
      Map_Generation_Algorithm.Experimental_Constraint,
      unresolved,
      search_budget_exhausted,
      search_node_count,
      propagation_removal_count,
      component_results);
  }

  private static List<Cell[]> collect_components(
    Map_State state,
    CancellationToken cancellation_token)
  {
    bool[,] visited = new bool[state.Width, state.Height];
    List<Cell[]> components = new List<Cell[]>();
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (visited[x, y] || state.Drawn[x, y] || state.Locked[x, y])
          continue;
        List<Cell> component = new List<Cell>();
        Queue<Cell> queue = new Queue<Cell>();
        queue.Enqueue(new Cell(x, y));
        visited[x, y] = true;
        while (queue.Count > 0)
        {
          cancellation_token.ThrowIfCancellationRequested();
          Cell cell = queue.Dequeue();
          component.Add(cell);
          foreach (byte direction in Directions)
          {
            Cell neighbor = neighbor_cell(cell, direction);
            if (state.is_off_map(neighbor.X, neighbor.Y)
              || visited[neighbor.X, neighbor.Y]
              || state.Drawn[neighbor.X, neighbor.Y]
              || state.Locked[neighbor.X, neighbor.Y])
              continue;
            visited[neighbor.X, neighbor.Y] = true;
            queue.Enqueue(neighbor);
          }
        }
        component.Sort((left, right) =>
          (left.X + left.Y * state.Width).CompareTo(right.X + right.Y * state.Width));
        components.Add(component.ToArray());
      }
    }
    components.Sort((left, right) =>
      (left[0].X + left[0].Y * state.Width).CompareTo(right[0].X + right[0].Y * state.Width));
    return components;
  }

  private long[] calculate_component_scores(
    Map_State state,
    IReadOnlyList<Cell[]> components)
  {
    long[] scores = new long[components.Count];
    for (int component = 0; component < components.Count; ++component)
    {
      long score = 0;
      foreach (Cell cell in components[component])
        score += Math.Max(1, this._model.terrain_candidate_count(state.Terrain[cell.X, cell.Y]));
      scores[component] = Math.Max(1, score);
    }
    return scores;
  }

  private static double diversity_penalty(
    IReadOnlyList<Cell> cells,
    IReadOnlyList<int> assignments,
    int map_width)
  {
    Dictionary<int, int> counts = new Dictionary<int, int>();
    Dictionary<int, int> assignments_by_cell = new Dictionary<int, int>();
    int assigned_count = 0;
    for (int variable = 0; variable < assignments.Count; ++variable)
    {
      int assignment = assignments[variable];
      if (assignment < 0)
        continue;
      ++assigned_count;
      counts[assignment] = counts.TryGetValue(assignment, out int count) ? count + 1 : 1;
      Cell cell = cells[variable];
      assignments_by_cell[cell.X + cell.Y * map_width] = assignment;
    }
    if (assigned_count < 20)
      return 0.0;

    int dominant_count = counts.Values.Max();
    double dominant_share = dominant_count / (double) assigned_count;
    double entropy = 0.0;
    foreach (int count in counts.Values)
    {
      double probability = count / (double) assigned_count;
      entropy -= probability * Math.Log2(probability);
    }
    int checked_neighbors = 0;
    int same_neighbors = 0;
    foreach (KeyValuePair<int, int> assignment in assignments_by_cell)
    {
      int x = assignment.Key % map_width;
      int y = assignment.Key / map_width;
      if (x + 1 < map_width
        && assignments_by_cell.TryGetValue(x + 1 + y * map_width, out int right))
      {
        ++checked_neighbors;
        if (right == assignment.Value)
          ++same_neighbors;
      }
      if (assignments_by_cell.TryGetValue(x + (y + 1) * map_width, out int down))
      {
        ++checked_neighbors;
        if (down == assignment.Value)
          ++same_neighbors;
      }
    }
    double same_neighbor_share = checked_neighbors == 0
      ? 0.0
      : same_neighbors / (double) checked_neighbors;
    double dominant_penalty = Math.Max(0.0, dominant_share - 0.25) * assigned_count * 0.25;
    double repetition_penalty = Math.Max(0.0, same_neighbor_share - 0.35) * assigned_count * 0.10;
    double entropy_penalty = Math.Max(0.0, 4.0 - entropy) * 2.0;
    return dominant_penalty + repetition_penalty + entropy_penalty;
  }

  private static int derive_seed(int seed, int component_index)
  {
    return derive_seed(seed, component_index, 0);
  }

  private static int derive_seed(int seed, int component_index, int restart_index)
  {
    ulong stream = (ulong) (component_index + 1) << 32 | (uint) (restart_index + 1);
    ulong value = (uint) seed + 0x9E3779B97F4A7C15UL * stream;
    value ^= value >> 30;
    value *= 0xBF58476D1CE4E5B9UL;
    value ^= value >> 27;
    value *= 0x94D049BB133111EBUL;
    value ^= value >> 31;
    return unchecked((int) (uint) value);
  }

  private void resolve_terrain_incompatible_cells(Map_State state)
  {
    if (this._terrain_tileset == null)
      return;
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (state.Locked[x, y])
          continue;
        int terrain = state.Terrain[x, y];
        int tile = state.Tiles[x, y];
        if (terrain == 0 || tile < 0 || tile >= this._terrain_tileset.Terrain_Tags.Count)
          continue;
        int tag = this._terrain_tileset.Terrain_Tags[tile];
        bool incompatible = terrain > 0 ? tag != terrain : tag == -terrain;
        if (!incompatible)
          continue;
        state.Tiles[x, y] = 0;
        state.Drawn[x, y] = false;
      }
    }
  }

  private static void reopen_cells_around_holes(
    Map_State state,
    int radius,
    CancellationToken cancellation_token)
  {
    List<Cell> holes = new List<Cell>();
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (!state.Drawn[x, y])
          holes.Add(new Cell(x, y));
      }
    }

    foreach (Cell hole in holes)
    {
      cancellation_token.ThrowIfCancellationRequested();
      for (int dy = -radius; dy <= radius; ++dy)
      {
        int max_dx = radius - Math.Abs(dy);
        for (int dx = -max_dx; dx <= max_dx; ++dx)
        {
          int x = hole.X + dx;
          int y = hole.Y + dy;
          if (state.is_off_map(x, y) || state.Locked[x, y])
            continue;
          state.Tiles[x, y] = 0;
          state.Drawn[x, y] = false;
        }
      }
    }
    foreach (Cell locked_hole in holes)
    {
      if (state.Locked[locked_hole.X, locked_hole.Y])
        state.Drawn[locked_hole.X, locked_hole.Y] = true;
    }
  }

  private static Map_State clone_state(Map_State state)
  {
    return new Map_State(
      (int[,]) state.Tiles.Clone(),
      (bool[,]) state.Drawn.Clone(),
      (bool[,]) state.Locked.Clone(),
      (int[,]) state.Terrain.Clone());
  }

  private enum Search_Mode
  {
    Partial,
    Complete,
  }

  private sealed class Nogood_Cache
  {
    private readonly int _capacity;
    private readonly Dictionary<string, int[]> _conflicts = new Dictionary<string, int[]>();
    private readonly Queue<string> _order = new Queue<string>();

    internal int Count => this._conflicts.Count;

    internal Nogood_Cache(int capacity)
    {
      this._capacity = capacity;
    }

    internal bool try_get(string key, out int[] conflict)
    {
      return this._conflicts.TryGetValue(key, out conflict);
    }

    internal bool add(string key, IEnumerable<int> conflict)
    {
      if (this._capacity <= 0 || this._conflicts.ContainsKey(key))
        return false;
      while (this._conflicts.Count >= this._capacity)
      {
        string oldest = this._order.Dequeue();
        this._conflicts.Remove(oldest);
      }
      this._conflicts.Add(key, conflict.Distinct().OrderBy(value => value).ToArray());
      this._order.Enqueue(key);
      return true;
    }
  }

  private sealed class Search
  {
    private const int Unassigned = -2;
    private const int Unresolved = -1;

    private readonly Experimental_Tile_Model _model;
    private readonly Map_State _state;
    private readonly int _depth;
    private readonly int _search_node_limit;
    private readonly Search_Mode _mode;
    private readonly Nogood_Cache _nogoods;
    private readonly bool _enable_conflict_learning;
    private readonly Random _random;
    private readonly CancellationToken _cancellation_token;
    private readonly IProgress<int> _progress;
    private readonly Cell[] _cells;
    private readonly Dictionary<int, int> _variable_at = new Dictionary<int, int>();
    private readonly ulong[][] _domains;
    private readonly int[] _domain_counts;
    private readonly int[] _assignments;
    private readonly int[] _candidate_usage_counts;
    private readonly List<Domain_Change> _trail = new List<Domain_Change>();
    private readonly SortedDictionary<int, SortedDictionary<short, Indexed_Variable_Set>> _selection_buckets =
      new SortedDictionary<int, SortedDictionary<short, Indexed_Variable_Set>>();
    private readonly short[] _selection_priorities;

    private int[] _best_assignments;
    private int _best_unresolved;
    private int _max_progress;
    private int _zero_domain_unassigned;
    private int _search_node_count;
    private bool _search_budget_exhausted;
    private int _propagation_removal_count;
    private int _nogood_learned_count;
    private int _nogood_hit_count;
    private int _backjump_count;

    internal Search(
      Experimental_Tile_Model model,
      Map_State state,
      Cell[] cells,
      int depth,
      int search_node_limit,
      Search_Mode mode,
      Nogood_Cache nogoods,
      bool enable_conflict_learning,
      Random random,
      CancellationToken cancellation_token,
      IProgress<int> progress)
    {
      this._model = model;
      this._state = state;
      this._depth = depth;
      this._search_node_limit = search_node_limit;
      this._mode = mode;
      this._nogoods = nogoods ?? throw new ArgumentNullException(nameof(nogoods));
      this._enable_conflict_learning = enable_conflict_learning;
      this._random = random;
      this._cancellation_token = cancellation_token;
      this._progress = progress;

      this._cells = cells ?? throw new ArgumentNullException(nameof(cells));
      for (int variable = 0; variable < this._cells.Length; ++variable)
      {
        Cell cell = this._cells[variable];
        this._variable_at.Add(cell.X + cell.Y * state.Width, variable);
      }
      this._domains = new ulong[this._cells.Length][];
      this._domain_counts = new int[this._cells.Length];
      this._assignments = Enumerable.Repeat(Unassigned, this._cells.Length).ToArray();
      this._candidate_usage_counts = new int[this._model.Candidate_Count];
      this._selection_priorities = new short[this._cells.Length];
      this._best_unresolved = this._cells.Length + 1;

      this.initialize_domains();
      this.initialize_selection_buckets();
    }

    internal Search_Result solve()
    {
      if (this._cells.Length == 0)
        return new Search_Result(this._cells, Array.Empty<int>(), false, 0, 0, 0, 0, 0);

      this.build_greedy_incumbent();
      if (this._best_unresolved > 0 && this._mode == Search_Mode.Complete)
        this.try_propagated_greedy_complete();
      if (this._best_unresolved > 0 && this._search_node_limit > 0)
      {
        if (this._mode == Search_Mode.Complete)
          this.search_complete_conflict();
        else
          this.search(0, 0);
      }
      else if (this._best_unresolved > 0 && this._search_node_limit == 0)
        this._search_budget_exhausted = true;
      if (this._best_assignments == null)
        this._best_assignments = Enumerable.Repeat(Unresolved, this._cells.Length).ToArray();
      return new Search_Result(
        this._cells,
        this._best_assignments,
        this._search_budget_exhausted,
        this._search_node_count,
        this._propagation_removal_count,
        this._nogood_learned_count,
        this._nogood_hit_count,
        this._backjump_count);
    }

    private void initialize_domains()
    {
      for (int variable = 0; variable < this._cells.Length; ++variable)
      {
        Cell cell = this._cells[variable];
        ulong[] domain = new ulong[this._model.Word_Count];
        for (int candidate = 0; candidate < this._model.Candidate_Count; ++candidate)
        {
          if (!this._model.terrain_allows_candidate(candidate, this._state.Terrain[cell.X, cell.Y]))
            continue;
          domain[candidate / 64] |= 1UL << (candidate % 64);
          ++this._domain_counts[variable];
        }
        this._domains[variable] = domain;

        foreach (byte direction in Directions)
        {
          Cell neighbor = neighbor_cell(cell, direction);
          if (this._state.is_off_map(neighbor.X, neighbor.Y)
            || !this._state.Drawn[neighbor.X, neighbor.Y]
            || !this._model.try_candidate(this._state.Tiles[neighbor.X, neighbor.Y], out int fixed_candidate))
            continue;
          this.intersect_domain(variable, this._model.allowed_neighbors(fixed_candidate, (byte) (10 - direction)), false);
        }
      }
    }

    private void try_propagated_greedy_complete()
    {
        int root_mark = this._trail.Count;
        List<Greedy_Decision> decisions = new List<Greedy_Decision>();
        bool complete = this.propagate_all_arcs();
        while (complete)
        {
          this._cancellation_token.ThrowIfCancellationRequested();
          int variable = this.select_variable();
          if (variable < 0)
            break;
          bool assigned = false;
          foreach (int candidate in this.ordered_candidates(variable))
          {
            int trail_mark = this._trail.Count;
            this.assign_variable(variable, candidate);
            this.restrict_domain_to_candidate(variable, candidate);
            if (this.propagate_from(variable))
            {
              decisions.Add(new Greedy_Decision(variable, candidate, trail_mark));
              assigned = true;
              break;
            }
            this.undo_to(trail_mark);
            this.unassign_variable(variable);
          }
          if (!assigned)
            complete = false;
        }
        if (complete && this.select_variable() < 0)
        {
          this._best_unresolved = 0;
          this._best_assignments = (int[]) this._assignments.Clone();
        }
        for (int index = decisions.Count - 1; index >= 0; --index)
        {
          Greedy_Decision decision = decisions[index];
          this.undo_to(decision.Trail_Mark);
          this.unassign_variable(decision.Variable);
        }
        this.undo_to(root_mark);
    }

    private bool propagate_all_arcs()
    {
        Queue<Arc> queue = new Queue<Arc>();
        for (int variable = 0; variable < this._cells.Length; ++variable)
        {
          Cell cell = this._cells[variable];
          foreach (byte direction in Directions)
          {
            Cell neighbor = neighbor_cell(cell, direction);
            if (this._state.is_off_map(neighbor.X, neighbor.Y))
              continue;
            int target = this.variable_at(neighbor.X, neighbor.Y);
            if (target >= 0)
              queue.Enqueue(new Arc(variable, target, direction));
          }
        }
        return this.propagate(queue);
    }

    private bool propagate_from(int variable)
    {
        Queue<Arc> queue = new Queue<Arc>();
        Cell cell = this._cells[variable];
        foreach (byte direction in Directions)
        {
          Cell neighbor = neighbor_cell(cell, direction);
          if (this._state.is_off_map(neighbor.X, neighbor.Y))
            continue;
          int target = this.variable_at(neighbor.X, neighbor.Y);
          if (target < 0 || this._assignments[target] == Unresolved)
            continue;
          queue.Enqueue(new Arc(target, variable, (byte) (10 - direction)));
          queue.Enqueue(new Arc(variable, target, direction));
        }
        return this.propagate(queue);
    }

    private bool propagate(Queue<Arc> queue)
    {
        while (queue.Count > 0)
        {
          this._cancellation_token.ThrowIfCancellationRequested();
          Arc arc = queue.Dequeue();
          if (this._assignments[arc.Source] == Unresolved
            || this._assignments[arc.Target] == Unresolved)
            continue;
          if (!this.revise(arc.Source, arc.Target, arc.Direction))
            continue;
          if (this._domain_counts[arc.Source] == 0)
            return false;
          Cell source_cell = this._cells[arc.Source];
          foreach (byte direction in Directions)
          {
            Cell neighbor = neighbor_cell(source_cell, direction);
            if (this._state.is_off_map(neighbor.X, neighbor.Y))
              continue;
            int neighbor_variable = this.variable_at(neighbor.X, neighbor.Y);
            if (neighbor_variable < 0
              || neighbor_variable == arc.Target
              || this._assignments[neighbor_variable] == Unresolved)
              continue;
            queue.Enqueue(new Arc(neighbor_variable, arc.Source, (byte) (10 - direction)));
          }
        }
        return true;
    }

    private bool revise(int source, int target, byte direction)
    {
        ulong[] supported = (ulong[]) this._domains[source].Clone();
        bool changed = false;
        foreach (int candidate in enumerate_candidates(this._domains[source]))
        {
          if (this._model.count_intersection(
            this._domains[target],
            this._model.allowed_neighbors(candidate, direction)) > 0)
            continue;
          supported[candidate / 64] &= ~(1UL << (candidate % 64));
          changed = true;
        }
        if (!changed)
          return false;
        int old_count = this._domain_counts[source];
        this.intersect_domain(source, supported, true);
        this._propagation_removal_count += old_count - this._domain_counts[source];
        return true;
    }

    private void restrict_domain_to_candidate(int variable, int candidate)
    {
      ulong[] singleton = new ulong[this._model.Word_Count];
      singleton[candidate / 64] = 1UL << (candidate % 64);
      this.intersect_domain(variable, singleton, true);
    }

    private void search(int assigned_count, int unresolved_count)
    {
      this._cancellation_token.ThrowIfCancellationRequested();
      if (this._search_budget_exhausted)
        return;
      if (this._search_node_count >= this._search_node_limit)
      {
        this._search_budget_exhausted = true;
        return;
      }
      ++this._search_node_count;
      if (this._best_unresolved == 0 || unresolved_count >= this._best_unresolved)
        return;

      if (unresolved_count + this._zero_domain_unassigned >= this._best_unresolved)
        return;

      int variable_to_assign = this.select_variable();
      if (variable_to_assign < 0)
      {
        this._best_unresolved = unresolved_count;
        this._best_assignments = (int[]) this._assignments.Clone();
        return;
      }

      this.report_progress(assigned_count + 1);
      foreach (int candidate in this.ordered_candidates(variable_to_assign))
      {
        int trail_mark = this._trail.Count;
        this.assign_variable(variable_to_assign, candidate);
        this.constrain_neighbors(variable_to_assign, candidate);
        this.search(assigned_count + 1, unresolved_count);
        this.undo_to(trail_mark);
        this.unassign_variable(variable_to_assign);
        if (this._best_unresolved == 0 || this._search_budget_exhausted)
          return;
      }

      if (unresolved_count + 1 < this._best_unresolved)
      {
        this.assign_variable(variable_to_assign, Unresolved);
        this.search(assigned_count + 1, unresolved_count + 1);
        this.unassign_variable(variable_to_assign);
      }
    }

    private void search_complete_conflict()
    {
      Complete_Outcome outcome = this.complete_search();
      if (!outcome.Success)
        return;
      this._best_unresolved = 0;
      this._best_assignments = outcome.Solution;
    }

    private Complete_Outcome complete_search()
    {
      this._cancellation_token.ThrowIfCancellationRequested();
      if (this._search_node_count >= this._search_node_limit)
      {
        this._search_budget_exhausted = true;
        return Complete_Outcome.budget();
      }
      ++this._search_node_count;

      string key = this.assignment_key();
      if (this._enable_conflict_learning && this._nogoods.try_get(key, out int[] cached_conflict))
      {
        ++this._nogood_hit_count;
        return Complete_Outcome.failure(cached_conflict);
      }

      int variable = this.select_complete_variable(out List<int> candidates);
      if (variable < 0)
        return Complete_Outcome.success((int[]) this._assignments.Clone());
      if (candidates.Count == 0)
      {
        HashSet<int> conflict = this.assigned_neighbor_conflict(variable);
        this.learn_nogood(key, conflict);
        return Complete_Outcome.failure(conflict);
      }

      HashSet<int> accumulated_conflict = this.assigned_neighbor_conflict(variable);
      foreach (int candidate in this.ordered_complete_candidates(variable, candidates))
      {
        this._assignments[variable] = candidate;
        ++this._candidate_usage_counts[candidate];
        Complete_Outcome child = this.complete_search();
        --this._candidate_usage_counts[candidate];
        this._assignments[variable] = Unassigned;
        if (child.Success || child.Budget_Exhausted)
          return child;
        if (this._enable_conflict_learning && !child.Conflict.Contains(variable))
        {
          ++this._backjump_count;
          return child;
        }
        foreach (int conflict_variable in child.Conflict)
        {
          if (conflict_variable != variable)
            accumulated_conflict.Add(conflict_variable);
        }
      }

      this.learn_nogood(key, accumulated_conflict);
      return Complete_Outcome.failure(accumulated_conflict);
    }

    private int select_complete_variable(out List<int> selected_candidates)
    {
      int minimum_count = int.MaxValue;
      short maximum_priority = short.MinValue;
      List<int> variables = new List<int>();
      selected_candidates = null;
      for (int variable = 0; variable < this._cells.Length; ++variable)
      {
        if (this._assignments[variable] != Unassigned)
          continue;
        List<int> candidates = this.complete_candidates(variable);
        int count = candidates.Count;
        short priority = this.neighbor_priority(variable);
        if (count < minimum_count || count == minimum_count && priority > maximum_priority)
        {
          minimum_count = count;
          maximum_priority = priority;
          variables.Clear();
          variables.Add(variable);
        }
        else if (count == minimum_count && priority == maximum_priority)
          variables.Add(variable);
      }
      if (variables.Count == 0)
      {
        selected_candidates = new List<int>();
        return -1;
      }
      int selected = variables[this._random.Next(variables.Count)];
      selected_candidates = this.complete_candidates(selected);
      return selected;
    }

    private List<int> complete_candidates(int variable)
    {
      List<int> candidates = new List<int>();
      Cell cell = this._cells[variable];
      foreach (int candidate in enumerate_candidates(this._domains[variable]))
      {
        bool valid = true;
        foreach (byte direction in Directions)
        {
          Cell neighbor = neighbor_cell(cell, direction);
          if (this._state.is_off_map(neighbor.X, neighbor.Y))
            continue;
          int neighbor_variable = this.variable_at(neighbor.X, neighbor.Y);
          if (neighbor_variable < 0)
            continue;
          int neighbor_assignment = this._assignments[neighbor_variable];
          if (neighbor_assignment >= 0 && !this._model.allows(candidate, direction, neighbor_assignment))
          {
            valid = false;
            break;
          }
        }
        if (valid)
          candidates.Add(candidate);
      }
      return candidates;
    }

    private List<int> ordered_complete_candidates(int variable, IEnumerable<int> candidates)
    {
      List<Weighted_Candidate> weighted = new List<Weighted_Candidate>();
      foreach (int candidate in candidates)
      {
        int support = this.complete_candidate_support(variable, candidate);
        double weight = this.selection_weight(variable, candidate, support);
        double sample = Math.Max(double.Epsilon, this._random.NextDouble());
        weighted.Add(new Weighted_Candidate(candidate, support, -Math.Log(sample) / weight));
      }
      return weighted
        .OrderBy(candidate => candidate.Key)
        .ThenBy(candidate => candidate.Candidate)
        .Select(candidate => candidate.Candidate)
        .ToList();
    }

    private int complete_candidate_support(int variable, int candidate)
    {
      int support = 0;
      Cell cell = this._cells[variable];
      foreach (byte direction in Directions)
      {
        Cell neighbor = neighbor_cell(cell, direction);
        if (this._state.is_off_map(neighbor.X, neighbor.Y))
          continue;
        int neighbor_variable = this.variable_at(neighbor.X, neighbor.Y);
        if (neighbor_variable < 0 || this._assignments[neighbor_variable] != Unassigned)
          continue;
        foreach (int neighbor_candidate in this.complete_candidates(neighbor_variable))
        {
          if (this._model.allows(candidate, direction, neighbor_candidate))
            ++support;
        }
      }
      return support;
    }

    private HashSet<int> assigned_neighbor_conflict(int variable)
    {
      HashSet<int> conflict = new HashSet<int>();
      Cell cell = this._cells[variable];
      foreach (byte direction in Directions)
      {
        Cell neighbor = neighbor_cell(cell, direction);
        if (this._state.is_off_map(neighbor.X, neighbor.Y))
          continue;
        int neighbor_variable = this.variable_at(neighbor.X, neighbor.Y);
        if (neighbor_variable >= 0 && this._assignments[neighbor_variable] >= 0)
          conflict.Add(neighbor_variable);
      }
      return conflict;
    }

    private string assignment_key()
    {
      return string.Join(
        ";",
        Enumerable.Range(0, this._assignments.Length)
          .Where(variable => this._assignments[variable] >= 0)
          .Select(variable => $"{variable}={this._assignments[variable]}"));
    }

    private void learn_nogood(string key, IEnumerable<int> conflict)
    {
      if (!this._enable_conflict_learning)
        return;
      if (this._nogoods.add(key, conflict))
        ++this._nogood_learned_count;
    }

    private void build_greedy_incumbent()
    {
      List<Greedy_Decision> decisions = new List<Greedy_Decision>();
      int unresolved_count = 0;
      while (true)
      {
        this._cancellation_token.ThrowIfCancellationRequested();
        int variable = this.select_variable();
        if (variable < 0)
          break;
        int trail_mark = this._trail.Count;
        int assignment;
        List<int> candidates = this.ordered_candidates(variable);
        if (candidates.Count == 0)
        {
          assignment = Unresolved;
          ++unresolved_count;
        }
        else
          assignment = candidates[0];

        this.assign_variable(variable, assignment);
        if (assignment >= 0)
          this.constrain_neighbors(variable, assignment);
        decisions.Add(new Greedy_Decision(variable, assignment, trail_mark));
        this.report_progress(decisions.Count);
      }

      this._best_unresolved = unresolved_count;
      this._best_assignments = (int[]) this._assignments.Clone();

      for (int index = decisions.Count - 1; index >= 0; --index)
      {
        Greedy_Decision decision = decisions[index];
        if (decision.Assignment >= 0)
          this.undo_to(decision.Trail_Mark);
        this.unassign_variable(decision.Variable);
      }
    }

    private int select_variable()
    {
      if (this._selection_buckets.Count == 0)
        return -1;
      SortedDictionary<short, Indexed_Variable_Set> priorities = this._selection_buckets.First().Value;
      Indexed_Variable_Set candidates = priorities[priorities.Keys.Max()];
      return candidates[this._random.Next(candidates.Count)];
    }

    private short neighbor_priority(int variable)
    {
      Cell cell = this._cells[variable];
      short result = short.MinValue;
      foreach (byte direction in Directions)
      {
        Cell neighbor = neighbor_cell(cell, direction);
        if (this._state.is_off_map(neighbor.X, neighbor.Y))
          continue;
        int neighbor_variable = this.variable_at(neighbor.X, neighbor.Y);
        if (neighbor_variable >= 0)
        {
          int assignment = this._assignments[neighbor_variable];
          if (assignment >= 0)
            result = Math.Max(result, this._model.priority(assignment));
        }
        else if (this._state.Drawn[neighbor.X, neighbor.Y]
          && this._model.try_candidate(this._state.Tiles[neighbor.X, neighbor.Y], out int fixed_candidate))
        {
          result = Math.Max(result, this._model.priority(fixed_candidate));
        }
      }
      return result;
    }

    private List<int> ordered_candidates(int variable)
    {
      List<int> candidates = enumerate_candidates(this._domains[variable]);
      List<Weighted_Candidate> weighted = new List<Weighted_Candidate>(candidates.Count);
      foreach (int candidate in candidates)
      {
        int support = this.partial_candidate_support(variable, candidate);
        double weight = this.selection_weight(variable, candidate, support);
        double sample = Math.Max(double.Epsilon, this._random.NextDouble());
        weighted.Add(new Weighted_Candidate(candidate, support, -Math.Log(sample) / weight));
      }
      return weighted
        .OrderBy(candidate => candidate.Key)
        .ThenBy(candidate => candidate.Candidate)
        .Select(candidate => candidate.Candidate)
        .ToList();
    }

    private int partial_candidate_support(int variable, int candidate)
    {
      int support = 0;
      Cell cell = this._cells[variable];
      foreach (byte direction in Directions)
      {
        Cell neighbor = neighbor_cell(cell, direction);
        if (this._state.is_off_map(neighbor.X, neighbor.Y))
          continue;
        int neighbor_variable = this.variable_at(neighbor.X, neighbor.Y);
        if (neighbor_variable < 0 || this._assignments[neighbor_variable] != Unassigned)
          continue;
        support += this._model.count_intersection(
          this._domains[neighbor_variable],
          this._model.allowed_neighbors(candidate, direction));
      }
      return support;
    }

    private long candidate_weight(int variable, int candidate)
    {
      Cell cell = this._cells[variable];
      long weight = 1;
      foreach (byte direction in Directions)
      {
        Cell neighbor = neighbor_cell(cell, direction);
        if (this._state.is_off_map(neighbor.X, neighbor.Y))
          continue;
        int neighbor_variable = this.variable_at(neighbor.X, neighbor.Y);
        if (neighbor_variable >= 0)
        {
          int neighbor_assignment = this._assignments[neighbor_variable];
          if (neighbor_assignment >= 0)
          {
            weight += this._model.weight(neighbor_assignment, (byte) (10 - direction), candidate);
          }
          else if (neighbor_assignment == Unassigned && this._depth == 2)
          {
            weight += this._model.count_intersection(
              this._domains[neighbor_variable],
              this._model.allowed_neighbors(candidate, direction));
          }
        }
        else if (this._state.Drawn[neighbor.X, neighbor.Y]
          && this._model.try_candidate(this._state.Tiles[neighbor.X, neighbor.Y], out int fixed_candidate))
        {
          weight += this._model.weight(fixed_candidate, (byte) (10 - direction), candidate);
        }
      }
      return Math.Max(1, weight);
    }

    private double selection_weight(int variable, int candidate, int support)
    {
      double learned = 1.0 + Math.Log(1.0 + this.candidate_weight(variable, candidate));
      double flexibility = 1.0 + Math.Log(1.0 + support);
      int local_repetitions = this.local_repetition_count(variable, candidate);
      double global_penalty = 1.0 + this._candidate_usage_counts[candidate];
      double local_penalty = 1.0 + local_repetitions * 4.0;
      return Math.Max(double.Epsilon, learned * flexibility / (global_penalty * local_penalty));
    }

    private int local_repetition_count(int variable, int candidate)
    {
      int repetitions = 0;
      Cell cell = this._cells[variable];
      for (int dy = -2; dy <= 2; ++dy)
      {
        int max_dx = 2 - Math.Abs(dy);
        for (int dx = -max_dx; dx <= max_dx; ++dx)
        {
          if (dx == 0 && dy == 0)
            continue;
          int x = cell.X + dx;
          int y = cell.Y + dy;
          if (this._state.is_off_map(x, y))
            continue;
          int neighbor_variable = this.variable_at(x, y);
          if (neighbor_variable >= 0)
          {
            if (this._assignments[neighbor_variable] == candidate)
              ++repetitions;
          }
          else if (this._state.Drawn[x, y]
            && this._model.try_candidate(this._state.Tiles[x, y], out int fixed_candidate)
            && fixed_candidate == candidate)
          {
            ++repetitions;
          }
        }
      }
      return repetitions;
    }

    private void constrain_neighbors(int variable, int candidate)
    {
      Cell cell = this._cells[variable];
      foreach (byte direction in Directions)
      {
        Cell neighbor = neighbor_cell(cell, direction);
        if (this._state.is_off_map(neighbor.X, neighbor.Y))
          continue;
        int neighbor_variable = this.variable_at(neighbor.X, neighbor.Y);
        if (neighbor_variable < 0 || this._assignments[neighbor_variable] != Unassigned)
          continue;
        this.intersect_domain(neighbor_variable, this._model.allowed_neighbors(candidate, direction), true);
      }
    }

    private void intersect_domain(int variable, ulong[] allowed, bool record_changes)
    {
      int old_count = this._domain_counts[variable];
      bool removed_any = false;
      for (int word = 0; word < this._domains[variable].Length; ++word)
      {
        ulong removed = this._domains[variable][word] & ~allowed[word];
        if (removed == 0)
          continue;
        if (!removed_any && record_changes && this._assignments[variable] == Unassigned)
          this.remove_selection(variable);
        removed_any = true;
        this._domains[variable][word] &= allowed[word];
        this._domain_counts[variable] -= BitOperations.PopCount(removed);
        if (record_changes)
          this._trail.Add(new Domain_Change(variable, word, removed));
      }
      if (!removed_any || !record_changes || this._assignments[variable] != Unassigned)
        return;
      if (old_count > 0 && this._domain_counts[variable] == 0)
        ++this._zero_domain_unassigned;
      this.add_selection(variable);
    }

    private void undo_to(int trail_mark)
    {
      for (int index = this._trail.Count - 1; index >= trail_mark; --index)
      {
        Domain_Change change = this._trail[index];
        int old_count = this._domain_counts[change.Variable];
        if (this._assignments[change.Variable] == Unassigned)
          this.remove_selection(change.Variable);
        this._domains[change.Variable][change.Word] |= change.Removed;
        this._domain_counts[change.Variable] += BitOperations.PopCount(change.Removed);
        if (this._assignments[change.Variable] == Unassigned)
        {
          if (old_count == 0 && this._domain_counts[change.Variable] > 0)
            --this._zero_domain_unassigned;
          this.add_selection(change.Variable);
        }
      }
      if (this._trail.Count > trail_mark)
        this._trail.RemoveRange(trail_mark, this._trail.Count - trail_mark);
    }

    private void report_progress(int value)
    {
      if (value <= this._max_progress)
        return;
      this._max_progress = value;
      this._progress?.Report(value);
    }

    private void initialize_selection_buckets()
    {
      for (int variable = 0; variable < this._cells.Length; ++variable)
      {
        this._selection_priorities[variable] = this.neighbor_priority(variable);
        this.add_selection(variable);
        if (this._domain_counts[variable] == 0)
          ++this._zero_domain_unassigned;
      }
    }

    private void assign_variable(int variable, int assignment)
    {
      this.remove_selection(variable);
      if (this._domain_counts[variable] == 0)
        --this._zero_domain_unassigned;
      this._assignments[variable] = assignment;
      if (assignment >= 0)
      {
        ++this._candidate_usage_counts[assignment];
        this.refresh_neighbor_priorities(variable);
      }
    }

    private void unassign_variable(int variable)
    {
      int old_assignment = this._assignments[variable];
      this._assignments[variable] = Unassigned;
      this._selection_priorities[variable] = this.neighbor_priority(variable);
      this.add_selection(variable);
      if (this._domain_counts[variable] == 0)
        ++this._zero_domain_unassigned;
      if (old_assignment >= 0)
      {
        --this._candidate_usage_counts[old_assignment];
        this.refresh_neighbor_priorities(variable);
      }
    }

    private void refresh_neighbor_priorities(int variable)
    {
      Cell cell = this._cells[variable];
      foreach (byte direction in Directions)
      {
        Cell neighbor = neighbor_cell(cell, direction);
        if (this._state.is_off_map(neighbor.X, neighbor.Y))
          continue;
        int neighbor_variable = this.variable_at(neighbor.X, neighbor.Y);
        if (neighbor_variable < 0 || this._assignments[neighbor_variable] != Unassigned)
          continue;
        this.remove_selection(neighbor_variable);
        this._selection_priorities[neighbor_variable] = this.neighbor_priority(neighbor_variable);
        this.add_selection(neighbor_variable);
      }
    }

    private void add_selection(int variable)
    {
      int count = this._domain_counts[variable];
      short priority = this._selection_priorities[variable];
      if (!this._selection_buckets.TryGetValue(count, out SortedDictionary<short, Indexed_Variable_Set> priorities))
      {
        priorities = new SortedDictionary<short, Indexed_Variable_Set>();
        this._selection_buckets.Add(count, priorities);
      }
      if (!priorities.TryGetValue(priority, out Indexed_Variable_Set variables))
      {
        variables = new Indexed_Variable_Set(this._cells.Length);
        priorities.Add(priority, variables);
      }
      variables.Add(variable);
    }

    private void remove_selection(int variable)
    {
      int count = this._domain_counts[variable];
      short priority = this._selection_priorities[variable];
      SortedDictionary<short, Indexed_Variable_Set> priorities = this._selection_buckets[count];
      Indexed_Variable_Set variables = priorities[priority];
      variables.Remove(variable);
      if (variables.Count == 0)
        priorities.Remove(priority);
      if (priorities.Count == 0)
        this._selection_buckets.Remove(count);
    }

    private static List<int> enumerate_candidates(ulong[] domain)
    {
      List<int> candidates = new List<int>();
      for (int word = 0; word < domain.Length; ++word)
      {
        ulong remaining = domain[word];
        while (remaining != 0)
        {
          int bit = BitOperations.TrailingZeroCount(remaining);
          candidates.Add(word * 64 + bit);
          remaining &= remaining - 1;
        }
      }
      return candidates;
    }

    private int variable_at(int x, int y)
    {
      return this._variable_at.TryGetValue(x + y * this._state.Width, out int variable)
        ? variable
        : -1;
    }

    private readonly struct Domain_Change
    {
      internal int Variable { get; }
      internal int Word { get; }
      internal ulong Removed { get; }

      internal Domain_Change(int variable, int word, ulong removed)
      {
        this.Variable = variable;
        this.Word = word;
        this.Removed = removed;
      }
    }

    private readonly struct Arc
    {
      internal int Source { get; }
      internal int Target { get; }
      internal byte Direction { get; }

      internal Arc(int source, int target, byte direction)
      {
        this.Source = source;
        this.Target = target;
        this.Direction = direction;
      }
    }

    private readonly struct Greedy_Decision
    {
      internal int Variable { get; }
      internal int Assignment { get; }
      internal int Trail_Mark { get; }

      internal Greedy_Decision(int variable, int assignment, int trail_mark)
      {
        this.Variable = variable;
        this.Assignment = assignment;
        this.Trail_Mark = trail_mark;
      }
    }

    private readonly struct Weighted_Candidate
    {
      internal int Candidate { get; }
      internal int Support { get; }
      internal double Key { get; }

      internal Weighted_Candidate(int candidate, int support, double key)
      {
        this.Candidate = candidate;
        this.Support = support;
        this.Key = key;
      }
    }

    private readonly struct Complete_Outcome
    {
      internal bool Success { get; }
      internal bool Budget_Exhausted { get; }
      internal IReadOnlyCollection<int> Conflict { get; }
      internal int[] Solution { get; }

      private Complete_Outcome(
        bool success,
        bool budget_exhausted,
        IReadOnlyCollection<int> conflict,
        int[] solution)
      {
        this.Success = success;
        this.Budget_Exhausted = budget_exhausted;
        this.Conflict = conflict;
        this.Solution = solution;
      }

      internal static Complete_Outcome success(int[] solution)
      {
        return new Complete_Outcome(true, false, Array.Empty<int>(), solution);
      }

      internal static Complete_Outcome budget()
      {
        return new Complete_Outcome(false, true, Array.Empty<int>(), null);
      }

      internal static Complete_Outcome failure(IEnumerable<int> conflict)
      {
        return new Complete_Outcome(false, false, conflict.Distinct().ToArray(), null);
      }
    }

    private sealed class Indexed_Variable_Set
    {
      private readonly bool[] _active;
      private readonly int[] _tree;

      internal int Count { get; private set; }

      internal int this[int index] => this.select(index);

      internal Indexed_Variable_Set(int capacity)
      {
        this._active = new bool[capacity];
        this._tree = new int[capacity + 1];
      }

      internal void Add(int variable)
      {
        if (this._active[variable])
          return;
        this._active[variable] = true;
        ++this.Count;
        this.update(variable, 1);
      }

      internal void Remove(int variable)
      {
        if (!this._active[variable])
          return;
        this._active[variable] = false;
        --this.Count;
        this.update(variable, -1);
      }

      private void update(int variable, int delta)
      {
        for (int index = variable + 1; index < this._tree.Length; index += index & -index)
          this._tree[index] += delta;
      }

      private int select(int rank)
      {
        int target = rank + 1;
        int index = 0;
        int bit = 1;
        while (bit <= (this._tree.Length - 1) / 2)
          bit <<= 1;
        for (; bit != 0; bit >>= 1)
        {
          int next = index + bit;
          if (next < this._tree.Length && this._tree[next] < target)
          {
            index = next;
            target -= this._tree[next];
          }
        }
        return index;
      }
    }
  }

  private sealed class Search_Result
  {
    internal Cell[] Cells { get; }
    internal int[] Assignments { get; }
    internal bool Search_Budget_Exhausted { get; }
    internal int Search_Node_Count { get; }
    internal int Propagation_Removal_Count { get; }
    internal int Nogood_Learned_Count { get; }
    internal int Nogood_Hit_Count { get; }
    internal int Backjump_Count { get; }

    internal Search_Result(
      Cell[] cells,
      int[] assignments,
      bool search_budget_exhausted,
      int search_node_count,
      int propagation_removal_count,
      int nogood_learned_count,
      int nogood_hit_count,
      int backjump_count)
    {
      this.Cells = cells;
      this.Assignments = assignments;
      this.Search_Budget_Exhausted = search_budget_exhausted;
      this.Search_Node_Count = search_node_count;
      this.Propagation_Removal_Count = propagation_removal_count;
      this.Nogood_Learned_Count = nogood_learned_count;
      this.Nogood_Hit_Count = nogood_hit_count;
      this.Backjump_Count = backjump_count;
    }
  }

  private sealed class Component_Progress : IProgress<int>
  {
    private readonly IProgress<int> _progress;
    private readonly int _offset;

    internal Component_Progress(IProgress<int> progress, int offset)
    {
      this._progress = progress;
      this._offset = offset;
    }

    public void Report(int value)
    {
      this._progress.Report(this._offset + value);
    }
  }

  private static Cell neighbor_cell(Cell cell, byte direction)
  {
    return direction switch
    {
      2 => new Cell(cell.X, cell.Y + 1),
      4 => new Cell(cell.X - 1, cell.Y),
      6 => new Cell(cell.X + 1, cell.Y),
      8 => new Cell(cell.X, cell.Y - 1),
      _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Direction must be 2, 4, 6, or 8."),
    };
  }
}
