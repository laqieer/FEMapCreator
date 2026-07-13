// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.MapRecolorReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class MapRecolorReader : ContentTypeReader<MapSpriteRecolorData>
{
  protected virtual MapSpriteRecolorData Read(
    ContentReader input,
    MapSpriteRecolorData existingInstance)
  {
    existingInstance = new MapSpriteRecolorData();
    existingInstance.data = new Dictionary<Color, List<Color>>();
    int num1 = ((BinaryReader) input).ReadInt32();
    for (int index1 = 0; index1 < num1; ++index1)
    {
      Color key = input.ReadColor();
      int num2 = ((BinaryReader) input).ReadInt32();
      List<Color> colorList = new List<Color>();
      for (int index2 = 0; index2 < num2; ++index2)
        colorList.Add(input.ReadColor());
      existingInstance.data.Add(key, colorList);
    }
    return existingInstance;
  }
}
