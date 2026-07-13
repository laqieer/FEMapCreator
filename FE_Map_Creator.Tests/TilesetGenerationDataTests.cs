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
