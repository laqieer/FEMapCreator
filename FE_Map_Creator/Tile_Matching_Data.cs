// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Tile_Matching_Data
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable
namespace FE_Map_Creator;

internal class Tile_Matching_Data
{
  private Dictionary<Tile_Directions, Dictionary<int, List<short>>> Same_Directions;
  private Dictionary<short, short> Identical_Tiles = new Dictionary<short, short>();

  public Dictionary<short, short> identical_tiles => this.Identical_Tiles;

  public IEnumerable<short> redundant_tiles
  {
    get
    {
      return this.Identical_Tiles.Keys.Where<short>((Func<short, bool>) (tile => (int) tile != (int) this.Identical_Tiles[tile]));
    }
  }

  public Tile_Matching_Data(HashSet<Tile_Directions> directions)
  {
    this.Same_Directions = directions.ToDictionary<Tile_Directions, Tile_Directions, Dictionary<int, List<short>>>((Func<Tile_Directions, Tile_Directions>) (p => p), (Func<Tile_Directions, Dictionary<int, List<short>>>) (p => new Dictionary<int, List<short>>()));
  }

  public bool has_index(Tile_Directions dir, int index)
  {
    return this.Same_Directions[dir].ContainsKey(index);
  }

  public void add(Tile_Directions dir, List<short> same)
  {
    foreach (int key in same)
      this.Same_Directions[dir][key] = new List<short>((IEnumerable<short>) same);
  }

  public List<short> matched_tiles(Tile_Directions dir, short index)
  {
    if (this.Same_Directions.Keys.Contains<Tile_Directions>(dir))
    {
      if (this.has_index(dir, (int) index))
        return this.Same_Directions[dir][(int) index];
      return new List<short>() { index };
    }
    Tile_Directions tileDirections1;
    Tile_Directions tileDirections2;
    switch (dir)
    {
      case Tile_Directions.SW:
        tileDirections1 = Tile_Directions.Down;
        tileDirections2 = Tile_Directions.Left;
        break;
      case Tile_Directions.Down:
        tileDirections1 = Tile_Directions.SW;
        tileDirections2 = Tile_Directions.SE;
        break;
      case Tile_Directions.SE:
        tileDirections1 = Tile_Directions.Down;
        tileDirections2 = Tile_Directions.Right;
        break;
      case Tile_Directions.Left:
        tileDirections1 = Tile_Directions.SW;
        tileDirections2 = Tile_Directions.NW;
        break;
      case Tile_Directions.Center:
        if (this.has_index(Tile_Directions.SW, (int) index) && this.has_index(Tile_Directions.SE, (int) index) && this.has_index(Tile_Directions.NW, (int) index) && this.has_index(Tile_Directions.NE, (int) index))
          return this.Same_Directions[Tile_Directions.SW][(int) index].Intersect<short>((IEnumerable<short>) this.Same_Directions[Tile_Directions.SE][(int) index]).Intersect<short>((IEnumerable<short>) this.Same_Directions[Tile_Directions.NW][(int) index]).Intersect<short>((IEnumerable<short>) this.Same_Directions[Tile_Directions.NE][(int) index]).ToList<short>();
        return new List<short>() { index };
      case Tile_Directions.Right:
        tileDirections1 = Tile_Directions.SE;
        tileDirections2 = Tile_Directions.NE;
        break;
      case Tile_Directions.NW:
        tileDirections1 = Tile_Directions.Left;
        tileDirections2 = Tile_Directions.Up;
        break;
      case Tile_Directions.Up:
        tileDirections1 = Tile_Directions.NW;
        tileDirections2 = Tile_Directions.NE;
        break;
      case Tile_Directions.NE:
        tileDirections1 = Tile_Directions.Right;
        tileDirections2 = Tile_Directions.Up;
        break;
      default:
        return new List<short>() { index };
    }
    if (this.has_index(tileDirections1, (int) index) && this.has_index(tileDirections2, (int) index))
      return this.Same_Directions[tileDirections1][(int) index].Intersect<short>((IEnumerable<short>) this.Same_Directions[tileDirections2][(int) index]).Except<short>(this.redundant_tiles).ToList<short>();
    return new List<short>() { index };
  }

  public static Tile_Directions side_from_corners(HashSet<byte> dirs)
  {
    if (dirs.Contains((byte) 1) && dirs.Contains((byte) 3))
      return Tile_Directions.Down;
    if (dirs.Contains((byte) 1) && dirs.Contains((byte) 7))
      return Tile_Directions.Left;
    if (dirs.Contains((byte) 3) && dirs.Contains((byte) 9))
      return Tile_Directions.Right;
    return dirs.Contains((byte) 7) && dirs.Contains((byte) 9) ? Tile_Directions.Up : Tile_Directions.Center;
  }

  public void refresh_identical(int tile_count)
  {
    this.Identical_Tiles.Clear();
    for (short i = 0; (int) i < tile_count; ++i)
    {
      if (!this.Identical_Tiles.ContainsKey(i))
      {
        List<short> source = this.matched_tiles(Tile_Directions.Center, i);
        if (source.Any<short>((Func<short, bool>) (tile => this.Identical_Tiles.ContainsKey(tile))))
          throw new Exception(" Error creating tile matching data.");
        if (source.Where<short>((Func<short, bool>) (tile => (int) tile != (int) i)).Count<short>() > 0)
        {
          foreach (short key in source)
            this.Identical_Tiles[key] = i;
        }
      }
    }
  }
}
