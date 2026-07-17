using FE_Map_Creator.Generation;
using FE_Map_Creator.Gui.Assets;
using FE_Map_Creator.Gui.Models;

namespace FE_Map_Creator.Gui.Tests;

[TestClass]
public sealed class BundledAssetCatalogTests
{
  [TestMethod]
  public void CatalogContainsEveryPairedRepositoryTileset()
  {
    Bundled_Asset_Catalog catalog = new Bundled_Asset_Catalog();

    Assert.HasCount(41, catalog.Tilesets);
    Assert.IsNotNull(catalog.find("01020304"));
    Assert.IsNotNull(catalog.find("FE6 - Fields - 01020304.png"));
    Assert.IsNull(catalog.find("Fields"));
    Assert.AreEqual("Plains", catalog.Terrains[1].Name);
  }

  [TestMethod]
  public void BundledFieldsTilesetLoadsImageWeightsAndTerrainMetadata()
  {
    Bundled_Asset_Catalog catalog = new Bundled_Asset_Catalog();
    Bundled_Tileset_Descriptor descriptor =
      catalog.find("FE6 - Fields - 01020304")
      ?? throw new AssertFailedException("Fields tileset was not found.");

    using Bundled_Tileset tileset = descriptor.load();

    Assert.IsGreaterThan(0, tileset.Tile_Count);
    Assert.AreEqual(0, tileset.Image.PixelSize.Width % 16);
    Assert.IsNotEmpty(tileset.Generation_Data.generation_data);
    Assert.IsNotNull(tileset.Metadata);
    Assert.AreEqual("Fields", tileset.Metadata.Graphic_Name);
  }

  [TestMethod]
  public void BundledFieldsDataGeneratesAnEditorMap()
  {
    Bundled_Asset_Catalog catalog = new Bundled_Asset_Catalog();
    Bundled_Tileset_Descriptor descriptor =
      catalog.find("FE6 - Fields - 01020304")
      ?? throw new AssertFailedException("Fields tileset was not found.");
    using Bundled_Tileset tileset = descriptor.load();
    Editor_Session session = new Editor_Session(8, 6);
    Map_Generation_Engine engine =
      new Map_Generation_Engine(tileset.Generation_Data, tileset.Metadata);

    Map_Generation_Result result = engine.generate(
      session.create_map_state(),
      new Map_Generation_Options
      {
        Algorithm = Map_Generation_Algorithm.Legacy,
        Depth = 1,
        Seed = 12345,
      });

    int drawn = 0;
    foreach (bool value in session.Drawn)
    {
      if (value)
        ++drawn;
    }
    Assert.AreEqual(12345, result.Seed);
    Assert.IsGreaterThan(0, drawn);
  }

  [TestMethod]
  public void EveryBundledTilesetLoadsAsACompleteEditorAsset()
  {
    Bundled_Asset_Catalog catalog = new Bundled_Asset_Catalog();

    foreach (Bundled_Tileset_Descriptor descriptor in catalog.Tilesets)
    {
      using Bundled_Tileset tileset = descriptor.load();
      Assert.IsGreaterThan(0, tileset.Tile_Count, descriptor.Name);
      Assert.AreEqual(0, tileset.Image.PixelSize.Width % 16, descriptor.Name);
      Assert.AreEqual(0, tileset.Image.PixelSize.Height % 16, descriptor.Name);
      Assert.IsNotEmpty(tileset.Generation_Data.generation_data, descriptor.Name);
      Assert.IsNotNull(tileset.Metadata, descriptor.Name);
    }
  }
}
