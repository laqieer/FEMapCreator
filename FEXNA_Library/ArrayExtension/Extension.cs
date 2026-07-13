// Decompiled with JetBrains decompiler
// Type: ArrayExtension.Extension
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System.Collections.Generic;
using System.IO;

#nullable disable
namespace ArrayExtension;

public static class Extension
{
  public static void write(this byte[] array, BinaryWriter writer)
  {
    writer.Write(array.Length);
    foreach (byte num in array)
      writer.Write(num);
  }

  public static byte[] read(this byte[] array, BinaryReader reader)
  {
    array = new byte[reader.ReadInt32()];
    for (int index = 0; index < array.Length; ++index)
      array[index] = reader.ReadByte();
    return array;
  }

  public static void write(this int[] array, BinaryWriter writer)
  {
    writer.Write(array.Length);
    foreach (int num in array)
      writer.Write(num);
  }

  public static int[] read(this int[] array, BinaryReader reader)
  {
    array = new int[reader.ReadInt32()];
    for (int index = 0; index < array.Length; ++index)
      array[index] = reader.ReadInt32();
    return array;
  }

  public static void write(this int[][] array, BinaryWriter writer)
  {
    writer.Write(array.Length);
    foreach (int[] array1 in array)
      array1.write(writer);
  }

  public static int[][] read(this int[][] array, BinaryReader reader)
  {
    array = new int[reader.ReadInt32()][];
    for (int index = 0; index < array.Length; ++index)
      array[index] = array[index].read(reader);
    return array;
  }

  public static void write(this bool[] array, BinaryWriter writer)
  {
    writer.Write(array.Length);
    foreach (bool flag in array)
      writer.Write(flag);
  }

  public static bool[] read(this bool[] array, BinaryReader reader)
  {
    array = new bool[reader.ReadInt32()];
    for (int index = 0; index < array.Length; ++index)
      array[index] = reader.ReadBoolean();
    return array;
  }

  public static void write(this string[] array, BinaryWriter writer)
  {
    writer.Write(array.Length);
    foreach (string str in array)
      writer.Write(str);
  }

  public static string[] read(this string[] array, BinaryReader reader)
  {
    array = new string[reader.ReadInt32()];
    for (int index = 0; index < array.Length; ++index)
      array[index] = reader.ReadString();
    return array;
  }

  public static void write(this List<int>[] array, BinaryWriter writer)
  {
    writer.Write(array.Length);
    foreach (List<int> list in array)
      list.write(writer);
  }

  public static List<int>[] read(this List<int>[] array, BinaryReader reader)
  {
    array = new List<int>[reader.ReadInt32()];
    for (int index = 0; index < array.Length; ++index)
    {
      array[index] = new List<int>();
      array[index].read(reader);
    }
    return array;
  }
}
