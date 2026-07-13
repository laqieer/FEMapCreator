// Decompiled with JetBrains decompiler
// Type: Vector2Extension.Extension
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework;
using System.IO;

#nullable disable
namespace Vector2Extension;

public static class Extension
{
  public static void write(this Vector2 vector, BinaryWriter writer)
  {
    writer.Write((double) vector.X);
    writer.Write((double) vector.Y);
  }

  public static Vector2 read(this Vector2 vector, BinaryReader reader)
  {
    vector.X = (float) reader.ReadDouble();
    vector.Y = (float) reader.ReadDouble();
    return vector;
  }
}
