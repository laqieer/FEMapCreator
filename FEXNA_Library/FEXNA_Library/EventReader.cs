// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.EventReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class EventReader : ContentTypeReader<Map_Event_Data>
{
  protected virtual Map_Event_Data Read(ContentReader input, Map_Event_Data existingInstance)
  {
    existingInstance = new Map_Event_Data();
    existingInstance.Events.read((BinaryReader) input);
    return existingInstance;
  }
}
