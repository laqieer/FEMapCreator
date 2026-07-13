using FEXNA_Library;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

#nullable disable
namespace FE_Map_Creator;

public sealed class Tileset_Metadata_Reader
{
  private const string EXPECTED_ASSET_TYPE = "Generic:Dictionary[int,FEXNA_Library.Data_Tileset]";

  public Dictionary<int, Data_Tileset> read(string filename)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A tileset metadata filename is required.", nameof (filename));
    XDocument xml = XDocument.Load(filename, LoadOptions.SetLineInfo);
    XElement root = xml.Root;
    if (root == null)
      throw new InvalidDataException("Tileset metadata is missing its root element.");
    XElement asset = root?.Elements().FirstOrDefault(element => element.Name.LocalName == "Asset");
    if (asset == null)
      throw new InvalidDataException("Tileset metadata is missing its Asset element.");
    string asset_type = asset.Attribute("Type")?.Value ?? "";
    if (!string.Equals(asset_type, EXPECTED_ASSET_TYPE, StringComparison.Ordinal))
      throw new InvalidDataException($"Tileset metadata Asset Type must be \"{EXPECTED_ASSET_TYPE}\".");
    Dictionary<int, Data_Tileset> result = new Dictionary<int, Data_Tileset>();
    foreach (XElement item in asset.Elements().Where(element => element.Name.LocalName == "Item"))
    {
      XElement value = item.Elements().FirstOrDefault(element => element.Name.LocalName == "Value");
      if (value == null)
        throw new InvalidDataException("Tileset metadata item is missing its Value element.");
      Data_Tileset tileset = new Data_Tileset();
      tileset.Id = required_int(value, "Id");
      validate_key(item, tileset.Id);
      tileset.Name = optional_text(value, "Name");
      tileset.Graphic_Name = optional_text(value, "Graphic_Name");
      string terrain_tags = optional_text(value, "Terrain_Tags");
      tileset.Terrain_Tags = new List<int>();
      foreach (string token in terrain_tags.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries))
      {
        if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int terrain))
          throw new InvalidDataException($"Tileset {tileset.Id} contains invalid terrain tag \"{token}\".");
        tileset.Terrain_Tags.Add(terrain);
      }
      if (!result.TryAdd(tileset.Id, tileset))
        throw new InvalidDataException($"Tileset metadata contains duplicate id {tileset.Id}.");
    }
    return result;
  }

  public Data_Tileset find_by_graphic_name(
    IReadOnlyDictionary<int, Data_Tileset> tilesets,
    string graphic_name)
  {
    if (tilesets == null)
      throw new ArgumentNullException(nameof (tilesets));
    if (string.IsNullOrWhiteSpace(graphic_name))
      return null;
    graphic_name = graphic_name.Trim();
    List<Data_Tileset> matches = tilesets.Values.Where(tileset =>
      string.Equals(tileset.Graphic_Name, graphic_name, StringComparison.OrdinalIgnoreCase)).ToList();
    if (matches.Count > 1)
      throw new InvalidDataException($"Tileset metadata contains duplicate graphic name \"{graphic_name}\".");
    return matches.Count == 1 ? matches[0] : null;
  }

  private static int required_int(XElement parent, string name)
  {
    string text = optional_text(parent, name);
    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
      throw new InvalidDataException($"Tileset metadata value {name} must be an integer.");
    return result;
  }

  private static string optional_text(XElement parent, string name)
  {
    return parent.Elements().FirstOrDefault(element => element.Name.LocalName == name)?.Value ?? "";
  }

  private static void validate_key(XElement item, int id)
  {
    string key_text = item.Elements().FirstOrDefault(element => element.Name.LocalName == "Key")?.Value ?? "";
    if (key_text.Length == 0)
      return;
    if (!int.TryParse(key_text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int key))
      throw new InvalidDataException($"Tileset metadata key \"{key_text}\" must be an integer.");
    if (key != id)
      throw new InvalidDataException($"Tileset metadata key {key} does not match tileset id {id}.");
  }
}
