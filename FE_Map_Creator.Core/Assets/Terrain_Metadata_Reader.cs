using FEXNA_Library;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

#nullable disable
namespace FE_Map_Creator;

public sealed class Terrain_Metadata_Reader
{
  private const string EXPECTED_ASSET_TYPE = "Generic:Dictionary[int,FEXNA_Library.Data_Terrain]";

  public Dictionary<int, Data_Terrain> read(string filename)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A terrain metadata filename is required.", nameof (filename));
    using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
      return this.read(stream);
  }

  public Dictionary<int, Data_Terrain> read(Stream stream)
  {
    if (stream == null)
      throw new ArgumentNullException(nameof (stream));
    if (!stream.CanRead)
      throw new ArgumentException("The terrain metadata stream must be readable.", nameof (stream));
    XDocument xml = XDocument.Load(stream, LoadOptions.SetLineInfo);
    XElement root = xml.Root;
    if (root == null)
      throw new InvalidDataException("Terrain metadata is missing its root element.");
    XElement asset = root.Elements().FirstOrDefault(element => element.Name.LocalName == "Asset");
    if (asset == null)
      throw new InvalidDataException("Terrain metadata is missing its Asset element.");
    string asset_type = asset.Attribute("Type")?.Value ?? "";
    if (!string.Equals(asset_type, EXPECTED_ASSET_TYPE, StringComparison.Ordinal))
      throw new InvalidDataException($"Terrain metadata Asset Type must be \"{EXPECTED_ASSET_TYPE}\".");

    Dictionary<int, Data_Terrain> result = new Dictionary<int, Data_Terrain>();
    foreach (XElement item in asset.Elements().Where(element => element.Name.LocalName == "Item"))
    {
      XElement value = item.Elements().FirstOrDefault(element => element.Name.LocalName == "Value");
      if (value == null)
        throw new InvalidDataException("Terrain metadata item is missing its Value element.");
      Data_Terrain terrain = new Data_Terrain
      {
        Id = required_int(value, "Id"),
        Name = optional_text(value, "Name"),
      };
      validate_key(item, terrain.Id);
      XElement move_costs = value.Elements().FirstOrDefault(element => element.Name.LocalName == "Move_Costs");
      if (move_costs != null)
      {
        int[][] parsed = move_costs.Elements()
          .Where(element => element.Name.LocalName == "Item")
          .Select((element, index) => parse_move_cost_row(element.Value, terrain.Id, index))
          .ToArray();
        if (parsed.Length > 0)
          terrain.Move_Costs = parsed;
      }
      if (!result.TryAdd(terrain.Id, terrain))
        throw new InvalidDataException($"Terrain metadata contains duplicate id {terrain.Id}.");
    }
    return result;
  }

  private static int[] parse_move_cost_row(string text, int terrain_id, int row)
  {
    string[] tokens = text.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
    int[] result = new int[tokens.Length];
    for (int index = 0; index < tokens.Length; ++index)
    {
      if (!int.TryParse(tokens[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out result[index]))
      {
        throw new InvalidDataException(
          $"Terrain {terrain_id} move-cost row {row + 1} contains invalid value \"{tokens[index]}\".");
      }
    }
    return result;
  }

  private static int required_int(XElement parent, string name)
  {
    string text = optional_text(parent, name);
    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
      throw new InvalidDataException($"Terrain metadata value {name} must be an integer.");
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
      throw new InvalidDataException($"Terrain metadata key \"{key_text}\" must be an integer.");
    if (key != id)
      throw new InvalidDataException($"Terrain metadata key {key} does not match terrain id {id}.");
  }
}
