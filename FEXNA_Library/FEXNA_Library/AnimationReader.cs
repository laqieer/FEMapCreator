// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.AnimationReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class AnimationReader : ContentTypeReader<Battle_Animation_Data>
{
  protected virtual Battle_Animation_Data Read(
    ContentReader input,
    Battle_Animation_Data existingInstance)
  {
    existingInstance = new Battle_Animation_Data();
    existingInstance.read((BinaryReader) input);
    return existingInstance;
  }
}
