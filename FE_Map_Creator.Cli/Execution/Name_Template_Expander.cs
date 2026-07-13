using System;
using FE_Map_Creator.Cli.Commands;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Expands a <c>--name-template</c> pattern (supporting <c>{index}</c> and <c>{seed}</c>
/// placeholders) into a concrete output file name for one job of a homogeneous
/// <c>generate --count</c> batch, appending the batch's resolved format extension when
/// the template does not already end in a recognized one.
/// </summary>
internal static class Name_Template_Expander
{
  /// <summary>
  /// Resolves the single format shared by every job in a --count batch: an explicit
  /// --format/spec format wins; otherwise, if the (unexpanded) name template itself
  /// already ends with a recognized map extension, that extension picks the format;
  /// otherwise the format defaults to plain text .map.
  /// </summary>
  internal static Cli_Map_Format resolve_batch_format(Cli_Map_Format? explicit_format, string spec_format, string name_template)
  {
    if (explicit_format.HasValue)
      return explicit_format.Value;
    Cli_Map_Format? parsed_spec_format = Cli_Validation.parse_format(spec_format);
    if (parsed_spec_format.HasValue)
      return parsed_spec_format.Value;
    Cli_Map_Format? template_format = format_from_extension(System.IO.Path.GetExtension(name_template));
    return template_format ?? Cli_Map_Format.Map;
  }

  internal static string extension_for(Cli_Map_Format format)
  {
    switch (format)
    {
      case Cli_Map_Format.Map:
        return ".map";
      case Cli_Map_Format.Mar:
        return ".mar";
      case Cli_Map_Format.Tmx:
        return ".tmx";
      default:
        throw new NotSupportedException($"Unsupported CLI map format \"{format}\".");
    }
  }

  internal static string expand(string name_template, int index, int seed, Cli_Map_Format batch_format)
  {
    string expanded = name_template
      .Replace("{index}", index.ToString(System.Globalization.CultureInfo.InvariantCulture))
      .Replace("{seed}", seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
    return format_from_extension(System.IO.Path.GetExtension(expanded)).HasValue
      ? expanded
      : expanded + extension_for(batch_format);
  }

  private static Cli_Map_Format? format_from_extension(string extension)
  {
    switch (extension?.ToLowerInvariant())
    {
      case ".map":
        return Cli_Map_Format.Map;
      case ".mar":
        return Cli_Map_Format.Mar;
      case ".tmx":
        return Cli_Map_Format.Tmx;
      default:
        return null;
    }
  }
}
