// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Event_Control
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ArrayExtension;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public struct Event_Control(int key, string[] value)
{
  public int Key = key;
  public string[] Value = value;

  public override string ToString()
  {
    return string.Format(\u0005.\u0002(-1223164889), (object) this.Key, (object) this.Value.Length);
  }

  public static Event_Control read(BinaryReader reader)
  {
    return new Event_Control(reader.ReadInt32(), new string[0].read(reader));
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.Key);
    this.Value.write(writer);
  }
}
