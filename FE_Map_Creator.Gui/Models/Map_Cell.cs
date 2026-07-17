#nullable disable
namespace FE_Map_Creator.Gui.Models;

public readonly struct Map_Cell
{
  public int X { get; }

  public int Y { get; }

  public Map_Cell(int x, int y)
  {
    this.X = x;
    this.Y = y;
  }
}
