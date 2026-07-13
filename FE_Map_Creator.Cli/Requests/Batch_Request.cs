#nullable disable
namespace FE_Map_Creator.Cli.Requests;

/// <summary>Bound options for the <c>batch --manifest</c> command.</summary>
internal sealed class Batch_Request
{
  internal string Manifest { get; init; }

  internal bool Fail_Fast { get; init; }
}
