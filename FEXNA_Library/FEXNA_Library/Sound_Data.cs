// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Sound_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System.IO;

#nullable disable
namespace FEXNA_Library;

public struct Sound_Data(int key, string value)
{
  public int Key = key;
  public string Value = value;

  public static Sound_Data read(BinaryReader reader)
  {
    return new Sound_Data(reader.ReadInt32(), reader.ReadString());
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.Key);
    writer.Write(this.Value);
  }
}
