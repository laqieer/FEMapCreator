// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Support
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Data_Support
{
  public string Key;
  public int Id1;
  public int Id2;
  public List<Support_Entry> Supports = new List<Support_Entry>();

  public Data_Support()
  {
  }

  public Data_Support(Data_Support data)
  {
    this.Key = data.Key;
    this.Id1 = data.Id1;
    this.Id2 = data.Id2;
    this.Supports = new List<Support_Entry>((IEnumerable<Support_Entry>) data.Supports);
  }

  public static Data_Support read(BinaryReader reader)
  {
    Data_Support dataSupport = new Data_Support();
    dataSupport.Key = reader.ReadString();
    dataSupport.Id1 = reader.ReadInt32();
    dataSupport.Id2 = reader.ReadInt32();
    dataSupport.Supports.read(reader);
    return dataSupport;
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.Key);
    writer.Write(this.Id1);
    writer.Write(this.Id2);
    this.Supports.write(writer);
  }

  public override string ToString()
  {
    return string.Format(\u0005.\u0002(-1223164748), (object) this.Key, (object) this.Id1, (object) this.Id2);
  }
}
