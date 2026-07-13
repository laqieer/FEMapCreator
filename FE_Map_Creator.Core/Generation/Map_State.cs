using System;

#nullable disable
namespace FE_Map_Creator.Generation;

/// <summary>
/// Mutable map state operated on by <see cref="Map_Generation_Engine"/>. Mirrors the
/// parallel [x, y] arrays owned by FE_Map_Creator_Form (Map_Tiles, Drawn_Tiles,
/// Locked_Tiles, Terrain_Types). Tile value 0 is a legitimate drawn tile, so "drawn"
/// state is tracked separately from the tile value; "locked" state is also independent
/// so callers can preserve a locked tile whose value happens to be 0.
/// </summary>
public sealed class Map_State
{
  public int[,] Tiles { get; }
  public bool[,] Drawn { get; }
  public bool[,] Locked { get; }
  public int[,] Terrain { get; }
  public int Width { get; }
  public int Height { get; }

  public Map_State(int[,] tiles, bool[,] drawn, bool[,] locked, int[,] terrain)
  {
    if (tiles == null)
      throw new ArgumentNullException(nameof(tiles));
    if (drawn == null)
      throw new ArgumentNullException(nameof(drawn));
    if (locked == null)
      throw new ArgumentNullException(nameof(locked));
    if (terrain == null)
      throw new ArgumentNullException(nameof(terrain));

    int width = tiles.GetLength(0);
    int height = tiles.GetLength(1);
    if (width <= 0 || height <= 0)
      throw new ArgumentException("Map dimensions must be greater than zero.", nameof(tiles));
    if (drawn.GetLength(0) != width || drawn.GetLength(1) != height
      || locked.GetLength(0) != width || locked.GetLength(1) != height
      || terrain.GetLength(0) != width || terrain.GetLength(1) != height)
      throw new ArgumentException("Tiles, Drawn, Locked, and Terrain arrays must share the same non-zero dimensions.");

    this.Tiles = tiles;
    this.Drawn = drawn;
    this.Locked = locked;
    this.Terrain = terrain;
    this.Width = width;
    this.Height = height;
  }

  public bool is_off_map(int x, int y) => x < 0 || y < 0 || x >= this.Width || y >= this.Height;
}
