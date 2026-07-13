// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Item
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#nullable disable
namespace FEXNA_Library;

public class Data_Item : Data_Equipment
{
  public int Heal_Val = 10;
  public float Heal_Percent;
  public bool Door_Key;
  public bool Chest_Key;
  public bool Dancer_Ring;
  public int Torch_Radius;
  public Placeables Placeable;
  [ContentSerializer(Optional = true)]
  public int Repair_Val;
  [ContentSerializer(ElementName = "Repair_Rate")]
  public float Repair_Percent;
  public string Boost_Text = string.Empty;
  public int[] Stat_Boost = new int[11];
  public int[] Growth_Boost = new int[7];
  public int[] Stat_Buff = new int[8];
  public List<int> Promotes = new List<int>();

  public Data_Item()
  {
  }

  public Data_Item(Data_Item item)
  {
    this.copy_traits((Data_Equipment) item);
    this.Heal_Val = item.Heal_Val;
    this.Heal_Percent = item.Heal_Percent;
    this.Door_Key = item.Door_Key;
    this.Chest_Key = item.Chest_Key;
    this.Dancer_Ring = item.Dancer_Ring;
    this.Torch_Radius = item.Torch_Radius;
    this.Placeable = item.Placeable;
    this.Repair_Val = item.Repair_Val;
    this.Repair_Percent = item.Repair_Percent;
    this.Boost_Text = item.Boost_Text;
    this.Stat_Boost = new int[item.Stat_Boost.Length];
    Array.Copy((Array) item.Stat_Boost, (Array) this.Stat_Boost, this.Stat_Boost.Length);
    this.Growth_Boost = new int[item.Growth_Boost.Length];
    Array.Copy((Array) item.Growth_Boost, (Array) this.Growth_Boost, this.Growth_Boost.Length);
    this.Stat_Buff = new int[item.Stat_Buff.Length];
    Array.Copy((Array) item.Stat_Buff, (Array) this.Stat_Buff, this.Stat_Buff.Length);
    this.Promotes = new List<int>((IEnumerable<int>) item.Promotes);
  }

  public static Data_Item read(BinaryReader reader)
  {
    Data_Item dataItem = new Data_Item();
    dataItem.read_equipment(reader);
    dataItem.Heal_Val = reader.ReadInt32();
    dataItem.Heal_Percent = (float) reader.ReadDouble();
    dataItem.Door_Key = reader.ReadBoolean();
    dataItem.Chest_Key = reader.ReadBoolean();
    dataItem.Dancer_Ring = reader.ReadBoolean();
    dataItem.Torch_Radius = reader.ReadInt32();
    dataItem.Placeable = (Placeables) reader.ReadInt32();
    dataItem.Repair_Val = reader.ReadInt32();
    dataItem.Repair_Percent = (float) reader.ReadDouble();
    dataItem.Boost_Text = reader.ReadString();
    dataItem.Stat_Boost = dataItem.Stat_Boost.read(reader);
    dataItem.Growth_Boost = dataItem.Growth_Boost.read(reader);
    dataItem.Stat_Buff = dataItem.Stat_Buff.read(reader);
    dataItem.Promotes.read(reader);
    return dataItem;
  }

  public override void write(BinaryWriter writer)
  {
    base.write(writer);
    writer.Write(this.Heal_Val);
    writer.Write((double) this.Heal_Percent);
    writer.Write(this.Door_Key);
    writer.Write(this.Chest_Key);
    writer.Write(this.Dancer_Ring);
    writer.Write(this.Torch_Radius);
    writer.Write((int) this.Placeable);
    writer.Write(this.Repair_Val);
    writer.Write((double) this.Repair_Percent);
    writer.Write(this.Boost_Text);
    this.Stat_Boost.write(writer);
    this.Growth_Boost.write(writer);
    this.Stat_Buff.write(writer);
    this.Promotes.write(writer);
  }

  public override string ToString() => this.ToString(0);

  public override string ToString(int uses_left)
  {
    return string.Format(\u0005.\u0002(-1223164717), (object) this.full_name(), uses_left == 0 ? (object) this.Uses.ToString() : (object) string.Format(\u0005.\u0002(-1223164679), (object) uses_left, (object) this.Uses));
  }

  public bool can_heal_hp() => this.Heal_Val > 0 || (double) this.Heal_Percent > 0.0;

  public bool is_for_healing()
  {
    return this.Heal_Val > 0 || (double) this.Heal_Percent > 0.0 || this.Status_Remove.Count > 0;
  }

  public bool can_heal(bool hp_full, List<int> statuses)
  {
    return !hp_full && (this.Heal_Val > 0 || (double) this.Heal_Percent > 0.0) || this.Status_Remove.Intersect<int>((IEnumerable<int>) statuses).Any<int>();
  }

  public bool can_repair => this.Repair_Val > 0 || (double) this.Repair_Percent > 0.0;

  public bool is_stat_booster()
  {
    foreach (int num in this.Stat_Boost)
    {
      if (num > 0)
        return true;
    }
    return false;
  }

  public bool is_growth_booster()
  {
    foreach (int num in this.Growth_Boost)
    {
      if (num > 0)
        return true;
    }
    return false;
  }

  public bool is_stat_buffer()
  {
    foreach (int num in this.Stat_Buff)
    {
      if (num > 0)
        return true;
    }
    return false;
  }

  public bool targets_inventory() => this.can_repair;

  public bool can_target_item(Item_Data item_data)
  {
    if (this.can_repair && item_data.is_weapon)
    {
      Data_Weapon toWeapon = item_data.to_weapon;
      if (!toWeapon.is_staff() && toWeapon.Uses > 0 && item_data.Uses < toWeapon.Uses)
        return true;
    }
    return false;
  }
}
