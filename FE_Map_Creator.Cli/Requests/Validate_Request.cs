#nullable disable
namespace FE_Map_Creator.Cli.Requests;

internal sealed class Validate_Request
{
  internal string Input { get; init; }
  internal string Spec { get; init; }
  internal string Algorithm { get; init; }
  internal string Tileset { get; init; }
  internal int? Width { get; init; }
  internal int? Height { get; init; }
  internal string Assets_Dir { get; init; }
  internal string Tileset_Image { get; init; }
  internal string Generation_Data { get; init; }
}
