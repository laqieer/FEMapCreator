using FEXNA_Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

#nullable disable
namespace FE_Map_Creator.Generation;

/// <summary>
/// Cross-platform map generation/repair core, faithfully extracted from
/// FE_Map_Creator_Form's generate_map/repair_map and their private helpers so it can be
/// shared by the WinForms GUI and other callers (CLI, tests) without any UI dependency.
///
/// Directions use the numeric-keypad convention used throughout the codebase:
/// 2 = down, 4 = left, 6 = right, 8 = up; the opposite direction is 10 - dir.
/// </summary>
public sealed class Map_Generation_Engine
{
  // Coordinate offset to the already-drawn neighbor in the direction opposite `dir`,
  // matching FE_Map_Creator_Form.REVERSE_DIRS (originally keyed with System.Drawing.Size).
  private static readonly Dictionary<byte, Cell> Reverse_Dirs = new Dictionary<byte, Cell>()
  {
    { 2, new Cell(0, -1) },
    { 4, new Cell(1, 0) },
    { 6, new Cell(-1, 0) },
    { 8, new Cell(0, 1) },
  };

  private readonly Tileset_Generation_Data _tileset_generation_data;
  private readonly Data_Tileset _terrain_tileset;
  private readonly Dictionary<short, short[]> _identical_aliases_by_canonical;

  public Map_Generation_Engine(Tileset_Generation_Data tileset_generation_data, Data_Tileset terrain_tileset = null)
  {
    this._tileset_generation_data = tileset_generation_data ?? throw new ArgumentNullException(nameof(tileset_generation_data));
    this._terrain_tileset = terrain_tileset;
    this._identical_aliases_by_canonical = tileset_generation_data.identical_tiles
      .GroupBy(pair => pair.Value)
      .ToDictionary(group => group.Key, group => group.Select(pair => pair.Key).ToArray());
  }

  private Dictionary<int, Tile_Data> tileset_config_data => this._tileset_generation_data.generation_data;

  /// <summary>
  /// Fills every open cell reachable from the current drawn/locked frontier, seeding a
  /// random valid tile to start (or restart, for any remaining disconnected unlocked
  /// region) whenever the frontier runs dry, until every fillable cell is drawn or no
  /// valid seed exists anywhere on the map. Mutates <paramref name="state"/> in place.
  /// </summary>
  public Map_Generation_Result generate(
    Map_State state,
    Map_Generation_Options options,
    CancellationToken cancellation_token = default,
    Tile_Drawn_Callback tile_drawn = null,
    IProgress<int> progress = null)
  {
    if (state == null)
      throw new ArgumentNullException(nameof(state));
    if (options == null)
      throw new ArgumentNullException(nameof(options));
    validate_depth(options.Depth);

    Random random = create_random(options.Seed, out int seed);
    if (options.Algorithm == Map_Generation_Algorithm.Experimental_Constraint)
    {
      validate_search_node_limit(options.Experimental_Search_Node_Limit);
      validate_restart_count(options.Experimental_Restart_Count);
      validate_nogood_limit(options.Experimental_Nogood_Limit);
      return new Experimental_Map_Generation_Solver(this._tileset_generation_data, this._terrain_tileset)
        .generate(
          state,
          options.Depth,
          options.Experimental_Search_Node_Limit,
          options.Experimental_Restart_Count,
          options.Experimental_Nogood_Limit,
          options.Experimental_Enable_Conflict_Learning,
          seed,
          cancellation_token,
          tile_drawn,
          progress);
    }
    validate_algorithm(options.Algorithm);
    int unresolved = this.run_generation(state, options.Depth, random, cancellation_token, tile_drawn, progress);
    return new Map_Generation_Result(unresolved, seed);
  }

  /// <summary>
  /// Clears cells whose drawn tile is no longer terrain-compatible, reopens unlocked
  /// drawn cells within Manhattan distance <see cref="Map_Repair_Options.Radius"/> of
  /// every resulting hole (tile index 0), then regenerates with the requested depth.
  /// Mutates <paramref name="state"/> in place.
  /// </summary>
  public Map_Generation_Result repair(
    Map_State state,
    Map_Repair_Options options,
    CancellationToken cancellation_token = default,
    Tile_Drawn_Callback tile_drawn = null,
    IProgress<int> progress = null)
  {
    if (state == null)
      throw new ArgumentNullException(nameof(state));
    if (options == null)
      throw new ArgumentNullException(nameof(options));
    validate_depth(options.Depth);
    if (options.Radius < 0)
      throw new ArgumentOutOfRangeException(nameof(options), options.Radius, "Repair radius must be zero or greater.");

    Random random = create_random(options.Seed, out int seed);
    if (options.Algorithm == Map_Generation_Algorithm.Experimental_Constraint)
    {
      validate_search_node_limit(options.Experimental_Search_Node_Limit);
      validate_restart_count(options.Experimental_Restart_Count);
      validate_nogood_limit(options.Experimental_Nogood_Limit);
      return new Experimental_Map_Generation_Solver(this._tileset_generation_data, this._terrain_tileset)
        .repair(
          state,
          options.Depth,
          options.Radius,
          options.Experimental_Search_Node_Limit,
          options.Experimental_Restart_Count,
          options.Experimental_Nogood_Limit,
          options.Experimental_Enable_Conflict_Learning,
          seed,
          cancellation_token,
          tile_drawn,
          progress);
    }
    validate_algorithm(options.Algorithm);
    this.resolve_terrain_incompatible_cells(state);
    reopen_cells_around_holes(state, options.Radius, cancellation_token);

    int unresolved = this.run_generation(state, options.Depth, random, cancellation_token, tile_drawn, progress);
    return new Map_Generation_Result(unresolved, seed);
  }

  private static void validate_depth(int depth)
  {
    if (depth != 1 && depth != 2)
      throw new ArgumentException("Generation depth must be 1 or 2.", nameof(depth));
  }

  private static void validate_algorithm(Map_Generation_Algorithm algorithm)
  {
    if (algorithm != Map_Generation_Algorithm.Legacy)
      throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unknown map generation algorithm.");
  }

  private static void validate_search_node_limit(int search_node_limit)
  {
    if (search_node_limit <= 0)
      throw new ArgumentOutOfRangeException(nameof(search_node_limit), search_node_limit, "Experimental search node limit must be positive.");
  }

  private static void validate_restart_count(int restart_count)
  {
    if (restart_count <= 0)
      throw new ArgumentOutOfRangeException(nameof(restart_count), restart_count, "Experimental restart count must be positive.");
  }

  private static void validate_nogood_limit(int nogood_limit)
  {
    if (nogood_limit < 0)
      throw new ArgumentOutOfRangeException(nameof(nogood_limit), nogood_limit, "Experimental nogood limit must be zero or greater.");
  }

  private static Random create_random(int? seed, out int actual_seed)
  {
    actual_seed = seed ?? new Random().Next();
    return new Random(actual_seed);
  }

  // Faithful port of the pre-pass in repairMapToolStripMenuItem_Click: cells whose
  // Terrain tag no longer matches the drawn tile's tag are cleared to tile 0. Positive
  // terrain values require an exact tag match; negative values forbid the tag equal to
  // their absolute value.
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
        if (terrain == 0)
          continue;
        int tile = state.Tiles[x, y];
        if (this._terrain_tileset.Terrain_Tags.Count <= tile)
          continue;
        bool incompatible = terrain > 0
          ? this._terrain_tileset.Terrain_Tags[tile] != terrain
          : this._terrain_tileset.Terrain_Tags[tile] == -terrain;
        if (incompatible)
          state.Tiles[x, y] = 0;
      }
    }
  }

  // Faithful port of get_open_tiles_for_repair: every tile-0 cell is a repair hole, and
  // every unlocked drawn cell within Manhattan distance `radius` of a hole is reopened
  // (tile reset to 0, drawn cleared). Locked cells are always preserved.
  private static void reopen_cells_around_holes(Map_State state, int radius, CancellationToken cancellation_token)
  {
    List<Cell> holes = new List<Cell>();
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (state.Tiles[x, y] == 0)
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
          int nx = hole.X + dx;
          int ny = hole.Y + dy;
          if (!state.is_off_map(nx, ny) && state.Drawn[nx, ny] && !state.Locked[nx, ny])
          {
            state.Tiles[nx, ny] = 0;
            state.Drawn[nx, ny] = false;
          }
        }
      }
    }
  }

  // Extracted and hardened from generate_map(int depth): repeatedly picks the
  // highest-priority open (drawn, frontier) cell, expands into a random open neighbor
  // direction, and commits the best-weighted valid candidate tile for that neighbor.
  // Unlike the original GUI algorithm (which only ever seeds once, at the very start,
  // and gives up on the whole current frontier), this drains the frontier of every
  // connected component: whenever the frontier empties out but unlocked, undrawn cells
  // remain elsewhere on the map (e.g. separated by a locked wall), it seeds a fresh
  // component and keeps going until every fillable cell is drawn or no valid seed
  // exists anywhere, so a run never silently reports completion while leaving cells
  // untouched.
  private int run_generation(
    Map_State state,
    int depth,
    Random random,
    CancellationToken cancellation_token,
    Tile_Drawn_Callback tile_drawn,
    IProgress<int> progress)
  {
    short[,] tile_priorities = this.compute_tile_priorities(state.Tiles);
    Generation_Frontier open_tiles = collect_frontier(state, tile_priorities);
    Lookahead_Scratch lookahead_scratch = new Lookahead_Scratch(state);
    Remaining_Fillable_Cells remaining_fillable = new Remaining_Fillable_Cells(state);
    int unresolved = 0;
    int drawn_count = 0;

    while (true)
    {
      cancellation_token.ThrowIfCancellationRequested();

      if (open_tiles.Count == 0)
      {
        if (remaining_fillable.Count == 0)
          break; // Every cell is already drawn or locked; nothing left to do.

        int seed_index = remaining_fillable.pick(random);
        Cell? seeded_cell = null;
        while (remaining_fillable.Count > 0)
        {
          cancellation_token.ThrowIfCancellationRequested();
          Cell seed_cell = new Cell(seed_index % state.Width, seed_index / state.Width);
          if (this.try_seed_tile(state, seed_cell, random, tile_drawn))
          {
            remaining_fillable.remove(seed_index);
            seeded_cell = seed_cell;
            break;
          }

          mark_cell_as_unresolved(state, seed_cell, tile_drawn);
          remaining_fillable.remove(seed_index);
          lookahead_scratch.sync_cell(state, seed_cell.X, seed_cell.Y);
          ++unresolved;
          ++drawn_count;
          progress?.Report(drawn_count);
          if (remaining_fillable.Count > 0)
            seed_index = remaining_fillable.next_after(seed_index);
        }
        if (!seeded_cell.HasValue)
          break;

        Cell seeded = seeded_cell.Value;
        tile_priorities[seeded.X, seeded.Y] = this.tile_priority(state.Tiles[seeded.X, seeded.Y]);
        lookahead_scratch.sync_cell(state, seeded.X, seeded.Y);
        if (is_open_tile(state, seeded.X, seeded.Y))
          open_tiles.add(seeded.X + seeded.Y * state.Width, tile_priorities[seeded.X, seeded.Y]);
        ++drawn_count;
        progress?.Report(drawn_count);
        continue;
      }

      int open_index = open_tiles.pick(random);
      int x = open_index % state.Width;
      int y = open_index / state.Width;
      int source_tile = state.Tiles[x, y];

      if (!is_open_tile(state, x, y))
      {
        open_tiles.remove(open_index);
        continue;
      }

      List<byte> open_dirs = get_open_dirs(state, x, y);
      byte dir = open_dirs[random.Next(open_dirs.Count)];
      Cell target = neighbor_cell(x, y, dir);

      List<short> candidates = this.test_valid_tiles(state, target.X, target.Y, depth, cancellation_token, lookahead_scratch);
      short chosen;
      if (candidates.Count == 0 || candidates.Count == 1 && candidates[0] == 0)
      {
        ++unresolved;
        chosen = 0;
      }
      else if (candidates.Count > 1)
      {
        List<short> weighted = new List<short>();
        foreach (short candidate in candidates)
        {
          int weight = this.valid_tile_priority(source_tile, dir, candidate);
          for (int i = 0; i < weight; ++i)
            weighted.Add(candidate);
        }
        chosen = weighted[random.Next(weighted.Count)];
      }
      else
      {
        chosen = candidates[random.Next(candidates.Count)];
      }

      if (this._tileset_generation_data.identical_tiles.TryGetValue(chosen, out short canonical)
        && this._identical_aliases_by_canonical.TryGetValue(canonical, out short[] group))
      {
        chosen = group[random.Next(group.Length)];
      }

      draw_tile(state, target.X, target.Y, chosen, tile_drawn);
      remaining_fillable.remove(target.X + target.Y * state.Width);
      tile_priorities[target.X, target.Y] = this.tile_priority(source_tile);
      lookahead_scratch.sync_cell(state, target.X, target.Y);
      if (is_open_tile(state, target.X, target.Y))
        open_tiles.add(target.X + target.Y * state.Width, tile_priorities[target.X, target.Y]);
      ++drawn_count;
      progress?.Report(drawn_count);

      if (!is_open_tile(state, x, y))
        open_tiles.remove(open_index);
    }

    return unresolved;
  }

  // Faithful port of the frontier filter at the start of generate_map(int depth) and of
  // the full-map scan in get_open_tiles_for_repair: every drawn cell that still has an
  // open (undrawn, unlocked) neighbor is part of the expansion frontier.
  private static Generation_Frontier collect_frontier(Map_State state, short[,] tile_priorities)
  {
    Generation_Frontier open_tiles = new Generation_Frontier();
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (state.Drawn[x, y] && is_open_tile(state, x, y))
          open_tiles.add(x + y * state.Width, tile_priorities[x, y]);
      }
    }
    return open_tiles;
  }

  private static bool is_open_tile(Map_State state, int x, int y) => get_open_dirs(state, x, y).Count > 0;

  private static List<byte> get_open_dirs(Map_State state, int x, int y)
  {
    List<byte> dirs = new List<byte>();
    if (y + 1 < state.Height && !state.Drawn[x, y + 1] && !state.Locked[x, y + 1])
      dirs.Add(2);
    if (x - 1 >= 0 && !state.Drawn[x - 1, y] && !state.Locked[x - 1, y])
      dirs.Add(4);
    if (x + 1 < state.Width && !state.Drawn[x + 1, y] && !state.Locked[x + 1, y])
      dirs.Add(6);
    if (y - 1 >= 0 && !state.Drawn[x, y - 1] && !state.Locked[x, y - 1])
      dirs.Add(8);
    return dirs;
  }

  private static Cell neighbor_cell(int x, int y, byte dir)
  {
    int dx = dir == 4 ? -1 : dir == 6 ? 1 : 0;
    int dy = dir == 2 ? 1 : dir == 8 ? -1 : 0;
    return new Cell(x + dx, y + dy);
  }

  // Hardened replacement for draw_random_tile: the caller already guarantees `cell` is
  // undrawn and unlocked (via Remaining_Fillable_Cells), so this only has to pick a random
  // terrain-compatible tile index for that exact cell instead of retrying random
  // positions. Returns false (without touching state) when no tile is valid for the
  // cell's terrain constraint or the tileset config is empty, so the caller can treat
  // that as "impossible to seed" rather than silently giving up after a fixed number
  // of attempts.
  private bool try_seed_tile(Map_State state, Cell cell, Random random, Tile_Drawn_Callback tile_drawn)
  {
    Dictionary<int, Tile_Data> config = this.tileset_config_data;
    if (config.Count == 0)
      return false;

    int index;
    int terrain = state.Terrain[cell.X, cell.Y];
    if (terrain != 0 && this._terrain_tileset != null)
    {
      List<int> candidates = terrain <= 0
        ? config.Keys.Where(tile => this._terrain_tileset.Terrain_Tags.Count > tile && this._terrain_tileset.Terrain_Tags[tile] != -terrain).ToList()
        : config.Keys.Where(tile => this._terrain_tileset.Terrain_Tags.Count > tile && this._terrain_tileset.Terrain_Tags[tile] == terrain).ToList();
      if (candidates.Count == 0)
        return false;
      index = candidates[random.Next(candidates.Count)];
    }
    else
    {
      List<int> keys = config.Keys.ToList();
      index = keys[random.Next(keys.Count)];
    }

    draw_tile(state, cell.X, cell.Y, index, tile_drawn);
    return true;
  }

  private static void mark_cell_as_unresolved(Map_State state, Cell cell, Tile_Drawn_Callback tile_drawn)
  {
    state.Tiles[cell.X, cell.Y] = 0;
    state.Drawn[cell.X, cell.Y] = true;
    tile_drawn?.Invoke(cell.X, cell.Y, 0);
  }

  private static void draw_tile(Map_State state, int x, int y, int index, Tile_Drawn_Callback tile_drawn)
  {
    state.Drawn[x, y] = true;
    state.Tiles[x, y] = index;
    tile_drawn?.Invoke(x, y, index);
  }

  private short[,] compute_tile_priorities(int[,] map_tiles)
  {
    int width = map_tiles.GetLength(0);
    int height = map_tiles.GetLength(1);
    short[,] priorities = new short[width, height];
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
        priorities[x, y] = this.tile_priority(map_tiles[x, y]);
    }
    return priorities;
  }

  private short tile_priority(int index)
  {
    return this.tileset_config_data.TryGetValue(index, out Tile_Data data) ? data.Priority : (short) -1;
  }

  private int valid_tile_priority(int index, byte dir, short other_tile)
  {
    if (this.tileset_config_data.TryGetValue(index, out Tile_Data data) && data.Valid_Tile_Priority[dir].TryGetValue(other_tile, out short priority))
    {
      if (priority <= 0)
        throw new InvalidOperationException($"Adjacency weight for tile {index}, direction {dir}, neighbor {other_tile} must be positive.");
      return priority;
    }
    return 1;
  }

  // Backtracking search over the diamond of cells within Manhattan distance `depth` of
  // (base_x, base_y), returning the list of valid tile candidates for (base_x, base_y)
  // itself once every cell in the diamond has at least one valid tile. Hardened from
  // the original (which mutated the real Drawn_Tiles array as scratch "committed"
  // state during the search): this uses a local scratch copy of Drawn instead, so a
  // cancellation or any other exception raised mid-search can never leave transient
  // lookahead commitments behind in the real Map_State. The caller commits the chosen
  // tile itself via draw_tile once this returns, so no cleanup step is needed here.
  private List<short> test_valid_tiles(
    Map_State state,
    int base_x,
    int base_y,
    int depth,
    CancellationToken cancellation_token,
    Lookahead_Scratch scratch)
  {
    List<Cell> locs = new List<Cell>();
    for (int ring = 0; ring <= depth; ++ring)
    {
      for (int dy = -ring; dy <= ring; ++dy)
      {
        for (int dx = Math.Abs(dy) - ring; dx <= ring - Math.Abs(dy); ++dx)
        {
          if (Math.Abs(dx) + Math.Abs(dy) == ring
            && !state.is_off_map(base_x + dx, base_y + dy)
            && (!state.Drawn[base_x + dx, base_y + dy]
              || state.Terrain[base_x + dx, base_y + dy] != 0 && this._terrain_tileset != null && state.Tiles[base_x + dx, base_y + dy] == 0))
            locs.Add(new Cell(base_x + dx, base_y + dy));
        }
      }
    }

    scratch.restore_cells(state, locs);
    int[,] scratch_tiles = scratch.Tiles;
    bool[,] scratch_drawn = scratch.Drawn;
    try
    {
      int[] candidate_index = new int[locs.Count];
      int cursor = 0;
      List<short>[] tiles = new List<short>[candidate_index.Length];
      tiles[cursor] = new List<short>(this.valid_tiles(state, locs[cursor].X, locs[cursor].Y, scratch_tiles, scratch_drawn));
      if (tiles[cursor].Count == 0 || cursor + 1 >= candidate_index.Length || tiles[cursor].Count == 1 && tiles[cursor][0] == 0)
        return tiles[cursor];

      candidate_index[cursor] = 0;
      scratch_drawn[locs[cursor].X, locs[cursor].Y] = true;
      scratch_tiles[locs[cursor].X, locs[cursor].Y] = tiles[cursor][candidate_index[cursor]];

      while (cursor != -1)
      {
        cancellation_token.ThrowIfCancellationRequested();
        if (this.valid_surrounding_tiles(state, cursor, candidate_index.Length, scratch_tiles, scratch_drawn, locs, tiles))
        {
          if (cursor + 2 == candidate_index.Length)
          {
            for (int i = cursor; i > 0; --i)
            {
              scratch_drawn[locs[i].X, locs[i].Y] = false;
              scratch_tiles[locs[i].X, locs[i].Y] = 0;
            }
            cursor = 0;
            ++candidate_index[cursor];
            if (candidate_index[cursor] < tiles[cursor].Count)
              scratch_tiles[locs[cursor].X, locs[cursor].Y] = tiles[cursor][candidate_index[cursor]];
            else
              break;
          }
          else
          {
            ++cursor;
            candidate_index[cursor] = 0;
            scratch_drawn[locs[cursor].X, locs[cursor].Y] = true;
            scratch_tiles[locs[cursor].X, locs[cursor].Y] = tiles[cursor][candidate_index[cursor]];
          }
        }
        else
        {
          do
          {
            ++candidate_index[cursor];
            if (cursor == 0)
            {
              --candidate_index[cursor];
              tiles[cursor].RemoveAt(candidate_index[cursor]);
            }
            if (candidate_index[cursor] >= tiles[cursor].Count)
            {
              scratch_drawn[locs[cursor].X, locs[cursor].Y] = false;
              scratch_tiles[locs[cursor].X, locs[cursor].Y] = 0;
              --cursor;
            }
            else
              goto assign_next_candidate;
          }
          while (cursor != -1);
          continue;
          assign_next_candidate:
          scratch_tiles[locs[cursor].X, locs[cursor].Y] = tiles[cursor][candidate_index[cursor]];
        }
      }

      return tiles[0];
    }
    finally
    {
      scratch.restore_cells(state, locs);
    }
  }

  private bool valid_surrounding_tiles(Map_State state, int cursor, int length, int[,] scratch_tiles, bool[,] scratch_drawn, List<Cell> locs, List<short>[] tiles)
  {
    for (int i = cursor + 1; i < length; ++i)
    {
      tiles[i] = new List<short>(this.valid_tiles(state, locs[i].X, locs[i].Y, scratch_tiles, scratch_drawn));
      if (tiles[i].Count <= 0)
        return false;
    }
    return true;
  }

  // Faithful port of valid_tiles: for an already-open terrain cell it returns every tile
  // matching (positive tag) or not forbidden by (negative tag) the required terrain.
  // Otherwise it intersects the valid-neighbor sets contributed by every already-drawn
  // orthogonal neighbor (canonicalized through identical_tiles), then applies the same
  // signed terrain constraint to the result. `drawn` is the scratch committed-state
  // array from test_valid_tiles (real map state plus any in-progress lookahead picks),
  // never the live Map_State.Drawn array.
  private HashSet<short> valid_tiles(Map_State state, int x, int y, int[,] map_tiles, bool[,] drawn)
  {
    if (map_tiles[x, y] == 0 && state.Terrain[x, y] != 0 && drawn[x, y] && this._terrain_tileset != null)
    {
      int terrain = state.Terrain[x, y];
      return terrain > 0
        ? new HashSet<short>(Enumerable.Range(0, this._terrain_tileset.Terrain_Tags.Count).Select(i => (short) i).Where(tile => this._terrain_tileset.Terrain_Tags[tile] == terrain))
        : new HashSet<short>(Enumerable.Range(0, this._terrain_tileset.Terrain_Tags.Count).Select(i => (short) i).Where(tile => this._terrain_tileset.Terrain_Tags[tile] != -terrain));
    }

    List<byte> known_dirs = new List<byte>();
    for (int i = 0; i < 4; ++i)
    {
      byte dir = (byte) ((i + 1) * 2);
      Cell offset = Reverse_Dirs[dir];
      int nx = x + offset.X;
      int ny = y + offset.Y;
      if (nx < 0 || nx >= state.Width || ny < 0 || ny >= state.Height || !drawn[nx, ny])
        continue;
      short neighbor_tile = (short) map_tiles[nx, ny];
      if (this._tileset_generation_data.identical_tiles.TryGetValue(neighbor_tile, out short canonical))
        neighbor_tile = canonical;
      if (this.tileset_config_data.ContainsKey(neighbor_tile))
        known_dirs.Add(dir);
    }

    if (known_dirs.Count == 0)
      return new HashSet<short>() { 0 };

    byte first_dir = known_dirs[0];
    Cell first_offset = Reverse_Dirs[first_dir];
    short first_tile = (short) map_tiles[x + first_offset.X, y + first_offset.Y];
    if (this._tileset_generation_data.identical_tiles.TryGetValue(first_tile, out short first_canonical))
      first_tile = first_canonical;

    HashSet<short> result = new HashSet<short>(this.tileset_config_data[first_tile].Valid_Tile_Priority[first_dir].Keys);
    foreach (byte dir in known_dirs)
    {
      if (dir == first_dir)
        continue;
      Cell offset = Reverse_Dirs[dir];
      short neighbor_tile = (short) map_tiles[x + offset.X, y + offset.Y];
      if (this._tileset_generation_data.identical_tiles.TryGetValue(neighbor_tile, out short canonical))
        neighbor_tile = canonical;
      result.IntersectWith(this.tileset_config_data[neighbor_tile].Valid_Tile_Priority[dir].Keys);
    }

    if (result.Count > 0 && state.Terrain[x, y] != 0 && this._terrain_tileset != null)
    {
      int terrain = state.Terrain[x, y];
      result = terrain <= 0
        ? new HashSet<short>(result.Where(tile => this._terrain_tileset.Terrain_Tags.Count > tile && this._terrain_tileset.Terrain_Tags[tile] != -terrain))
        : new HashSet<short>(result.Where(tile => this._terrain_tileset.Terrain_Tags.Count > tile && this._terrain_tileset.Terrain_Tags[tile] == terrain));
    }

    return result;
  }

  private sealed class Remaining_Fillable_Cells
  {
    private readonly bool[] _active;
    private readonly int[] _tree;

    public int Count { get; private set; }

    public Remaining_Fillable_Cells(Map_State state)
    {
      int cell_count = checked(state.Width * state.Height);
      this._active = new bool[cell_count];
      this._tree = new int[cell_count + 1];
      for (int y = 0; y < state.Height; ++y)
      {
        for (int x = 0; x < state.Width; ++x)
        {
          if (state.Drawn[x, y] || state.Locked[x, y])
            continue;
          int index = x + y * state.Width;
          this._active[index] = true;
          this._tree[index + 1] = 1;
          ++this.Count;
        }
      }
      for (int index = 1; index < this._tree.Length; ++index)
      {
        int parent = index + (index & -index);
        if (parent < this._tree.Length)
          this._tree[parent] += this._tree[index];
      }
    }

    public int pick(Random random)
    {
      return this.select_by_rank(random.Next(this.Count));
    }

    public int next_after(int cell)
    {
      int active_through_cell = this.prefix_count(cell);
      return active_through_cell < this.Count
        ? this.select_by_rank(active_through_cell)
        : this.select_by_rank(0);
    }

    public void remove(int cell)
    {
      if (!this._active[cell])
        return;
      this._active[cell] = false;
      --this.Count;
      for (int index = cell + 1; index < this._tree.Length; index += index & -index)
        --this._tree[index];
    }

    private int prefix_count(int cell)
    {
      int count = 0;
      for (int index = cell + 1; index > 0; index -= index & -index)
        count += this._tree[index];
      return count;
    }

    private int select_by_rank(int rank)
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

  private sealed class Generation_Frontier
  {
    private readonly Dictionary<int, short> _priorities_by_tile = new Dictionary<int, short>();
    private readonly Dictionary<short, HashSet<int>> _tiles_by_priority = new Dictionary<short, HashSet<int>>();
    private readonly SortedSet<short> _active_priorities = new SortedSet<short>();

    public int Count => this._priorities_by_tile.Count;

    public void add(int tile, short priority)
    {
      if (this._priorities_by_tile.TryGetValue(tile, out short old_priority))
      {
        if (old_priority == priority)
          return;
        this.remove(tile);
      }

      if (!this._tiles_by_priority.TryGetValue(priority, out HashSet<int> tiles))
      {
        tiles = new HashSet<int>();
        this._tiles_by_priority.Add(priority, tiles);
        this._active_priorities.Add(priority);
      }
      tiles.Add(tile);
      this._priorities_by_tile.Add(tile, priority);
    }

    public void remove(int tile)
    {
      if (!this._priorities_by_tile.Remove(tile, out short priority))
        return;
      HashSet<int> tiles = this._tiles_by_priority[priority];
      tiles.Remove(tile);
      if (tiles.Count == 0)
      {
        this._tiles_by_priority.Remove(priority);
        this._active_priorities.Remove(priority);
      }
    }

    public int pick(Random random)
    {
      HashSet<int> candidates = this._tiles_by_priority[this._active_priorities.Max];
      return candidates.ElementAt(random.Next(candidates.Count));
    }
  }

  private sealed class Lookahead_Scratch
  {
    public readonly int[,] Tiles;
    public readonly bool[,] Drawn;

    public Lookahead_Scratch(Map_State state)
    {
      this.Tiles = new int[state.Width, state.Height];
      Array.Copy(state.Tiles, this.Tiles, state.Tiles.Length);
      this.Drawn = (bool[,]) state.Drawn.Clone();
    }

    public void sync_cell(Map_State state, int x, int y)
    {
      this.Tiles[x, y] = state.Tiles[x, y];
      this.Drawn[x, y] = state.Drawn[x, y];
    }

    public void restore_cells(Map_State state, IEnumerable<Cell> cells)
    {
      foreach (Cell cell in cells)
        this.sync_cell(state, cell.X, cell.Y);
    }
  }
}
