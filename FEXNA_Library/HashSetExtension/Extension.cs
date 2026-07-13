// Decompiled with JetBrains decompiler
// Type: HashSetExtension.Extension
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace HashSetExtension;

public static class Extension
{
  public static void write(this HashSet<int> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (int num in list)
      writer.Write(num);
  }

  public static void read(this HashSet<int> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(reader.ReadInt32());
  }

  public static void write(this HashSet<string> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (string str in list)
      writer.Write(str);
  }

  public static void read(this HashSet<string> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(reader.ReadString());
  }

  public static void write(this HashSet<Vector2> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Vector2 vector in list)
      vector.write(writer);
  }

  public static void read(this HashSet<Vector2> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    Vector2 zero = Vector2.Zero;
    for (int index = 0; index < num; ++index)
      list.Add(zero.read(reader));
  }
}
