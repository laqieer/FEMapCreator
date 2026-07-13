using System;
using System.IO;

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
    FileInfo file = new FileInfo(filename);
    if (file.Length != expected_length)
      throw new InvalidDataException($"MAR file length is {file.Length} bytes; expected {expected_length} for {width}x{height}.");
    int[,] tiles = new int[width, height];
    using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
    using (BinaryReader reader = new BinaryReader(stream))
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
    return new Map_Document(tiles, options.Tileset);
  }

  public void write(string filename, Map_Document document, Map_Write_Options options = null)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A map filename is required.", nameof (filename));
    if (document == null)
      throw new ArgumentNullException(nameof (document));
    string directory = Path.GetDirectoryName(Path.GetFullPath(filename));
    Directory.CreateDirectory(directory);
    using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
    using (BinaryWriter writer = new BinaryWriter(stream))
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
    }
  }
}
