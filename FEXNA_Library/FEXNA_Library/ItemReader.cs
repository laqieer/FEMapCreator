// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.ItemReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class ItemReader : ContentTypeReader<Data_Item>
{
  protected virtual Data_Item Read(ContentReader input, Data_Item existingInstance)
  {
    existingInstance = Data_Item.read((BinaryReader) input);
    return existingInstance;
  }
}
