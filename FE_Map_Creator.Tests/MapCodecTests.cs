namespace FE_Map_Creator.Tests;

[TestClass]
public sealed class MapCodecTests
{
  [TestMethod]
  public void TextMapRoundTripPreservesTilesetAndDimensions()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "test.map");
      Map_Document expected = text_sample_document();
      new Text_Map_Codec().write(filename, expected);

      Map_Document actual = new Text_Map_Codec().read(filename);

      assert_document(expected, actual);
      Assert.AreEqual("01020304", actual.Tileset);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void TextMapReaderRejectsMalformedRow()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "bad.map");
      File.WriteAllText(filename,
        """
        01020304
        2 2
        1 2
        3
        """);

      Assert.Throws<InvalidDataException>(() => new Text_Map_Codec().read(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void TextMapReaderRejectsTrailingContent()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "extra.map");
      File.WriteAllText(filename,
        """
        01020304
        1 1
        0
        unexpected
        """);

      Assert.Throws<InvalidDataException>(() => new Text_Map_Codec().read(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void MarRoundTripRequiresMetadataAndPreservesSignedValues()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "test.mar");
      Map_Document expected = mar_sample_document();
      Mar_Map_Codec codec = new Mar_Map_Codec();
      codec.write(filename, expected);

      Map_Document actual = codec.read(filename, new Map_Read_Options()
      {
        Width = expected.Width,
        Height = expected.Height,
        Tileset = expected.Tileset
      });

      assert_document(expected, actual);
      Assert.Throws<InvalidDataException>(() => codec.read(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void MarReaderRejectsUnexpectedLength()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "bad.mar");
      File.WriteAllBytes(filename, new byte[3]);

      Assert.Throws<InvalidDataException>(() => new Mar_Map_Codec().read(filename, new Map_Read_Options()
      {
        Width = 1,
        Height = 1,
        Tileset = "test"
      }));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void MarReaderRejectsInvalidEncoding()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "bad.mar");
      using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
      using (BinaryWriter writer = new BinaryWriter(stream))
        writer.Write((short) 33);

      Assert.Throws<InvalidDataException>(() => new Mar_Map_Codec().read(filename, new Map_Read_Options()
      {
        Width = 1,
        Height = 1,
        Tileset = "test"
      }));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void MarWriterRejectsOutOfRangeTiles()
  {
    string directory = create_temp_directory();
    try
    {
      Mar_Map_Codec codec = new Mar_Map_Codec();

      Assert.Throws<InvalidDataException>(() =>
        codec.write(Path.Combine(directory, "high.mar"), new Map_Document(new int[,] { { 1024 } }, "test")));
      Assert.Throws<InvalidDataException>(() =>
        codec.write(Path.Combine(directory, "low.mar"), new Map_Document(new int[,] { { -1025 } }, "test")));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void TmxRoundTripUsesExplicitTileElementsAndFirstGid()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "test.tmx");
      Map_Document expected = text_sample_document();
      expected.Tileset_Image_Source = "tiles.png";
      Tmx_Map_Codec codec = new Tmx_Map_Codec();
      codec.write(filename, expected, new Map_Write_Options()
      {
        Tileset = expected.Tileset,
        Tileset_Image_Source = expected.Tileset_Image_Source,
        First_Gid = 7
      });

      Map_Document actual = codec.read(filename);

      assert_document(expected, actual);
      Assert.AreEqual("tiles.png", actual.Tileset_Image_Source);
      string xml = File.ReadAllText(filename);
      StringAssert.Contains(xml, "<tile gid=");
      StringAssert.Contains(xml, "firstgid=\"7\"");
      StringAssert.Contains(xml, "orientation=\"orthogonal\"");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void TmxReaderRejectsCsvData()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "csv.tmx");
      File.WriteAllText(filename,
        """
        <map orientation="orthogonal" width="1" height="1">
          <tileset firstgid="1" name="test"><image source="test.png"/></tileset>
          <layer width="1" height="1"><data encoding="csv">1</data></layer>
        </map>
        """);

      Assert.Throws<NotSupportedException>(() => new Tmx_Map_Codec().read(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void TmxReaderRejectsBase64Data()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "base64.tmx");
      File.WriteAllText(filename,
        """
        <map orientation="orthogonal" width="1" height="1">
          <tileset firstgid="1" name="test"><image source="test.png"/></tileset>
          <layer width="1" height="1"><data encoding="base64">AQAAAA==</data></layer>
        </map>
        """);

      Assert.Throws<NotSupportedException>(() => new Tmx_Map_Codec().read(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void TmxReaderRejectsNonOrthogonalOrientation()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "iso.tmx");
      File.WriteAllText(filename,
        """
        <map orientation="isometric" width="1" height="1">
          <tileset firstgid="7" name="test"><image source="test.png"/></tileset>
          <layer width="1" height="1">
            <data>
              <tile gid="7"/>
            </data>
          </layer>
        </map>
        """);

      Assert.Throws<InvalidDataException>(() => new Tmx_Map_Codec().read(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  private static Map_Document text_sample_document()
  {
    return new Map_Document(new int[,]
    {
      { 0, 3 },
      { 2, 4 }
    }, "01020304");
  }

  private static Map_Document mar_sample_document()
  {
    return new Map_Document(new int[,]
    {
      { -1024, 1023 },
      { -1, 0 }
    }, "01020304");
  }

  private static void assert_document(Map_Document expected, Map_Document actual)
  {
    Assert.AreEqual(expected.Width, actual.Width);
    Assert.AreEqual(expected.Height, actual.Height);
    Assert.AreEqual(expected.Tileset, actual.Tileset);
    for (int y = 0; y < expected.Height; ++y)
    {
      for (int x = 0; x < expected.Width; ++x)
        Assert.AreEqual(expected.Tiles[x, y], actual.Tiles[x, y], $"Tile mismatch at ({x},{y}).");
    }
  }

  private static string create_temp_directory()
  {
    string directory = Path.Combine(Path.GetTempPath(), $"FEMapCreator-{Guid.NewGuid():N}");
    Directory.CreateDirectory(directory);
    return directory;
  }
}
