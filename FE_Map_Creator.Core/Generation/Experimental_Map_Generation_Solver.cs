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
    Random random,
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
      random,
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
    Random random,
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
      random,
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
    Random random,
    int seed,
    CancellationToken cancellation_token,
    Tile_Drawn_Callback tile_drawn,
    IProgress<int> progress)
  {
    Search search = new Search(
      this._model,
      working,
      depth,
      search_node_limit,
      random,
      cancellation_token,
      progress);
    Search_Result search_result = search.solve();
    List<Cell> unresolved = new List<Cell>();

    for (int variable = 0; variable < search_result.Cells.Length; ++variable)
    {
      Cell cell = search_result.Cells[variable];
      int candidate = search_result.Assignments[variable];
      if (candidate < 0)
      {
        working.Tiles[cell.X, cell.Y] = 0;
        working.Drawn[cell.X, cell.Y] = false;
        unresolved.Add(cell);
      }
      else
      {
        working.Tiles[cell.X, cell.Y] = this._model.pick_alias(candidate, working.Terrain[cell.X, cell.Y], random);
        working.Drawn[cell.X, cell.Y] = true;
      }
    }

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
      search_result.Search_Budget_Exhausted,
      search_result.Search_Node_Count);
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

  private sealed class Search
  {
    private const int Unassigned = -2;
    private const int Unresolved = -1;

    private readonly Experimental_Tile_Model _model;
    private readonly Map_State _state;
    private readonly int _depth;
    private readonly int _search_node_limit;
    private readonly Random _random;
    private readonly CancellationToken _cancellation_token;
    private readonly IProgress<int> _progress;
    private readonly Cell[] _cells;
    private readonly int[,] _variable_at;
    private readonly ulong[][] _domains;
    private readonly int[] _domain_counts;
    private readonly int[] _assignments;
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

    internal Search(
      Experimental_Tile_Model model,
      Map_State state,
      int depth,
      int search_node_limit,
      Random random,
      CancellationToken cancellation_token,
      IProgress<int> progress)
    {
      this._model = model;
      this._state = state;
      this._depth = depth;
      this._search_node_limit = search_node_limit;
      this._random = random;
      this._cancellation_token = cancellation_token;
      this._progress = progress;

      List<Cell> cells = new List<Cell>();
      this._variable_at = new int[state.Width, state.Height];
      for (int y = 0; y < state.Height; ++y)
      {
        for (int x = 0; x < state.Width; ++x)
          this._variable_at[x, y] = -1;
      }
      for (int y = 0; y < state.Height; ++y)
      {
        for (int x = 0; x < state.Width; ++x)
        {
          if (state.Drawn[x, y] || state.Locked[x, y])
            continue;
          this._variable_at[x, y] = cells.Count;
          cells.Add(new Cell(x, y));
        }
      }
      this._cells = cells.ToArray();
      this._domains = new ulong[this._cells.Length][];
      this._domain_counts = new int[this._cells.Length];
      this._assignments = Enumerable.Repeat(Unassigned, this._cells.Length).ToArray();
      this._selection_priorities = new short[this._cells.Length];
      this._best_unresolved = this._cells.Length + 1;

      this.initialize_domains();
      this.initialize_selection_buckets();
    }

    internal Search_Result solve()
    {
      if (this._cells.Length == 0)
        return new Search_Result(this._cells, Array.Empty<int>(), false, 0);

      this.build_greedy_incumbent();
      if (this._best_unresolved > 0)
        this.search(0, 0);
      if (this._best_assignments == null)
        this._best_assignments = Enumerable.Repeat(Unresolved, this._cells.Length).ToArray();
      return new Search_Result(
        this._cells,
        this._best_assignments,
        this._search_budget_exhausted,
        this._search_node_count);
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
        int neighbor_variable = this._variable_at[neighbor.X, neighbor.Y];
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
        long weight = this.candidate_weight(variable, candidate);
        double sample = Math.Max(double.Epsilon, this._random.NextDouble());
        weighted.Add(new Weighted_Candidate(candidate, -Math.Log(sample) / weight));
      }
      return weighted
        .OrderBy(candidate => candidate.Key)
        .ThenBy(candidate => candidate.Candidate)
        .Select(candidate => candidate.Candidate)
        .ToList();
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
        int neighbor_variable = this._variable_at[neighbor.X, neighbor.Y];
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

    private void constrain_neighbors(int variable, int candidate)
    {
      Cell cell = this._cells[variable];
      foreach (byte direction in Directions)
      {
        Cell neighbor = neighbor_cell(cell, direction);
        if (this._state.is_off_map(neighbor.X, neighbor.Y))
          continue;
        int neighbor_variable = this._variable_at[neighbor.X, neighbor.Y];
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
        this.refresh_neighbor_priorities(variable);
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
        this.refresh_neighbor_priorities(variable);
    }

    private void refresh_neighbor_priorities(int variable)
    {
      Cell cell = this._cells[variable];
      foreach (byte direction in Directions)
      {
        Cell neighbor = neighbor_cell(cell, direction);
        if (this._state.is_off_map(neighbor.X, neighbor.Y))
          continue;
        int neighbor_variable = this._variable_at[neighbor.X, neighbor.Y];
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
        variables = new Indexed_Variable_Set();
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
      internal double Key { get; }

      internal Weighted_Candidate(int candidate, double key)
      {
        this.Candidate = candidate;
        this.Key = key;
      }
    }

    private sealed class Indexed_Variable_Set
    {
      private readonly List<int> _variables = new List<int>();
      private readonly Dictionary<int, int> _indices = new Dictionary<int, int>();

      internal int Count => this._variables.Count;

      internal int this[int index] => this._variables[index];

      internal void Add(int variable)
      {
        if (this._indices.ContainsKey(variable))
          return;
        this._indices.Add(variable, this._variables.Count);
        this._variables.Add(variable);
      }

      internal void Remove(int variable)
      {
        if (!this._indices.Remove(variable, out int index))
          return;
        int last_index = this._variables.Count - 1;
        int last_variable = this._variables[last_index];
        this._variables[index] = last_variable;
        this._variables.RemoveAt(last_index);
        if (index < this._variables.Count)
          this._indices[last_variable] = index;
      }
    }
  }

  private sealed class Search_Result
  {
    internal Cell[] Cells { get; }
    internal int[] Assignments { get; }
    internal bool Search_Budget_Exhausted { get; }
    internal int Search_Node_Count { get; }

    internal Search_Result(
      Cell[] cells,
      int[] assignments,
      bool search_budget_exhausted,
      int search_node_count)
    {
      this.Cells = cells;
      this.Assignments = assignments;
      this.Search_Budget_Exhausted = search_budget_exhausted;
      this.Search_Node_Count = search_node_count;
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
