using System.Text;

namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class TilesetGenerationDataTests
{
  [TestMethod]
  public void BundledFieldsGenerationDataReadsWithExpectedRepresentativeValues()
  {
    string filename = Path.Combine(
      repository_root(),
      "Tileset Generation Data",
      "FE6 - Fields - 01020304.dat");

    Tileset_Generation_Data data;
    using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
      data = Tileset_Generation_Data.read(reader);

    Assert.HasCount(297, data.identical_tiles);
    Assert.HasCount(625, data.generation_data);
    Assert.AreEqual((short) 5, data.identical_tiles[(short) 842]);
    Assert.AreEqual((short) 3, data.generation_data[2].Valid_Tile_Priority[(byte) 6][(short) 230]);
    Assert.AreEqual((short) 3, data.generation_data[3].Valid_Tile_Priority[(byte) 8][(short) 305]);
    Assert.AreEqual((short) 3, data.generation_data[4].Valid_Tile_Priority[(byte) 4][(short) 303]);
  }

  [TestMethod]
  public void FixIdenticalMergesGroupsRemapsEdgesAndIsStable()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = create_tile_data(2, (6, 3, 4));
    data.generation_data[2] = create_tile_data(4, (6, 3, 8));
    data.generation_data[3] = create_tile_data(6, (4, 1, 4), (4, 2, 8));
    Dictionary<short, short> identical = new Dictionary<short, short>()
    {
      { 1, 1 },
      { 2, 1 }
    };

    Assert.IsFalse(data.fix_identical(identical));

    Assert.AreEqual((short) 1, data.identical_tiles[(short) 1]);
    Assert.AreEqual((short) 1, data.identical_tiles[(short) 2]);
    Assert.IsFalse(data.generation_data.ContainsKey(2));
    Assert.AreEqual((short) 3, data.generation_data[1].Priority);
    Assert.AreEqual((short) 6, data.generation_data[1].Valid_Tile_Priority[(byte) 6][(short) 3]);
    Assert.AreEqual((short) 6, data.generation_data[3].Valid_Tile_Priority[(byte) 4][(short) 1]);

    byte[] first_result = serialize(data);
    Assert.IsTrue(data.fix_identical(new Dictionary<short, short>(identical)));
    CollectionAssert.AreEqual(first_result, serialize(data));
  }

  [TestMethod]
  public void FixIdenticalKeepsFirstEdgeWhenAliasesCollapseToSameTarget()
  {
    Tileset_Generation_Data data = create_generation_data();
    data.generation_data[1] = create_tile_data(1, (6, 3, 5), (6, 4, 9));
    data.generation_data[3] = new Tile_Data();
    data.generation_data[4] = new Tile_Data();

    Assert.IsFalse(data.fix_identical(new Dictionary<short, short>()
    {
      { 1, 1 },
      { 3, 3 },
      { 4, 3 }
    }));

    Assert.HasCount(1, data.generation_data[1].Valid_Tile_Priority[(byte) 6]);
    Assert.AreEqual((short) 5, data.generation_data[1].Valid_Tile_Priority[(byte) 6][(short) 3]);
  }

  private static Tileset_Generation_Data create_generation_data()
  {
    return new Tileset_Generation_Data(
      0,
      new Tile_Matching_Data(new HashSet<Tile_Directions>()));
  }

  private static Tile_Data create_tile_data(short priority, params (byte Direction, short Tile, short Weight)[] edges)
  {
    Tile_Data data = new Tile_Data()
    {
      Priority = priority
    };
    foreach ((byte direction, short tile, short weight) in edges)
      data.Valid_Tile_Priority[direction][tile] = weight;
    return data;
  }

  private static byte[] serialize(Tileset_Generation_Data data)
  {
    using MemoryStream stream = new MemoryStream();
    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
      data.write(writer);
    return stream.ToArray();
  }

  private static string repository_root()
  {
    DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory != null)
    {
      if (Directory.Exists(Path.Combine(directory.FullName, "Tileset Generation Data")) &&
          File.Exists(Path.Combine(directory.FullName, "Tileset_Data.xml")))
      {
        return directory.FullName;
      }
      directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("Could not locate the repository root.");
  }
}
