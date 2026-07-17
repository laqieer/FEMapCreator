using FE_Map_Creator.Generation;
using System;
using System.Collections.Generic;

#nullable disable
namespace FE_Map_Creator.Gui.Models;

public sealed class Editor_Session
{
  private const int HISTORY_LIMIT = 100;

  private readonly List<Editor_Snapshot> Undo_History = new List<Editor_Snapshot>();
  private readonly List<Editor_Snapshot> Redo_History = new List<Editor_Snapshot>();
  private Editor_Snapshot Pending_Edit;
  private bool Pending_Edit_Changed;

  public int[,] Tiles { get; private set; }

  public bool[,] Drawn { get; private set; }

  public bool[,] Locked { get; private set; }

  public int[,] Terrain { get; private set; }

  public int Width => this.Tiles.GetLength(0);

  public int Height => this.Tiles.GetLength(1);

  public bool Is_Dirty { get; private set; }

  public bool Can_Undo => this.Undo_History.Count > 0;

  public bool Can_Redo => this.Redo_History.Count > 0;

  public Editor_Session(int width = 20, int height = 20)
  {
    validate_dimensions(width, height);
    this.Tiles = new int[width, height];
    this.Drawn = new bool[width, height];
    this.Locked = new bool[width, height];
    this.Terrain = new int[width, height];
  }

  public void new_map(int width, int height)
  {
    validate_dimensions(width, height);
    this.Tiles = new int[width, height];
    this.Drawn = new bool[width, height];
    this.Locked = new bool[width, height];
    this.Terrain = new int[width, height];
    this.clear_history();
    this.Is_Dirty = true;
  }

  public void load_map(Map_Document document)
  {
    if (document == null)
      throw new ArgumentNullException(nameof (document));
    this.Tiles = clone(document.Tiles);
    this.Drawn = new bool[document.Width, document.Height];
    this.Locked = new bool[document.Width, document.Height];
    this.Terrain = new int[document.Width, document.Height];
    for (int y = 0; y < document.Height; ++y)
    {
      for (int x = 0; x < document.Width; ++x)
        this.Drawn[x, y] = true;
    }
    this.clear_history();
    this.Is_Dirty = false;
  }

  public Map_Document create_document(string tileset)
  {
    return new Map_Document(clone(this.Tiles), tileset ?? "");
  }

  public Map_State create_map_state()
  {
    return new Map_State(this.Tiles, this.Drawn, this.Locked, this.Terrain);
  }

  public void mark_saved() => this.Is_Dirty = false;

  public void begin_edit()
  {
    if (this.Pending_Edit != null)
      return;
    this.Pending_Edit = this.capture();
    this.Pending_Edit_Changed = false;
  }

  public void commit_external_edit()
  {
    if (this.Pending_Edit == null)
      throw new InvalidOperationException("No editor transaction is active.");
    this.Pending_Edit_Changed = true;
    this.end_edit();
  }

  public void rollback_edit()
  {
    if (this.Pending_Edit == null)
      return;
    this.restore(this.Pending_Edit);
    this.Pending_Edit = null;
    this.Pending_Edit_Changed = false;
  }

  public void end_edit()
  {
    if (this.Pending_Edit == null)
      return;
    if (this.Pending_Edit_Changed)
    {
      this.push_history(this.Undo_History, this.Pending_Edit);
      this.Redo_History.Clear();
      this.Is_Dirty = true;
    }
    this.Pending_Edit = null;
    this.Pending_Edit_Changed = false;
  }

  public bool apply_cell(
    int x,
    int y,
    Editor_Mode mode,
    int selected_tile,
    int selected_terrain,
    bool? lock_value = null)
  {
    if (this.is_off_map(x, y))
      return false;
    switch (mode)
    {
      case Editor_Mode.Tile:
        return this.assign_cell(x, y, selected_tile, true, false, 0);
      case Editor_Mode.Erase:
        return this.assign_cell(x, y, 0, false, false, 0);
      case Editor_Mode.Lock:
        bool locked = lock_value ?? !this.Locked[x, y];
        return this.assign_cell(x, y, this.Tiles[x, y], locked || this.Drawn[x, y], locked, 0);
      case Editor_Mode.Terrain_Required:
        if (selected_terrain <= 0)
          throw new ArgumentOutOfRangeException(nameof (selected_terrain), "A positive terrain id is required.");
        return this.assign_cell(x, y, this.Tiles[x, y], this.Drawn[x, y], false, selected_terrain);
      case Editor_Mode.Terrain_Forbidden:
        if (selected_terrain <= 0)
          throw new ArgumentOutOfRangeException(nameof (selected_terrain), "A positive terrain id is required.");
        return this.assign_cell(x, y, this.Tiles[x, y], this.Drawn[x, y], false, -selected_terrain);
      default:
        throw new ArgumentOutOfRangeException(nameof (mode), mode, "Unknown editor mode.");
    }
  }

  public void apply_rectangle(
    Map_Cell start,
    Map_Cell end,
    Editor_Mode mode,
    int selected_tile,
    int selected_terrain,
    bool? lock_value = null)
  {
    int left = Math.Max(0, Math.Min(start.X, end.X));
    int right = Math.Min(this.Width - 1, Math.Max(start.X, end.X));
    int top = Math.Max(0, Math.Min(start.Y, end.Y));
    int bottom = Math.Min(this.Height - 1, Math.Max(start.Y, end.Y));
    for (int y = top; y <= bottom; ++y)
    {
      for (int x = left; x <= right; ++x)
        this.apply_cell(x, y, mode, selected_tile, selected_terrain, lock_value);
    }
  }

  public void flood_fill(
    int start_x,
    int start_y,
    Editor_Mode mode,
    int selected_tile,
    int selected_terrain,
    bool? lock_value = null)
  {
    if (this.is_off_map(start_x, start_y))
      return;
    int target_tile = this.Tiles[start_x, start_y];
    bool target_drawn = this.Drawn[start_x, start_y];
    bool target_locked = this.Locked[start_x, start_y];
    int target_terrain = this.Terrain[start_x, start_y];
    bool[,] visited = new bool[this.Width, this.Height];
    Queue<Map_Cell> open = new Queue<Map_Cell>();
    open.Enqueue(new Map_Cell(start_x, start_y));
    while (open.Count > 0)
    {
      Map_Cell cell = open.Dequeue();
      if (this.is_off_map(cell.X, cell.Y) || visited[cell.X, cell.Y])
        continue;
      visited[cell.X, cell.Y] = true;
      if (!this.matches_fill_target(
        cell.X, cell.Y, mode, target_tile, target_drawn, target_locked, target_terrain))
      {
        continue;
      }
      this.apply_cell(cell.X, cell.Y, mode, selected_tile, selected_terrain, lock_value);
      open.Enqueue(new Map_Cell(cell.X - 1, cell.Y));
      open.Enqueue(new Map_Cell(cell.X + 1, cell.Y));
      open.Enqueue(new Map_Cell(cell.X, cell.Y - 1));
      open.Enqueue(new Map_Cell(cell.X, cell.Y + 1));
    }
  }

  public void resize(int width, int height)
  {
    validate_dimensions(width, height);
    if (width == this.Width && height == this.Height)
      return;
    this.begin_edit();
    int[,] tiles = new int[width, height];
    bool[,] drawn = new bool[width, height];
    bool[,] locked = new bool[width, height];
    int[,] terrain = new int[width, height];
    int copy_width = Math.Min(width, this.Width);
    int copy_height = Math.Min(height, this.Height);
    for (int y = 0; y < copy_height; ++y)
    {
      for (int x = 0; x < copy_width; ++x)
      {
        tiles[x, y] = this.Tiles[x, y];
        drawn[x, y] = this.Drawn[x, y];
        locked[x, y] = this.Locked[x, y];
        terrain[x, y] = this.Terrain[x, y];
      }
    }
    this.Tiles = tiles;
    this.Drawn = drawn;
    this.Locked = locked;
    this.Terrain = terrain;
    this.Pending_Edit_Changed = true;
    this.end_edit();
  }

  public void clear_locks()
  {
    this.begin_edit();
    for (int y = 0; y < this.Height; ++y)
    {
      for (int x = 0; x < this.Width; ++x)
      {
        if (this.Locked[x, y])
        {
          this.Locked[x, y] = false;
          this.Pending_Edit_Changed = true;
        }
      }
    }
    this.end_edit();
  }

  public void clear_terrain()
  {
    this.begin_edit();
    for (int y = 0; y < this.Height; ++y)
    {
      for (int x = 0; x < this.Width; ++x)
      {
        if (this.Terrain[x, y] != 0)
        {
          this.Terrain[x, y] = 0;
          this.Pending_Edit_Changed = true;
        }
      }
    }
    this.end_edit();
  }

  public bool undo()
  {
    this.end_edit();
    if (!this.Can_Undo)
      return false;
    this.push_history(this.Redo_History, this.capture());
    Editor_Snapshot snapshot = this.Undo_History[this.Undo_History.Count - 1];
    this.Undo_History.RemoveAt(this.Undo_History.Count - 1);
    this.restore(snapshot);
    this.Is_Dirty = true;
    return true;
  }

  public bool redo()
  {
    this.end_edit();
    if (!this.Can_Redo)
      return false;
    this.push_history(this.Undo_History, this.capture());
    Editor_Snapshot snapshot = this.Redo_History[this.Redo_History.Count - 1];
    this.Redo_History.RemoveAt(this.Redo_History.Count - 1);
    this.restore(snapshot);
    this.Is_Dirty = true;
    return true;
  }

  public bool is_off_map(int x, int y)
  {
    return x < 0 || y < 0 || x >= this.Width || y >= this.Height;
  }

  private bool assign_cell(int x, int y, int tile, bool drawn, bool locked, int terrain)
  {
    if (this.Tiles[x, y] == tile &&
        this.Drawn[x, y] == drawn &&
        this.Locked[x, y] == locked &&
        this.Terrain[x, y] == terrain)
    {
      return false;
    }
    this.Tiles[x, y] = tile;
    this.Drawn[x, y] = drawn;
    this.Locked[x, y] = locked;
    this.Terrain[x, y] = terrain;
    this.Pending_Edit_Changed = true;
    return true;
  }

  private bool matches_fill_target(
    int x,
    int y,
    Editor_Mode mode,
    int target_tile,
    bool target_drawn,
    bool target_locked,
    int target_terrain)
  {
    switch (mode)
    {
      case Editor_Mode.Tile:
      case Editor_Mode.Erase:
        return this.Tiles[x, y] == target_tile && this.Drawn[x, y] == target_drawn;
      case Editor_Mode.Lock:
        return this.Locked[x, y] == target_locked;
      case Editor_Mode.Terrain_Required:
      case Editor_Mode.Terrain_Forbidden:
        return this.Terrain[x, y] == target_terrain;
      default:
        return false;
    }
  }

  private void clear_history()
  {
    this.Undo_History.Clear();
    this.Redo_History.Clear();
    this.Pending_Edit = null;
    this.Pending_Edit_Changed = false;
  }

  private Editor_Snapshot capture()
  {
    return new Editor_Snapshot(this.Tiles, this.Drawn, this.Locked, this.Terrain);
  }

  private void restore(Editor_Snapshot snapshot)
  {
    this.Tiles = clone(snapshot.Tiles);
    this.Drawn = clone(snapshot.Drawn);
    this.Locked = clone(snapshot.Locked);
    this.Terrain = clone(snapshot.Terrain);
  }

  private void push_history(List<Editor_Snapshot> history, Editor_Snapshot snapshot)
  {
    history.Add(snapshot);
    if (history.Count > HISTORY_LIMIT)
      history.RemoveAt(0);
  }

  private static void validate_dimensions(int width, int height)
  {
    if (width <= 0)
      throw new ArgumentOutOfRangeException(nameof (width), width, "Map width must be positive.");
    if (height <= 0)
      throw new ArgumentOutOfRangeException(nameof (height), height, "Map height must be positive.");
  }

  private static int[,] clone(int[,] source)
  {
    int[,] result = new int[source.GetLength(0), source.GetLength(1)];
    Array.Copy(source, result, source.Length);
    return result;
  }

  private static bool[,] clone(bool[,] source)
  {
    bool[,] result = new bool[source.GetLength(0), source.GetLength(1)];
    Array.Copy(source, result, source.Length);
    return result;
  }
}
