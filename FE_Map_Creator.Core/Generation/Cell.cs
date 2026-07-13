#nullable disable
namespace FE_Map_Creator.Generation;

/// <summary>
/// A simple map-cell coordinate. Used instead of System.Drawing.Point so this project
/// does not need to reference System.Drawing.Common.
/// </summary>
public readonly struct Cell
{
  public int X { get; }
  public int Y { get; }

  public Cell(int x, int y)
  {
    this.X = x;
    this.Y = y;
  }
}
