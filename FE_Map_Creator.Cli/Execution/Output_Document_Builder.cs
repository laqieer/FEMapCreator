using System.IO;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Builds the <see cref="Map_Document"/>/<see cref="Map_Write_Options"/> pair passed to
/// <see cref="Map_Codec_Registry.write"/>, picking the tileset identifier convention
/// each format expects: text <c>.map</c> files use the short trailing "Identifier"
/// segment bundled real map corpora use (e.g. "01020304"), while TMX uses the full
/// descriptive asset name plus its PNG file name as the image source. MAR has no
/// embedded tileset identifier at all, so the value is unused there.
/// </summary>
internal static class Output_Document_Builder
{
  internal static (Map_Document document, Map_Write_Options write_options) build(
    int[,] tiles, Map_Format output_format, Tileset_Asset asset)
  {
    string tileset_value = output_format == Map_Format.Text
      ? Asset_Naming.identifier(asset.Name)
      : asset.Name;
    string image_source = output_format == Map_Format.Tmx && asset.Has_Image
      ? Path.GetFileName(asset.Image_Path)
      : "";

    Map_Document document = new Map_Document(tiles, tileset_value)
    {
      Tileset_Image_Source = image_source,
    };
    Map_Write_Options write_options = new Map_Write_Options
    {
      Tileset = tileset_value,
      Tileset_Image_Source = image_source,
    };
    return (document, write_options);
  }
}
