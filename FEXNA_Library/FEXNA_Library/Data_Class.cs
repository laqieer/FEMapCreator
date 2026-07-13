// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Class
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using DictionaryExtension;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Data_Class
{
  public static readonly List<int> GENERIC_CAPS = new List<int>()
  {
    80 /*0x50*/,
    20,
    20,
    20,
    20,
    20,
    20
  };
  public int Id;
  public string Name;
  public List<ClassTypes> Class_Types;
  public List<int> Skills;
  public string Description;
  public List<int>[] Caps;
  public List<int> Max_WLvl;
  public Dictionary<int, List<int>[]> Promotion;
  public int Tier;
  public int Mov;
  public int Mov_Cap;
  public MovementTypes Movement_Type;
  public List<List<int>[]> Generic_Stats;

  public Data_Class()
  {
    this.Id = 0;
    this.Name = string.Empty;
    this.Class_Types = new List<ClassTypes>();
    this.Skills = new List<int>();
    this.Description = string.Empty;
    this.Caps = new List<int>[2]
    {
      new List<int>((IEnumerable<int>) Data_Class.GENERIC_CAPS),
      new List<int>((IEnumerable<int>) Data_Class.GENERIC_CAPS)
    };
    this.Max_WLvl = new List<int>()
    {
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0,
      0
    };
    this.Promotion = new Dictionary<int, List<int>[]>();
    this.Mov = 5;
    this.Mov_Cap = 15;
    this.Movement_Type = MovementTypes.Light;
    List<List<int>[]> intListArrayList = new List<List<int>[]>();
    intListArrayList.Add(new List<int>[2]
    {
      new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0 },
      new List<int>() { 0, 0, 0, 0, 0, 0, 0 }
    });
    intListArrayList.Add(new List<int>[2]
    {
      new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0 },
      new List<int>() { 0, 0, 0, 0, 0, 0, 0 }
    });
    intListArrayList.Add(new List<int>[2]
    {
      new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0 },
      new List<int>() { 0, 0, 0, 0, 0, 0, 0 }
    });
    intListArrayList.Add(new List<int>[2]
    {
      new List<int>() { 0, 0, 0, 0, 0, 0, 0, 0 },
      new List<int>() { 0, 0, 0, 0, 0, 0, 0 }
    });
    this.Generic_Stats = intListArrayList;
  }

  public Data_Class(Data_Class data)
  {
    this.Id = data.Id;
    this.Name = data.Name;
    this.Class_Types = new List<ClassTypes>((IEnumerable<ClassTypes>) data.Class_Types);
    this.Skills = new List<int>((IEnumerable<int>) data.Skills);
    this.Description = data.Description;
    if (data.Caps == null)
    {
      this.Caps = (List<int>[]) null;
    }
    else
    {
      this.Caps = new List<int>[data.Caps.Length];
      for (int index = 0; index < this.Caps.Length; ++index)
        this.Caps[index] = new List<int>((IEnumerable<int>) data.Caps[index]);
    }
    this.Max_WLvl = new List<int>((IEnumerable<int>) data.Max_WLvl);
    this.Promotion = new Dictionary<int, List<int>[]>();
    foreach (KeyValuePair<int, List<int>[]> keyValuePair in data.Promotion)
    {
      this.Promotion.Add(keyValuePair.Key, new List<int>[keyValuePair.Value.Length]);
      for (int index = 0; index < keyValuePair.Value.Length; ++index)
        this.Promotion[keyValuePair.Key][index] = new List<int>((IEnumerable<int>) keyValuePair.Value[index]);
    }
    this.Tier = data.Tier;
    this.Mov = data.Mov;
    this.Mov_Cap = data.Mov_Cap;
    this.Movement_Type = data.Movement_Type;
    this.Generic_Stats = new List<List<int>[]>();
    foreach (List<int>[] genericStat in data.Generic_Stats)
    {
      this.Generic_Stats.Add(new List<int>[genericStat.Length]);
      for (int index = 0; index < genericStat.Length; ++index)
        this.Generic_Stats[this.Generic_Stats.Count - 1][index] = new List<int>((IEnumerable<int>) genericStat[index]);
    }
  }

  public string name => this.Name.Split('_')[0];

  public static Data_Class read(BinaryReader reader)
  {
    Data_Class dataClass = new Data_Class();
    dataClass.Id = reader.ReadInt32();
    dataClass.Name = reader.ReadString();
    dataClass.Class_Types.read(reader);
    dataClass.Skills.read(reader);
    dataClass.Description = reader.ReadString();
    dataClass.Caps = !reader.ReadBoolean() ? (List<int>[]) null : dataClass.Caps.read(reader);
    dataClass.Max_WLvl.read(reader);
    dataClass.Promotion.read(reader);
    dataClass.Tier = reader.ReadInt32();
    dataClass.Mov = reader.ReadInt32();
    dataClass.Mov_Cap = reader.ReadInt32();
    dataClass.Movement_Type = (MovementTypes) reader.ReadInt32();
    dataClass.Generic_Stats.read(reader);
    return dataClass;
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.Id);
    writer.Write(this.Name);
    this.Class_Types.write(writer);
    this.Skills.write(writer);
    writer.Write(this.Description);
    writer.Write(this.Caps != null);
    if (this.Caps != null)
      this.Caps.write(writer);
    this.Max_WLvl.write(writer);
    this.Promotion.write(writer);
    writer.Write(this.Tier);
    writer.Write(this.Mov);
    writer.Write(this.Mov_Cap);
    writer.Write((int) this.Movement_Type);
    this.Generic_Stats.write(writer);
  }

  public List<int> caps() => this.caps(0);

  public List<int> caps(int gender)
  {
    return this.Caps == null ? Data_Class.GENERIC_CAPS : this.Caps[gender % 2];
  }
}
