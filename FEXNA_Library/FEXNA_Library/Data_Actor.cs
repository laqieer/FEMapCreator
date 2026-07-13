// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Actor
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using System;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Data_Actor
{
  public int Id;
  public string Name = \u0005.\u0002(-1223164663);
  public string Description = string.Empty;
  public int ClassId = 1;
  public int Level = 1;
  public List<int> BaseStats = new List<int>()
  {
    20,
    5,
    5,
    5,
    5,
    5,
    5,
    5
  };
  public List<int> Growths = new List<int>()
  {
    50,
    25,
    25,
    25,
    25,
    25,
    25
  };
  public List<int> WLvl = new List<int>()
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
  public int Gender;
  public Affinities Affinity = Affinities.None;
  public List<int[]> Items = new List<int[]>()
  {
    new int[3],
    new int[3],
    new int[3],
    new int[3],
    new int[3]
  };
  public List<string> Supports = new List<string>();
  public List<int> Skills = new List<int>();

  public Data_Actor()
  {
  }

  public Data_Actor(Data_Actor actor)
  {
    this.Id = actor.Id;
    this.Name = actor.Name;
    this.Description = actor.Description;
    this.ClassId = actor.ClassId;
    this.Level = actor.Level;
    this.BaseStats = new List<int>((IEnumerable<int>) actor.BaseStats);
    this.Growths = new List<int>((IEnumerable<int>) actor.Growths);
    this.WLvl = new List<int>((IEnumerable<int>) actor.WLvl);
    this.Gender = actor.Gender;
    this.Affinity = actor.Affinity;
    this.Items = new List<int[]>();
    foreach (int[] sourceArray in actor.Items)
    {
      int[] destinationArray = new int[sourceArray.Length];
      Array.Copy((Array) sourceArray, (Array) destinationArray, sourceArray.Length);
      this.Items.Add(destinationArray);
    }
    this.Supports = new List<string>((IEnumerable<string>) actor.Supports);
    this.Skills = new List<int>((IEnumerable<int>) actor.Skills);
  }

  public static Data_Actor read(BinaryReader reader)
  {
    Data_Actor dataActor = new Data_Actor();
    dataActor.Id = reader.ReadInt32();
    dataActor.Name = reader.ReadString();
    dataActor.Description = reader.ReadString();
    dataActor.ClassId = reader.ReadInt32();
    dataActor.Level = reader.ReadInt32();
    dataActor.BaseStats.read(reader);
    dataActor.Growths.read(reader);
    dataActor.WLvl.read(reader);
    dataActor.Gender = reader.ReadInt32();
    dataActor.Affinity = (Affinities) reader.ReadInt32();
    dataActor.Items.read(reader);
    dataActor.Supports.read(reader);
    dataActor.Skills.read(reader);
    return dataActor;
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.Id);
    writer.Write(this.Name);
    writer.Write(this.Description);
    writer.Write(this.ClassId);
    writer.Write(this.Level);
    this.BaseStats.write(writer);
    this.Growths.write(writer);
    this.WLvl.write(writer);
    writer.Write(this.Gender);
    writer.Write((int) this.Affinity);
    this.Items.write(writer);
    this.Supports.write(writer);
    this.Skills.write(writer);
  }

  public override string ToString()
  {
    return string.Format(\u0005.\u0002(-1223164615), (object) this.Name);
  }
}
