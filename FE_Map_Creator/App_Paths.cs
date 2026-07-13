using System;
using System.IO;

#nullable disable
namespace FE_Map_Creator;

internal static class App_Paths
{
  internal static string Base_Directory => AppContext.BaseDirectory;

  internal static string Terrain_Data_File => Path.Combine(Base_Directory, "Terrain_Data.xml");

  internal static string Terrain_Images_File => Path.Combine(Base_Directory, "Terrain_Images.png");

  internal static string Tileset_Data_File => Path.Combine(Base_Directory, "Tileset_Data.xml");

  internal static string Tilesets_Directory => Path.Combine(Base_Directory, "Tilesets");

  internal static string Tileset_Generation_Directory => Path.Combine(Base_Directory, "Tileset Generation Data");

  internal static string tileset_generation_file(string tileset_filename)
  {
    return Path.Combine(Tileset_Generation_Directory, $"{Path.GetFileNameWithoutExtension(tileset_filename)}.dat");
  }
}
