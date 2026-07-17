using System;
using System.IO;
using System.Text;

#nullable disable
namespace FE_Map_Creator;

public sealed class Mar_Map_Codec : IMap_Codec
{
  private const int TILE_SCALE = 32;

  public Map_Format Format => Map_Format.Mar;

  public Map_Document read(string filename, Map_Read_Options options = null)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A map filename is required.", nameof (filename));
    using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
      return this.read(stream, options);
  }

  public Map_Document read(Stream stream, Map_Read_Options options = null)
  {
    if (stream == null)
      throw new ArgumentNullException(nameof (stream));
    if (!stream.CanRead)
      throw new ArgumentException("The map stream must be readable.", nameof (stream));
    if (options == null || !options.Width.HasValue || !options.Height.HasValue ||
        options.Width.Value <= 0 || options.Height.Value <= 0)
    {
      throw new InvalidDataException("MAR input requires positive width and height metadata.");
    }
    if (string.IsNullOrWhiteSpace(options.Tileset))
      throw new InvalidDataException("MAR input requires a tileset identifier.");
    int width = options.Width.Value;
    int height = options.Height.Value;
    long expected_length = checked((long) width * height * sizeof (short));
    if (stream.CanSeek)
    {
      long actual_length = stream.Length - stream.Position;
      if (actual_length != expected_length)
        throw new InvalidDataException($"MAR stream length is {actual_length} bytes; expected {expected_length} for {width}x{height}.");
    }
    int[,] tiles = new int[width, height];
    using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
    {
      try
      {
        for (int y = 0; y < height; ++y)
        {
          for (int x = 0; x < width; ++x)
          {
            short value = reader.ReadInt16();
            if (value % TILE_SCALE != 0)
              throw new InvalidDataException($"MAR tile at ({x},{y}) has invalid encoded value {value}.");
            tiles[x, y] = value / TILE_SCALE;
          }
        }
      }
      catch (EndOfStreamException ex)
      {
        throw new InvalidDataException(
          $"MAR stream ended before {expected_length} bytes were read for {width}x{height}.", ex);
      }
      if (!stream.CanSeek && reader.BaseStream.ReadByte() != -1)
        throw new InvalidDataException($"MAR stream contains more than {expected_length} bytes for {width}x{height}.");
    }
    return new Map_Document(tiles, options.Tileset);
  }

  public void write(string filename, Map_Document document, Map_Write_Options options = null)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A map filename is required.", nameof (filename));
    string directory = Path.GetDirectoryName(Path.GetFullPath(filename));
    Directory.CreateDirectory(directory);
    using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
      this.write(stream, document, options);
  }

  public void write(Stream stream, Map_Document document, Map_Write_Options options = null)
  {
    if (stream == null)
      throw new ArgumentNullException(nameof (stream));
    if (!stream.CanWrite)
      throw new ArgumentException("The map stream must be writable.", nameof (stream));
    if (document == null)
      throw new ArgumentNullException(nameof (document));
    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
    {
      for (int y = 0; y < document.Height; ++y)
      {
        for (int x = 0; x < document.Width; ++x)
        {
          int tile = document.Tiles[x, y];
          long encoded = (long) tile * TILE_SCALE;
          if (encoded < short.MinValue || encoded > short.MaxValue)
          {
            throw new InvalidDataException(
              $"Map tile at ({x},{y}) with index {tile} is outside the MAR range {short.MinValue / TILE_SCALE} to {short.MaxValue / TILE_SCALE}.");
          }
          writer.Write((short) encoded);
        }
      }
      writer.Flush();
    }
  }
}
