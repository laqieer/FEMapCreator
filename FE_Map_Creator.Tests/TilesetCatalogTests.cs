namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class TilesetCatalogTests
{
  [TestMethod]
  public void ResolvesUniqueShortIdentifier()
  {
    string directory = create_catalog(
      "FE6 - Fields - 01020304",
      "FE7 - Fields - 1c1d1e1f");
    try
    {
      Tileset_Asset asset = new Tileset_Catalog(directory).resolve("01020304");

      Assert.AreEqual("FE6 - Fields - 01020304", asset.Name);
      Assert.IsTrue(asset.Has_Image);
      Assert.IsTrue(asset.Has_Generation_Data);
      Assert.AreEqual("", asset.Missing_Pair_Diagnostic);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ResolvesExactBasenameWithExtension()
  {
    string directory = create_catalog("FE6 - Fields - 01020304");
    try
    {
      Tileset_Asset asset = new Tileset_Catalog(directory).resolve("FE6 - Fields - 01020304.png");

      Assert.AreEqual("FE6 - Fields - 01020304", asset.Name);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void RejectsAmbiguousDescriptiveSelector()
  {
    string directory = create_catalog(
      "FE6 - Fields - 01020304",
      "FE7 - Fields - 1c1d1e1f");
    try
    {
      Tileset_Catalog catalog = new Tileset_Catalog(directory);

      InvalidOperationException ex =
        Assert.Throws<InvalidOperationException>(() => catalog.resolve("Fields"));
      StringAssert.Contains(ex.Message, "ambiguous");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ExplicitOverridesDoNotRequireBundledCatalogEntry()
  {
    string directory = create_catalog();
    try
    {
      string image = Path.Combine(directory, "custom.png");
      string data = Path.Combine(directory, "custom.dat");
      File.WriteAllBytes(image, Array.Empty<byte>());
      File.WriteAllBytes(data, Array.Empty<byte>());

      Tileset_Asset asset = new Tileset_Catalog(directory).resolve("custom", image, data);

      Assert.AreEqual(Path.GetFullPath(image), asset.Image_Path);
      Assert.AreEqual(Path.GetFullPath(data), asset.Generation_Data_Path);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void AllowsGenerationDataOnlyWhenImageIsOptional()
  {
    string directory = create_catalog();
    try
    {
      create_data_file(directory, "FE6 - Fields - 01020304");
      Tileset_Asset asset = new Tileset_Catalog(directory).resolve(
        "01020304",
        require_image: false,
        require_generation_data: true);

      Assert.IsFalse(asset.Has_Image);
      Assert.IsTrue(asset.Has_Generation_Data);
      Assert.AreEqual("Missing PNG image.", asset.Missing_Pair_Diagnostic);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void ReportsMissingGenerationDataPair()
  {
    string directory = create_catalog();
    try
    {
      create_image_file(directory, "FE6 - Fields - 01020304");
      Tileset_Catalog catalog = new Tileset_Catalog(directory);

      FileNotFoundException ex = Assert.Throws<FileNotFoundException>(() => catalog.resolve("01020304"));
      StringAssert.Contains(ex.Message, "generation-data");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void MetadataReaderParsesRepositoryXml()
  {
    string filename = Path.Combine(repository_root(), "Tileset_Data.xml");
    Tileset_Metadata_Reader reader = new Tileset_Metadata_Reader();

    Dictionary<int, FEXNA_Library.Data_Tileset> tilesets = reader.read(filename);
    FEXNA_Library.Data_Tileset fields = reader.find_by_graphic_name(tilesets, "Fields");

    Assert.IsNotNull(fields);
    Assert.AreEqual(0, fields.Id);
    Assert.IsNotEmpty(fields.Terrain_Tags);
  }

  [TestMethod]
  public void MetadataReaderParsesStreamAndResolvesBundledAssetName()
  {
    string filename = Path.Combine(repository_root(), "Tileset_Data.xml");
    Tileset_Metadata_Reader reader = new Tileset_Metadata_Reader();
    using FileStream stream = File.OpenRead(filename);

    Dictionary<int, FEXNA_Library.Data_Tileset> tilesets = reader.read(stream);
    FEXNA_Library.Data_Tileset fields =
      reader.find_for_asset_name(tilesets, "FE6 - Fields - 01020304");

    Assert.IsNotNull(fields);
    Assert.AreEqual("Fields", fields.Graphic_Name);
    Assert.IsTrue(stream.CanRead);
  }

  [TestMethod]
  public void EveryBundledTilesetResolvesTerrainMetadata()
  {
    string root = repository_root();
    Tileset_Catalog catalog = new Tileset_Catalog(root);
    Tileset_Metadata_Reader reader = new Tileset_Metadata_Reader();
    Dictionary<int, FEXNA_Library.Data_Tileset> metadata =
      reader.read(Path.Combine(root, "Tileset_Data.xml"));

    Assert.HasCount(41, catalog.tilesets);
    foreach (Tileset_Asset asset in catalog.tilesets)
    {
      Assert.IsNotNull(
        reader.find_for_asset_name(metadata, asset.Name),
        $"Bundled tileset \"{asset.Name}\" has no terrain metadata.");
    }
  }

  [TestMethod]
  public void MetadataReaderRejectsMismatchedItemKey()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "Tileset_Data.xml");
      File.WriteAllText(filename,
        """
        <XnaContent xmlns:Generic="System.Collections.Generic">
          <Asset Type="Generic:Dictionary[int,FEXNA_Library.Data_Tileset]">
            <Item>
              <Key>2</Key>
              <Value>
                <Id>1</Id>
                <Graphic_Name>Fields</Graphic_Name>
                <Terrain_Tags>0 1</Terrain_Tags>
              </Value>
            </Item>
          </Asset>
        </XnaContent>
        """);

      Assert.Throws<InvalidDataException>(() => new Tileset_Metadata_Reader().read(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void MetadataReaderRejectsInvalidTerrainTag()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "Tileset_Data.xml");
      File.WriteAllText(filename,
        """
        <XnaContent xmlns:Generic="System.Collections.Generic">
          <Asset Type="Generic:Dictionary[int,FEXNA_Library.Data_Tileset]">
            <Item>
              <Key>1</Key>
              <Value>
                <Id>1</Id>
                <Graphic_Name>Fields</Graphic_Name>
                <Terrain_Tags>0 nope 2</Terrain_Tags>
              </Value>
            </Item>
          </Asset>
        </XnaContent>
        """);

      Assert.Throws<InvalidDataException>(() => new Tileset_Metadata_Reader().read(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void TerrainMetadataReaderParsesRepositoryXmlStream()
  {
    string filename = Path.Combine(repository_root(), "Terrain_Data.xml");
    Terrain_Metadata_Reader reader = new Terrain_Metadata_Reader();
    using FileStream stream = File.OpenRead(filename);

    Dictionary<int, FEXNA_Library.Data_Terrain> terrains = reader.read(stream);

    Assert.AreEqual("Plains", terrains[1].Name);
    Assert.IsNotEmpty(terrains[1].Move_Costs);
    Assert.IsTrue(stream.CanRead);
  }

  private static string create_catalog(params string[] names)
  {
    string directory = create_temp_directory();
    Directory.CreateDirectory(Path.Combine(directory, "Tilesets"));
    Directory.CreateDirectory(Path.Combine(directory, "Tileset Generation Data"));
    foreach (string name in names)
    {
      create_image_file(directory, name);
      create_data_file(directory, name);
    }
    return directory;
  }

  private static void create_image_file(string directory, string name)
  {
    string images = Path.Combine(directory, "Tilesets");
    Directory.CreateDirectory(images);
    File.WriteAllBytes(Path.Combine(images, $"{name}.png"), Array.Empty<byte>());
  }

  private static void create_data_file(string directory, string name)
  {
    string data = Path.Combine(directory, "Tileset Generation Data");
    Directory.CreateDirectory(data);
    File.WriteAllBytes(Path.Combine(data, $"{name}.dat"), Array.Empty<byte>());
  }

  private static string create_temp_directory()
  {
    string directory = Path.Combine(Path.GetTempPath(), $"FEMapCreator-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    return directory;
  }

  private static string repository_root()
  {
    DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory != null)
    {
      if (File.Exists(Path.Combine(directory.FullName, "Tileset_Data.xml")))
        return directory.FullName;
      directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("Could not locate the repository root.");
  }
}
