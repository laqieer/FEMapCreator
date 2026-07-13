// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.WeaponReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class WeaponReader : ContentTypeReader<Data_Weapon>
{
  protected virtual Data_Weapon Read(ContentReader input, Data_Weapon existingInstance)
  {
    existingInstance = new Data_Weapon();
    existingInstance.Id = ((BinaryReader) input).ReadInt32();
    existingInstance.Name = ((BinaryReader) input).ReadString();
    existingInstance.Full_Name = ((BinaryReader) input).ReadString();
    existingInstance.Description = ((BinaryReader) input).ReadString();
    existingInstance.Quick_Desc = ((BinaryReader) input).ReadString();
    existingInstance.Image_Name = ((BinaryReader) input).ReadString();
    existingInstance.Image_Index = ((BinaryReader) input).ReadInt32();
    existingInstance.Mgt = ((BinaryReader) input).ReadInt32();
    existingInstance.Hit = ((BinaryReader) input).ReadInt32();
    existingInstance.Crt = ((BinaryReader) input).ReadInt32();
    existingInstance.Wgt = ((BinaryReader) input).ReadInt32();
    existingInstance.Uses = ((BinaryReader) input).ReadInt32();
    existingInstance.Min_Range = ((BinaryReader) input).ReadInt32();
    existingInstance.Max_Range = ((BinaryReader) input).ReadInt32();
    existingInstance.Mag_Range = ((BinaryReader) input).ReadBoolean();
    existingInstance.No_Counter = ((BinaryReader) input).ReadBoolean();
    existingInstance.Long_Range = ((BinaryReader) input).ReadBoolean();
    existingInstance.Main_Type = (Weapon_Types) ((BinaryReader) input).ReadInt32();
    existingInstance.Scnd_Type = (Weapon_Types) ((BinaryReader) input).ReadInt32();
    existingInstance.Rank = (Weapon_Ranks) ((BinaryReader) input).ReadInt32();
    existingInstance.Attack_Type = (Attack_Types) ((BinaryReader) input).ReadInt32();
    existingInstance.Cost = ((BinaryReader) input).ReadInt32();
    existingInstance.WExp = ((BinaryReader) input).ReadInt32();
    existingInstance.Staff_Exp = ((BinaryReader) input).ReadInt32();
    existingInstance.Skills.read((BinaryReader) input);
    existingInstance.Traits = existingInstance.Traits.read((BinaryReader) input);
    existingInstance.Staff_Traits = existingInstance.Staff_Traits.read((BinaryReader) input);
    existingInstance.Effectiveness = existingInstance.Effectiveness.read((BinaryReader) input);
    existingInstance.Status_Inflict.read((BinaryReader) input);
    existingInstance.Status_Remove.read((BinaryReader) input);
    existingInstance.Prf_Character.read((BinaryReader) input);
    existingInstance.Prf_Class.read((BinaryReader) input);
    existingInstance.Prf_Type.read((BinaryReader) input);
    existingInstance.Can_Sell = ((BinaryReader) input).ReadBoolean();
    return existingInstance;
  }
}
