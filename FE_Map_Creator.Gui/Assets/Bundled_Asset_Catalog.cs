using FEXNA_Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

#nullable disable
namespace FE_Map_Creator.Gui.Assets;

public sealed class Bundled_Asset_Catalog
{
  private const string IMAGE_PREFIX = "FE_Map_Creator.Gui.Assets.Tilesets.";
  private const string DATA_PREFIX = "FE_Map_Creator.Gui.Assets.GenerationData.";
  private const string TILESET_METADATA = "FE_Map_Creator.Gui.Assets.Tileset_Data.xml";
  private const string TERRAIN_METADATA = "FE_Map_Creator.Gui.Assets.Terrain_Data.xml";

  private readonly List<Bundled_Tileset_Descriptor> Tileset_List;

  public IReadOnlyList<Bundled_Tileset_Descriptor> Tilesets => this.Tileset_List;

  public IReadOnlyDictionary<int, Data_Terrain> Terrains { get; }

  public Bundled_Asset_Catalog()
  {
    Assembly assembly = typeof(Bundled_Asset_Catalog).Assembly;
    string[] resources = assembly.GetManifestResourceNames();
    Dictionary<string, string> images = resource_map(resources, IMAGE_PREFIX, ".png");
    Dictionary<string, string> data = resource_map(resources, DATA_PREFIX, ".dat");
    if (images.Count == 0 || data.Count == 0)
      throw new InvalidDataException("The cross-platform GUI contains no embedded tileset assets.");

    Dictionary<int, Data_Tileset> metadata;
    using (Stream stream = open_resource(assembly, TILESET_METADATA))
      metadata = new Tileset_Metadata_Reader().read(stream);
    using (Stream stream = open_resource(assembly, TERRAIN_METADATA))
      this.Terrains = new Terrain_Metadata_Reader().read(stream);

    HashSet<string> names = new HashSet<string>(images.Keys, StringComparer.OrdinalIgnoreCase);
    names.UnionWith(data.Keys);
    List<string> incomplete = names
      .Where(name => !images.ContainsKey(name) || !data.ContainsKey(name))
      .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
      .ToList();
    if (incomplete.Count > 0)
    {
      throw new InvalidDataException(
        $"Embedded tileset assets are incomplete: {string.Join(", ", incomplete)}.");
    }

    this.Tileset_List = names
      .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
      .Select(name => new Bundled_Tileset_Descriptor(
        assembly, name, images[name], data[name], metadata))
      .ToList();
  }

  public Bundled_Tileset_Descriptor find(string selector)
  {
    if (string.IsNullOrWhiteSpace(selector))
      return null;
    string filename = Path.GetFileNameWithoutExtension(selector.Trim());
    string normalized = normalize(filename);
    List<Bundled_Tileset_Descriptor> matches = this.Tileset_List
      .Where(asset => string.Equals(asset.Name, filename, StringComparison.OrdinalIgnoreCase))
      .ToList();
    if (matches.Count == 1)
      return matches[0];
    matches = this.Tileset_List.Where(asset => normalize(asset.Name) == normalized).ToList();
    if (matches.Count == 1)
      return matches[0];
    matches = this.Tileset_List.Where(asset =>
      normalize(Tileset_Asset_Naming.identifier(asset.Name)) == normalized ||
      normalize(Tileset_Asset_Naming.name_without_identifier(asset.Name)) == normalized ||
      normalize(Tileset_Asset_Naming.description(asset.Name)) == normalized).ToList();
    return matches.Count == 1 ? matches[0] : null;
  }

  private static Dictionary<string, string> resource_map(
    IEnumerable<string> resources,
    string prefix,
    string extension)
  {
    return resources
      .Where(resource =>
        resource.StartsWith(prefix, StringComparison.Ordinal) &&
        resource.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
      .ToDictionary(
        resource => resource.Substring(prefix.Length, resource.Length - prefix.Length - extension.Length),
        resource => resource,
        StringComparer.OrdinalIgnoreCase);
  }

  private static Stream open_resource(Assembly assembly, string name)
  {
    return assembly.GetManifestResourceStream(name)
      ?? throw new FileNotFoundException($"Embedded resource \"{name}\" was not found.");
  }

  private static string normalize(string value)
  {
    StringBuilder result = new StringBuilder(value.Length);
    foreach (char character in value)
    {
      if (char.IsLetterOrDigit(character))
        result.Append(char.ToLowerInvariant(character));
    }
    return result.ToString();
  }
}
