using System.IO;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Loads an optional per-file spec sidecar for directory repair, recognizing both the
/// "basename" convention (<c>Foo.mapgen.json</c> for input <c>Foo.map</c>) and the
/// "full filename" convention (<c>Foo.map.mapgen.json</c>), preferring the former when
/// both happen to exist. Sidecar-relative fields (template/output/asset overrides, etc.)
/// resolve relative to the sidecar file's own directory -- the same directory as the
/// input file it describes.
/// </summary>
internal static class Sidecar_Spec_Loader
{
  internal const string Sidecar_Suffix = ".mapgen.json";

  internal static (Map_Job_Spec spec, string directory) load(string input_file)
  {
    string directory = Path.GetDirectoryName(input_file);
    string basename = Path.GetFileNameWithoutExtension(input_file);
    string filename = Path.GetFileName(input_file);

    string basename_sidecar = Path.Combine(directory, basename + Sidecar_Suffix);
    string filename_sidecar = Path.Combine(directory, filename + Sidecar_Suffix);
    string sidecar_path = File.Exists(basename_sidecar)
      ? basename_sidecar
      : (File.Exists(filename_sidecar) ? filename_sidecar : null);
    if (sidecar_path == null)
      return (null, directory);

    Map_Job_Spec spec = new Map_Job_Spec_Reader().read_job(sidecar_path);
    Job_Merge.validate_operation(spec.Operation, "repair", sidecar_path);
    return (spec, Path.GetDirectoryName(sidecar_path));
  }

  /// <summary>True for any file this loader itself would recognize as a sidecar, so
  /// directory enumeration can exclude sidecars even under a broad --pattern.</summary>
  internal static bool is_sidecar(string file_path)
  {
    return file_path.EndsWith(Sidecar_Suffix, System.StringComparison.OrdinalIgnoreCase);
  }
}
