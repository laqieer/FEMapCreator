using System.Text;

namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class TileDataTests
{
  [TestMethod]
  public void BinaryRoundTripPreservesPriorities()
  {
    Tile_Data expected = new Tile_Data
    {
      Priority = 9
    };
    expected.Valid_Tile_Priority[(byte) 2][(short) 17] = (short) 3;
    expected.Valid_Tile_Priority[(byte) 4][(short) 23] = (short) 5;
    expected.Valid_Tile_Priority[(byte) 6][(short) 42] = (short) 7;
    expected.Valid_Tile_Priority[(byte) 8][(short) 99] = (short) 11;

    byte[] serialized;
    using (MemoryStream stream = new MemoryStream())
    {
      using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
        expected.write(writer);
      serialized = stream.ToArray();
    }

    Tile_Data actual;
    using (MemoryStream stream = new MemoryStream(serialized))
    using (BinaryReader reader = new BinaryReader(stream))
      actual = Tile_Data.read(reader);

    Assert.AreEqual(expected.Priority, actual.Priority);
    foreach (byte direction in new byte[4] { 2, 4, 6, 8 })
      CollectionAssert.AreEquivalent(expected.Valid_Tile_Priority[direction].ToArray(), actual.Valid_Tile_Priority[direction].ToArray());
  }
}
