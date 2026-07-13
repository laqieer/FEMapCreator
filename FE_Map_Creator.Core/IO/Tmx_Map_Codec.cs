using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

#nullable disable
namespace FE_Map_Creator;

public sealed class Tmx_Map_Codec : IMap_Codec
{
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
    if (data.Attribute("encoding") != null || data.Attribute("compression") != null)
      throw new NotSupportedException("Only explicit <tile gid=\"...\"/> TMX layer data is supported.");
    XElement[] tile_elements = data.Elements().Where(element => element.Name.LocalName == "tile").ToArray();
    int expected = checked(layer_width * layer_height);
    if (tile_elements.Length != expected)
      throw new InvalidDataException($"TMX layer contains {tile_elements.Length} tile elements; expected {expected}.");
    for (int index = 0; index < tile_elements.Length; ++index)
    {
      int x = index % layer_width;
      int y = index / layer_width;
      if (x < x_offset || y < y_offset || x >= x_offset + active_width || y >= y_offset + active_height ||
          x < 0 || y < 0 || x >= map_width || y >= map_height)
      {
        continue;
      }
      string gid_text = tile_elements[index].Attribute("gid")?.Value;
      if (!int.TryParse(gid_text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int gid) || gid < 0)
        throw new InvalidDataException($"TMX tile {index} has an invalid gid.");
      int tile = gid - first_gid;
      if (tile >= 0)
        tiles[x, y] = tile;
    }
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
