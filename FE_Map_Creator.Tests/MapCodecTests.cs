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
  public void TextMapStreamRoundTripLeavesCallerStreamOpen()
  {
    Map_Document expected = text_sample_document();
    Text_Map_Codec codec = new Text_Map_Codec();
    using MemoryStream stream = new MemoryStream();

    codec.write(stream, expected);
    Assert.IsTrue(stream.CanWrite);
    stream.Position = 0;
    Map_Document actual = codec.read(stream);

    assert_document(expected, actual);
    Assert.IsTrue(stream.CanRead);
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
  public void MarMapStreamRoundTripLeavesCallerStreamOpen()
  {
    Map_Document expected = mar_sample_document();
    Mar_Map_Codec codec = new Mar_Map_Codec();
    using MemoryStream stream = new MemoryStream();

    codec.write(stream, expected);
    Assert.IsTrue(stream.CanWrite);
    stream.Position = 0;
    Map_Document actual = codec.read(stream, new Map_Read_Options()
    {
      Width = expected.Width,
      Height = expected.Height,
      Tileset = expected.Tileset
    });

    assert_document(expected, actual);
    Assert.IsTrue(stream.CanRead);
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
  public void TmxMapStreamRoundTripLeavesCallerStreamOpen()
  {
    Map_Document expected = text_sample_document();
    expected.Tileset_Image_Source = "tiles.png";
    Tmx_Map_Codec codec = new Tmx_Map_Codec();
    using MemoryStream stream = new MemoryStream();

    codec.write(stream, expected);
    Assert.IsTrue(stream.CanWrite);
    stream.Position = 0;
    Map_Document actual = codec.read(stream);

    assert_document(expected, actual);
    Assert.IsTrue(stream.CanRead);
  }

  [TestMethod]
  [DataRow(Map_Format.Text)]
  [DataRow(Map_Format.Mar)]
  [DataRow(Map_Format.Tmx)]
  public async Task RegistryAsyncWriteSupportsAsyncOnlyStreams(Map_Format format)
  {
    Map_Document expected = text_sample_document();
    expected.Tileset_Image_Source = "tiles.png";
    Map_Write_Options write_options = new Map_Write_Options()
    {
      Tileset = expected.Tileset,
      Tileset_Image_Source = expected.Tileset_Image_Source
    };
    Map_Codec_Registry registry = new Map_Codec_Registry();
    using Async_Only_Write_Stream output = new Async_Only_Write_Stream();

    await registry.write_async(output, format, expected, write_options);
    await output.FlushAsync();

    Assert.IsTrue(output.CanWrite);
    using MemoryStream input = new MemoryStream(output.to_array());
    Map_Read_Options? read_options = format == Map_Format.Mar
      ? new Map_Read_Options()
      {
        Width = expected.Width,
        Height = expected.Height,
        Tileset = expected.Tileset
      }
      : null;
    Map_Document actual = registry.read(input, format, read_options);
    assert_document(expected, actual);
    if (format == Map_Format.Tmx)
      Assert.AreEqual(expected.Tileset_Image_Source, actual.Tileset_Image_Source);
  }

  [TestMethod]
  [DataRow(Map_Format.Text)]
  [DataRow(Map_Format.Mar)]
  [DataRow(Map_Format.Tmx)]
  public async Task RegistryAsyncReadSupportsAsyncOnlyStreams(Map_Format format)
  {
    Map_Document expected = format == Map_Format.Mar
      ? mar_sample_document()
      : text_sample_document();
    expected.Tileset_Image_Source = "tiles.png";
    Map_Write_Options write_options = new Map_Write_Options()
    {
      Tileset = expected.Tileset,
      Tileset_Image_Source = expected.Tileset_Image_Source
    };
    Map_Codec_Registry registry = new Map_Codec_Registry();
    using MemoryStream serialized = new MemoryStream();
    registry.write(serialized, format, expected, write_options);
    using Async_Only_Read_Stream input = new Async_Only_Read_Stream(serialized.ToArray());
    Map_Read_Options? read_options = format == Map_Format.Mar
      ? new Map_Read_Options()
      {
        Width = expected.Width,
        Height = expected.Height,
        Tileset = expected.Tileset
      }
      : null;

    Map_Document actual = await registry.read_async(input, format, read_options);

    Assert.IsTrue(input.CanRead);
    assert_document(expected, actual);
    if (format == Map_Format.Tmx)
      Assert.AreEqual(expected.Tileset_Image_Source, actual.Tileset_Image_Source);
  }

  [TestMethod]
  public void TmxReaderDecodesCsvWithOffsetsAndEmptyGids()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "csv.tmx");
      File.WriteAllText(filename,
        """
        <map orientation="orthogonal" width="3" height="2">
          <tileset firstgid="7" name="test"><image source="test.png"/></tileset>
          <layer width="3" height="2">
            <properties>
              <property name="X" value="1"/>
              <property name="Y" value="0"/>
              <property name="Width" value="2"/>
              <property name="Height" value="2"/>
            </properties>
            <data encoding="csv">
              7, 8, 9,
              10, 0, 12
            </data>
          </layer>
        </map>
        """);

      Map_Document actual = new Tmx_Map_Codec().read(filename);

      assert_document(new Map_Document(new int[,]
      {
        { 0, 0 },
        { 1, 0 },
        { 2, 5 }
      }, "test")
      {
        Tileset_Image_Source = "test.png"
      }, actual);
      Assert.AreEqual("test.png", actual.Tileset_Image_Source);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void TmxReaderDecodesUncompressedBase64()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "base64.tmx");
      File.WriteAllText(filename,
        """
        <map orientation="orthogonal" width="2" height="2">
          <tileset firstgid="7" name="test"><image source="test.png"/></tileset>
          <layer width="2" height="2">
            <data encoding="base64">
              AAAAAAcAAAAIAAAACgAAAA==
            </data>
          </layer>
        </map>
        """);

      assert_tmx_fixture(new Tmx_Map_Codec().read(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  [DataRow("gzip", "H4sIAAAAAAACCmNgYGBgB2IOIOYCYgALWXskEAAAAA==")]
  [DataRow("zlib", "eJxjYGBgYAdiDiDmAmIAAMwAGg==")]
  public void TmxReaderDecodesCompressedBase64Fixtures(string compression, string fixture)
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, $"{compression}.tmx");
      File.WriteAllText(filename,
        $"""
        <map orientation="orthogonal" width="2" height="2">
          <tileset firstgid="7" name="test"><image source="test.png"/></tileset>
          <layer width="2" height="2">
            <data encoding="base64" compression="{compression}">
              {fixture}
            </data>
          </layer>
        </map>
        """);

      assert_tmx_fixture(new Tmx_Map_Codec().read(filename));
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  [DataRow("csv", "1,invalid", "CSV gid 1 is invalid")]
  [DataRow("csv", "1", "CSV data contains 1 gids; expected 2")]
  [DataRow("base64", "not base64!", "base64 data is invalid")]
  [DataRow("base64", "AQAAAA==", "base64 data contains 4 bytes; expected 8")]
  public void TmxReaderRejectsMalformedEncodedData(string encoding, string data, string expected_error)
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "malformed.tmx");
      File.WriteAllText(filename,
        $"""
        <map orientation="orthogonal" width="2" height="1">
          <tileset firstgid="1" name="test"><image source="test.png"/></tileset>
          <layer width="2" height="1"><data encoding="{encoding}">{data}</data></layer>
        </map>
        """);

      InvalidDataException exception =
        Assert.Throws<InvalidDataException>(() => new Tmx_Map_Codec().read(filename));

      StringAssert.Contains(exception.Message, expected_error);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  public void TmxReaderRejectsMalformedCompressedBase64()
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "malformed-gzip.tmx");
      File.WriteAllText(filename,
        """
        <map orientation="orthogonal" width="1" height="1">
          <tileset firstgid="1" name="test"><image source="test.png"/></tileset>
          <layer width="1" height="1">
            <data encoding="base64" compression="gzip">AQAAAA==</data>
          </layer>
        </map>
        """);

      InvalidDataException exception =
        Assert.Throws<InvalidDataException>(() => new Tmx_Map_Codec().read(filename));

      StringAssert.Contains(exception.Message, "base64 data with gzip compression is invalid");
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  [DataRow("json", null, "TMX layer encoding \"json\" is not supported")]
  [DataRow("base64", "zstd", "encoding \"base64\" with compression \"zstd\" is not supported")]
  [DataRow("csv", "gzip", "encoding \"csv\" with compression \"gzip\" is not supported")]
  public void TmxReaderRejectsUnsupportedEncodingOrCompression(
    string encoding,
    string compression,
    string expected_error)
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "unsupported.tmx");
      string compression_attribute = compression == null ? "" : $" compression=\"{compression}\"";
      File.WriteAllText(filename,
        $"""
        <map orientation="orthogonal" width="1" height="1">
          <tileset firstgid="1" name="test"><image source="test.png"/></tileset>
          <layer width="1" height="1">
            <data encoding="{encoding}"{compression_attribute}>AQAAAA==</data>
          </layer>
        </map>
        """);

      NotSupportedException exception =
        Assert.Throws<NotSupportedException>(() => new Tmx_Map_Codec().read(filename));

      StringAssert.Contains(exception.Message, expected_error);
    }
    finally
    {
      Directory.Delete(directory, true);
    }
  }

  [TestMethod]
  [DataRow(null, "<tile gid=\"2147483656\"/>")]
  [DataRow("csv", "2147483656")]
  [DataRow("base64", "CAAAgA==")]
  public void TmxReaderRejectsUnsupportedTransformFlags(string encoding, string data)
  {
    string directory = create_temp_directory();
    try
    {
      string filename = Path.Combine(directory, "transformed.tmx");
      string encoding_attribute = encoding == null ? "" : $" encoding=\"{encoding}\"";
      File.WriteAllText(filename,
        $"""
        <map orientation="orthogonal" width="1" height="1">
          <tileset firstgid="7" name="test"><image source="test.png"/></tileset>
          <layer width="1" height="1"><data{encoding_attribute}>{data}</data></layer>
        </map>
        """);

      InvalidDataException exception =
        Assert.Throws<InvalidDataException>(() => new Tmx_Map_Codec().read(filename));

      StringAssert.Contains(exception.Message, "unsupported transform flags");
      StringAssert.Contains(exception.Message, "0x80000000");
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

  private static void assert_tmx_fixture(Map_Document actual)
  {
    assert_document(new Map_Document(new int[,]
    {
      { 0, 1 },
      { 0, 3 }
    }, "test"), actual);
    Assert.AreEqual("test.png", actual.Tileset_Image_Source);
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

  private sealed class Async_Only_Write_Stream : Stream
  {
    private readonly MemoryStream Buffer = new MemoryStream();

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => this.Buffer.CanWrite;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
      get => throw new NotSupportedException();
      set => throw new NotSupportedException();
    }

    public byte[] to_array() => this.Buffer.ToArray();

    public override void Flush()
    {
      throw new NotSupportedException("Synchronous flush is not supported.");
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
      return this.Buffer.FlushAsync(cancellationToken);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
      throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new InvalidOperationException("Browser supports only WriteAsync.");
    }

    public override Task WriteAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken)
    {
      return this.Buffer.WriteAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask WriteAsync(
      ReadOnlyMemory<byte> buffer,
      CancellationToken cancellationToken = default)
    {
      return this.Buffer.WriteAsync(buffer, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
        this.Buffer.Dispose();
      base.Dispose(disposing);
    }
  }

  private sealed class Async_Only_Read_Stream : Stream
  {
    private readonly MemoryStream Buffer;

    internal Async_Only_Read_Stream(byte[] buffer)
    {
      this.Buffer = new MemoryStream(buffer, writable: false);
    }

    public override bool CanRead => this.Buffer.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => this.Buffer.Length;

    public override long Position
    {
      get => this.Buffer.Position;
      set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      throw new InvalidOperationException("Browser supports only ReadAsync.");
    }

    public override Task<int> ReadAsync(
      byte[] buffer,
      int offset,
      int count,
      CancellationToken cancellationToken)
    {
      return this.Buffer.ReadAsync(buffer, offset, count, cancellationToken);
    }

    public override ValueTask<int> ReadAsync(
      Memory<byte> buffer,
      CancellationToken cancellationToken = default)
    {
      return this.Buffer.ReadAsync(buffer, cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
      throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotSupportedException();
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
        this.Buffer.Dispose();
      base.Dispose(disposing);
    }
  }
}
