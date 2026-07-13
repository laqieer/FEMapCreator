// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Unit
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System.IO;

#nullable disable
namespace FEXNA_Library;

public struct Data_Unit(string type, string identifier, string data)
{
  public string type = type;
  public string identifier = identifier;
  public string data = data;

  public static Data_Unit read(BinaryReader reader)
  {
    return new Data_Unit(reader.ReadString(), reader.ReadString(), reader.ReadString());
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.type);
    writer.Write(this.identifier);
    writer.Write(this.data);
  }

  public void reset(Data_Unit unit)
  {
    this.type = unit.type;
    this.identifier = unit.identifier;
    this.data = unit.data;
  }
}
