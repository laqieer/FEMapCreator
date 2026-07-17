using System;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

internal static class Map_Edit_Output_Builder
{
  internal static (Map_Document document, Map_Write_Options write_options) build(
    int[,] tiles,
    Map_Format output_format,
    string tileset_selector,
    bool tileset_was_overridden,
    string tileset_image_path,
    string assets_root,
    Map_Document input_document,
    Map_Format? input_format,
    string input_path,
    string output_path)
  {
    if (output_format == Map_Format.Mar)
      return (new Map_Document(tiles, tileset_selector ?? ""), new Map_Write_Options());

    bool preserve_tmx_metadata =
      output_format == Map_Format.Tmx &&
      input_format == Map_Format.Tmx &&
      !tileset_was_overridden &&
      string.IsNullOrWhiteSpace(tileset_image_path);
    bool preserve_text_metadata =
      output_format == Map_Format.Text &&
      input_format == Map_Format.Text &&
      !tileset_was_overridden;
    Tileset_Asset asset = preserve_tmx_metadata || preserve_text_metadata
      ? null
      : resolve_optional_asset(
        assets_root,
        tileset_selector,
        tileset_image_path,
        output_format == Map_Format.Tmx);

    if (output_format == Map_Format.Text)
    {
      string tileset = preserve_text_metadata
        ? input_document?.Tileset
        : asset != null
          ? Tileset_Asset_Naming.identifier(asset.Name)
          : tileset_selector;
      if (string.IsNullOrWhiteSpace(tileset))
        throw new InvalidOperationException("Text map output requires --tileset or tileset metadata from the input map.");
      Map_Document document = new Map_Document(tiles, tileset);
      return (document, new Map_Write_Options { Tileset = tileset });
    }

    string tmx_tileset = preserve_tmx_metadata
      ? input_document.Tileset
      : asset?.Name ?? tileset_selector;
    string image_source = preserve_tmx_metadata
      ? Tmx_Image_Source.rebase(
        input_document.Tileset_Image_Source,
        input_path,
        output_path)
      : !string.IsNullOrWhiteSpace(tileset_image_path)
        ? Tmx_Image_Source.from_file(tileset_image_path, output_path)
        : asset != null && asset.Has_Image
          ? Tmx_Image_Source.from_file(asset.Image_Path, output_path)
          : "";
    if (string.IsNullOrWhiteSpace(tmx_tileset))
      throw new InvalidOperationException("TMX output requires --tileset or tileset metadata from the input map.");
    if (string.IsNullOrWhiteSpace(image_source))
    {
      throw new InvalidOperationException(
        "TMX output requires a tileset image source. Supply --tileset-image, select a bundled tileset with a PNG, or preserve it from TMX input.");
    }
    Map_Document tmx_document = new Map_Document(tiles, tmx_tileset)
    {
      Tileset_Image_Source = image_source,
    };
    return (
      tmx_document,
      new Map_Write_Options
      {
        Tileset = tmx_tileset,
        Tileset_Image_Source = image_source,
      });
  }

  private static Tileset_Asset resolve_optional_asset(
    string assets_root,
    string selector,
    string image_path,
    bool required)
  {
    Tileset_Catalog catalog = new Tileset_Catalog(assets_root);
    if (!string.IsNullOrWhiteSpace(image_path))
    {
      return catalog.resolve(
        selector,
        image_path,
        generation_data_override: null,
        require_image: true,
        require_generation_data: false);
    }

    List<Tileset_Asset> matches = catalog.find_matches(selector);
    if (matches.Count == 1)
      return matches[0];
    if (matches.Count > 1)
    {
      return catalog.resolve(
        selector,
        require_image: required,
        require_generation_data: false);
    }
    if (required)
    {
      throw new FileNotFoundException(
        $"No bundled tileset with a PNG image matches \"{selector}\". Supply --tileset-image explicitly.");
    }
    return null;
  }
}
