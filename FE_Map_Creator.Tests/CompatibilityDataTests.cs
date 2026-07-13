using FEXNA_Library;

namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class CompatibilityDataTests
{
  [TestMethod]
  public void TerrainDefaultsPreserveLegacyMovementCosts()
  {
    Data_Terrain terrain = new Data_Terrain();

    Assert.HasCount(3, terrain.Move_Costs);
    foreach (int[] movementGroup in terrain.Move_Costs)
      CollectionAssert.AreEqual(new int[5] { 1, 1, 1, 1, 1 }, movementGroup);
  }
}
