// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Item_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Item_Data
{
  public Item_Data_Type Type;
  public int Id;
  public int Uses;
  private static IEquipmentService \u0002;

  public Item_Data()
  {
  }

  public Item_Data(Item_Data_Type type, int id, int uses)
  {
    this.Type = type;
    this.Id = id;
    this.Uses = uses;
  }

  public Item_Data(int type, int id, int uses)
    : this((Item_Data_Type) type, id, uses)
  {
  }

  public Item_Data(Item_Data data)
    : this(data.Type, data.Id, data.Uses)
  {
  }

  internal static IEquipmentService \u0002() => Item_Data.\u0002;

  private static void \u0002(IEquipmentService _param0) => Item_Data.\u0002 = _param0;

  public static IEquipmentService equipment_data
  {
    get => Item_Data.\u0002();
    set
    {
      if (Item_Data.\u0002() != null)
        return;
      Item_Data.\u0002(value);
    }
  }

  public Data_Equipment to_equipment
  {
    get => Item_Data.\u0002() != null ? Item_Data.\u0002().equipment(this) : (Data_Equipment) null;
  }

  public Data_Weapon to_weapon
  {
    get => this.is_weapon ? (Data_Weapon) this.to_equipment : (Data_Weapon) null;
  }

  public Data_Item to_item => this.is_item ? (Data_Item) this.to_equipment : (Data_Item) null;

  public string name => this.to_equipment.Name;

  public int max_uses => this.to_equipment.Uses;

  public int cost => this.to_equipment.Cost;

  public static Item_Data read(BinaryReader reader)
  {
    return reader.ReadInt32() == 3 ? new Item_Data(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32()) : new Item_Data(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(3);
    writer.Write((int) this.Type);
    writer.Write(this.Id);
    writer.Write(this.Uses);
  }

  public override string ToString()
  {
    if (this.blank_item)
      return \u0005.\u0002(-1223164647);
    return this.to_equipment != null ? this.to_equipment.ToString(this.Uses) : base.ToString();
  }

  public bool is_weapon => this.Id > 0 && this.Type == Item_Data_Type.Weapon;

  public bool is_item => this.Type == Item_Data_Type.Item;

  public bool blank_item => this.Type == Item_Data_Type.Weapon && this.Id == 0 && this.Uses == 0;

  public bool non_equipment => this.Id <= 0 || this.to_equipment == null;

  public bool same_item(Item_Data item) => this.Type == item.Type && this.Id == item.Id;

  public void repair(bool repair_fully = true, int uses = -1)
  {
    Data_Equipment toEquipment = this.to_equipment;
    if (toEquipment == null)
      return;
    if (repair_fully)
    {
      this.Uses = toEquipment.Uses;
    }
    else
    {
      if (toEquipment.Uses == -1)
        return;
      this.Uses = Math.Max(0, Math.Min(this.Uses + uses, toEquipment.Uses));
    }
  }
}
