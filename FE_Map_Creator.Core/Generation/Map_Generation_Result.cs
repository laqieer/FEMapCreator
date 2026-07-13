#nullable disable
namespace FE_Map_Creator.Generation;

/// <summary>
/// Outcome of a <see cref="Map_Generation_Engine.generate"/> or
/// <see cref="Map_Generation_Engine.repair"/> call.
/// </summary>
public sealed class Map_Generation_Result
{
  /// <summary>
  /// Number of cells that had no valid tile candidate and were left as a drawn tile 0.
  /// </summary>
  public int Unresolved_Tile_Count { get; }

  /// <summary>
  /// The random seed actually used for this run (either the one supplied in the
  /// options, or one generated automatically when none was supplied).
  /// </summary>
  public int Seed { get; }

  public Map_Generation_Result(int unresolved_tile_count, int seed)
  {
    this.Unresolved_Tile_Count = unresolved_tile_count;
    this.Seed = seed;
  }
}
