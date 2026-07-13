// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Event_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Event_Data
{
  public string name;
  public int trigger;
  public int trigger_action;
  public int trigger_timing;
  public int trigger_turn;
  public List<Event_Control> data = new List<Event_Control>();

  public static Event_Data read(BinaryReader reader)
  {
    Event_Data eventData = new Event_Data()
    {
      data = new List<Event_Control>()
    };
    eventData.name = reader.ReadString();
    eventData.trigger = reader.ReadInt32();
    eventData.trigger_action = reader.ReadInt32();
    eventData.trigger_timing = reader.ReadInt32();
    eventData.trigger_turn = reader.ReadInt32();
    eventData.data.read(reader);
    return eventData;
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.name);
    writer.Write(this.trigger);
    writer.Write(this.trigger_action);
    writer.Write(this.trigger_timing);
    writer.Write(this.trigger_turn);
    this.data.write(writer);
  }

  public Event_Data copy()
  {
    return new Event_Data()
    {
      name = this.name,
      trigger = this.trigger,
      trigger_action = this.trigger_action,
      trigger_timing = this.trigger_timing,
      trigger_turn = this.trigger_turn,
      data = new List<Event_Control>((IEnumerable<Event_Control>) this.data)
    };
  }
}
