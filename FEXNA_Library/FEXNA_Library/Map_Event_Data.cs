// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Map_Event_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Map_Event_Data
{
  public List<Event_Data> Events = new List<Event_Data>();

  public void write(BinaryWriter writer) => this.Events.write(writer);

  public static Map_Event_Data read(BinaryReader reader)
  {
    Map_Event_Data mapEventData = new Map_Event_Data();
    mapEventData.Events.read(reader);
    return mapEventData;
  }

  public Map_Event_Data copy()
  {
    Map_Event_Data mapEventData = new Map_Event_Data();
    foreach (Event_Data eventData in this.Events)
      mapEventData.Events.Add(eventData.copy());
    return mapEventData;
  }
}
