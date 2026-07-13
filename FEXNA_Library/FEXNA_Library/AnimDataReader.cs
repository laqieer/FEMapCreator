// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.AnimDataReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class AnimDataReader : ContentTypeReader<Frame_Data>
{
  protected virtual Frame_Data Read(ContentReader input, Frame_Data existingInstance)
  {
    existingInstance = new Frame_Data();
    existingInstance.name = ((BinaryReader) input).ReadString();
    existingInstance.offsets.read((BinaryReader) input);
    existingInstance.src_rects.read((BinaryReader) input);
    return existingInstance;
  }
}
