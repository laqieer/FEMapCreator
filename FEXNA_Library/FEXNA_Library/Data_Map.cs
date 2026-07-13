// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Map
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Data_Map
{
  protected int[,] Values = new int[0, 0];
  protected int Tileset;
  public List<Vector2> Seize_Points = new List<Vector2>();
  public List<Vector2> Defend_Points = new List<Vector2>();
  public List<Vector2> Escape_Points = new List<Vector2>();
  public List<Vector2> Thief_Escape_Points = new List<Vector2>();

  public Data_Map()
  {
  }

  public Data_Map(int[,] values, int tileset)
  {
    this.Values = values;
    this.Tileset = tileset;
  }

  public Data_Map(Data_Map src_map)
  {
    this.Values = new int[src_map.values.GetLength(0), src_map.values.GetLength(1)];
    Array.Copy((Array) src_map.values, (Array) this.Values, this.Values.Length);
    this.Tileset = src_map.GetTileset();
    this.Seize_Points = new List<Vector2>();
    this.Seize_Points.AddRange((IEnumerable<Vector2>) src_map.Seize_Points);
    this.Defend_Points = new List<Vector2>();
    this.Defend_Points.AddRange((IEnumerable<Vector2>) src_map.Defend_Points);
    this.Escape_Points = new List<Vector2>();
    this.Escape_Points.AddRange((IEnumerable<Vector2>) src_map.Escape_Points);
    this.Thief_Escape_Points = new List<Vector2>();
    this.Thief_Escape_Points.AddRange((IEnumerable<Vector2>) src_map.Thief_Escape_Points);
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.Tileset);
    writer.Write(this.Columns);
    writer.Write(this.Rows);
    for (int index1 = 0; index1 < this.Rows; ++index1)
    {
      for (int index2 = 0; index2 < this.Columns; ++index2)
        writer.Write(this.Values[index2, index1]);
    }
    this.Seize_Points.write(writer);
    this.Defend_Points.write(writer);
    this.Escape_Points.write(writer);
    this.Thief_Escape_Points.write(writer);
  }

  public void read(BinaryReader reader)
  {
    this.Tileset = reader.ReadInt32();
    int length1 = reader.ReadInt32();
    int length2 = reader.ReadInt32();
    this.Values = new int[length1, length2];
    for (int index1 = 0; index1 < length2; ++index1)
    {
      for (int index2 = 0; index2 < length1; ++index2)
        this.Values[index2, index1] = reader.ReadInt32();
    }
    this.Seize_Points.read(reader);
    this.Defend_Points.read(reader);
    this.Escape_Points.read(reader);
    this.Thief_Escape_Points.read(reader);
  }

  public int GetTileset() => this.Tileset;

  public int[,] values => this.Values;

  public int GetValue(int column, int row) => this.Values[column, row];

  public void set_value(int column, int row, int new_value)
  {
    if (column < 0 || column >= this.Values.GetLength(0) || row < 0 || row >= this.Values.GetLength(1))
      return;
    this.Values[column, row] = new_value;
  }

  public int Columns => this.Values.GetLength(0);

  public int Rows => this.Values.GetLength(1);
}
