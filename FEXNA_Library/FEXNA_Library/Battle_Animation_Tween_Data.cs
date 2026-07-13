// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Battle_Animation_Tween_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System.IO;

#nullable disable
namespace FEXNA_Library;

public struct Battle_Animation_Tween_Data
{
  public int layer;
  public Frame_Tween_Types data;
  public Frame_Tween_Functions function;
  public int start_frame;
  public int end_frame;
  public Frame_Tween_Intervals interval_type;
  public int interval;
  public float magnitude;
  public int period;
  public int offset;

  public static Battle_Animation_Tween_Data read(BinaryReader reader)
  {
    return new Battle_Animation_Tween_Data()
    {
      layer = reader.ReadInt32(),
      data = (Frame_Tween_Types) reader.ReadInt32(),
      function = (Frame_Tween_Functions) reader.ReadInt32(),
      start_frame = reader.ReadInt32(),
      end_frame = reader.ReadInt32(),
      interval_type = (Frame_Tween_Intervals) reader.ReadInt32(),
      interval = reader.ReadInt32(),
      magnitude = (float) reader.ReadDouble(),
      period = reader.ReadInt32(),
      offset = reader.ReadInt32()
    };
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.layer);
    writer.Write((int) this.data);
    writer.Write((int) this.function);
    writer.Write(this.start_frame);
    writer.Write(this.end_frame);
    writer.Write((int) this.interval_type);
    writer.Write(this.interval);
    writer.Write((double) this.magnitude);
    writer.Write(this.period);
    writer.Write(this.offset);
  }
}
