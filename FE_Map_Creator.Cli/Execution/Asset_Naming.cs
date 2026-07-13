using System;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Parses <see cref="Tileset_Catalog"/>'s bundled "Family - Description - Identifier"
/// asset naming convention. <see cref="Tileset_Catalog"/> only exposes selector
/// matching (which already understands this convention internally), not these
/// individual segments, so this mirrors its private asset_identifier/
/// asset_name_without_identifier/asset_description helpers exactly.
/// </summary>
internal static class Asset_Naming
{
  /// <summary>The trailing "Identifier" segment, e.g. "01020304" from "FE6 - Fields - 01020304".
  /// This is the convention real bundled text .map files use for their tileset header line.</summary>
  internal static string identifier(string asset_name)
  {
    int separator = asset_name.LastIndexOf(" - ", StringComparison.Ordinal);
    return separator >= 0 ? asset_name.Substring(separator + 3).Trim() : asset_name;
  }

  /// <summary>The name with its trailing " - Identifier" segment removed, e.g. "FE6 - Fields".</summary>
  internal static string name_without_identifier(string asset_name)
  {
    int separator = asset_name.LastIndexOf(" - ", StringComparison.Ordinal);
    return separator >= 0 ? asset_name.Substring(0, separator).Trim() : asset_name;
  }

  /// <summary>The middle "Description" segment, e.g. "Fields" from "FE6 - Fields - 01020304".
  /// This is the key <c>Tileset_Data.xml</c>'s Graphic_Name entries use.</summary>
  internal static string description(string asset_name)
  {
    string without_identifier = name_without_identifier(asset_name);
    int separator = without_identifier.IndexOf(" - ", StringComparison.Ordinal);
    return separator >= 0 ? without_identifier.Substring(separator + 3).Trim() : without_identifier;
  }
}
