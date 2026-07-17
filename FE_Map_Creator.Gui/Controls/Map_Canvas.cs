using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using FE_Map_Creator.Gui.Models;
using System;

#nullable disable
namespace FE_Map_Creator.Gui.Controls;

public sealed class Map_Canvas : Control
{
  private const int TILE_SIZE = 16;

  private static readonly IBrush Open_Brush_1 = new SolidColorBrush(Color.Parse("#242833"));
  private static readonly IBrush Open_Brush_2 = new SolidColorBrush(Color.Parse("#2D3340"));
  private static readonly IBrush Invalid_Tile_Brush = new SolidColorBrush(Color.Parse("#8A1D5D"));
  private static readonly IPen Grid_Pen = new Pen(new SolidColorBrush(Color.FromArgb(52, 255, 255, 255)), 1);
  private static readonly IPen Lock_Pen = new Pen(new SolidColorBrush(Color.Parse("#FFD54F")), 2);
  private static readonly IPen Required_Terrain_Pen = new Pen(new SolidColorBrush(Color.Parse("#4FC3F7")), 2);
  private static readonly IPen Forbidden_Terrain_Pen = new Pen(new SolidColorBrush(Color.Parse("#EF5350")), 2);
  private static readonly IBrush Lock_Overlay = new SolidColorBrush(Color.FromArgb(55, 255, 213, 79));
  private static readonly IBrush Required_Terrain_Overlay = new SolidColorBrush(Color.FromArgb(72, 79, 195, 247));
  private static readonly IBrush Forbidden_Terrain_Overlay = new SolidColorBrush(Color.FromArgb(72, 239, 83, 80));
  private static readonly IPen Selection_Pen = new Pen(new SolidColorBrush(Color.Parse("#FFFFFF")), 2);

  private Editor_Session _session;
  private Bitmap _tileset;
  private double _zoom = 2;
  private bool _pointer_down;
  private Map_Cell? _selection_start;
  private Map_Cell? _selection_end;

  public event EventHandler<Map_Cell_Event_Args> Cell_Pressed;

  public event EventHandler<Map_Cell_Event_Args> Cell_Moved;

  public event EventHandler<Map_Cell_Event_Args> Cell_Released;

  public event EventHandler<Map_Cell_Event_Args> Cell_Hovered;

  public event EventHandler Stroke_Cancelled;

  public Editor_Session Session
  {
    get => this._session;
    set
    {
      this._session = value;
      this.InvalidateMeasure();
      this.InvalidateVisual();
    }
  }

  public Bitmap Tileset
  {
    get => this._tileset;
    set
    {
      this._tileset = value;
      this.InvalidateVisual();
    }
  }

  public double Zoom
  {
    get => this._zoom;
    set
    {
      this._zoom = Math.Clamp(value, 0.5, 6);
      this.InvalidateMeasure();
      this.InvalidateVisual();
    }
  }

  public Map_Canvas()
  {
    this.Focusable = true;
    RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.None);
  }

  public void set_selection(Map_Cell? start, Map_Cell? end)
  {
    this._selection_start = start;
    this._selection_end = end;
    this.InvalidateVisual();
  }

  public override void Render(DrawingContext context)
  {
    base.Render(context);
    if (this._session == null)
      return;
    double tile_size = TILE_SIZE * this._zoom;
    int tileset_columns = this._tileset == null ? 0 : this._tileset.PixelSize.Width / TILE_SIZE;
    int tileset_count = this._tileset == null
      ? 0
      : tileset_columns * (this._tileset.PixelSize.Height / TILE_SIZE);

    for (int y = 0; y < this._session.Height; ++y)
    {
      for (int x = 0; x < this._session.Width; ++x)
      {
        Rect destination = new Rect(x * tile_size, y * tile_size, tile_size, tile_size);
        int tile = this._session.Tiles[x, y];
        if (!this._session.Drawn[x, y])
        {
          context.FillRectangle(((x + y) & 1) == 0 ? Open_Brush_1 : Open_Brush_2, destination);
        }
        else if (this._tileset != null && tile >= 0 && tile < tileset_count)
        {
          Rect source = new Rect(
            tile % tileset_columns * TILE_SIZE,
            tile / tileset_columns * TILE_SIZE,
            TILE_SIZE,
            TILE_SIZE);
          context.DrawImage(this._tileset, source, destination);
        }
        else
        {
          context.FillRectangle(Invalid_Tile_Brush, destination);
        }

        int terrain = this._session.Terrain[x, y];
        if (terrain > 0)
        {
          context.FillRectangle(Required_Terrain_Overlay, destination);
          context.DrawRectangle(null, Required_Terrain_Pen, inset(destination, 1));
        }
        else if (terrain < 0)
        {
          context.FillRectangle(Forbidden_Terrain_Overlay, destination);
          context.DrawRectangle(null, Forbidden_Terrain_Pen, inset(destination, 1));
          context.DrawLine(
            Forbidden_Terrain_Pen,
            destination.TopLeft + new Vector(2, 2),
            destination.BottomRight - new Vector(2, 2));
          context.DrawLine(
            Forbidden_Terrain_Pen,
            destination.TopRight + new Vector(-2, 2),
            destination.BottomLeft + new Vector(2, -2));
        }
        if (this._session.Locked[x, y])
        {
          context.FillRectangle(Lock_Overlay, destination);
          context.DrawRectangle(null, Lock_Pen, inset(destination, 2));
        }
      }
    }

    for (int x = 0; x <= this._session.Width; ++x)
    {
      double position = x * tile_size;
      context.DrawLine(Grid_Pen, new Point(position, 0), new Point(position, this._session.Height * tile_size));
    }
    for (int y = 0; y <= this._session.Height; ++y)
    {
      double position = y * tile_size;
      context.DrawLine(Grid_Pen, new Point(0, position), new Point(this._session.Width * tile_size, position));
    }

    if (this._selection_start.HasValue && this._selection_end.HasValue)
    {
      Map_Cell start = this._selection_start.Value;
      Map_Cell end = this._selection_end.Value;
      int left = Math.Min(start.X, end.X);
      int top = Math.Min(start.Y, end.Y);
      int right = Math.Max(start.X, end.X);
      int bottom = Math.Max(start.Y, end.Y);
      Rect selection = new Rect(
        left * tile_size + 1,
        top * tile_size + 1,
        (right - left + 1) * tile_size - 2,
        (bottom - top + 1) * tile_size - 2);
      context.DrawRectangle(null, Selection_Pen, selection);
    }
  }

  protected override Size MeasureOverride(Size availableSize)
  {
    if (this._session == null)
      return new Size(320, 320);
    return new Size(
      this._session.Width * TILE_SIZE * this._zoom,
      this._session.Height * TILE_SIZE * this._zoom);
  }

  protected override void OnPointerPressed(PointerPressedEventArgs e)
  {
    base.OnPointerPressed(e);
    PointerPoint point = e.GetCurrentPoint(this);
    if (!point.Properties.IsLeftButtonPressed || !this.try_get_cell(point.Position, out Map_Cell cell))
      return;
    this._pointer_down = true;
    e.Pointer.Capture(this);
    this.Focus();
    this.Cell_Pressed?.Invoke(this, new Map_Cell_Event_Args(cell));
    e.Handled = true;
  }

  protected override void OnPointerMoved(PointerEventArgs e)
  {
    base.OnPointerMoved(e);
    if (!this.try_get_cell(e.GetPosition(this), out Map_Cell cell))
      return;
    this.Cell_Hovered?.Invoke(this, new Map_Cell_Event_Args(cell));
    if (this._pointer_down)
    {
      this.Cell_Moved?.Invoke(this, new Map_Cell_Event_Args(cell));
      e.Handled = true;
    }
  }

  protected override void OnPointerReleased(PointerReleasedEventArgs e)
  {
    base.OnPointerReleased(e);
    if (!this._pointer_down)
      return;
    this._pointer_down = false;
    e.Pointer.Capture(null);
    if (this.try_get_cell(e.GetPosition(this), out Map_Cell cell))
      this.Cell_Released?.Invoke(this, new Map_Cell_Event_Args(cell));
    else
      this.Stroke_Cancelled?.Invoke(this, EventArgs.Empty);
    e.Handled = true;
  }

  protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
  {
    base.OnPointerCaptureLost(e);
    bool was_drawing = this._pointer_down;
    this._pointer_down = false;
    if (was_drawing)
      this.Stroke_Cancelled?.Invoke(this, EventArgs.Empty);
  }

  private bool try_get_cell(Point point, out Map_Cell cell)
  {
    cell = default;
    if (this._session == null)
      return false;
    double tile_size = TILE_SIZE * this._zoom;
    int x = (int) Math.Floor(point.X / tile_size);
    int y = (int) Math.Floor(point.Y / tile_size);
    if (this._session.is_off_map(x, y))
      return false;
    cell = new Map_Cell(x, y);
    return true;
  }

  private static Rect inset(Rect rect, double amount)
  {
    return new Rect(
      rect.X + amount,
      rect.Y + amount,
      Math.Max(0, rect.Width - amount * 2),
      Math.Max(0, rect.Height - amount * 2));
  }
}
