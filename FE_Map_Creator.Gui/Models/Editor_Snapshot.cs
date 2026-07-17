using System;

#nullable disable
namespace FE_Map_Creator.Gui.Models;

internal sealed class Editor_Snapshot
{
  internal int[,] Tiles { get; }

  internal bool[,] Drawn { get; }

  internal bool[,] Locked { get; }

  internal int[,] Terrain { get; }

  internal Editor_Snapshot(int[,] tiles, bool[,] drawn, bool[,] locked, int[,] terrain)
  {
    this.Tiles = clone(tiles);
    this.Drawn = clone(drawn);
    this.Locked = clone(locked);
    this.Terrain = clone(terrain);
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
