// Decompiled with JetBrains decompiler
// Type: MapGenDictionaryExtension.Extensions
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using FE_Map_Creator;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace MapGenDictionaryExtension;

public static class Extensions
{
  public static void write(
    this Dictionary<int, Dictionary<int, int>> dictionary,
    BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<int, Dictionary<int, int>> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(
    this Dictionary<int, Dictionary<int, int>> dictionary,
    BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      int key = reader.ReadInt32();
      Dictionary<int, int> dictionary1 = new Dictionary<int, int>();
      dictionary1.read(reader);
      dictionary.Add(key, dictionary1);
    }
  }

  public static void write(
    this Dictionary<byte, Dictionary<short, short>> dictionary,
    BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<byte, Dictionary<short, short>> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(
    this Dictionary<byte, Dictionary<short, short>> dictionary,
    BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      byte key = reader.ReadByte();
      Dictionary<short, short> dictionary1 = new Dictionary<short, short>();
      dictionary1.read(reader);
      dictionary.Add(key, dictionary1);
    }
  }

  public static void write(this Dictionary<int, Tile_Data> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<int, Tile_Data> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(this Dictionary<int, Tile_Data> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      int key = reader.ReadInt32();
      Tile_Data tileData = Tile_Data.read(reader);
      dictionary.Add(key, tileData);
    }
  }

  public static void write(
    this Dictionary<string, Dictionary<int, Tile_Data>> dictionary,
    BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<string, Dictionary<int, Tile_Data>> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(
    this Dictionary<string, Dictionary<int, Tile_Data>> dictionary,
    BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      string key = reader.ReadString();
      Dictionary<int, Tile_Data> dictionary1 = new Dictionary<int, Tile_Data>();
      dictionary1.read(reader);
      dictionary.Add(key, dictionary1);
    }
  }
}
