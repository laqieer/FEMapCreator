// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Tileset_Generation_Data
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#nullable disable
namespace FE_Map_Creator;

internal class Tileset_Generation_Data
{
  private Dictionary<short, short> Identical_Tiles = new Dictionary<short, short>();
  private Dictionary<int, Tile_Data> Generation_Data = new Dictionary<int, Tile_Data>();

  public Dictionary<short, short> identical_tiles => this.Identical_Tiles;

  public Dictionary<int, Tile_Data> generation_data => this.Generation_Data;

  public HashSet<short> tiles_with_identical
  {
    get => new HashSet<short>((IEnumerable<short>) this.Identical_Tiles.Values);
  }

  public void write(BinaryWriter writer)
  {
    this.Identical_Tiles.write(writer);
    this.Generation_Data.write(writer);
  }

  public static Tileset_Generation_Data read(BinaryReader reader)
  {
    Tileset_Generation_Data tilesetGenerationData = new Tileset_Generation_Data();
    tilesetGenerationData.Identical_Tiles.read(reader);
    tilesetGenerationData.Generation_Data.read(reader);
    return tilesetGenerationData;
  }

  private Tileset_Generation_Data()
  {
  }

  public Tileset_Generation_Data(int tile_count, Tile_Matching_Data tile_matches)
  {
    this.Identical_Tiles = new Dictionary<short, short>((IDictionary<short, short>) tile_matches.identical_tiles);
  }

  public bool fix_identical(Dictionary<short, short> identical)
  {
    if (this.Identical_Tiles.Count == identical.Count && this.Identical_Tiles.Keys.Intersect<short>((IEnumerable<short>) identical.Keys).Count<short>() == this.Identical_Tiles.Count && this.Identical_Tiles.All<KeyValuePair<short, short>>((Func<KeyValuePair<short, short>, bool>) (pair => (int) identical[pair.Key] == (int) pair.Value)))
      return true;
    HashSet<short> shortSet1 = new HashSet<short>();
    foreach (short key in this.Identical_Tiles.Keys.Union<short>((IEnumerable<short>) identical.Keys))
    {
      if (!this.Identical_Tiles.ContainsKey(key) || !identical.ContainsKey(key) || (int) this.Identical_Tiles[key] != (int) identical[key])
        shortSet1.Add(key);
    }
    HashSet<short> shortSet2 = new HashSet<short>(shortSet1.Where<short>((Func<short, bool>) (tile => this.Identical_Tiles.ContainsKey(tile))).Select<short, short>((Func<short, short>) (tile => this.Identical_Tiles[tile])));
    HashSet<short> shortSet3 = new HashSet<short>(shortSet1.Where<short>((Func<short, bool>) (tile => identical.ContainsKey(tile))).Select<short, short>((Func<short, short>) (tile => identical[tile])));
    foreach (short num in shortSet2)
    {
      short target_tile = num;
      shortSet1.UnionWith(this.Identical_Tiles.Keys.Where<short>((Func<short, bool>) (tile => (int) this.Identical_Tiles[tile] == (int) target_tile)));
    }
    foreach (short num in shortSet3)
    {
      short target_tile = num;
      shortSet1.UnionWith(identical.Keys.Where<short>((Func<short, bool>) (tile => (int) identical[tile] == (int) target_tile)));
    }
    Dictionary<short, Tile_Data> old_data = new Dictionary<short, Tile_Data>();
    foreach (short key in shortSet1)
    {
      Tile_Data tileData = (Tile_Data) null;
      if (this.Identical_Tiles.ContainsKey(key))
      {
        if (this.Generation_Data.ContainsKey((int) this.Identical_Tiles[key]))
          tileData = this.Generation_Data[(int) this.Identical_Tiles[key]];
      }
      else if (this.Generation_Data.ContainsKey((int) key))
        tileData = this.Generation_Data[(int) key];
      old_data[key] = tileData;
    }
    List<short> list = shortSet1.ToList<short>();
    list.Sort();
    Dictionary<short, HashSet<short>> groups = new Dictionary<short, HashSet<short>>();
    foreach (short key in list)
    {
      if (!identical.ContainsKey(key))
        groups.Add(key, new HashSet<short>() { key });
      else if (!groups.ContainsKey(identical[key]))
        groups.Add(identical[key], new HashSet<short>()
        {
          key
        });
      else
        groups[identical[key]].Add(key);
    }
    Dictionary<short, Tile_Data> dictionary1 = new Dictionary<short, Tile_Data>();
    foreach (KeyValuePair<short, HashSet<short>> keyValuePair in groups)
    {
      IEnumerable<Tile_Data> tileDatas = keyValuePair.Value.Select<short, Tile_Data>((Func<short, Tile_Data>) (tile => old_data[tile])).Where<Tile_Data>((Func<Tile_Data, bool>) (old => old != null));
      dictionary1[keyValuePair.Key] = tileDatas.Count<Tile_Data>() != 0 ? new Tile_Data(tileDatas) : (Tile_Data) null;
    }
    foreach (short key in shortSet1)
    {
      this.Identical_Tiles.Remove(key);
      this.Generation_Data.Remove((int) key);
    }
    Dictionary<Tuple<byte, short, short>, short> source = new Dictionary<Tuple<byte, short, short>, short>();
    for (byte key1 = 2; key1 <= (byte) 8; key1 += (byte) 2)
    {
      foreach (KeyValuePair<short, HashSet<short>> keyValuePair in groups)
      {
        while (true)
        {
          if (dictionary1[keyValuePair.Key] != null && dictionary1[keyValuePair.Key].Valid_Tile_Priority[key1].Keys.Intersect<short>((IEnumerable<short>) shortSet1).Count<short>() > 0)
          {
            short key2 = dictionary1[keyValuePair.Key].Valid_Tile_Priority[key1].Keys.Intersect<short>((IEnumerable<short>) shortSet1).First<short>();
            source.Add(new Tuple<byte, short, short>(key1, keyValuePair.Key, key2), dictionary1[keyValuePair.Key].Valid_Tile_Priority[key1][key2]);
            dictionary1[keyValuePair.Key].Valid_Tile_Priority[key1].Remove(key2);
          }
          else
            goto label_55;
        }
label_55:;
      }
    }
    List<short> group_keys = new List<short>((IEnumerable<short>) groups.Keys);
    group_keys.Sort();
    Dictionary<Tuple<byte, short, short>, short> dictionary2 = new Dictionary<Tuple<byte, short, short>, short>();
    for (int i = 0; i < group_keys.Count; ++i)
    {
      for (int j = 0; j < group_keys.Count; ++j)
      {
        while (source.Any<KeyValuePair<Tuple<byte, short, short>, short>>((Func<KeyValuePair<Tuple<byte, short, short>, short>, bool>) (p =>
        {
          foreach (short num3 in groups[group_keys[i]])
          {
            foreach (short num4 in groups[group_keys[j]])
            {
              if ((int) p.Key.Item2 == (int) num3 && (int) p.Key.Item3 == (int) num4)
                return true;
            }
          }
          return false;
        })))
        {
          KeyValuePair<Tuple<byte, short, short>, short> keyValuePair = source.First<KeyValuePair<Tuple<byte, short, short>, short>>((Func<KeyValuePair<Tuple<byte, short, short>, short>, bool>) (p =>
          {
            foreach (short num1 in groups[group_keys[i]])
            {
              foreach (short num2 in groups[group_keys[j]])
              {
                if ((int) p.Key.Item2 == (int) num1 && (int) p.Key.Item3 == (int) num2)
                  return true;
              }
            }
            return false;
          }));
          dictionary2.Add(new Tuple<byte, short, short>(keyValuePair.Key.Item1, group_keys[i], group_keys[j]), keyValuePair.Value);
          source.Remove(keyValuePair.Key);
        }
      }
    }
    foreach (KeyValuePair<Tuple<byte, short, short>, short> keyValuePair in dictionary2)
    {
      if (dictionary1[keyValuePair.Key.Item2] == null)
        dictionary1[keyValuePair.Key.Item2] = new Tile_Data();
      dictionary1[keyValuePair.Key.Item2].Valid_Tile_Priority[keyValuePair.Key.Item1].Add(keyValuePair.Key.Item3, keyValuePair.Value);
    }
    foreach (short key3 in shortSet1)
    {
      foreach (short key4 in this.Generation_Data.Keys)
      {
        for (byte key5 = 2; key5 <= (byte) 8; key5 += (byte) 2)
          this.Generation_Data[(int) key4].Valid_Tile_Priority[key5].Remove(key3);
      }
    }
    foreach (KeyValuePair<short, HashSet<short>> keyValuePair in groups)
    {
      foreach (short key in keyValuePair.Value)
        this.Identical_Tiles[key] = keyValuePair.Key;
      this.Generation_Data[(int) keyValuePair.Key] = dictionary1[keyValuePair.Key];
      if (this.Generation_Data[(int) keyValuePair.Key] != null)
      {
        for (byte key6 = 2; key6 <= (byte) 8; key6 += (byte) 2)
        {
          foreach (short key7 in this.Generation_Data[(int) keyValuePair.Key].Valid_Tile_Priority[key6].Keys.Select<short, short>((Func<short, short>) (x => x)).Except<short>((IEnumerable<short>) shortSet1))
          {
            if (!this.Generation_Data.ContainsKey((int) key7))
              this.Generation_Data[(int) key7] = new Tile_Data();
            else if (this.Generation_Data[(int) key7] == null)
              this.Generation_Data[(int) key7] = new Tile_Data();
            this.Generation_Data[(int) key7].Valid_Tile_Priority[(byte) (10U - (uint) key6)].Add(keyValuePair.Key, this.Generation_Data[(int) keyValuePair.Key].Valid_Tile_Priority[key6][key7]);
          }
        }
      }
    }
    return false;
  }
}
