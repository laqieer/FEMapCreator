#nullable disable
namespace FE_Map_Creator.Cli.Requests;

/// <summary>Bound options for the <c>map inspect</c> command.</summary>
internal sealed class Map_Inspect_Request
{
  internal string Input { get; init; }

  internal int? Width { get; init; }

  internal int? Height { get; init; }

  internal string Tileset { get; init; }

  internal bool Json { get; init; }
}
