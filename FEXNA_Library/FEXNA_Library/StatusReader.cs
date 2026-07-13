// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.StatusReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class StatusReader : ContentTypeReader<Data_Status>
{
  protected virtual Data_Status Read(ContentReader input, Data_Status existingInstance)
  {
    existingInstance = new Data_Status();
    existingInstance.Id = ((BinaryReader) input).ReadInt32();
    existingInstance.Name = ((BinaryReader) input).ReadString();
    existingInstance.Description = ((BinaryReader) input).ReadString();
    existingInstance.Turns = ((BinaryReader) input).ReadInt32();
    existingInstance.Negative = ((BinaryReader) input).ReadBoolean();
    existingInstance.Damage_Per_Turn = (float) ((BinaryReader) input).ReadDouble();
    existingInstance.Unselectable = ((BinaryReader) input).ReadBoolean();
    existingInstance.Ai_Controlled = ((BinaryReader) input).ReadBoolean();
    existingInstance.Attacks_Allies = ((BinaryReader) input).ReadBoolean();
    existingInstance.No_Magic = ((BinaryReader) input).ReadBoolean();
    existingInstance.Skills.read((BinaryReader) input);
    existingInstance.Image_Index = ((BinaryReader) input).ReadInt32();
    existingInstance.Map_Anim_Id = ((BinaryReader) input).ReadInt32();
    existingInstance.Battle_Color = input.ReadColor();
    return existingInstance;
  }
}
