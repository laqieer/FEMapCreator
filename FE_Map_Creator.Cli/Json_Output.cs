using System.IO;
using System.Text.Json;

#nullable disable
namespace FE_Map_Creator.Cli;

internal static class Json_Output
{
  private static readonly JsonSerializerOptions Options = new JsonSerializerOptions()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
  };

  internal static void write(TextWriter writer, object value)
  {
    writer.WriteLine(JsonSerializer.Serialize(value, Options));
  }
}
