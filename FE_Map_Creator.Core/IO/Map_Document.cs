using System;

#nullable disable
namespace FE_Map_Creator;

public sealed class Map_Document
{
  public int[,] Tiles { get; }

  public string Tileset { get; set; }

  public string Tileset_Image_Source { get; set; }

  public int Width => this.Tiles.GetLength(0);

  public int Height => this.Tiles.GetLength(1);

  public Map_Document(int[,] tiles, string tileset = "")
  {
    if (tiles == null)
      throw new ArgumentNullException(nameof (tiles));
    if (tiles.GetLength(0) <= 0 || tiles.GetLength(1) <= 0)
      throw new ArgumentException("Map dimensions must be positive.", nameof (tiles));
    this.Tiles = tiles;
    this.Tileset = tileset ?? "";
    this.Tileset_Image_Source = "";
  }

  public Map_Document clone()
  {
    int[,] tiles = new int[this.Width, this.Height];
    Array.Copy((Array) this.Tiles, (Array) tiles, this.Tiles.Length);
    return new Map_Document(tiles, this.Tileset)
    {
      Tileset_Image_Source = this.Tileset_Image_Source
    };
  }
}
