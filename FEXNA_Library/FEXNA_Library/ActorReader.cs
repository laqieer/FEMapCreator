// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.ActorReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class ActorReader : ContentTypeReader<Data_Actor>
{
  protected virtual Data_Actor Read(ContentReader input, Data_Actor existingInstance)
  {
    existingInstance = new Data_Actor();
    existingInstance.Id = ((BinaryReader) input).ReadInt32();
    existingInstance.Name = ((BinaryReader) input).ReadString();
    existingInstance.Description = ((BinaryReader) input).ReadString();
    existingInstance.ClassId = ((BinaryReader) input).ReadInt32();
    existingInstance.Level = ((BinaryReader) input).ReadInt32();
    existingInstance.BaseStats.read((BinaryReader) input);
    existingInstance.Growths.read((BinaryReader) input);
    existingInstance.WLvl.read((BinaryReader) input);
    existingInstance.Gender = ((BinaryReader) input).ReadInt32();
    existingInstance.Affinity = (Affinities) ((BinaryReader) input).ReadInt32();
    existingInstance.Items.read((BinaryReader) input);
    existingInstance.Supports.read((BinaryReader) input);
    existingInstance.Skills.read((BinaryReader) input);
    return existingInstance;
  }
}
