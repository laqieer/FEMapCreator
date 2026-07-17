using System;

#nullable disable
namespace FE_Map_Creator;

public static class Tileset_Asset_Naming
{
  public static string identifier(string asset_name)
  {
    if (string.IsNullOrWhiteSpace(asset_name))
      return "";
    int separator = asset_name.LastIndexOf(" - ", StringComparison.Ordinal);
    return separator >= 0 ? asset_name.Substring(separator + 3).Trim() : asset_name.Trim();
  }

  public static string name_without_identifier(string asset_name)
  {
    if (string.IsNullOrWhiteSpace(asset_name))
      return "";
    int separator = asset_name.LastIndexOf(" - ", StringComparison.Ordinal);
    return separator >= 0 ? asset_name.Substring(0, separator).Trim() : asset_name.Trim();
  }

  public static string description(string asset_name)
  {
    string without_identifier = name_without_identifier(asset_name);
    int separator = without_identifier.IndexOf(" - ", StringComparison.Ordinal);
    return separator >= 0 ? without_identifier.Substring(separator + 3).Trim() : without_identifier;
  }
}
