#nullable disable
namespace FE_Map_Creator.Cli.Execution;

internal static class Map_Format_Names
{
  internal static string stable(Map_Format format)
  {
    switch (format)
    {
      case Map_Format.Text:
        return "map";
      case Map_Format.Mar:
        return "mar";
      case Map_Format.Tmx:
        return "tmx";
      default:
        throw new System.NotSupportedException($"Unsupported map format \"{format}\".");
    }
  }
}
