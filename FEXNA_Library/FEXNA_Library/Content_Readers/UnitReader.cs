// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Content_Readers.UnitReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using DictionaryExtension;
using ListExtension;
using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library.Content_Readers;

public class UnitReader : ContentTypeReader<Map_Unit_Data>
{
  protected virtual Map_Unit_Data Read(ContentReader input, Map_Unit_Data existingInstance)
  {
    existingInstance = new Map_Unit_Data();
    existingInstance.Units.read((BinaryReader) input);
    existingInstance.Reinforcements.read((BinaryReader) input);
    return existingInstance;
  }
}
