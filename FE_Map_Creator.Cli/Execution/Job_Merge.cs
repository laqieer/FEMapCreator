using System;
using System.IO;
using FE_Map_Creator.Cli.Requests;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Merge helpers implementing "explicit CLI values override spec values" precedence, and
/// path resolution relative to the spec file for spec-supplied paths (CLI-supplied paths
/// resolve relative to the current directory as usual).
/// </summary>
internal static class Job_Merge
{
  internal static string merge_string(string cli_value, string spec_value)
  {
    return !string.IsNullOrWhiteSpace(cli_value) ? cli_value : spec_value;
  }

  internal static int? merge_int(int? cli_value, int? spec_value)
  {
    return cli_value ?? spec_value;
  }

  /// <summary>
  /// Resolves a path supplied either directly on the CLI (relative to the current
  /// directory) or from a loaded spec (relative to the spec file's directory, so specs
  /// remain portable regardless of the caller's working directory).
  /// </summary>
  internal static string resolve_path(string cli_value, string spec_value, string spec_directory)
  {
    if (!string.IsNullOrWhiteSpace(cli_value))
      return Path.GetFullPath(cli_value);
    if (string.IsNullOrWhiteSpace(spec_value))
      return null;
    return spec_directory != null ? Path.GetFullPath(Path.Combine(spec_directory, spec_value)) : Path.GetFullPath(spec_value);
  }

  internal static void validate_operation(string spec_operation, string expected_operation, string spec_path)
  {
    if (string.IsNullOrWhiteSpace(spec_operation))
      return;
    if (!string.Equals(spec_operation.Trim(), expected_operation, StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException(
        $"Spec \"{spec_path}\" declares operation \"{spec_operation}\", but the \"{expected_operation}\" command was run.");
    }
  }
}
