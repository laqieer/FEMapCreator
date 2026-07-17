#nullable disable
namespace FE_Map_Creator.Cli.Requests;

/// <summary>Bound options for the <c>tilesets list</c> command.</summary>
internal sealed class Tilesets_List_Request
{
  internal string Assets_Dir { get; init; }

  internal bool Json { get; init; }
}
