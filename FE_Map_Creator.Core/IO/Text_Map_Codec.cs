using System;
using System.Globalization;
using System.IO;
using System.Text;

#nullable disable
namespace FE_Map_Creator;

public sealed class Text_Map_Codec : IMap_Codec
{
  public Map_Format Format => Map_Format.Text;

  public Map_Document read(string filename, Map_Read_Options options = null)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A map filename is required.", nameof (filename));
    using (StreamReader reader = new StreamReader(filename))
    {
      string tileset = reader.ReadLine();
      if (tileset == null)
        throw new InvalidDataException("The map is missing its tileset line.");
      tileset = tileset.Trim();
      if (tileset.Length == 0)
        throw new InvalidDataException("The map tileset line cannot be blank.");
      string dimensions = reader.ReadLine();
      if (dimensions == null)
        throw new InvalidDataException("The map is missing its dimensions line.");
      string[] dimension_values = dimensions.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
      if (dimension_values.Length != 2 ||
          !int.TryParse(dimension_values[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height) ||
          !int.TryParse(dimension_values[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width) ||
          width <= 0 ||
          height <= 0)
      {
        throw new InvalidDataException("Map dimensions must be written as \"<height> <width>\" using positive integers.");
      }
      int[,] tiles = new int[width, height];
      for (int y = 0; y < height; ++y)
      {
        string row = reader.ReadLine();
        if (row == null)
          throw new InvalidDataException($"The map ended before row {y + 1} of {height}.");
        string[] values = row.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
        if (values.Length != width)
          throw new InvalidDataException($"Map row {y + 1} contains {values.Length} tiles; expected {width}.");
        for (int x = 0; x < width; ++x)
        {
          if (!int.TryParse(values[x], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tile) || tile < 0)
            throw new InvalidDataException($"Map tile at ({x},{y}) is not a non-negative integer.");
          tiles[x, y] = tile;
        }
      }
      for (int line = height + 3; !reader.EndOfStream; ++line)
      {
        string extra = reader.ReadLine();
        if (!string.IsNullOrWhiteSpace(extra))
          throw new InvalidDataException($"The map contains unexpected trailing content on line {line}.");
      }
      return new Map_Document(tiles, tileset);
    }
  }

  public void write(string filename, Map_Document document, Map_Write_Options options = null)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A map filename is required.", nameof (filename));
    if (document == null)
      throw new ArgumentNullException(nameof (document));
    string tileset = options != null && !string.IsNullOrWhiteSpace(options.Tileset) ?
      options.Tileset :
      document.Tileset;
    if (string.IsNullOrWhiteSpace(tileset))
      throw new InvalidOperationException("Text maps require a tileset identifier.");
    string directory = Path.GetDirectoryName(Path.GetFullPath(filename));
    Directory.CreateDirectory(directory);
    using (StreamWriter writer = new StreamWriter(filename, false, new UTF8Encoding(false)))
    {
      writer.WriteLine(tileset);
      writer.WriteLine($"{document.Height.ToString(CultureInfo.InvariantCulture)} {document.Width.ToString(CultureInfo.InvariantCulture)}");
      StringBuilder row = new StringBuilder();
      for (int y = 0; y < document.Height; ++y)
      {
        row.Clear();
        for (int x = 0; x < document.Width; ++x)
        {
          int tile = document.Tiles[x, y];
          if (tile < 0)
            throw new InvalidDataException($"Map tile at ({x},{y}) cannot be negative.");
          if (x > 0)
            row.Append(' ');
          row.Append(tile.ToString(CultureInfo.InvariantCulture));
        }
        writer.WriteLine(row.ToString());
      }
    }
  }
}
