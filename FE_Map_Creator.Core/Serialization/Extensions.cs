// Ported from FE_Map_Creator\MapGenDictionaryExtension\Extensions.cs into
// FE_Map_Creator.Core so it can be shared between the WinForms GUI and other callers
// (CLI, tests). Binary layout is unchanged from the original decompiled source.

using FE_Map_Creator;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace MapGenDictionaryExtension;

public static class Extensions
{
  public static void write(this Dictionary<int, int> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<int, int> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      writer.Write(keyValuePair.Value);
    }
  }

  public static void read(this Dictionary<int, int> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int count = reader.ReadInt32();
    for (int index = 0; index < count; ++index)
      dictionary.Add(reader.ReadInt32(), reader.ReadInt32());
  }

  public static void write(this Dictionary<short, short> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<short, short> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      writer.Write(keyValuePair.Value);
    }
  }

  public static void read(this Dictionary<short, short> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int count = reader.ReadInt32();
    for (int index = 0; index < count; ++index)
      dictionary.Add(reader.ReadInt16(), reader.ReadInt16());
  }

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
