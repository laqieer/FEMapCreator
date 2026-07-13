using System;
using System.Collections.Generic;
using System.IO;
using FEXNA_Library;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>Everything resolved from a tileset selector: the bundled asset paths, the
/// binary generation weights read from its .dat file, and (optionally) its terrain
/// metadata from Tileset_Data.xml.</summary>
internal sealed class Resolved_Tileset
{
  internal Tileset_Asset Asset { get; init; }
  internal Tileset_Generation_Data Generation_Data { get; init; }
  internal Data_Tileset Terrain_Metadata { get; init; }
}

/// <summary>
/// Resolves a tileset selector (plus optional explicit PNG/DAT overrides) against a
/// <see cref="Tileset_Catalog"/> rooted at --assets-dir/spec/AppContext.BaseDirectory,
/// then loads its binary generation data and (if present) terrain metadata.
/// </summary>
internal static class Asset_Resolution
{
  internal static Resolved_Tileset resolve(
    string assets_dir_cli,
    string assets_dir_spec,
    string spec_directory,
    string tileset_selector,
    string tileset_image_cli,
    string tileset_image_spec,
    string generation_data_cli,
    string generation_data_spec,
    bool require_image)
  {
    string assets_root = Job_Merge.resolve_path(assets_dir_cli, assets_dir_spec, spec_directory)
      ?? AppContext.BaseDirectory;
    string image_override = Job_Merge.resolve_path(tileset_image_cli, tileset_image_spec, spec_directory);
    string data_override = Job_Merge.resolve_path(generation_data_cli, generation_data_spec, spec_directory);

    Tileset_Catalog catalog = new Tileset_Catalog(assets_root);
    Tileset_Asset asset = catalog.resolve(
      tileset_selector, image_override, data_override, require_image, require_generation_data: true);

    Tileset_Generation_Data generation_data;
    using (FileStream stream = new FileStream(asset.Generation_Data_Path, FileMode.Open, FileAccess.Read, FileShare.Read))
    using (BinaryReader reader = new BinaryReader(stream))
      generation_data = Tileset_Generation_Data.read(reader);

    Tileset_Metadata_Reader metadata_reader = new Tileset_Metadata_Reader();
    Dictionary<int, Data_Tileset> terrain_catalog = Terrain_Resolver.read_metadata(assets_root, metadata_reader);
    Data_Tileset terrain_metadata = Terrain_Resolver.find(terrain_catalog, metadata_reader, asset.Name);

    return new Resolved_Tileset
    {
      Asset = asset,
      Generation_Data = generation_data,
      Terrain_Metadata = terrain_metadata,
    };
  }

  /// <summary>
  /// Throws a clear error if the map has a nonzero terrain constraint but no terrain
  /// metadata was resolved, since <see cref="Map_Generation_Engine"/> silently ignores
  /// every terrain constraint whenever its terrain_tileset argument is null.
  /// </summary>
  internal static void require_terrain_metadata_if_constrained(
    int[,] terrain, int width, int height, Data_Tileset terrain_metadata, string asset_name)
  {
    if (terrain_metadata != null)
      return;
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
      {
        if (terrain[x, y] != 0)
        {
          throw new InvalidOperationException(
            $"Cell ({x},{y}) has a terrain constraint, but no terrain metadata for tileset \"{asset_name}\" " +
            "was found in Tileset_Data.xml. Terrain constraints would otherwise be silently ignored.");
        }
      }
    }
  }
}
