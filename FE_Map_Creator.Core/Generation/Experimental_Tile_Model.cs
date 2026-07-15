using FEXNA_Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

#nullable disable
namespace FE_Map_Creator.Generation;

internal sealed class Experimental_Tile_Model
{
  private static readonly byte[] Directions = new byte[] { 2, 4, 6, 8 };

  private readonly Tileset_Generation_Data _generation_data;
  private readonly Data_Tileset _terrain_tileset;
  private readonly Dictionary<short, int> _candidate_by_canonical;
  private readonly short[][] _aliases_by_candidate;
  private readonly ulong[][][] _allowed_neighbors;
  private readonly short[][][] _weights;
  private readonly short[] _priorities;

  internal int Candidate_Count => this._aliases_by_candidate.Length;
  internal int Word_Count { get; }

  internal Experimental_Tile_Model(Tileset_Generation_Data generation_data, Data_Tileset terrain_tileset)
  {
    this._generation_data = generation_data ?? throw new ArgumentNullException(nameof(generation_data));
    this._terrain_tileset = terrain_tileset;

    short[] source_tiles = generation_data.generation_data.Keys
      .OrderBy(tile => tile)
      .Select(tile =>
      {
        if (tile < short.MinValue || tile > short.MaxValue)
          throw new InvalidOperationException($"Generation tile index {tile} is outside the supported Int16 range.");
        return (short) tile;
      })
      .ToArray();
    short[] canonical_tiles = source_tiles
      .Select(this.canonicalize)
      .Distinct()
      .OrderBy(tile => tile)
      .ToArray();
    foreach (short canonical in canonical_tiles)
    {
      if (!generation_data.generation_data.TryGetValue(canonical, out Tile_Data data) || data == null)
        throw new InvalidOperationException($"Canonical generation tile {canonical} has no tile data.");
    }

    this._candidate_by_canonical = new Dictionary<short, int>();
    for (int candidate = 0; candidate < canonical_tiles.Length; ++candidate)
      this._candidate_by_canonical.Add(canonical_tiles[candidate], candidate);

    this.Word_Count = (canonical_tiles.Length + 63) / 64;
    this._aliases_by_candidate = new short[canonical_tiles.Length][];
    this._priorities = new short[canonical_tiles.Length];
    this._weights = new short[canonical_tiles.Length][][];
    this._allowed_neighbors = new ulong[canonical_tiles.Length][][];

    for (int candidate = 0; candidate < canonical_tiles.Length; ++candidate)
    {
      short canonical = canonical_tiles[candidate];
      this._aliases_by_candidate[candidate] = generation_data.identical_tiles
        .Where(pair => pair.Value == canonical)
        .Select(pair => pair.Key)
        .Append(canonical)
        .Distinct()
        .OrderBy(tile => tile)
        .ToArray();
      this._priorities[candidate] = generation_data.generation_data[canonical].Priority;
      this._weights[candidate] = new short[Directions.Length][];
      this._allowed_neighbors[candidate] = new ulong[Directions.Length][];
      for (int direction_slot = 0; direction_slot < Directions.Length; ++direction_slot)
      {
        this._weights[candidate][direction_slot] = new short[canonical_tiles.Length];
        this._allowed_neighbors[candidate][direction_slot] = new ulong[this.Word_Count];
      }
    }

    for (int source = 0; source < canonical_tiles.Length; ++source)
    {
      Tile_Data source_data = generation_data.generation_data[canonical_tiles[source]];
      for (int direction_slot = 0; direction_slot < Directions.Length; ++direction_slot)
      {
        byte direction = Directions[direction_slot];
        if (!source_data.Valid_Tile_Priority.TryGetValue(direction, out Dictionary<short, short> neighbors))
          throw new InvalidOperationException($"Generation tile {canonical_tiles[source]} has no direction {direction} data.");
        foreach (KeyValuePair<short, short> neighbor in neighbors)
        {
          if (neighbor.Value <= 0)
          {
            throw new InvalidOperationException(
              $"Adjacency weight for tile {canonical_tiles[source]}, direction {direction}, neighbor {neighbor.Key} must be positive.");
          }
          short canonical_neighbor = this.canonicalize(neighbor.Key);
          if (!this._candidate_by_canonical.TryGetValue(canonical_neighbor, out int target))
          {
            throw new InvalidOperationException(
              $"Generation tile {canonical_tiles[source]} references unknown neighbor {neighbor.Key} in direction {direction}.");
          }
          this._weights[source][direction_slot][target] = neighbor.Value;
        }
      }
    }

    for (int source = 0; source < canonical_tiles.Length; ++source)
    {
      for (int direction_slot = 0; direction_slot < Directions.Length; ++direction_slot)
      {
        int opposite_index = direction_index(Directions[direction_slot] == 2 ? (byte) 8
          : Directions[direction_slot] == 4 ? (byte) 6
          : Directions[direction_slot] == 6 ? (byte) 4
          : (byte) 2);
        for (int target = 0; target < canonical_tiles.Length; ++target)
        {
          if (this._weights[source][direction_slot][target] <= 0
            || this._weights[target][opposite_index][source] <= 0)
            continue;
          this._allowed_neighbors[source][direction_slot][target / 64] |= 1UL << (target % 64);
        }
      }
    }
  }

  internal ulong[] allowed_neighbors(int candidate, byte direction)
  {
    return this._allowed_neighbors[candidate][direction_index(direction)];
  }

  internal bool allows(int source, byte direction, int target)
  {
    ulong[] allowed = this.allowed_neighbors(source, direction);
    return (allowed[target / 64] & 1UL << (target % 64)) != 0;
  }

  internal int weight(int source, byte direction, int target)
  {
    int index = direction_index(direction);
    int opposite = direction_index((byte) (10 - direction));
    return this._weights[source][index][target] + this._weights[target][opposite][source];
  }

  internal short priority(int candidate) => this._priorities[candidate];

  internal bool try_candidate(int actual_tile, out int candidate)
  {
    if (actual_tile < short.MinValue || actual_tile > short.MaxValue)
    {
      candidate = -1;
      return false;
    }
    short canonical = this.canonicalize((short) actual_tile);
    return this._candidate_by_canonical.TryGetValue(canonical, out candidate);
  }

  internal bool terrain_allows_candidate(int candidate, int terrain)
  {
    return this.aliases_for_terrain(candidate, terrain).Length > 0;
  }

  internal short pick_alias(int candidate, int terrain, Random random)
  {
    short[] aliases = this.aliases_for_terrain(candidate, terrain);
    if (aliases.Length == 0)
      throw new InvalidOperationException($"Candidate {candidate} has no alias compatible with terrain {terrain}.");
    return aliases[random.Next(aliases.Length)];
  }

  internal int count_intersection(ulong[] domain, ulong[] allowed)
  {
    int count = 0;
    for (int word = 0; word < domain.Length; ++word)
      count += BitOperations.PopCount(domain[word] & allowed[word]);
    return count;
  }

  private short[] aliases_for_terrain(int candidate, int terrain)
  {
    short[] aliases = this._aliases_by_candidate[candidate];
    if (terrain == 0 || this._terrain_tileset == null)
      return aliases;
    return aliases.Where(alias =>
    {
      if (alias < 0 || alias >= this._terrain_tileset.Terrain_Tags.Count)
        return false;
      int tag = this._terrain_tileset.Terrain_Tags[alias];
      return terrain > 0 ? tag == terrain : tag != -terrain;
    }).ToArray();
  }

  private short canonicalize(short tile)
  {
    return this._generation_data.identical_tiles.TryGetValue(tile, out short canonical) ? canonical : tile;
  }

  private static int direction_index(byte direction)
  {
    return direction switch
    {
      2 => 0,
      4 => 1,
      6 => 2,
      8 => 3,
      _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, "Direction must be 2, 4, 6, or 8."),
    };
  }
}
