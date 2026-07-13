// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Map_Unit_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using DictionaryExtension;
using ListExtension;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Map_Unit_Data
{
  public Dictionary<Vector2, Data_Unit> Units = new Dictionary<Vector2, Data_Unit>();
  public List<Data_Unit> Reinforcements = new List<Data_Unit>();

  public void write(BinaryWriter writer)
  {
    this.Units.write(writer);
    this.Reinforcements.write(writer);
  }

  public static Map_Unit_Data read(BinaryReader reader)
  {
    Map_Unit_Data mapUnitData = new Map_Unit_Data();
    mapUnitData.Units.read(reader);
    mapUnitData.Reinforcements.read(reader);
    return mapUnitData;
  }
}
