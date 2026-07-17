using System.Collections.Generic;
using System.IO;
using FEXNA_Library;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Resolves a bundled tileset's terrain metadata (<see cref="Data_Tileset"/>) by asset
/// name, matching <c>Tileset_Data.xml</c>'s Graphic_Name entries.
/// </summary>
internal static class Terrain_Resolver
{
  /// <summary>
  /// Finds terrain metadata for a resolved asset name, trying (in order) the
  /// "description" segment of a "Family - Description - Identifier" name, the name
  /// without its trailing identifier, the full asset name, and the trailing identifier
  /// itself, against <c>Tileset_Data.xml</c>'s Graphic_Name entries.
  /// </summary>
  internal static Data_Tileset find(
    IReadOnlyDictionary<int, Data_Tileset> tilesets,
    Tileset_Metadata_Reader reader,
    string asset_name)
  {
    if (string.IsNullOrWhiteSpace(asset_name))
      return null;
    return reader.find_for_asset_name(tilesets, asset_name);
  }

  internal static Dictionary<int, Data_Tileset> read_metadata(string assets_root, Tileset_Metadata_Reader reader)
  {
    string path = Path.Combine(assets_root, "Tileset_Data.xml");
    if (!File.Exists(path))
      return new Dictionary<int, Data_Tileset>();
    return reader.read(path);
  }
}
