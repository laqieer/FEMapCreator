using System;
using FE_Map_Creator.Cli.Commands;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Resolves the output <see cref="Map_Format"/> for a job: an explicit CLI/spec format
/// wins, otherwise it is inferred from the (already-resolved) output path's extension
/// via <see cref="Map_Codec_Registry"/> -- which for in-place repair naturally matches
/// the input's own format, satisfying "repair defaults to input format".
/// </summary>
internal static class Output_Format_Resolver
{
  internal static Map_Format resolve(
    Cli_Map_Format? explicit_cli_format,
    string spec_format,
    string output_path,
    Map_Codec_Registry registry)
  {
    if (explicit_cli_format.HasValue)
      return to_core_format(explicit_cli_format.Value);
    Cli_Map_Format? parsed_spec_format = Cli_Validation.parse_format(spec_format);
    if (!string.IsNullOrWhiteSpace(spec_format) && !parsed_spec_format.HasValue)
      throw new InvalidOperationException($"Spec format \"{spec_format}\" is not one of map, mar, or tmx.");
    if (parsed_spec_format.HasValue)
      return to_core_format(parsed_spec_format.Value);
    if (string.IsNullOrWhiteSpace(output_path))
      throw new InvalidOperationException("An output format could not be determined; supply --format, a spec format, or an --output extension.");
    return registry.format_from_path(output_path);
  }

  private static Map_Format to_core_format(Cli_Map_Format format)
  {
    switch (format)
    {
      case Cli_Map_Format.Map:
        return Map_Format.Text;
      case Cli_Map_Format.Mar:
        return Map_Format.Mar;
      case Cli_Map_Format.Tmx:
        return Map_Format.Tmx;
      default:
        throw new NotSupportedException($"Unsupported CLI map format \"{format}\".");
    }
  }
}
