using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

#nullable disable
namespace FE_Map_Creator.Gui.Controls;

public sealed class Tile_Palette : Control
{
  private const int TILE_SIZE = 16;
  private const int COLUMNS = 8;
  private const double DISPLAY_SCALE = 2;

  private static readonly IBrush Background_Brush = new SolidColorBrush(Color.Parse("#1D2028"));
  private static readonly IPen Grid_Pen = new Pen(new SolidColorBrush(Color.FromArgb(45, 255, 255, 255)), 1);
  private static readonly IPen Selection_Pen = new Pen(new SolidColorBrush(Color.Parse("#FFFFFF")), 3);

  private Bitmap _tileset;
  private int _selected_tile;

  public event EventHandler Selected_Tile_Changed;

  public Bitmap Tileset
  {
    get => this._tileset;
    set
    {
      this._tileset = value;
      this._selected_tile = 0;
      this.InvalidateMeasure();
      this.InvalidateVisual();
      this.Selected_Tile_Changed?.Invoke(this, EventArgs.Empty);
    }
  }

  public int Selected_Tile
  {
    get => this._selected_tile;
    set
    {
      int count = this.tile_count;
      int selected = count == 0 ? 0 : Math.Clamp(value, 0, count - 1);
      if (selected == this._selected_tile)
        return;
      this._selected_tile = selected;
      this.InvalidateVisual();
      this.Selected_Tile_Changed?.Invoke(this, EventArgs.Empty);
    }
  }

  private int tile_count => this._tileset == null
    ? 0
    : this._tileset.PixelSize.Width / TILE_SIZE * (this._tileset.PixelSize.Height / TILE_SIZE);

  public Tile_Palette()
  {
    RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
  }

  public override void Render(DrawingContext context)
  {
    base.Render(context);
    context.FillRectangle(Background_Brush, new Rect(this.Bounds.Size));
    if (this._tileset == null)
      return;
    int source_columns = this._tileset.PixelSize.Width / TILE_SIZE;
    double display_size = TILE_SIZE * DISPLAY_SCALE;
    for (int tile = 0; tile < this.tile_count; ++tile)
    {
      int x = tile % COLUMNS;
      int y = tile / COLUMNS;
      Rect source = new Rect(
        tile % source_columns * TILE_SIZE,
        tile / source_columns * TILE_SIZE,
        TILE_SIZE,
        TILE_SIZE);
      Rect destination = new Rect(x * display_size, y * display_size, display_size, display_size);
      context.DrawImage(this._tileset, source, destination);
      context.DrawRectangle(null, Grid_Pen, destination);
    }
    int selected_x = this._selected_tile % COLUMNS;
    int selected_y = this._selected_tile / COLUMNS;
    context.DrawRectangle(
      null,
      Selection_Pen,
      new Rect(
        selected_x * display_size + 1,
        selected_y * display_size + 1,
        display_size - 2,
        display_size - 2));
  }

  protected override Size MeasureOverride(Size availableSize)
  {
    double display_size = TILE_SIZE * DISPLAY_SCALE;
    int rows = Math.Max(1, (this.tile_count + COLUMNS - 1) / COLUMNS);
    return new Size(COLUMNS * display_size, rows * display_size);
  }

  protected override void OnPointerPressed(PointerPressedEventArgs e)
  {
    base.OnPointerPressed(e);
    if (this._tileset == null)
      return;
    Point point = e.GetPosition(this);
    double display_size = TILE_SIZE * DISPLAY_SCALE;
    int x = (int) Math.Floor(point.X / display_size);
    int y = (int) Math.Floor(point.Y / display_size);
    int tile = x + y * COLUMNS;
    if (x >= 0 && x < COLUMNS && tile >= 0 && tile < this.tile_count)
    {
      this.Selected_Tile = tile;
      e.Handled = true;
    }
  }
}
