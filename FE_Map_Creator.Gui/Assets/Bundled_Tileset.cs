using Avalonia.Media.Imaging;
using FEXNA_Library;
using System;

#nullable disable
namespace FE_Map_Creator.Gui.Assets;

public sealed class Bundled_Tileset : IDisposable
{
  public string Name { get; }

  public Bitmap Image { get; }

  public Tileset_Generation_Data Generation_Data { get; }

  public Data_Tileset Metadata { get; }

  public int Tile_Columns => this.Image.PixelSize.Width / 16;

  public int Tile_Rows => this.Image.PixelSize.Height / 16;

  public int Tile_Count => this.Tile_Columns * this.Tile_Rows;

  internal Bundled_Tileset(
    string name,
    Bitmap image,
    Tileset_Generation_Data generation_data,
    Data_Tileset metadata)
  {
    this.Name = name;
    this.Image = image;
    this.Generation_Data = generation_data;
    this.Metadata = metadata;
  }

  public void Dispose() => this.Image.Dispose();
}
