// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.SkillReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class SkillReader : ContentTypeReader<Data_Skill>
{
  protected virtual Data_Skill Read(ContentReader input, Data_Skill existingInstance)
  {
    existingInstance = new Data_Skill();
    existingInstance.Id = ((BinaryReader) input).ReadInt32();
    existingInstance.Name = ((BinaryReader) input).ReadString();
    existingInstance.Description = ((BinaryReader) input).ReadString();
    existingInstance.Abstract = ((BinaryReader) input).ReadString();
    existingInstance.Image_Name = ((BinaryReader) input).ReadString();
    existingInstance.Image_Index = ((BinaryReader) input).ReadInt32();
    existingInstance.Animation_Id = ((BinaryReader) input).ReadInt32();
    existingInstance.Map_Anim_Id = ((BinaryReader) input).ReadInt32();
    return existingInstance;
  }
}
