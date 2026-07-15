using System;

#nullable disable
namespace FE_Map_Creator;

public sealed class Map_Job_Spec
{
  public int Version { get; set; } = 1;

  public string Operation { get; set; }

  public string Input { get; set; }

  public string Template { get; set; }

  public string Output { get; set; }

  public string Format { get; set; }

  public int? Width { get; set; }

  public int? Height { get; set; }

  public string Tileset { get; set; }

  public string TilesetImage { get; set; }

  public string GenerationData { get; set; }

  public string AssetsDir { get; set; }

  public string Algorithm { get; set; }

  public int? ExperimentalSearchNodeLimit { get; set; }

  public int? ExperimentalRestartCount { get; set; }

  public int? ExperimentalNogoodLimit { get; set; }

  public bool? ExperimentalEnableConflictLearning { get; set; }

  public int? Depth { get; set; }

  public int? RepairRadius { get; set; }

  public int? Seed { get; set; }

  public bool[][] Drawn { get; set; }

  public bool[][] Locked { get; set; }

  public int[][] Terrain { get; set; }

  public void validate()
  {
    validate_algorithm();
    if (this.ExperimentalSearchNodeLimit.HasValue && this.ExperimentalSearchNodeLimit.Value <= 0)
      throw new InvalidOperationException("ExperimentalSearchNodeLimit must be positive.");
    if (this.ExperimentalRestartCount.HasValue && this.ExperimentalRestartCount.Value <= 0)
      throw new InvalidOperationException("ExperimentalRestartCount must be positive.");
    if (this.ExperimentalNogoodLimit.HasValue && this.ExperimentalNogoodLimit.Value < 0)
      throw new InvalidOperationException("ExperimentalNogoodLimit must be zero or greater.");
    validate_optional_dimensions();
    Matrix_Dimensions? dimensions = null;
    dimensions = validate_rectangular_rows(this.Drawn, nameof (this.Drawn), dimensions);
    dimensions = validate_rectangular_rows(this.Locked, nameof (this.Locked), dimensions);
    dimensions = validate_rectangular_rows(this.Terrain, nameof (this.Terrain), dimensions);
    if (dimensions.HasValue)
    {
      if (this.Width.HasValue && this.Width.Value != dimensions.Value.Width)
      {
        throw new InvalidOperationException(
          $"Width is {this.Width.Value} but the constraint matrices contain {dimensions.Value.Width} columns.");
      }

      if (this.Height.HasValue && this.Height.Value != dimensions.Value.Height)
      {
        throw new InvalidOperationException(
          $"Height is {this.Height.Value} but the constraint matrices contain {dimensions.Value.Height} rows.");
      }
      this.validate_constraints(dimensions.Value.Width, dimensions.Value.Height);
    }
  }

  private void validate_algorithm()
  {
    if (string.IsNullOrWhiteSpace(this.Algorithm))
      return;
    string algorithm = this.Algorithm.Trim();
    if (!string.Equals(algorithm, "legacy", StringComparison.OrdinalIgnoreCase)
      && !string.Equals(algorithm, "experimental", StringComparison.OrdinalIgnoreCase))
    {
      throw new InvalidOperationException(
        $"Algorithm \"{this.Algorithm}\" is invalid; expected \"legacy\" or \"experimental\".");
    }
  }

  public bool[,] drawn_array(int width, int height, bool default_value)
  {
    return convert(this.Drawn, width, height, default_value, nameof (this.Drawn));
  }

  public bool[,] locked_array(int width, int height)
  {
    return convert(this.Locked, width, height, false, nameof (this.Locked));
  }

  public int[,] terrain_array(int width, int height)
  {
    validate_dimensions(width, height);
    if (this.Terrain == null)
      return new int[width, height];
    validate_rows(this.Terrain, width, height, nameof (this.Terrain));
    int[,] result = new int[width, height];
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
        result[x, y] = this.Terrain[y][x];
    }
    return result;
  }

  public void validate_constraints(int width, int height)
  {
    validate_dimensions(width, height);
    bool[,] locked = this.locked_array(width, height);
    int[,] terrain = this.terrain_array(width, height);
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
      {
        if (locked[x, y] && terrain[x, y] != 0)
          throw new InvalidOperationException($"Cell ({x},{y}) cannot be both locked and terrain-constrained.");
      }
    }
  }

  private static bool[,] convert(
    bool[][] rows,
    int width,
    int height,
    bool default_value,
    string name)
  {
    validate_dimensions(width, height);
    bool[,] result = new bool[width, height];
    if (rows == null)
    {
      if (default_value)
      {
        for (int y = 0; y < height; ++y)
        {
          for (int x = 0; x < width; ++x)
            result[x, y] = true;
        }
      }
      return result;
    }
    validate_rows(rows, width, height, name);
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
        result[x, y] = rows[y][x];
    }
    return result;
  }

  private static void validate_rows<T>(T[][] rows, int width, int height, string name)
  {
    validate_dimensions(width, height);
    Matrix_Dimensions dimensions = validate_rectangular_rows(rows, name, null).Value;
    if (dimensions.Height != height)
      throw new InvalidOperationException($"{name} contains {dimensions.Height} rows; expected {height}.");
    if (dimensions.Width != width)
      throw new InvalidOperationException($"{name} contains {dimensions.Width} columns; expected {width}.");
  }

  private static Matrix_Dimensions? validate_rectangular_rows<T>(
    T[][] rows,
    string name,
    Matrix_Dimensions? expected)
  {
    if (rows == null)
      return expected;
    if (rows.Length == 0)
      throw new InvalidOperationException($"{name} must contain at least one row.");
    int width = -1;
    for (int y = 0; y < rows.Length; ++y)
    {
      if (rows[y] == null)
        throw new InvalidOperationException($"{name} row {y + 1} is missing.");
      if (width < 0)
      {
        width = rows[y].Length;
        if (width == 0)
          throw new InvalidOperationException($"{name} row 1 must contain at least one value.");
      }
      else if (rows[y].Length != width)
      {
        throw new InvalidOperationException(
          $"{name} row {y + 1} contains {rows[y].Length} values; expected {width}.");
      }
    }
    Matrix_Dimensions dimensions = new Matrix_Dimensions(width, rows.Length);
    if (expected.HasValue &&
        (expected.Value.Width != dimensions.Width || expected.Value.Height != dimensions.Height))
    {
      throw new InvalidOperationException(
        $"{name} dimensions {dimensions.Width}x{dimensions.Height} do not match the other constraint matrices {expected.Value.Width}x{expected.Value.Height}.");
    }
    return dimensions;
  }

  private static void validate_dimensions(int width, int height)
  {
    if (width <= 0)
      throw new ArgumentOutOfRangeException(nameof (width), "Constraint width must be positive.");
    if (height <= 0)
      throw new ArgumentOutOfRangeException(nameof (height), "Constraint height must be positive.");
  }

  private void validate_optional_dimensions()
  {
    if (this.Width.HasValue != this.Height.HasValue)
      throw new InvalidOperationException("Width and Height must either both be provided or both be omitted.");
    if (this.Width.HasValue)
      validate_dimensions(this.Width.Value, this.Height.Value);
  }

  private readonly struct Matrix_Dimensions
  {
    public int Width { get; }

    public int Height { get; }

    public Matrix_Dimensions(int width, int height)
    {
      this.Width = width;
      this.Height = height;
    }
  }
}

public sealed class Map_Job_Manifest
{
  public int Version { get; set; } = 1;

  public Map_Job_Spec[] Jobs { get; set; }
}
