// Ported from FE_Map_Creator\Tile_Data.cs into FE_Map_Creator.Core so it can be shared
// between the WinForms GUI and other callers (CLI, tests). Logic and binary layout are
// unchanged from the original decompiled source.

using MapGenDictionaryExtension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#nullable disable
namespace FE_Map_Creator;

public class Tile_Data
{
  public short Priority;
  public Dictionary<byte, Dictionary<short, short>> Valid_Tile_Priority = new Dictionary<byte, Dictionary<short, short>>()
  {
    {
      (byte) 2,
      new Dictionary<short, short>()
    },
    {
      (byte) 4,
      new Dictionary<short, short>()
    },
    {
      (byte) 6,
      new Dictionary<short, short>()
    },
    {
      (byte) 8,
      new Dictionary<short, short>()
    }
  };

  public void write(BinaryWriter writer)
  {
    writer.Write(this.Priority);
    this.Valid_Tile_Priority.write(writer);
  }

  public static Tile_Data read(BinaryReader reader)
  {
    Tile_Data tileData = new Tile_Data();
    tileData.Priority = reader.ReadInt16();
    tileData.Valid_Tile_Priority.read(reader);
    return tileData;
  }

  public override string ToString()
  {
    return $"Tile Data: D:{this.Valid_Tile_Priority[(byte) 2].Count}, L:{this.Valid_Tile_Priority[(byte) 4].Count}, R:{this.Valid_Tile_Priority[(byte) 6].Count}, U:{this.Valid_Tile_Priority[(byte) 8].Count}";
  }

  public Tile_Data()
  {
  }

  public void increment_valid_tile_priority(byte direction, short tile)
  {
    if (!this.Valid_Tile_Priority.TryGetValue(direction, out Dictionary<short, short> priorities))
      throw new ArgumentOutOfRangeException(nameof(direction), direction, "Direction must be 2, 4, 6, or 8.");
    if (!priorities.TryGetValue(tile, out short priority))
    {
      priorities[tile] = 1;
      return;
    }
    if (priority <= 0)
      throw new InvalidOperationException($"Adjacency weight for direction {direction}, tile {tile} must be positive.");
    if (priority < short.MaxValue)
      priorities[tile] = (short) (priority + 1);
  }

  public Tile_Data(Tile_Data source)
  {
    this.Priority = source.Priority;
    foreach (KeyValuePair<byte, Dictionary<short, short>> keyValuePair in source.Valid_Tile_Priority)
      this.Valid_Tile_Priority[keyValuePair.Key] = new Dictionary<short, short>((IDictionary<short, short>) keyValuePair.Value);
  }

  public Tile_Data(IEnumerable<Tile_Data> sources)
  {
    if (sources.Count<Tile_Data>() == 0)
      return;
    this.Priority = (short) sources.Select<Tile_Data, int>((Func<Tile_Data, int>) (source => (int) source.Priority)).Average();
    foreach (byte num1 in new HashSet<byte>(sources.SelectMany<Tile_Data, byte>((Func<Tile_Data, IEnumerable<byte>>) (x => (IEnumerable<byte>) x.Valid_Tile_Priority.Keys))))
    {
      byte dir = num1;
      foreach (short num2 in new HashSet<short>(sources.SelectMany<Tile_Data, short>((Func<Tile_Data, IEnumerable<short>>) (x => (IEnumerable<short>) x.Valid_Tile_Priority[dir].Keys))))
      {
        short num3 = Tile_Data.rms_priority(sources, dir, num2);
        this.Valid_Tile_Priority[dir][num2] = num3;
      }
    }
  }

  private static short min_priority(IEnumerable<Tile_Data> data, byte dir, short tile)
  {
    return data.Where<Tile_Data>((Func<Tile_Data, bool>) (x => x.Valid_Tile_Priority[dir].ContainsKey(tile))).Select<Tile_Data, short>((Func<Tile_Data, short>) (x => x.Valid_Tile_Priority[dir][tile])).Min<short>();
  }

  private static short max_priority(IEnumerable<Tile_Data> data, byte dir, short tile)
  {
    return data.Where<Tile_Data>((Func<Tile_Data, bool>) (x => x.Valid_Tile_Priority[dir].ContainsKey(tile))).Select<Tile_Data, short>((Func<Tile_Data, short>) (x => x.Valid_Tile_Priority[dir][tile])).Max<short>();
  }

  private static short average_priority(IEnumerable<Tile_Data> data, byte dir, short tile)
  {
    return (short) Math.Max(1.0, data.Where<Tile_Data>((Func<Tile_Data, bool>) (x => x.Valid_Tile_Priority[dir].ContainsKey(tile))).Select<Tile_Data, int>((Func<Tile_Data, int>) (x => (int) x.Valid_Tile_Priority[dir][tile])).Average());
  }

  private static short rms_priority(IEnumerable<Tile_Data> data, byte dir, short tile)
  {
    return (short) Math.Max(1.0, Math.Sqrt(data.Where<Tile_Data>((Func<Tile_Data, bool>) (x => x.Valid_Tile_Priority[dir].ContainsKey(tile))).Select<Tile_Data, double>((Func<Tile_Data, double>) (x => Math.Pow((double) x.Valid_Tile_Priority[dir][tile], 2.0))).Average()));
  }
}
