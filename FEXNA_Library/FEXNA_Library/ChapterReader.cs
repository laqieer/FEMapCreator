// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.ChapterReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework.Content;

#nullable disable
namespace FEXNA_Library;

public class ChapterReader : ContentTypeReader<Data_Chapter>
{
  protected virtual Data_Chapter Read(ContentReader input, Data_Chapter existingInstance)
  {
    existingInstance = new Data_Chapter();
    existingInstance.\u0002(input);
    return existingInstance;
  }
}
