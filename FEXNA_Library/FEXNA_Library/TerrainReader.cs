// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.TerrainReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class TerrainReader : ContentTypeReader<Data_Terrain>
{
  protected virtual Data_Terrain Read(ContentReader input, Data_Terrain existingInstance)
  {
    existingInstance = new Data_Terrain();
    existingInstance.Id = ((BinaryReader) input).ReadInt32();
    existingInstance.Name = ((BinaryReader) input).ReadString();
    existingInstance.Avoid = ((BinaryReader) input).ReadInt32();
    existingInstance.Def = ((BinaryReader) input).ReadInt32();
    existingInstance.Res = ((BinaryReader) input).ReadInt32();
    existingInstance.Stats_Visible = ((BinaryReader) input).ReadBoolean();
    existingInstance.Step_Sound_Group = ((BinaryReader) input).ReadInt32();
    existingInstance.Platform_Rename = ((BinaryReader) input).ReadString();
    existingInstance.Background_Rename = ((BinaryReader) input).ReadString();
    existingInstance.Dust_Type = ((BinaryReader) input).ReadInt32();
    existingInstance.Fire_Through = ((BinaryReader) input).ReadBoolean();
    existingInstance.Move_Costs = existingInstance.Move_Costs.read((BinaryReader) input);
    if (((BinaryReader) input).ReadBoolean())
      existingInstance.Heal = existingInstance.Heal.read((BinaryReader) input);
    existingInstance.Minimap = ((BinaryReader) input).ReadInt32();
    existingInstance.Minimap_Group.read((BinaryReader) input);
    return existingInstance;
  }
}
