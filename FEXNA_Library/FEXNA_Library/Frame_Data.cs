// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Frame_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework;
using System.Collections.Generic;

#nullable disable
namespace FEXNA_Library;

public class Frame_Data
{
  public string name = string.Empty;
  public List<Vector2> offsets = new List<Vector2>();
  public List<Rectangle> src_rects = new List<Rectangle>();
}
