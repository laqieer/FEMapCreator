#nullable disable
namespace FE_Map_Creator;

public sealed class Map_Read_Options
{
  public int? Width { get; set; }

  public int? Height { get; set; }

  public string Tileset { get; set; }
}

public sealed class Map_Write_Options
{
  public string Tileset { get; set; }

  public string Tileset_Image_Source { get; set; }

  public int First_Gid { get; set; } = 1;
}
