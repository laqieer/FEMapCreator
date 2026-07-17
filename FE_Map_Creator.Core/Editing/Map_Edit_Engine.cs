using FE_Map_Creator.Generation;
using System;
using System.Collections.Generic;
using System.Threading;

#nullable disable
namespace FE_Map_Creator.Editing;

public sealed class Map_Edit_Engine
{
  public Map_State apply(
    Map_State state,
    IReadOnlyList<Map_Edit_Operation> operations,
    CancellationToken cancellation_token = default)
  {
    if (state == null)
      throw new ArgumentNullException(nameof (state));
    if (operations == null || operations.Count == 0)
      return clone(state);

    Map_State edited = clone(state);
    for (int index = 0; index < operations.Count; ++index)
    {
      cancellation_token.ThrowIfCancellationRequested();
      Map_Edit_Operation operation = operations[index];
      if (operation == null)
        throw new InvalidOperationException($"Edits[{index}] is null.");
      string context = $"Edits[{index}]";
      operation.validate(context);
      edited = this.apply_operation(edited, operation, context);
    }
    return edited;
  }

  public bool apply_cell(
    Map_State state,
    int x,
    int y,
    Map_Edit_Action action,
    int selected_tile = 0,
    int selected_terrain = 0)
  {
    if (state == null)
      throw new ArgumentNullException(nameof (state));
    if (state.is_off_map(x, y))
      return false;
    switch (action)
    {
      case Map_Edit_Action.Set_Tile:
        if (selected_tile < 0)
          throw new ArgumentOutOfRangeException(nameof (selected_tile), "A non-negative tile index is required.");
        return assign_cell(state, x, y, selected_tile, true, false, 0);
      case Map_Edit_Action.Erase:
        return assign_cell(state, x, y, 0, false, false, 0);
      case Map_Edit_Action.Lock:
        return assign_cell(state, x, y, state.Tiles[x, y], true, true, 0);
      case Map_Edit_Action.Unlock:
        return assign_cell(state, x, y, state.Tiles[x, y], state.Drawn[x, y], false, 0);
      case Map_Edit_Action.Require_Terrain:
        validate_terrain(selected_terrain);
        return assign_cell(
          state, x, y, state.Tiles[x, y], state.Drawn[x, y], false, selected_terrain);
      case Map_Edit_Action.Forbid_Terrain:
        validate_terrain(selected_terrain);
        return assign_cell(
          state, x, y, state.Tiles[x, y], state.Drawn[x, y], false, -selected_terrain);
      default:
        throw new ArgumentOutOfRangeException(
          nameof (action), action, "The selected map edit action cannot be applied to cells.");
    }
  }

  public bool apply_rectangle(
    Map_State state,
    Cell start,
    Cell end,
    Map_Edit_Action action,
    int selected_tile = 0,
    int selected_terrain = 0)
  {
    if (state == null)
      throw new ArgumentNullException(nameof (state));
    int left = Math.Max(0, Math.Min(start.X, end.X));
    int right = Math.Min(state.Width - 1, Math.Max(start.X, end.X));
    int top = Math.Max(0, Math.Min(start.Y, end.Y));
    int bottom = Math.Min(state.Height - 1, Math.Max(start.Y, end.Y));
    bool changed = false;
    for (int y = top; y <= bottom; ++y)
    {
      for (int x = left; x <= right; ++x)
        changed |= this.apply_cell(state, x, y, action, selected_tile, selected_terrain);
    }
    return changed;
  }

  public bool flood_fill(
    Map_State state,
    int start_x,
    int start_y,
    Map_Edit_Action action,
    int selected_tile = 0,
    int selected_terrain = 0)
  {
    if (state == null)
      throw new ArgumentNullException(nameof (state));
    if (state.is_off_map(start_x, start_y))
      return false;
    int target_tile = state.Tiles[start_x, start_y];
    bool target_drawn = state.Drawn[start_x, start_y];
    bool target_locked = state.Locked[start_x, start_y];
    int target_terrain = state.Terrain[start_x, start_y];
    bool[,] visited = new bool[state.Width, state.Height];
    Queue<Cell> open = new Queue<Cell>();
    open.Enqueue(new Cell(start_x, start_y));
    bool changed = false;
    while (open.Count > 0)
    {
      Cell cell = open.Dequeue();
      if (state.is_off_map(cell.X, cell.Y) || visited[cell.X, cell.Y])
        continue;
      visited[cell.X, cell.Y] = true;
      if (!matches_fill_target(
        state, cell.X, cell.Y, action, target_tile, target_drawn, target_locked, target_terrain))
      {
        continue;
      }
      changed |= this.apply_cell(
        state, cell.X, cell.Y, action, selected_tile, selected_terrain);
      open.Enqueue(new Cell(cell.X - 1, cell.Y));
      open.Enqueue(new Cell(cell.X + 1, cell.Y));
      open.Enqueue(new Cell(cell.X, cell.Y - 1));
      open.Enqueue(new Cell(cell.X, cell.Y + 1));
    }
    return changed;
  }

  public Map_State resize(Map_State state, int width, int height)
  {
    if (state == null)
      throw new ArgumentNullException(nameof (state));
    if (width <= 0)
      throw new ArgumentOutOfRangeException(nameof (width), width, "Map width must be positive.");
    if (height <= 0)
      throw new ArgumentOutOfRangeException(nameof (height), height, "Map height must be positive.");
    if (width == state.Width && height == state.Height)
      return state;

    int[,] tiles = new int[width, height];
    bool[,] drawn = new bool[width, height];
    bool[,] locked = new bool[width, height];
    int[,] terrain = new int[width, height];
    int copy_width = Math.Min(width, state.Width);
    int copy_height = Math.Min(height, state.Height);
    for (int y = 0; y < copy_height; ++y)
    {
      for (int x = 0; x < copy_width; ++x)
      {
        tiles[x, y] = state.Tiles[x, y];
        drawn[x, y] = state.Drawn[x, y];
        locked[x, y] = state.Locked[x, y];
        terrain[x, y] = state.Terrain[x, y];
      }
    }
    return new Map_State(tiles, drawn, locked, terrain);
  }

  public bool clear_locks(Map_State state)
  {
    if (state == null)
      throw new ArgumentNullException(nameof (state));
    bool changed = false;
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (state.Locked[x, y])
        {
          state.Locked[x, y] = false;
          changed = true;
        }
      }
    }
    return changed;
  }

  public bool clear_terrain(Map_State state)
  {
    if (state == null)
      throw new ArgumentNullException(nameof (state));
    bool changed = false;
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (state.Terrain[x, y] != 0)
        {
          state.Terrain[x, y] = 0;
          changed = true;
        }
      }
    }
    return changed;
  }

  private Map_State apply_operation(
    Map_State state,
    Map_Edit_Operation operation,
    string context)
  {
    Map_Edit_Action action = operation.action(context);
    switch (action)
    {
      case Map_Edit_Action.Resize:
        return this.resize(state, operation.Width.Value, operation.Height.Value);
      case Map_Edit_Action.Clear_Locks:
        this.clear_locks(state);
        return state;
      case Map_Edit_Action.Clear_Terrain:
        this.clear_terrain(state);
        return state;
    }

    Map_Edit_Shape shape = operation.shape(context);
    if (shape == Map_Edit_Shape.Cell || shape == Map_Edit_Shape.Flood_Fill)
      require_on_map(state, operation.X.Value, operation.Y.Value, context);
    switch (shape)
    {
      case Map_Edit_Shape.Cell:
        this.apply_cell(
          state, operation.X.Value, operation.Y.Value, action,
          operation.Tile ?? 0, operation.Terrain ?? 0);
        break;
      case Map_Edit_Shape.Rectangle:
        this.apply_rectangle(
          state,
          new Cell(operation.X.Value, operation.Y.Value),
          new Cell(operation.EndX.Value, operation.EndY.Value),
          action,
          operation.Tile ?? 0,
          operation.Terrain ?? 0);
        break;
      case Map_Edit_Shape.Flood_Fill:
        this.flood_fill(
          state, operation.X.Value, operation.Y.Value, action,
          operation.Tile ?? 0, operation.Terrain ?? 0);
        break;
      default:
        throw new NotSupportedException($"Unsupported map edit shape \"{shape}\".");
    }
    return state;
  }

  private static bool assign_cell(
    Map_State state,
    int x,
    int y,
    int tile,
    bool drawn,
    bool locked,
    int terrain)
  {
    if (state.Tiles[x, y] == tile &&
        state.Drawn[x, y] == drawn &&
        state.Locked[x, y] == locked &&
        state.Terrain[x, y] == terrain)
    {
      return false;
    }
    state.Tiles[x, y] = tile;
    state.Drawn[x, y] = drawn;
    state.Locked[x, y] = locked;
    state.Terrain[x, y] = terrain;
    return true;
  }

  private static bool matches_fill_target(
    Map_State state,
    int x,
    int y,
    Map_Edit_Action action,
    int target_tile,
    bool target_drawn,
    bool target_locked,
    int target_terrain)
  {
    switch (action)
    {
      case Map_Edit_Action.Set_Tile:
      case Map_Edit_Action.Erase:
        return state.Tiles[x, y] == target_tile && state.Drawn[x, y] == target_drawn;
      case Map_Edit_Action.Lock:
      case Map_Edit_Action.Unlock:
        return state.Locked[x, y] == target_locked;
      case Map_Edit_Action.Require_Terrain:
      case Map_Edit_Action.Forbid_Terrain:
        return state.Terrain[x, y] == target_terrain;
      default:
        return false;
    }
  }

  private static void validate_terrain(int terrain)
  {
    if (terrain <= 0)
      throw new ArgumentOutOfRangeException(nameof (terrain), "A positive terrain id is required.");
  }

  private static void require_on_map(Map_State state, int x, int y, string context)
  {
    if (state.is_off_map(x, y))
      throw new InvalidOperationException(
        $"{context} coordinate ({x},{y}) is outside the {state.Width}x{state.Height} map.");
  }

  private static Map_State clone(Map_State state)
  {
    return new Map_State(
      (int[,]) state.Tiles.Clone(),
      (bool[,]) state.Drawn.Clone(),
      (bool[,]) state.Locked.Clone(),
      (int[,]) state.Terrain.Clone());
  }
}
