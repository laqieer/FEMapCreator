#nullable disable
namespace FE_Map_Creator.Cli.Requests;

/// <summary>Bound options for the <c>map edit</c> command.</summary>
internal sealed class Map_Edit_Request
{
  internal string Input { get; init; }

  internal string Output { get; init; }

  internal bool In_Place { get; init; }

  internal string Spec { get; init; }

  internal Cli_Map_Format? Format { get; init; }

  internal int? Width { get; init; }

  internal int? Height { get; init; }

  internal string Tileset { get; init; }

  internal string Assets_Dir { get; init; }

  internal string Tileset_Image { get; init; }

  internal bool Force { get; init; }
}
