using System;
using System.IO;

#nullable disable
namespace FE_Map_Creator.Cli;

internal static class Safe_Output
{
  internal static void write(
    string target,
    bool overwrite,
    Action<string> write_temporary)
  {
    if (string.IsNullOrWhiteSpace(target))
      throw new ArgumentException("An output path is required.", nameof (target));
    if (write_temporary == null)
      throw new ArgumentNullException(nameof (write_temporary));
    string full_target = Path.GetFullPath(target);
    if (File.Exists(full_target) && !overwrite)
      throw new IOException($"Output file \"{full_target}\" already exists. Use --force to replace it.");
    string directory = Path.GetDirectoryName(full_target);
    Directory.CreateDirectory(directory);
    string temporary = Path.Combine(
      directory,
      $".{Path.GetFileName(full_target)}.{Guid.NewGuid():N}.tmp");
    try
    {
      write_temporary(temporary);
      File.Move(temporary, full_target, true);
    }
    finally
    {
      if (File.Exists(temporary))
        File.Delete(temporary);
    }
  }
}
