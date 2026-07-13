// Decompiled with JetBrains decompiler
// Type: RectangleExtension.Extension
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework;
using System.IO;

#nullable disable
namespace RectangleExtension;

public static class Extension
{
  public static void write(this Rectangle rect, BinaryWriter writer)
  {
    writer.Write(rect.X);
    writer.Write(rect.Y);
    writer.Write(rect.Width);
    writer.Write(rect.Height);
  }

  public static Rectangle read(this Rectangle rect, BinaryReader reader)
  {
    rect.X = reader.ReadInt32();
    rect.Y = reader.ReadInt32();
    rect.Width = reader.ReadInt32();
    rect.Height = reader.ReadInt32();
    return rect;
  }
}
