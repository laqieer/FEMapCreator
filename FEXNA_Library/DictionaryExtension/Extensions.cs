// Decompiled with JetBrains decompiler
// Type: DictionaryExtension.Extensions
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using FEXNA_Library;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace DictionaryExtension;

public static class Extensions
{
  public static void write(this Dictionary<byte, List<short>> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<byte, List<short>> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(this Dictionary<byte, List<short>> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      byte key = reader.ReadByte();
      List<short> list = new List<short>();
      list.read(reader);
      dictionary.Add(key, list);
    }
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
    int num1 = reader.ReadInt32();
    for (int index = 0; index < num1; ++index)
    {
      short key = reader.ReadInt16();
      short num2 = reader.ReadInt16();
      dictionary.Add(key, num2);
    }
  }

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
    int num1 = reader.ReadInt32();
    for (int index = 0; index < num1; ++index)
    {
      int key = reader.ReadInt32();
      int num2 = reader.ReadInt32();
      dictionary.Add(key, num2);
    }
  }

  public static void write(this Dictionary<int, string> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<int, string> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      writer.Write(keyValuePair.Value);
    }
  }

  public static void read(this Dictionary<int, string> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      int key = reader.ReadInt32();
      string str = reader.ReadString();
      dictionary.Add(key, str);
    }
  }

  public static void write(this Dictionary<int, List<int>> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<int, List<int>> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(this Dictionary<int, List<int>> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      int key = reader.ReadInt32();
      List<int> list = new List<int>();
      list.read(reader);
      dictionary.Add(key, list);
    }
  }

  public static void write(this Dictionary<int, List<int>[]> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<int, List<int>[]> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(this Dictionary<int, List<int>[]> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      int key = reader.ReadInt32();
      List<int>[] array = new List<int>[0];
      dictionary.Add(key, array.read(reader));
    }
  }

  public static void write(this Dictionary<int, List<Rectangle>> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<int, List<Rectangle>> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(this Dictionary<int, List<Rectangle>> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      int key = reader.ReadInt32();
      List<Rectangle> list = new List<Rectangle>();
      list.read(reader);
      dictionary.Add(key, list);
    }
  }

  public static void write(this Dictionary<int, HashSet<int>> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<int, HashSet<int>> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(this Dictionary<int, HashSet<int>> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      int key = reader.ReadInt32();
      HashSet<int> list = new HashSet<int>();
      list.read(reader);
      dictionary.Add(key, list);
    }
  }

  public static void write(this Dictionary<int, HashSet<string>> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<int, HashSet<string>> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(this Dictionary<int, HashSet<string>> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      int key = reader.ReadInt32();
      HashSet<string> list = new HashSet<string>();
      list.read(reader);
      dictionary.Add(key, list);
    }
  }

  public static void write(
    this Dictionary<int, List<Support_Entry>> dictionary,
    BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<int, List<Support_Entry>> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(
    this Dictionary<int, List<Support_Entry>> dictionary,
    BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      int key = reader.ReadInt32();
      List<Support_Entry> list = new List<Support_Entry>();
      list.read(reader);
      dictionary.Add(key, list);
    }
  }

  public static void write(this Dictionary<string, int> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<string, int> keyValuePair in dictionary)
    {
      writer.Write(keyValuePair.Key);
      writer.Write(keyValuePair.Value);
    }
  }

  public static void read(this Dictionary<string, int> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num1 = reader.ReadInt32();
    for (int index = 0; index < num1; ++index)
    {
      string key = reader.ReadString();
      int num2 = reader.ReadInt32();
      dictionary.Add(key, num2);
    }
  }

  public static void write(this Dictionary<Vector2, Vector2> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<Vector2, Vector2> keyValuePair in dictionary)
    {
      keyValuePair.Key.write(writer);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(this Dictionary<Vector2, Vector2> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      reader.ReadInt32();
      Vector2 zero1 = Vector2.Zero;
      Vector2 zero2 = Vector2.Zero;
      zero1.read(reader);
      zero2.read(reader);
      dictionary.Add(zero1, zero2);
    }
  }

  public static void write(this Dictionary<Vector2, Data_Unit> dictionary, BinaryWriter writer)
  {
    writer.Write(dictionary.Count);
    foreach (KeyValuePair<Vector2, Data_Unit> keyValuePair in dictionary)
    {
      keyValuePair.Key.write(writer);
      keyValuePair.Value.write(writer);
    }
  }

  public static void read(this Dictionary<Vector2, Data_Unit> dictionary, BinaryReader reader)
  {
    dictionary.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      Vector2 key = Vector2.Zero.read(reader);
      Data_Unit dataUnit = Data_Unit.read(reader);
      dictionary.Add(key, dataUnit);
    }
  }
}
