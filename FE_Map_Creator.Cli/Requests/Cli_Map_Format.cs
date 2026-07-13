namespace FE_Map_Creator.Cli.Requests;

/// <summary>
/// Output map format accepted by the CLI. Mirrors the map file formats supported by
/// the core codecs (text <c>.map</c>, Mappy <c>.mar</c>, and the explicit-tile TMX subset).
/// </summary>
internal enum Cli_Map_Format
{
  Map,
  Mar,
  Tmx
}
