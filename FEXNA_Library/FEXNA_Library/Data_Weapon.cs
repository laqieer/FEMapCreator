// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Weapon
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ArrayExtension;
using System;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Data_Weapon : Data_Equipment
{
  public static readonly int PHYSICAL_TYPES = 4;
  public static readonly int MAGICAL_TYPES = 6;
  public static readonly int[] WLVL_THRESHOLDS = new int[7]
  {
    0,
    1,
    31 /*0x1F*/,
    71,
    121,
    181,
    251
  };
  public static readonly string[] WEAPON_TYPE_NAMES = new string[11]
  {
    \u0005.\u0002(-1223164542),
    \u0005.\u0002(-1223164485),
    \u0005.\u0002(-1223164497),
    \u0005.\u0002(-1223164509),
    \u0005.\u0002(-1223164455),
    \u0005.\u0002(-1223164465),
    \u0005.\u0002(-1223164478),
    \u0005.\u0002(-1223164426),
    \u0005.\u0002(-1223164435),
    \u0005.\u0002(-1223164447),
    \u0005.\u0002(-1223164396)
  };
  public static readonly string[] WLVL_LETTERS = new string[7]
  {
    \u0005.\u0002(-1223164408),
    \u0005.\u0002(-1223164416),
    \u0005.\u0002(-1223164360),
    \u0005.\u0002(-1223164368),
    \u0005.\u0002(-1223164376),
    \u0005.\u0002(-1223164384),
    \u0005.\u0002(-1223164328)
  };
  public static readonly int[] ANIMA_TYPES = new int[3]
  {
    5,
    6,
    7
  };
  public int Mgt;
  public int Hit;
  public int Crt;
  public int Wgt;
  public int Min_Range = 1;
  public int Max_Range = 1;
  public bool Mag_Range;
  public bool No_Counter;
  public bool Long_Range;
  public Weapon_Types Main_Type = Weapon_Types.Sword;
  public Weapon_Types Scnd_Type;
  public Weapon_Ranks Rank = Weapon_Ranks.E;
  public Attack_Types Attack_Type;
  public int WExp = 1;
  public int Staff_Exp;
  public bool[] Traits = new bool[10];
  public bool[] Staff_Traits = new bool[7];
  public int[] Effectiveness = new int[10]
  {
    1,
    1,
    1,
    1,
    1,
    1,
    1,
    1,
    1,
    1
  };

  public Data_Weapon()
  {
  }

  public Data_Weapon(Data_Weapon weapon)
  {
    this.copy_traits((Data_Equipment) weapon);
    this.Mgt = weapon.Mgt;
    this.Hit = weapon.Hit;
    this.Crt = weapon.Crt;
    this.Wgt = weapon.Wgt;
    this.Min_Range = weapon.Min_Range;
    this.Max_Range = weapon.Max_Range;
    this.Mag_Range = weapon.Mag_Range;
    this.No_Counter = weapon.No_Counter;
    this.Long_Range = weapon.Long_Range;
    this.Main_Type = weapon.Main_Type;
    this.Scnd_Type = weapon.Scnd_Type;
    this.Rank = weapon.Rank;
    this.Attack_Type = weapon.Attack_Type;
    this.WExp = weapon.WExp;
    this.Staff_Exp = weapon.Staff_Exp;
    this.Traits = new bool[weapon.Traits.Length];
    Array.Copy((Array) weapon.Traits, (Array) this.Traits, this.Traits.Length);
    this.Staff_Traits = new bool[weapon.Staff_Traits.Length];
    Array.Copy((Array) weapon.Staff_Traits, (Array) this.Staff_Traits, this.Staff_Traits.Length);
    this.Effectiveness = new int[weapon.Effectiveness.Length];
    Array.Copy((Array) weapon.Effectiveness, (Array) this.Effectiveness, this.Effectiveness.Length);
  }

  public static int WEAPON_TYPES => Data_Weapon.PHYSICAL_TYPES + Data_Weapon.MAGICAL_TYPES;

  public static Data_Weapon read(BinaryReader reader)
  {
    Data_Weapon dataWeapon = new Data_Weapon();
    dataWeapon.read_equipment(reader);
    dataWeapon.Mgt = reader.ReadInt32();
    dataWeapon.Hit = reader.ReadInt32();
    dataWeapon.Crt = reader.ReadInt32();
    dataWeapon.Wgt = reader.ReadInt32();
    dataWeapon.Min_Range = reader.ReadInt32();
    dataWeapon.Max_Range = reader.ReadInt32();
    dataWeapon.Mag_Range = reader.ReadBoolean();
    dataWeapon.No_Counter = reader.ReadBoolean();
    dataWeapon.Long_Range = reader.ReadBoolean();
    dataWeapon.Main_Type = (Weapon_Types) reader.ReadInt32();
    dataWeapon.Scnd_Type = (Weapon_Types) reader.ReadInt32();
    dataWeapon.Rank = (Weapon_Ranks) reader.ReadInt32();
    dataWeapon.Attack_Type = (Attack_Types) reader.ReadInt32();
    dataWeapon.WExp = reader.ReadInt32();
    dataWeapon.Staff_Exp = reader.ReadInt32();
    dataWeapon.Traits = dataWeapon.Traits.read(reader);
    dataWeapon.Staff_Traits = dataWeapon.Staff_Traits.read(reader);
    dataWeapon.Effectiveness = dataWeapon.Effectiveness.read(reader);
    return dataWeapon;
  }

  public override void write(BinaryWriter writer)
  {
    base.write(writer);
    writer.Write(this.Mgt);
    writer.Write(this.Hit);
    writer.Write(this.Crt);
    writer.Write(this.Wgt);
    writer.Write(this.Min_Range);
    writer.Write(this.Max_Range);
    writer.Write(this.Mag_Range);
    writer.Write(this.No_Counter);
    writer.Write(this.Long_Range);
    writer.Write((int) this.Main_Type);
    writer.Write((int) this.Scnd_Type);
    writer.Write((int) this.Rank);
    writer.Write((int) this.Attack_Type);
    writer.Write(this.WExp);
    writer.Write(this.Staff_Exp);
    this.Traits.write(writer);
    this.Staff_Traits.write(writer);
    this.Effectiveness.write(writer);
  }

  public override string ToString() => this.ToString(0);

  public override string ToString(int uses_left)
  {
    return string.Format(\u0005.\u0002(-1223164557), (object) this.full_name(), (object) this.Mgt, uses_left == 0 ? (object) this.Uses.ToString() : (object) string.Format(\u0005.\u0002(-1223164679), (object) uses_left, (object) this.Uses));
  }

  public override bool is_weapon => true;

  public string type => Data_Weapon.WEAPON_TYPE_NAMES[(int) this.Main_Type];

  public string rank
  {
    get
    {
      return this.Rank == Weapon_Ranks.None ? \u0005.\u0002(-1223164532) : Data_Weapon.WLVL_LETTERS[(int) this.Rank];
    }
  }

  public bool is_staff() => this.Main_Type == Weapon_Types.Staff;

  public bool is_magic()
  {
    int physicalTypes = Data_Weapon.PHYSICAL_TYPES;
    return this.Main_Type > (Weapon_Types) physicalTypes || this.Scnd_Type > (Weapon_Types) physicalTypes;
  }

  public bool is_always_magic()
  {
    int physicalTypes = Data_Weapon.PHYSICAL_TYPES;
    return (this.Main_Type > (Weapon_Types) physicalTypes || this.Scnd_Type > (Weapon_Types) physicalTypes) && this.Attack_Type == Attack_Types.Magical;
  }

  public bool is_ranged_magic()
  {
    int physicalTypes = Data_Weapon.PHYSICAL_TYPES;
    return (this.Main_Type > (Weapon_Types) physicalTypes || this.Scnd_Type > (Weapon_Types) physicalTypes) && this.Attack_Type == Attack_Types.Magic_At_Range;
  }

  public bool is_imbued()
  {
    int physicalTypes = Data_Weapon.PHYSICAL_TYPES;
    return this.Main_Type <= (Weapon_Types) physicalTypes && this.Scnd_Type > (Weapon_Types) physicalTypes;
  }

  public bool Thrown() => this.Traits[0];

  public bool Reaver() => this.Traits[1];

  public bool Brave() => this.Traits[2];

  public bool Cursed() => this.Traits[3];

  public bool Hits_All_in_Range() => this.Traits[4];

  public bool Ballista() => this.Traits[5];

  public bool Ignores_Pow() => this.Traits[6];

  public bool Drains_HP() => this.Traits[7];

  public bool Ignores_Def() => this.Traits[8];

  public bool Halves_HP() => this.Traits[9];

  public bool Heals() => this.Staff_Traits[0];

  public bool Torch() => this.Staff_Traits[1];

  public bool Unlock() => this.Staff_Traits[2];

  public bool Repair() => this.Staff_Traits[3];

  public bool Barrier() => this.Staff_Traits[4];

  public bool Rescue() => this.Staff_Traits[5];

  public bool Warp() => this.Staff_Traits[6];
}
