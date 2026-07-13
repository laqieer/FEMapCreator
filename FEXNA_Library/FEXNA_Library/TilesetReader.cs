// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.TilesetReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using DictionaryExtension;
using ListExtension;
using Microsoft.Xna.Framework.Content;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class TilesetReader : ContentTypeReader<Data_Tileset>
{
  protected virtual Data_Tileset Read(ContentReader input, Data_Tileset existingInstance)
  {
    existingInstance = new Data_Tileset();
    existingInstance.Id = ((BinaryReader) input).ReadInt32();
    existingInstance.Name = ((BinaryReader) input).ReadString();
    existingInstance.Graphic_Name = ((BinaryReader) input).ReadString();
    existingInstance.Animated_Tile_Names.read((BinaryReader) input);
    existingInstance.Animated_Tile_Data.read((BinaryReader) input);
    existingInstance.Pillage_Tile_Changes.read((BinaryReader) input);
    existingInstance.Terrain_Tags.read((BinaryReader) input);
    return existingInstance;
  }
}
