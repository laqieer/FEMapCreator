using System.Text;
using FE_Map_Creator.Generation;

namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class ExperimentalDiversityTests
{
  [TestMethod]
  [DataRow("FE6 - Castle - 2c002d2e.dat")]
  [DataRow("FE8 - Fields - 01000203.dat")]
  public void ExperimentalBlankMapAvoidsDominantTileCollapse(string generation_data_file)
  {
    Tileset_Generation_Data data;
    string filename = Path.Combine(
      repository_root(),
      "Tileset Generation Data",
      generation_data_file);
    using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
      data = Tileset_Generation_Data.read(reader);
    Map_State state = new Map_State(
      new int[20, 15],
      new bool[20, 15],
      new bool[20, 15],
      new int[20, 15]);

    Map_Generation_Result result = new Map_Generation_Engine(data).generate(
      state,
      new Map_Generation_Options()
      {
        Algorithm = Map_Generation_Algorithm.Experimental_Constraint,
        Seed = 42
      });

    int[] tiles = state.Tiles
      .Cast<int>()
      .Select(tile =>
      {
        short value = (short) tile;
        return (int) (data.identical_tiles.TryGetValue(value, out short canonical)
          ? canonical
          : value);
      })
      .ToArray();
    IGrouping<int, int>[] groups = tiles
      .GroupBy(tile => tile)
      .OrderByDescending(group => group.Count())
      .ToArray();
    double dominant_share = groups[0].Count() / (double) tiles.Length;
    double entropy = groups.Sum(group =>
    {
      double probability = group.Count() / (double) tiles.Length;
      return -probability * Math.Log2(probability);
    });
    int checked_neighbors = 0;
    int same_neighbors = 0;
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (x + 1 < state.Width)
        {
          ++checked_neighbors;
          if (canonical_tile(data, state.Tiles[x, y]) == canonical_tile(data, state.Tiles[x + 1, y]))
            ++same_neighbors;
        }
        if (y + 1 < state.Height)
        {
          ++checked_neighbors;
          if (canonical_tile(data, state.Tiles[x, y]) == canonical_tile(data, state.Tiles[x, y + 1]))
            ++same_neighbors;
        }
      }
    }
    double same_neighbor_share = same_neighbors / (double) checked_neighbors;

    Assert.AreEqual(0, result.Unresolved_Tile_Count);
    Assert.IsGreaterThanOrEqualTo(50, groups.Length);
    Assert.IsLessThanOrEqualTo(0.15, dominant_share);
    Assert.IsGreaterThanOrEqualTo(5.0, entropy);
    Assert.IsLessThanOrEqualTo(0.25, same_neighbor_share);
  }

  private static string repository_root()
  {
    DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "global.json")))
      directory = directory.Parent;
    return directory?.FullName
      ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
  }

  private static int canonical_tile(Tileset_Generation_Data data, int tile)
  {
    short value = (short) tile;
    return data.identical_tiles.TryGetValue(value, out short canonical)
      ? canonical
      : value;
  }
}
