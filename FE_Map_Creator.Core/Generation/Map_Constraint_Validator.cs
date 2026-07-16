using FEXNA_Library;
using System;
using System.Collections.Generic;

#nullable disable
namespace FE_Map_Creator.Generation;

public static class Map_Constraint_Validator
{
  public static Map_Validation_Result validate(
    int[,] tiles,
    Tileset_Generation_Data generation_data,
    Data_Tileset terrain_tileset = null,
    int[,] terrain = null,
    Map_Generation_Algorithm algorithm = Map_Generation_Algorithm.Experimental_Constraint)
  {
    if (tiles == null)
      throw new ArgumentNullException(nameof(tiles));
    if (generation_data == null)
      throw new ArgumentNullException(nameof(generation_data));
    int width = tiles.GetLength(0);
    int height = tiles.GetLength(1);
    if (terrain != null
      && (terrain.GetLength(0) != width || terrain.GetLength(1) != height))
      throw new ArgumentException("Terrain dimensions must match the tile grid.", nameof(terrain));

    List<string> errors = new List<string>();
    int checked_adjacencies = 0;
    int skipped_zero_cells = 0;
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
      {
        int tile = tiles[x, y];
        if (tile == 0)
        {
          ++skipped_zero_cells;
          continue;
        }
        if (!try_canonical(generation_data, tile, out short canonical)
          || !generation_data.generation_data.ContainsKey(canonical))
        {
          errors.Add($"Cell ({x},{y}) uses tile {tile}, which is absent from generation data.");
          continue;
        }
        if (terrain != null && terrain[x, y] != 0)
          validate_terrain(errors, terrain_tileset, terrain[x, y], tile, x, y);
        if (x + 1 < width && tiles[x + 1, y] != 0)
        {
          ++checked_adjacencies;
          validate_edge(
            errors,
            generation_data,
            tile,
            tiles[x + 1, y],
            6,
            x,
            y,
            x + 1,
            y,
            algorithm);
        }
        if (y + 1 < height && tiles[x, y + 1] != 0)
        {
          ++checked_adjacencies;
          validate_edge(
            errors,
            generation_data,
            tile,
            tiles[x, y + 1],
            2,
            x,
            y,
            x,
            y + 1,
            algorithm);
        }
      }
    }
    return new Map_Validation_Result(checked_adjacencies, skipped_zero_cells, errors);
  }

  private static void validate_edge(
    List<string> errors,
    Tileset_Generation_Data generation_data,
    int source_tile,
    int target_tile,
    byte direction,
    int source_x,
    int source_y,
    int target_x,
    int target_y,
    Map_Generation_Algorithm algorithm)
  {
    if (!try_canonical(generation_data, source_tile, out short source)
      || !try_canonical(generation_data, target_tile, out short target)
      || !generation_data.generation_data.TryGetValue(source, out Tile_Data source_data)
      || !generation_data.generation_data.TryGetValue(target, out Tile_Data target_data))
      return;
    bool forward = source_data.Valid_Tile_Priority[direction].ContainsKey(target);
    bool reverse = target_data.Valid_Tile_Priority[(byte) (10 - direction)].ContainsKey(source);
    bool valid = algorithm == Map_Generation_Algorithm.Experimental_Constraint
      ? forward && reverse
      : forward || reverse;
    if (!valid)
    {
      errors.Add(
        $"Cells ({source_x},{source_y}) tile {source_tile} and ({target_x},{target_y}) tile {target_tile} " +
        $"are not valid in either learned adjacency direction.");
    }
  }

  private static void validate_terrain(
    List<string> errors,
    Data_Tileset terrain_tileset,
    int constraint,
    int tile,
    int x,
    int y)
  {
    if (terrain_tileset == null)
    {
      errors.Add($"Cell ({x},{y}) has terrain constraint {constraint}, but terrain metadata is unavailable.");
      return;
    }
    if (tile < 0 || tile >= terrain_tileset.Terrain_Tags.Count)
    {
      errors.Add($"Cell ({x},{y}) tile {tile} has no terrain tag for constraint {constraint}.");
      return;
    }
    int tag = terrain_tileset.Terrain_Tags[tile];
    bool valid = constraint > 0 ? tag == constraint : tag != -constraint;
    if (!valid)
      errors.Add($"Cell ({x},{y}) tile {tile} with terrain tag {tag} violates constraint {constraint}.");
  }

  private static bool try_canonical(
    Tileset_Generation_Data generation_data,
    int tile,
    out short canonical)
  {
    if (tile < short.MinValue || tile > short.MaxValue)
    {
      canonical = 0;
      return false;
    }
    short value = (short) tile;
    canonical = generation_data.identical_tiles.TryGetValue(value, out short mapped)
      ? mapped
      : value;
    return true;
  }
}
