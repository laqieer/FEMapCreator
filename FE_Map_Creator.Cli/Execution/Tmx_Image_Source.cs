using System;
using System.IO;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

internal static class Tmx_Image_Source
{
  internal static string from_file(string image_path, string output_path)
  {
    if (string.IsNullOrWhiteSpace(image_path))
      return "";
    return relative_to_output(Path.GetFullPath(image_path), output_path);
  }

  internal static string rebase(string source, string input_path, string output_path)
  {
    if (string.IsNullOrWhiteSpace(source))
      return "";
    string filesystem_source;
    if (Uri.TryCreate(source, UriKind.Absolute, out Uri uri))
    {
      if (!uri.IsFile)
        return source;
      filesystem_source = uri.LocalPath;
    }
    else
    {
      filesystem_source = source
        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
    string absolute_source = Path.IsPathRooted(filesystem_source)
      ? Path.GetFullPath(filesystem_source)
      : Path.GetFullPath(Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(input_path)),
        filesystem_source));
    return relative_to_output(absolute_source, output_path);
  }

  private static string relative_to_output(string image_path, string output_path)
  {
    string output_directory = Path.GetDirectoryName(Path.GetFullPath(output_path));
    return Path.GetRelativePath(output_directory, image_path)
      .Replace(Path.DirectorySeparatorChar, '/');
  }
}
