// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.SupportReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class SupportReader : ContentTypeReader<Data_Support>
{
  protected virtual Data_Support Read(ContentReader input, Data_Support existingInstance)
  {
    existingInstance = new Data_Support();
    existingInstance.Key = ((BinaryReader) input).ReadString();
    existingInstance.Id1 = ((BinaryReader) input).ReadInt32();
    existingInstance.Id2 = ((BinaryReader) input).ReadInt32();
    existingInstance.Supports.read((BinaryReader) input);
    return existingInstance;
  }
}
