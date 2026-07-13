// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.MapReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class MapReader : ContentTypeReader<Data_Map>
{
  protected virtual Data_Map Read(ContentReader input, Data_Map existingInstance)
  {
    int tileset = ((BinaryReader) input).ReadInt32();
    int length1 = ((BinaryReader) input).ReadInt32();
    int length2 = ((BinaryReader) input).ReadInt32();
    int[,] values = new int[length1, length2];
    for (int index1 = 0; index1 < length2; ++index1)
    {
      for (int index2 = 0; index2 < length1; ++index2)
        values[index2, index1] = ((BinaryReader) input).ReadInt32();
    }
    return new Data_Map(values, tileset);
  }
}
