// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Tileset
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using DictionaryExtension;
using ListExtension;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Data_Tileset
{
  public int Id;
  public string Name = \u0005.\u0002(-1223164693);
  public string Graphic_Name = string.Empty;
  public List<string> Animated_Tile_Names = new List<string>();
  public List<Rectangle> Animated_Tile_Data = new List<Rectangle>();
  public Dictionary<Vector2, Vector2> Pillage_Tile_Changes = new Dictionary<Vector2, Vector2>();
  public List<int> Terrain_Tags = new List<int>();

  public Data_Tileset()
  {
  }

  public Data_Tileset(Data_Tileset tileset)
  {
    this.Id = tileset.Id;
    this.Name = tileset.Name;
    this.Graphic_Name = tileset.Graphic_Name;
    this.Animated_Tile_Names = new List<string>((IEnumerable<string>) tileset.Animated_Tile_Names);
    this.Animated_Tile_Data = new List<Rectangle>((IEnumerable<Rectangle>) tileset.Animated_Tile_Data);
    this.Pillage_Tile_Changes = new Dictionary<Vector2, Vector2>((IDictionary<Vector2, Vector2>) tileset.Pillage_Tile_Changes);
    this.Terrain_Tags = new List<int>((IEnumerable<int>) tileset.Terrain_Tags);
  }

  public static Data_Tileset read(BinaryReader reader)
  {
    Data_Tileset dataTileset = new Data_Tileset();
    dataTileset.Id = reader.ReadInt32();
    dataTileset.Name = reader.ReadString();
    dataTileset.Graphic_Name = reader.ReadString();
    dataTileset.Animated_Tile_Names.read(reader);
    dataTileset.Animated_Tile_Data.read(reader);
    dataTileset.Pillage_Tile_Changes.read(reader);
    dataTileset.Terrain_Tags.read(reader);
    return dataTileset;
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.Id);
    writer.Write(this.Name);
    writer.Write(this.Graphic_Name);
    this.Animated_Tile_Names.write(writer);
    this.Animated_Tile_Data.write(writer);
    this.Pillage_Tile_Changes.write(writer);
    this.Terrain_Tags.write(writer);
  }
}
