#nullable disable
namespace FE_Map_Creator.Cli.Requests;

/// <summary>Bound options for the <c>tilesets terrain</c> command.</summary>
internal sealed class Tilesets_Terrain_Request
{
  internal string Tileset { get; init; }

  internal string Assets_Dir { get; init; }

  internal bool Json { get; init; }
}
