using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    string[] candidates =
    {
      Asset_Naming.description(asset_name),
      Asset_Naming.name_without_identifier(asset_name),
      asset_name,
      Asset_Naming.identifier(asset_name),
    };
    foreach (string candidate in candidates.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct())
    {
      Data_Tileset match = reader.find_by_graphic_name(tilesets, candidate);
      if (match != null)
        return match;
    }
    return null;
  }

  internal static Dictionary<int, Data_Tileset> read_metadata(string assets_root, Tileset_Metadata_Reader reader)
  {
    string path = Path.Combine(assets_root, "Tileset_Data.xml");
    if (!File.Exists(path))
      return new Dictionary<int, Data_Tileset>();
    return reader.read(path);
  }
}
