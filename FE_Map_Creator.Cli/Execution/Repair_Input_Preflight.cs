using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

internal readonly struct Repair_Input_Metadata
{
  internal string Input_Path { get; }
  internal Map_Format Input_Format { get; }
  internal int? Width { get; }
  internal int? Height { get; }
  internal string Tileset { get; }

  internal Repair_Input_Metadata(
    string input_path,
    Map_Format input_format,
    int? width,
    int? height,
    string tileset)
  {
    this.Input_Path = input_path;
    this.Input_Format = input_format;
    this.Width = width;
    this.Height = height;
    this.Tileset = tileset;
  }
}

internal static class Repair_Input_Preflight
{
  internal static Repair_Input_Metadata resolve(
    Repair_Request request,
    Map_Job_Spec spec,
    string spec_directory,
    Map_Codec_Registry registry)
  {
    string input_path = Job_Merge.resolve_path(request.Input, spec?.Input, spec_directory);
    if (string.IsNullOrWhiteSpace(input_path))
      throw new InvalidOperationException("--input is required (directly or via --spec).");

    Map_Format input_format = registry.format_from_path(input_path);
    int? width = request.Width ?? spec?.Width;
    int? height = request.Height ?? spec?.Height;
    string tileset = Job_Merge.merge_string(request.Tileset, spec?.Tileset);
    if (input_format == Map_Format.Mar)
      validate_mar_metadata(input_path, width, height, tileset);
    return new Repair_Input_Metadata(input_path, input_format, width, height, tileset);
  }

  internal static void validate_manifest_mar_job(Map_Job_Spec spec, string manifest_directory)
  {
    string input_path = Job_Merge.resolve_path(null, spec?.Input, manifest_directory);
    if (string.IsNullOrWhiteSpace(input_path) ||
        !string.Equals(Path.GetExtension(input_path), ".mar", StringComparison.OrdinalIgnoreCase))
    {
      return;
    }
    validate_mar_metadata(input_path, spec.Width, spec.Height, spec.Tileset);
  }

  private static void validate_mar_metadata(string input_path, int? width, int? height, string tileset)
  {
    List<string> requirements = new List<string>();
    if (!width.HasValue || width.Value <= 0)
      requirements.Add("positive width");
    if (!height.HasValue || height.Value <= 0)
      requirements.Add("positive height");
    if (string.IsNullOrWhiteSpace(tileset))
      requirements.Add("a tileset identifier");
    if (requirements.Count == 0)
      return;

    throw new InvalidOperationException(
      $"MAR input \"{input_path}\" requires {join(requirements)}. " +
      "MAR files do not contain dimensions or a tileset identifier, so these values are never inferred. " +
      "Supply --width, --height, and --tileset; the corresponding width, height, and tileset fields in --spec or a batch manifest job; " +
      "or a .mapgen.json sidecar for directory repair.");
  }

  private static string join(IReadOnlyList<string> values)
  {
    if (values.Count == 1)
      return values[0];
    if (values.Count == 2)
      return $"{values[0]} and {values[1]}";
    return $"{string.Join(", ", values.Take(values.Count - 1))}, and {values[values.Count - 1]}";
  }
}
