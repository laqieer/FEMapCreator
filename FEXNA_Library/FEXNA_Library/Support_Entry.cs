// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Support_Entry
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System.IO;

#nullable disable
namespace FEXNA_Library;

public struct Support_Entry
{
  public int Turns;
  public string Field_Convo;
  public string Base_Convo;

  public Support_Entry(int turns, string field_convo, string base_convo)
  {
    this.Turns = turns;
    this.Field_Convo = field_convo;
    this.Base_Convo = base_convo;
  }

  public Support_Entry(Support_Entry data)
  {
    this.Turns = data.Turns;
    this.Field_Convo = data.Field_Convo;
    this.Base_Convo = data.Base_Convo;
  }

  public static Support_Entry read(BinaryReader reader)
  {
    return new Support_Entry()
    {
      Turns = reader.ReadInt32(),
      Field_Convo = reader.ReadString(),
      Base_Convo = reader.ReadString()
    };
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.Turns);
    writer.Write(this.Field_Convo);
    writer.Write(this.Base_Convo);
  }
}
