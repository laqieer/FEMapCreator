using System;
using System.Buffers.Binary;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

#nullable disable
namespace FE_Map_Creator;

public sealed class Tmx_Map_Codec : IMap_Codec
{
  private const uint TRANSFORM_FLAGS = 0xF0000000U;

  public Map_Format Format => Map_Format.Tmx;

  public Map_Document read(string filename, Map_Read_Options options = null)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A map filename is required.", nameof (filename));
    XDocument xml = XDocument.Load(filename, LoadOptions.SetLineInfo);
    XElement root = xml.Root;
    if (root == null || root.Name.LocalName != "map")
      throw new InvalidDataException("TMX input is missing its map root element.");
    string orientation = root.Attribute("orientation")?.Value ?? "";
    if (!string.Equals(orientation, "orthogonal", StringComparison.OrdinalIgnoreCase))
      throw new InvalidDataException($"TMX orientation \"{orientation}\" is not supported; expected orthogonal.");
    int width = required_positive_int(root, "width", "TMX map");
    int height = required_positive_int(root, "height", "TMX map");
    XElement tileset = root.Elements().FirstOrDefault(element => element.Name.LocalName == "tileset");
    if (tileset == null)
      throw new InvalidDataException("TMX input is missing a tileset element.");
    int first_gid = required_positive_int(tileset, "firstgid", "TMX tileset");
    XElement image = tileset.Elements().FirstOrDefault(element => element.Name.LocalName == "image");
    string image_source = image?.Attribute("source")?.Value ?? "";
    string tileset_name = tileset.Attribute("name")?.Value;
    if (string.IsNullOrWhiteSpace(tileset_name))
      tileset_name = image_source;
    XElement layer = root.Elements().FirstOrDefault(element => element.Name.LocalName == "layer");
    if (layer == null)
      throw new InvalidDataException("TMX input is missing a tile layer.");
    int[,] tiles = new int[width, height];
    read_layer(layer, first_gid, tiles, width, height);
    return new Map_Document(tiles, tileset_name ?? "")
    {
      Tileset_Image_Source = image_source
    };
  }

  public void write(string filename, Map_Document document, Map_Write_Options options = null)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A map filename is required.", nameof (filename));
    if (document == null)
      throw new ArgumentNullException(nameof (document));
    int first_gid = options?.First_Gid ?? 1;
    if (first_gid <= 0)
      throw new ArgumentOutOfRangeException(nameof (options), "TMX firstgid must be positive.");
    string tileset = options != null && !string.IsNullOrWhiteSpace(options.Tileset) ?
      options.Tileset :
      document.Tileset;
    string image_source = options != null && !string.IsNullOrWhiteSpace(options.Tileset_Image_Source) ?
      options.Tileset_Image_Source :
      document.Tileset_Image_Source;
    if (string.IsNullOrWhiteSpace(image_source))
      image_source = tileset;
    if (string.IsNullOrWhiteSpace(tileset) || string.IsNullOrWhiteSpace(image_source))
      throw new InvalidOperationException("TMX output requires a tileset name and image source.");
    XElement data = new XElement("data");
    for (int y = 0; y < document.Height; ++y)
    {
      for (int x = 0; x < document.Width; ++x)
      {
        int tile = document.Tiles[x, y];
        if (tile < 0)
          throw new InvalidDataException($"Map tile at ({x},{y}) cannot be negative.");
        data.Add(new XElement("tile", new XAttribute("gid", checked(tile + first_gid))));
      }
    }
    XDocument xml = new XDocument(
      new XDeclaration("1.0", "UTF-8", null),
      new XElement("map",
        new XAttribute("version", "1.0"),
        new XAttribute("orientation", "orthogonal"),
        new XAttribute("width", document.Width),
        new XAttribute("height", document.Height),
        new XAttribute("tilewidth", 16),
        new XAttribute("tileheight", 16),
        new XElement("tileset",
          new XAttribute("firstgid", first_gid),
          new XAttribute("name", tileset),
          new XAttribute("tilewidth", 16),
          new XAttribute("tileheight", 16),
          new XElement("image",
            new XAttribute("source", image_source),
            new XAttribute("trans", "ffffff"))),
        new XElement("layer",
          new XAttribute("name", "Tile Layer 1"),
          new XAttribute("width", document.Width),
          new XAttribute("height", document.Height),
          new XElement("properties",
            new XElement("property",
              new XAttribute("name", "Main"),
              new XAttribute("value", ""))),
          data)));
    string directory = Path.GetDirectoryName(Path.GetFullPath(filename));
    Directory.CreateDirectory(directory);
    xml.Save(filename);
  }

  private static void read_layer(
    XElement layer,
    int first_gid,
    int[,] tiles,
    int map_width,
    int map_height)
  {
    int layer_width = required_positive_int(layer, "width", "TMX layer");
    int layer_height = required_positive_int(layer, "height", "TMX layer");
    int x_offset = 0;
    int y_offset = 0;
    int active_width = layer_width;
    int active_height = layer_height;
    XElement properties = layer.Elements().FirstOrDefault(element => element.Name.LocalName == "properties");
    if (properties != null)
    {
      var property_values = properties.Elements()
        .Where(element => element.Name.LocalName == "property")
        .Where(element => element.Attribute("name") != null)
        .ToDictionary(
          element => element.Attribute("name").Value,
          element => element.Attribute("value")?.Value ?? element.Value,
          StringComparer.Ordinal);
      if (property_values.ContainsKey("Width"))
      {
        x_offset = required_property_int(property_values, "X");
        y_offset = required_property_int(property_values, "Y");
        active_width = required_property_int(property_values, "Width");
        active_height = required_property_int(property_values, "Height");
      }
    }
    XElement data = layer.Elements().FirstOrDefault(element => element.Name.LocalName == "data");
    if (data == null)
      throw new InvalidDataException("TMX layer is missing its data element.");
    int expected = checked(layer_width * layer_height);
    uint[] gids = read_layer_gids(data, expected);
    for (int index = 0; index < gids.Length; ++index)
    {
      int x = index % layer_width;
      int y = index / layer_width;
      if (x < x_offset || y < y_offset || x >= x_offset + active_width || y >= y_offset + active_height ||
          x < 0 || y < 0 || x >= map_width || y >= map_height)
      {
        continue;
      }
      uint raw_gid = gids[index];
      uint transform_flags = raw_gid & TRANSFORM_FLAGS;
      if (transform_flags != 0)
      {
        throw new InvalidDataException(
          $"TMX tile {index} gid {raw_gid} uses unsupported transform flags 0x{transform_flags:X8}.");
      }
      uint gid = raw_gid & ~TRANSFORM_FLAGS;
      if (gid == 0 || gid < first_gid)
        continue;
      uint tile = gid - (uint) first_gid;
      if (tile > int.MaxValue)
        throw new InvalidDataException($"TMX tile {index} gid {gid} is outside the supported tile index range.");
      tiles[x, y] = (int) tile;
    }
  }

  private static uint[] read_layer_gids(XElement data, int expected)
  {
    string encoding = data.Attribute("encoding")?.Value?.Trim();
    string compression = data.Attribute("compression")?.Value?.Trim();
    if (string.IsNullOrEmpty(encoding))
    {
      if (data.Attribute("compression") != null)
        throw unsupported_data_encoding("explicit tile elements", compression);
      return read_explicit_gids(data, expected);
    }
    if (string.Equals(encoding, "csv", StringComparison.OrdinalIgnoreCase))
    {
      if (data.Attribute("compression") != null)
        throw unsupported_data_encoding(encoding, compression);
      return read_csv_gids(data, expected);
    }
    if (string.Equals(encoding, "base64", StringComparison.OrdinalIgnoreCase))
      return read_base64_gids(data, compression, expected);
    throw new NotSupportedException($"TMX layer encoding \"{encoding}\" is not supported.");
  }

  private static uint[] read_explicit_gids(XElement data, int expected)
  {
    XElement[] tile_elements = data.Elements().Where(element => element.Name.LocalName == "tile").ToArray();
    if (tile_elements.Length != expected)
      throw new InvalidDataException($"TMX layer contains {tile_elements.Length} tile elements; expected {expected}.");
    uint[] gids = new uint[expected];
    for (int index = 0; index < tile_elements.Length; ++index)
    {
      string gid_text = tile_elements[index].Attribute("gid")?.Value;
      if (!uint.TryParse(gid_text, NumberStyles.Integer, CultureInfo.InvariantCulture, out gids[index]))
        throw new InvalidDataException($"TMX tile {index} has an invalid gid.");
    }
    return gids;
  }

  private static uint[] read_csv_gids(XElement data, int expected)
  {
    string[] values = data.Value.Split(',');
    if (values.Length != expected)
      throw new InvalidDataException($"TMX layer CSV data contains {values.Length} gids; expected {expected}.");
    uint[] gids = new uint[expected];
    for (int index = 0; index < values.Length; ++index)
    {
      if (!uint.TryParse(values[index].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out gids[index]))
        throw new InvalidDataException($"TMX layer CSV gid {index} is invalid.");
    }
    return gids;
  }

  private static uint[] read_base64_gids(XElement data, string compression, int expected)
  {
    byte[] encoded;
    try
    {
      encoded = Convert.FromBase64String(data.Value);
    }
    catch (FormatException ex)
    {
      throw new InvalidDataException("TMX layer base64 data is invalid.", ex);
    }

    int expected_bytes = checked(expected * sizeof (uint));
    byte[] bytes;
    if (string.IsNullOrEmpty(compression))
    {
      bytes = encoded;
    }
    else if (string.Equals(compression, "gzip", StringComparison.OrdinalIgnoreCase))
    {
      bytes = decompress(encoded, expected_bytes, compression, stream => new GZipStream(stream, CompressionMode.Decompress));
    }
    else if (string.Equals(compression, "zlib", StringComparison.OrdinalIgnoreCase))
    {
      bytes = decompress(encoded, expected_bytes, compression, stream => new ZLibStream(stream, CompressionMode.Decompress));
    }
    else
    {
      throw unsupported_data_encoding("base64", compression);
    }

    if (bytes.Length != expected_bytes)
    {
      throw new InvalidDataException(
        $"TMX layer base64 data contains {bytes.Length} bytes; expected {expected_bytes} for {expected} gids.");
    }
    uint[] gids = new uint[expected];
    for (int index = 0; index < expected; ++index)
      gids[index] = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(index * sizeof (uint), sizeof (uint)));
    return gids;
  }

  private static byte[] decompress(
    byte[] encoded,
    int expected_bytes,
    string compression,
    Func<Stream, Stream> create_stream)
  {
    try
    {
      using MemoryStream input = new MemoryStream(encoded, false);
      using Stream compressed = create_stream(input);
      byte[] bytes = new byte[expected_bytes];
      int length = 0;
      while (length < bytes.Length)
      {
        int read = compressed.Read(bytes, length, bytes.Length - length);
        if (read == 0)
          break;
        length += read;
      }
      if (length != expected_bytes)
      {
        throw new InvalidDataException(
          $"TMX layer base64 data with {compression} compression contains {length} decompressed bytes; expected {expected_bytes}.");
      }
      if (compressed.ReadByte() != -1)
      {
        throw new InvalidDataException(
          $"TMX layer base64 data with {compression} compression contains more than {expected_bytes} decompressed bytes.");
      }
      return bytes;
    }
    catch (InvalidDataException ex) when (!ex.Message.StartsWith("TMX layer", StringComparison.Ordinal))
    {
      throw new InvalidDataException($"TMX layer base64 data with {compression} compression is invalid.", ex);
    }
  }

  private static NotSupportedException unsupported_data_encoding(string encoding, string compression)
  {
    return new NotSupportedException(
      $"TMX layer encoding \"{encoding}\" with compression \"{compression ?? ""}\" is not supported.");
  }

  private static int required_positive_int(XElement element, string attribute, string context)
  {
    string value = element.Attribute(attribute)?.Value;
    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) || result <= 0)
      throw new InvalidDataException($"{context} requires a positive {attribute} attribute.");
    return result;
  }

  private static int required_property_int(
    System.Collections.Generic.Dictionary<string, string> values,
    string name)
  {
    if (!values.TryGetValue(name, out string value) ||
        !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
    {
      throw new InvalidDataException($"TMX layer property {name} must be an integer.");
    }
    return result;
  }
}
