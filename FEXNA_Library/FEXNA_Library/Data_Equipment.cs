// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Equipment
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using System;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public abstract class Data_Equipment
{
  public int Id;
  public string Name = string.Empty;
  public string Full_Name = string.Empty;
  public string Description = string.Empty;
  public string Quick_Desc = string.Empty;
  public string Image_Name = string.Empty;
  public int Image_Index;
  public int Uses = 1;
  public int Cost = 1;
  public List<int> Prf_Character = new List<int>();
  public List<int> Prf_Class = new List<int>();
  public List<int> Prf_Type = new List<int>();
  public List<int> Skills = new List<int>();
  public List<int> Status_Inflict = new List<int>();
  public List<int> Status_Remove = new List<int>();
  public bool Can_Sell = true;

  protected void read_equipment(BinaryReader reader)
  {
    this.Id = reader.ReadInt32();
    this.Name = reader.ReadString();
    this.Full_Name = reader.ReadString();
    this.Description = reader.ReadString();
    this.Quick_Desc = reader.ReadString();
    this.Image_Name = reader.ReadString();
    this.Image_Index = reader.ReadInt32();
    this.Uses = reader.ReadInt32();
    this.Cost = reader.ReadInt32();
    this.Prf_Character.read(reader);
    this.Prf_Class.read(reader);
    this.Prf_Type.read(reader);
    this.Skills.read(reader);
    this.Status_Inflict.read(reader);
    this.Status_Remove.read(reader);
    this.Can_Sell = reader.ReadBoolean();
  }

  public virtual void write(BinaryWriter writer)
  {
    writer.Write(this.Id);
    writer.Write(this.Name);
    writer.Write(this.Full_Name);
    writer.Write(this.Description);
    writer.Write(this.Quick_Desc);
    writer.Write(this.Image_Name);
    writer.Write(this.Image_Index);
    writer.Write(this.Uses);
    writer.Write(this.Cost);
    this.Prf_Character.write(writer);
    this.Prf_Class.write(writer);
    this.Prf_Type.write(writer);
    this.Skills.write(writer);
    this.Status_Inflict.write(writer);
    this.Status_Remove.write(writer);
    writer.Write(this.Can_Sell);
  }

  public abstract string ToString(int uses_left);

  protected void copy_traits(Data_Equipment equipment)
  {
    this.Id = equipment.Id;
    this.Name = equipment.Name;
    this.Full_Name = equipment.Full_Name;
    this.Description = equipment.Description;
    this.Quick_Desc = equipment.Quick_Desc;
    this.Image_Name = equipment.Image_Name;
    this.Image_Index = equipment.Image_Index;
    this.Uses = equipment.Uses;
    this.Cost = equipment.Cost;
    this.Prf_Character = new List<int>((IEnumerable<int>) equipment.Prf_Character);
    this.Prf_Class = new List<int>((IEnumerable<int>) equipment.Prf_Class);
    this.Prf_Type = new List<int>((IEnumerable<int>) equipment.Prf_Type);
    this.Skills = new List<int>((IEnumerable<int>) equipment.Skills);
    this.Status_Inflict = new List<int>((IEnumerable<int>) equipment.Status_Inflict);
    this.Status_Remove = new List<int>((IEnumerable<int>) equipment.Status_Remove);
    this.Can_Sell = equipment.Can_Sell;
  }

  public string full_name() => this.Full_Name.Length > 0 ? this.Full_Name : this.Name;

  public int full_price() => this.Cost * Math.Max(1, this.Uses);

  public virtual bool is_weapon => false;

  public bool is_prf
  {
    get => this.Prf_Character.Count > 0 || this.Prf_Class.Count > 0 || this.Prf_Type.Count > 0;
  }
}
