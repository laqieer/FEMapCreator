// Decompiled with JetBrains decompiler
// Type: ListExtension.Extension
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using FEXNA_Library;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace ListExtension;

public static class Extension
{
  public static void write(this List<byte> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (byte num in list)
      writer.Write(num);
  }

  public static void read(this List<byte> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(reader.ReadByte());
  }

  public static void write(this List<short> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (short num in list)
      writer.Write(num);
  }

  public static void read(this List<short> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(reader.ReadInt16());
  }

  public static void write(this List<int> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (int num in list)
      writer.Write(num);
  }

  public static void read(this List<int> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(reader.ReadInt32());
  }

  public static void write(this List<int[]> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (int[] array in list)
      array.write(writer);
  }

  public static void read(this List<int[]> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(new int[0].read(reader));
  }

  public static void write(this List<List<int>> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (List<int> intList in list)
    {
      writer.Write(intList.Count);
      foreach (int num in intList)
        writer.Write(num);
    }
  }

  public static void read(this List<List<int>> list, BinaryReader reader)
  {
    list.Clear();
    int num1 = reader.ReadInt32();
    for (int index1 = 0; index1 < num1; ++index1)
    {
      int num2 = reader.ReadInt32();
      list.Add(new List<int>());
      for (int index2 = 0; index2 < num2; ++index2)
        list[list.Count - 1].Add(reader.ReadInt32());
    }
  }

  public static void write(this List<string> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (string str in list)
      writer.Write(str);
  }

  public static void read(this List<string> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(reader.ReadString());
  }

  public static void write(this List<string[]> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (string[] strArray in list)
    {
      writer.Write(strArray.Length);
      foreach (string str in strArray)
        writer.Write(str);
    }
  }

  public static void read(this List<string[]> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index1 = 0; index1 < num; ++index1)
    {
      int length = reader.ReadInt32();
      list.Add(new string[length]);
      for (int index2 = 0; index2 < length; ++index2)
        list[list.Count - 1][index2] = reader.ReadString();
    }
  }

  public static Vector2 pop(this List<Vector2> list)
  {
    Vector2 vector2 = list.Count != 0 ? list[list.Count - 1] : throw new IndexOutOfRangeException(\u0005.\u0002(-1223164307));
    list.RemoveAt(list.Count - 1);
    return vector2;
  }

  public static void write(this List<Vector2> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Vector2 vector in list)
      vector.write(writer);
  }

  public static void read(this List<Vector2> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      list.Add(Vector2.Zero);
      list[list.Count - 1] = list[list.Count - 1].read(reader);
    }
  }

  public static void write(this List<Rectangle> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Rectangle rect in list)
      rect.write(writer);
  }

  public static void read(this List<Rectangle> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      list.Add(new Rectangle());
      list[list.Count - 1] = list[list.Count - 1].read(reader);
    }
  }

  public static void write(this List<ClassTypes> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (int num in list)
      writer.Write(num);
  }

  public static void read(this List<ClassTypes> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add((ClassTypes) reader.ReadInt32());
  }

  public static void write(this List<Item_Data> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Item_Data itemData in list)
      itemData.write(writer);
  }

  public static void read(this List<Item_Data> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(Item_Data.read(reader));
  }

  public static void write(this List<Battle_Frame_Data> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Battle_Frame_Data battleFrameData in list)
      battleFrameData.write(writer);
  }

  public static void read(this List<Battle_Frame_Data> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
    {
      list.Add(new Battle_Frame_Data());
      list[list.Count - 1].read(reader);
    }
  }

  public static void write(this List<Battle_Frame_Image_Data> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Battle_Frame_Image_Data battleFrameImageData in list)
      battleFrameImageData.write(writer);
  }

  public static void read(this List<Battle_Frame_Image_Data> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(Battle_Frame_Image_Data.read(reader));
  }

  public static void write(this List<Sound_Data> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Sound_Data soundData in list)
      soundData.write(writer);
  }

  public static void read(this List<Sound_Data> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(Sound_Data.read(reader));
  }

  public static void write(this List<Battle_Animation_Tween_Data> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Battle_Animation_Tween_Data animationTweenData in list)
      animationTweenData.write(writer);
  }

  public static void read(this List<Battle_Animation_Tween_Data> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(Battle_Animation_Tween_Data.read(reader));
  }

  public static void write(this List<Event_Data> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Event_Data eventData in list)
      eventData.write(writer);
  }

  public static void read(this List<Event_Data> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(Event_Data.read(reader));
  }

  public static void write(this List<Event_Control> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Event_Control eventControl in list)
      eventControl.write(writer);
  }

  public static void read(this List<Event_Control> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(Event_Control.read(reader));
  }

  public static void write(this List<Support_Entry> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Support_Entry supportEntry in list)
      supportEntry.write(writer);
  }

  public static void read(this List<Support_Entry> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(Support_Entry.read(reader));
  }

  public static void write(this List<Data_Unit> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (Data_Unit dataUnit in list)
      dataUnit.write(writer);
  }

  public static void read(this List<Data_Unit> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(Data_Unit.read(reader));
  }

  public static void write(this List<List<int>[]> list, BinaryWriter writer)
  {
    writer.Write(list.Count);
    foreach (List<int>[] array in list)
      array.write(writer);
  }

  public static void read(this List<List<int>[]> list, BinaryReader reader)
  {
    list.Clear();
    int num = reader.ReadInt32();
    for (int index = 0; index < num; ++index)
      list.Add(new List<int>[0].read(reader));
  }
}
