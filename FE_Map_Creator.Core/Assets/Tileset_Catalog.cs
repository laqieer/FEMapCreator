using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

#nullable disable
namespace FE_Map_Creator;

public sealed class Tileset_Catalog
{
  private readonly List<Tileset_Asset> Assets;

  public string Asset_Root { get; }

  public IReadOnlyList<Tileset_Asset> tilesets => this.Assets;

  public Tileset_Catalog(string asset_root)
  {
    if (string.IsNullOrWhiteSpace(asset_root))
      throw new ArgumentException("An asset root is required.", nameof (asset_root));
    this.Asset_Root = Path.GetFullPath(asset_root);
    this.Assets = scan(this.Asset_Root);
  }

  public Tileset_Asset resolve(
    string selector,
    string image_override = null,
    string generation_data_override = null,
    bool require_image = true,
    bool require_generation_data = true)
  {
    string image_path = normalize_override(image_override, "tileset image");
    string data_path = normalize_override(generation_data_override, "generation data");
    if (string.IsNullOrWhiteSpace(selector) &&
        string.IsNullOrWhiteSpace(image_path) &&
        string.IsNullOrWhiteSpace(data_path))
    {
      throw new ArgumentException("A tileset selector or explicit asset override is required.", nameof (selector));
    }
    if ((!require_image || !string.IsNullOrWhiteSpace(image_path)) &&
        (!require_generation_data || !string.IsNullOrWhiteSpace(data_path)) &&
        (!string.IsNullOrWhiteSpace(image_path) || !string.IsNullOrWhiteSpace(data_path)))
    {
      return new Tileset_Asset(
        common_name(image_path, data_path, selector),
        image_path,
        data_path);
    }
    string lookup_selector = selector;
    List<Tileset_Asset> matches = this.find_matches(lookup_selector);
    if (matches.Count == 0)
    {
      string override_name = !string.IsNullOrWhiteSpace(image_path) ?
        Path.GetFileNameWithoutExtension(image_path) :
        (!string.IsNullOrWhiteSpace(data_path) ? Path.GetFileNameWithoutExtension(data_path) : "");
      if (!string.IsNullOrWhiteSpace(override_name))
      {
        lookup_selector = override_name;
        matches = this.find_matches(lookup_selector);
      }
    }
    if (matches.Count == 0)
      throw new FileNotFoundException($"No bundled tileset matches \"{lookup_selector}\".");
    if (matches.Count > 1)
    {
      throw new InvalidOperationException(
        $"Tileset selector \"{lookup_selector}\" is ambiguous: {string.Join(", ", matches.Select(asset => asset.Name))}.");
    }
    Tileset_Asset match = matches[0];
    image_path ??= match.Image_Path;
    data_path ??= match.Generation_Data_Path;
    if (require_image && string.IsNullOrWhiteSpace(image_path))
      throw new FileNotFoundException($"Tileset \"{match.Name}\" has no PNG image.");
    if (require_generation_data && string.IsNullOrWhiteSpace(data_path))
      throw new FileNotFoundException($"Tileset \"{match.Name}\" has no generation-data file.");
    return new Tileset_Asset(match.Name, image_path, data_path);
  }

  public List<Tileset_Asset> find_matches(string selector)
  {
    if (string.IsNullOrWhiteSpace(selector))
      return new List<Tileset_Asset>();
    string filename = Path.GetFileNameWithoutExtension(selector.Trim());
    string normalized = normalize(filename);
    List<Tileset_Asset> exact = this.Assets.Where(asset =>
      string.Equals(asset.Name, filename, StringComparison.OrdinalIgnoreCase)).ToList();
    if (exact.Count > 0)
      return exact;
    exact = this.Assets.Where(asset =>
      string.Equals(normalize(asset.Name), normalized, StringComparison.Ordinal)).ToList();
    if (exact.Count > 0)
      return exact;
    exact = this.Assets.Where(asset =>
      string.Equals(asset_identifier(asset.Name), filename, StringComparison.OrdinalIgnoreCase) ||
      normalize(asset_identifier(asset.Name)) == normalized).ToList();
    if (exact.Count > 0)
      return exact;
    return this.Assets.Where(asset =>
      string.Equals(asset_name_without_identifier(asset.Name), filename, StringComparison.OrdinalIgnoreCase) ||
      normalize(asset_name_without_identifier(asset.Name)) == normalized ||
      string.Equals(asset_description(asset.Name), filename, StringComparison.OrdinalIgnoreCase) ||
      normalize(asset_description(asset.Name)) == normalized).ToList();
  }

  private static List<Tileset_Asset> scan(string root)
  {
    string images_directory = Path.Combine(root, "Tilesets");
    string data_directory = Path.Combine(root, "Tileset Generation Data");
    Dictionary<string, string> images = files_by_basename(images_directory, "*.png");
    Dictionary<string, string> data = files_by_basename(data_directory, "*.dat");
    HashSet<string> names = new HashSet<string>(images.Keys, StringComparer.OrdinalIgnoreCase);
    names.UnionWith(data.Keys);
    return names
      .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
      .Select(name => new Tileset_Asset(
        name,
        images.TryGetValue(name, out string image) ? image : "",
        data.TryGetValue(name, out string generation_data) ? generation_data : ""))
      .ToList();
  }

  private static Dictionary<string, string> files_by_basename(string directory, string pattern)
  {
    Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (!Directory.Exists(directory))
      return result;
    foreach (string file in Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories))
    {
      string name = Path.GetFileNameWithoutExtension(file);
      if (result.TryGetValue(name, out string existing))
      {
        throw new InvalidDataException(
          $"Duplicate asset basename \"{name}\" found at \"{existing}\" and \"{file}\".");
      }
      result.Add(name, Path.GetFullPath(file));
    }
    return result;
  }

  private static string normalize_override(string path, string description)
  {
    if (string.IsNullOrWhiteSpace(path))
      return null;
    string full_path = Path.GetFullPath(path);
    if (!File.Exists(full_path))
      throw new FileNotFoundException($"The {description} file does not exist.", full_path);
    return full_path;
  }

  private static string common_name(string image, string data, string selector)
  {
    if (!string.IsNullOrWhiteSpace(selector))
      return Path.GetFileNameWithoutExtension(selector);
    string image_name = string.IsNullOrWhiteSpace(image) ? "" : Path.GetFileNameWithoutExtension(image);
    string data_name = string.IsNullOrWhiteSpace(data) ? "" : Path.GetFileNameWithoutExtension(data);
    if (string.IsNullOrWhiteSpace(image_name))
      return data_name;
    if (string.IsNullOrWhiteSpace(data_name))
      return image_name;
    return string.Equals(image_name, data_name, StringComparison.OrdinalIgnoreCase) ?
      image_name :
      $"{image_name} / {data_name}";
  }

  private static string asset_identifier(string name)
  {
    int separator = name.LastIndexOf(" - ", StringComparison.Ordinal);
    return separator >= 0 ? name.Substring(separator + 3).Trim() : name;
  }

  private static string asset_name_without_identifier(string name)
  {
    int separator = name.LastIndexOf(" - ", StringComparison.Ordinal);
    return separator >= 0 ? name.Substring(0, separator).Trim() : name;
  }

  private static string asset_description(string name)
  {
    string without_identifier = asset_name_without_identifier(name);
    int separator = without_identifier.IndexOf(" - ", StringComparison.Ordinal);
    return separator >= 0 ? without_identifier.Substring(separator + 3).Trim() : without_identifier;
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
