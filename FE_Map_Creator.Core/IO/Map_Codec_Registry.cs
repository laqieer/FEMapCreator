using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

#nullable disable
namespace FE_Map_Creator;

public sealed class Map_Codec_Registry
{
  private readonly Dictionary<Map_Format, IMap_Codec> Codecs;

  public Map_Codec_Registry()
  {
    this.Codecs = new Dictionary<Map_Format, IMap_Codec>()
    {
      { Map_Format.Text, new Text_Map_Codec() },
      { Map_Format.Mar, new Mar_Map_Codec() },
      { Map_Format.Tmx, new Tmx_Map_Codec() }
    };
  }

  public IMap_Codec codec(Map_Format format) => this.Codecs[format];

  public Map_Format format_from_path(string filename)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A map filename is required.", nameof (filename));
    switch (Path.GetExtension(filename).ToLowerInvariant())
    {
      case ".map":
        return Map_Format.Text;
      case ".mar":
        return Map_Format.Mar;
      case ".tmx":
        return Map_Format.Tmx;
      default:
        throw new NotSupportedException($"Unsupported map extension \"{Path.GetExtension(filename)}\".");
    }
  }

  public Map_Document read(
    string filename,
    Map_Read_Options options = null,
    Map_Format? format = null)
  {
    return this.codec(format ?? this.format_from_path(filename)).read(filename, options);
  }

  public Map_Document read(
    Stream stream,
    Map_Format format,
    Map_Read_Options options = null)
  {
    if (stream == null)
      throw new ArgumentNullException(nameof (stream));
    return this.codec(format).read(stream, options);
  }

  public async Task<Map_Document> read_async(
    Stream stream,
    Map_Format format,
    Map_Read_Options options = null)
  {
    if (stream == null)
      throw new ArgumentNullException(nameof (stream));
    if (!stream.CanRead)
      throw new ArgumentException("The map stream must be readable.", nameof (stream));
    using (MemoryStream buffer = new MemoryStream())
    {
      await stream.CopyToAsync(buffer);
      buffer.Position = 0;
      return this.codec(format).read(buffer, options);
    }
  }

  public void write(
    string filename,
    Map_Document document,
    Map_Write_Options options = null,
    Map_Format? format = null)
  {
    this.codec(format ?? this.format_from_path(filename)).write(filename, document, options);
  }

  public void write(
    Stream stream,
    Map_Format format,
    Map_Document document,
    Map_Write_Options options = null)
  {
    if (stream == null)
      throw new ArgumentNullException(nameof (stream));
    this.codec(format).write(stream, document, options);
  }

  public async Task write_async(
    Stream stream,
    Map_Format format,
    Map_Document document,
    Map_Write_Options options = null)
  {
    if (stream == null)
      throw new ArgumentNullException(nameof (stream));
    if (!stream.CanWrite)
      throw new ArgumentException("The map stream must be writable.", nameof (stream));
    using (MemoryStream buffer = new MemoryStream())
    {
      this.codec(format).write(buffer, document, options);
      await stream.WriteAsync(buffer.GetBuffer(), 0, checked((int) buffer.Length));
    }
  }
}
