// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Tileset_Palette_Form
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

#nullable disable
namespace FE_Map_Creator;

public class Tileset_Palette_Form : Form
{
  private int Tileset_Width;
  private int Tileset_Height;
  private bool Selecting_Tile;
  private bool Selecting_Edit_Tiles;
  private Point Selecting_Base_Tile;
  private Point Selected_Base_Tile;
  private Size Selected_Tile_Size = new Size(1, 1);
  private HashSet<int> Selected_Edit_Tiles = new HashSet<int>();
  private int[,] Selected_Tile_Brush;
  private bool Multiple_Edit_Disclaimer_Accepted;
  private Size Edit_Form_Size;
  private IContainer components = new Container();
  private TableLayoutPanel TilesetPaletteTable;
  private PictureBox pictureBox1;
  private Panel TilesetPanel;
  private Button EditTileButton;
  private FlowLayoutPanel flowLayoutPanel1;
  private CheckBox TerrainCheckBox;

  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public int index
  {
    set
    {
      this.Selected_Base_Tile = new Point(value % this.Tileset_Width, value / this.Tileset_Width);
      this.Selected_Tile_Size = new Size(1, 1);
      this.Selected_Tile_Brush = (int[,]) null;
      this.Selected_Edit_Tiles.Clear();
      this.refresh_edit_button();
      this.pictureBox1.Invalidate();
      this.EditTileButton.Enabled = this.can_edit_tileset;
      this.refresh_edit_button();
    }
  }

  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public int[,] indices
  {
    get
    {
      if (this.Selected_Tile_Brush != null)
        return this.Selected_Tile_Brush;
      if (this.Selected_Edit_Tiles.Count > 0)
        return new int[0, 0];
      int[,] indices = new int[this.Selected_Tile_Size.Width, this.Selected_Tile_Size.Height];
      for (int index1 = 0; index1 < this.Selected_Tile_Size.Height; ++index1)
      {
        for (int index2 = 0; index2 < this.Selected_Tile_Size.Width; ++index2)
          indices[index2, index1] = this.Selected_Base_Tile.X + index2 + (this.Selected_Base_Tile.Y + index1) * this.Tileset_Width;
      }
      return indices;
    }
    set
    {
      if (value.GetLength(0) == 1 && value.GetLength(1) == 1)
      {
        this.index = value[0, 0];
      }
      else
      {
        this.Selected_Tile_Brush = value;
        this.Selected_Edit_Tiles.Clear();
        this.refresh_edit_button();
        this.pictureBox1.Invalidate();
        this.EditTileButton.Enabled = this.can_edit_tileset;
        this.refresh_edit_button();
      }
    }
  }

  private bool can_edit_tileset
  {
    get => (this.Owner as FE_Map_Creator_Form).can_edit_tileset && this.Selected_Tile_Brush == null;
  }

  private bool multiple_tiles_selected
  {
    get => this.Selected_Edit_Tiles.Count > 1 || this.indices.Length > 1;
  }

  public Tileset_Palette_Form()
  {
    this.InitializeComponent();
    this.pictureBox1.MouseDown += new MouseEventHandler(this.pictureBox1_MouseDown);
    this.pictureBox1.MouseMove += new MouseEventHandler(this.pictureBox1_MouseMove);
    this.pictureBox1.MouseUp += new MouseEventHandler(this.pictureBox1_MouseUp);
    this.pictureBox1.Paint += new PaintEventHandler(this.pictureBox1_Paint);
  }

  public void load_tileset(string filename)
  {
    if (filename == "")
      return;
    if (this.pictureBox1.Image != null)
    {
      this.pictureBox1.Image.Dispose();
      this.pictureBox1.Image = (Image) null;
    }
    this.pictureBox1.Image = (Image) new Bitmap(filename);
    this.Selected_Base_Tile = new Point(0, 0);
    this.Selected_Tile_Size = new Size(1, 1);
    this.Selected_Tile_Brush = (int[,]) null;
    this.Selected_Edit_Tiles.Clear();
    this.EditTileButton.Enabled = this.can_edit_tileset;
    this.refresh_edit_button();
    this.Tileset_Width = this.pictureBox1.Image.Width / 16 /*0x10*/;
    this.Tileset_Height = this.pictureBox1.Image.Height / 16 /*0x10*/;
    this.update_form_size(this.pictureBox1.Image.Width);
    this.Text = $"Tileset: {Path.GetFileNameWithoutExtension(filename)}";
  }

  public void refresh_edit_ready()
  {
    this.EditTileButton.Enabled = !this.Selecting_Tile && this.can_edit_tileset && this.Selected_Tile_Size.Width == 1 && this.Selected_Tile_Size.Height == 1;
    this.pictureBox1.Invalidate();
  }

  private void update_form_size(int width)
  {
    this.MaximumSize = new Size(width + (SystemInformation.FrameBorderSize.Width * 2 + SystemInformation.VerticalScrollBarWidth), Tileset_Palette_Form.all_screen_height());
    this.Size = new Size(Math.Min(width + (SystemInformation.FrameBorderSize.Width * 2 + SystemInformation.VerticalScrollBarWidth), Screen.GetBounds((Control) this).Width), this.Size.Height);
  }

  public static int all_screen_height()
  {
    return ((IEnumerable<Screen>) Screen.AllScreens).Sum<Screen>((Func<Screen, int>) (s => s.Bounds.Height));
  }

  private void refresh_edit_button()
  {
    this.EditTileButton.Text = !this.multiple_tiles_selected || !this.EditTileButton.Enabled ? "Edit Tile" : "Edit Multiple Tiles";
  }

  private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
  {
    if (this.pictureBox1.Image == null)
      return;
    this.Selected_Tile_Brush = (int[,]) null;
    this.Selecting_Edit_Tiles = this.can_edit_tileset && (Control.ModifierKeys & Keys.Control) == Keys.Control;
    if (!this.Selecting_Edit_Tiles)
      this.Selected_Edit_Tiles.Clear();
    else if (this.Selected_Edit_Tiles.Count == 0)
    {
      for (int index1 = 0; index1 < this.Selected_Tile_Size.Height; ++index1)
      {
        for (int index2 = 0; index2 < this.Selected_Tile_Size.Width; ++index2)
        {
          if (e.Button == MouseButtons.Left)
            this.Selected_Edit_Tiles.Add(this.Tileset_Width * (this.Selected_Base_Tile.Y + index1) + (this.Selected_Base_Tile.X + index2));
          else
            this.Selected_Edit_Tiles.Remove(this.Tileset_Width * (this.Selected_Base_Tile.Y + index1) + (this.Selected_Base_Tile.X + index2));
        }
      }
    }
    this.Selecting_Tile = true;
    this.Selected_Base_Tile = this.Selecting_Base_Tile = new Point(e.X / 16 /*0x10*/, e.Y / 16 /*0x10*/);
    this.Selected_Tile_Size = new Size(1, 1);
    this.refresh_edit_button();
    this.pictureBox1.Invalidate();
  }

  private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
  {
    if (!this.Selecting_Tile)
      return;
    Point point1 = new Point(Math.Min(this.Tileset_Width - 1, Math.Max(0, e.X / 16 /*0x10*/)), Math.Min(this.Tileset_Height - 1, Math.Max(0, e.Y / 16 /*0x10*/)));
    if (e.Button == MouseButtons.Right && !this.Selecting_Edit_Tiles)
      this.Selecting_Base_Tile = point1;
    Point point2 = new Point(Math.Min(this.Selecting_Base_Tile.X, point1.X), Math.Min(this.Selecting_Base_Tile.Y, point1.Y));
    Size size = new Size(Math.Abs(this.Selecting_Base_Tile.X - point1.X) + 1, Math.Abs(this.Selecting_Base_Tile.Y - point1.Y) + 1);
    if (!(this.Selected_Base_Tile != point2) && !(this.Selected_Tile_Size != size))
      return;
    this.Selected_Base_Tile = point2;
    this.Selected_Tile_Size = size;
    this.pictureBox1.Invalidate();
  }

  private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
  {
    this.EditTileButton.Enabled = this.can_edit_tileset;
    if (this.Selecting_Edit_Tiles)
    {
      for (int index1 = 0; index1 < this.Selected_Tile_Size.Height; ++index1)
      {
        for (int index2 = 0; index2 < this.Selected_Tile_Size.Width; ++index2)
        {
          if (e.Button == MouseButtons.Left)
            this.Selected_Edit_Tiles.Add(this.Tileset_Width * (this.Selected_Base_Tile.Y + index1) + (this.Selected_Base_Tile.X + index2));
          else
            this.Selected_Edit_Tiles.Remove(this.Tileset_Width * (this.Selected_Base_Tile.Y + index1) + (this.Selected_Base_Tile.X + index2));
        }
      }
      this.pictureBox1.Invalidate();
    }
    this.refresh_edit_button();
    this.tile_select();
  }

  public event EventHandler Tile_Selected;

  private void tile_select()
  {
    this.Selecting_Tile = false;
    this.Selecting_Edit_Tiles = false;
    this.Tile_Selected((object) this, new EventArgs());
  }

  private void pictureBox1_Paint(object sender, PaintEventArgs e)
  {
    if (this.pictureBox1.Image == null || this.Selected_Tile_Brush != null)
      return;
    short index1 = (short) (this.Selected_Base_Tile.X + this.Selected_Base_Tile.Y * this.Tileset_Width);
    if (this.TerrainCheckBox.Checked)
      this.draw_terrain_colors(e.Graphics);
    else
      this.draw_matching_tiles(e.Graphics, index1);
    if (this.Selected_Edit_Tiles.Count > 0)
    {
      Dictionary<Rectangle, int> source = new Dictionary<Rectangle, int>();
      foreach (int selectedEditTile in this.Selected_Edit_Tiles)
      {
        for (int width = 0; width < 2; ++width)
        {
          for (int index2 = 0; index2 < 2; ++index2)
          {
            Rectangle key1 = new Rectangle(selectedEditTile % this.Tileset_Width + (1 - width) * index2, selectedEditTile / this.Tileset_Width + width * index2, width, 1 - width);
            if (!source.ContainsKey(key1))
              source.Add(key1, 0);
            Dictionary<Rectangle, int> dictionary;
            Rectangle key2;
            (dictionary = source)[key2 = key1] = dictionary[key2] + 1;
          }
        }
      }
      int num = 16 /*0x10*/;
      using (Pen pen = new Pen(Brushes.Black, 4f))
      {
        foreach (KeyValuePair<Rectangle, int> keyValuePair in source.Where<KeyValuePair<Rectangle, int>>((Func<KeyValuePair<Rectangle, int>, bool>) (pair => pair.Value == 1)))
        {
          if (keyValuePair.Key.Width == 1)
            e.Graphics.DrawLine(pen, keyValuePair.Key.Left * num - 2, keyValuePair.Key.Top * num, keyValuePair.Key.Right * num + 2, keyValuePair.Key.Bottom * num);
          else
            e.Graphics.DrawLine(pen, keyValuePair.Key.Left * num, keyValuePair.Key.Top * num - 2, keyValuePair.Key.Right * num, keyValuePair.Key.Bottom * num + 2);
        }
      }
      using (Pen pen = new Pen(Brushes.Gray, 2f))
      {
        foreach (KeyValuePair<Rectangle, int> keyValuePair in source.Where<KeyValuePair<Rectangle, int>>((Func<KeyValuePair<Rectangle, int>, bool>) (pair => pair.Value == 1)))
        {
          if (keyValuePair.Key.Width == 1)
            e.Graphics.DrawLine(pen, keyValuePair.Key.Left * num - 1, keyValuePair.Key.Top * num, keyValuePair.Key.Right * num + 1, keyValuePair.Key.Bottom * num);
          else
            e.Graphics.DrawLine(pen, keyValuePair.Key.Left * num, keyValuePair.Key.Top * num - 1, keyValuePair.Key.Right * num, keyValuePair.Key.Bottom * num + 1);
        }
      }
    }
    if (this.Selected_Edit_Tiles.Count != 0 && !this.Selecting_Edit_Tiles)
      return;
    int x = this.Selected_Base_Tile.X * 16 /*0x10*/;
    int y = this.Selected_Base_Tile.Y * 16 /*0x10*/;
    int width1 = this.Selected_Tile_Size.Width * 16 /*0x10*/;
    int height = this.Selected_Tile_Size.Height * 16 /*0x10*/;
    using (Pen pen = new Pen(Brushes.Black, 4f))
      e.Graphics.DrawRectangles(pen, new Rectangle[1]
      {
        new Rectangle(x, y, width1, height)
      });
    using (Pen pen = new Pen(Brushes.White, 2f))
      e.Graphics.DrawRectangles(pen, new Rectangle[1]
      {
        new Rectangle(x, y, width1, height)
      });
  }

  private void draw_terrain_colors(Graphics g)
  {
    int[] terrainTypes = (this.Owner as FE_Map_Creator_Form).get_terrain_types();
    if (terrainTypes == null)
      return;
    for (int y = 0; y < this.Tileset_Height; ++y)
    {
      for (int x = 0; x < this.Tileset_Width; ++x)
        Terrain_Color_Data.draw(g, x, y, terrainTypes[y * this.Tileset_Width + x], 16 /*0x10*/, true);
    }
  }

  private void draw_matching_tiles(Graphics g, short index)
  {
    if (!this.can_edit_tileset || this.Selected_Tile_Size.Width != 1 || this.Selected_Tile_Size.Height != 1 || this.Selected_Edit_Tiles.Count != 0)
      return;
    using (Pen p = new Pen(Brushes.White, 1f))
    {
      using (Pen p1 = new Pen(Brushes.Black, 3f))
      {
        using (Pen p2 = new Pen(Brushes.Black, 1f))
        {
          HashSet<short> shortSet = new HashSet<short>();
          for (byte dir = 1; dir <= (byte) 9; dir += (byte) 2)
            shortSet.UnionWith((IEnumerable<short>) (this.Owner as FE_Map_Creator_Form).same_side(index, dir));
          foreach (short num in shortSet)
          {
            short tile = num;
            if ((int) tile != (int) index)
            {
              HashSet<byte> matching_corners = new HashSet<byte>(Enumerable.Range(0, 5).Select<int, int>((Func<int, int>) (i => i * 2 + 1)).Select<int, byte>((Func<int, byte>) (dir => (byte) dir)).Where<byte>((Func<byte, bool>) (dir => (this.Owner as FE_Map_Creator_Form).same_side(index, dir).Contains(tile))));
              if (matching_corners.Count == 1 || matching_corners.Count == 4)
                this.draw_corners(tile, matching_corners, g, p, p1, p2);
              else if (matching_corners.Count == 2 && (matching_corners.Contains((byte) 1) && matching_corners.Contains((byte) 9) || matching_corners.Contains((byte) 3) && matching_corners.Contains((byte) 7)))
                this.draw_corners(tile, matching_corners, g, p, p1, p2);
              else if (matching_corners.Count == 2)
                this.draw_one_side(tile, matching_corners, g, p, p1, p2);
              else
                this.draw_sides_and_corners(tile, matching_corners, g, p, p1, p2);
            }
          }
        }
      }
    }
  }

  private void draw_corners(
    short tile,
    HashSet<byte> matching_corners,
    Graphics g,
    Pen p,
    Pen p1,
    Pen p2)
  {
    int num1 = (int) tile % this.Tileset_Width * 16 /*0x10*/ + 2;
    int num2 = (int) tile / this.Tileset_Width * 16 /*0x10*/ + 2;
    int width = 1;
    int height = 1;
    int num3 = 5;
    for (byte index = 1; index <= (byte) 9; index += (byte) 2)
    {
      if (index != (byte) 5)
      {
        int num4 = ((int) index - 1) % 3;
        int num5 = 2 - ((int) index - 1) / 3;
        if (matching_corners.Contains(index))
        {
          g.DrawRectangles(p1, new Rectangle[1]
          {
            new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
          });
          g.DrawRectangles(p, new Rectangle[1]
          {
            new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
          });
        }
        else
          g.DrawRectangles(p2, new Rectangle[1]
          {
            new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
          });
      }
    }
  }

  private void draw_sides(
    short tile,
    HashSet<byte> matching_sides,
    Graphics g,
    Pen p,
    Pen p1,
    Pen p2)
  {
    int num1 = (int) tile % this.Tileset_Width * 16 /*0x10*/ + 2;
    int num2 = (int) tile / this.Tileset_Width * 16 /*0x10*/ + 2;
    int width = 1;
    int height = 1;
    int num3 = 5;
    for (byte index = 2; index <= (byte) 8; index += (byte) 2)
    {
      int num4 = ((int) index - 1) % 3;
      int num5 = 2 - ((int) index - 1) / 3;
      if (matching_sides.Contains(index))
      {
        g.DrawRectangles(p1, new Rectangle[1]
        {
          new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
        });
        g.DrawRectangles(p, new Rectangle[1]
        {
          new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
        });
      }
      else
        g.DrawRectangles(p2, new Rectangle[1]
        {
          new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
        });
    }
  }

  private void draw_one_side(
    short tile,
    HashSet<byte> matching_corners,
    Graphics g,
    Pen p,
    Pen p1,
    Pen p2)
  {
    byte num1 = (byte) Tile_Matching_Data.side_from_corners(matching_corners);
    HashSet<byte> byteSet = new HashSet<byte>(Enumerable.Range(0, 5).Select<int, int>((Func<int, int>) (i => i * 2 + 1)).Select<int, byte>((Func<int, byte>) (dir => (byte) dir)).Except<byte>((IEnumerable<byte>) matching_corners));
    int num2 = (int) tile % this.Tileset_Width * 16 /*0x10*/ + 2;
    int num3 = (int) tile / this.Tileset_Width * 16 /*0x10*/ + 2;
    int width = 1;
    int height = 1;
    int num4 = 5;
    for (byte index = 1; index <= (byte) 9; ++index)
    {
      if (index != (byte) 5)
      {
        int num5 = ((int) index - 1) % 3;
        int num6 = 2 - ((int) index - 1) / 3;
        if ((int) num1 == (int) index || matching_corners.Contains(index))
        {
          g.DrawRectangles(p1, new Rectangle[1]
          {
            new Rectangle(num2 + num4 * num5, num3 + num4 * num6, width, height)
          });
          g.DrawRectangles(p, new Rectangle[1]
          {
            new Rectangle(num2 + num4 * num5, num3 + num4 * num6, width, height)
          });
        }
        else if (byteSet.Contains(index))
          g.DrawRectangles(p2, new Rectangle[1]
          {
            new Rectangle(num2 + num4 * num5, num3 + num4 * num6, width, height)
          });
      }
    }
  }

  private void draw_sides_and_corners(
    short tile,
    HashSet<byte> matching_corners,
    Graphics g,
    Pen p,
    Pen p1,
    Pen p2)
  {
    HashSet<byte> byteSet1 = new HashSet<byte>();
    if (matching_corners.Contains((byte) 1) && matching_corners.Contains((byte) 3))
      byteSet1.Add((byte) 2);
    if (matching_corners.Contains((byte) 1) && matching_corners.Contains((byte) 7))
      byteSet1.Add((byte) 4);
    if (matching_corners.Contains((byte) 3) && matching_corners.Contains((byte) 9))
      byteSet1.Add((byte) 6);
    if (matching_corners.Contains((byte) 7) && matching_corners.Contains((byte) 9))
      byteSet1.Add((byte) 8);
    byteSet1.UnionWith((IEnumerable<byte>) matching_corners);
    HashSet<byte> byteSet2 = new HashSet<byte>(Enumerable.Range(0, 5).Select<int, int>((Func<int, int>) (i => i * 2 + 1)).Select<int, byte>((Func<int, byte>) (dir => (byte) dir)).Except<byte>((IEnumerable<byte>) matching_corners));
    int num1 = (int) tile % this.Tileset_Width * 16 /*0x10*/ + 2;
    int num2 = (int) tile / this.Tileset_Width * 16 /*0x10*/ + 2;
    int width = 1;
    int height = 1;
    int num3 = 5;
    for (byte index = 1; index <= (byte) 9; ++index)
    {
      if (index != (byte) 5)
      {
        int num4 = ((int) index - 1) % 3;
        int num5 = 2 - ((int) index - 1) / 3;
        if (byteSet1.Contains(index))
        {
          g.DrawRectangles(p1, new Rectangle[1]
          {
            new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
          });
          g.DrawRectangles(p, new Rectangle[1]
          {
            new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
          });
        }
        else if (byteSet2.Contains(index))
          g.DrawRectangles(p2, new Rectangle[1]
          {
            new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
          });
      }
    }
  }

  private void draw_all_sides_and_corners(
    short tile,
    HashSet<byte> matching_dirs,
    Graphics g,
    Pen p,
    Pen p1,
    Pen p2)
  {
    int num1 = (int) tile % this.Tileset_Width * 16 /*0x10*/ + 2;
    int num2 = (int) tile / this.Tileset_Width * 16 /*0x10*/ + 2;
    int width = 1;
    int height = 1;
    int num3 = 5;
    for (byte index = 1; index <= (byte) 9; ++index)
    {
      if (index != (byte) 5)
      {
        int num4 = ((int) index - 1) % 3;
        int num5 = 2 - ((int) index - 1) / 3;
        if (matching_dirs.Contains(index))
        {
          g.DrawRectangles(p1, new Rectangle[1]
          {
            new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
          });
          g.DrawRectangles(p, new Rectangle[1]
          {
            new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
          });
        }
        else
          g.DrawRectangles(p2, new Rectangle[1]
          {
            new Rectangle(num1 + num3 * num4, num2 + num3 * num5, width, height)
          });
      }
    }
  }

  private void EditTileButton_Click(object sender, EventArgs e)
  {
    List<short> indices1;
    if (this.multiple_tiles_selected)
    {
      if (this.Selected_Edit_Tiles.Count > 0)
      {
        indices1 = new List<short>(this.Selected_Edit_Tiles.Select<int, short>((Func<int, short>) (x => (short) x)));
      }
      else
      {
        indices1 = new List<short>();
        int[,] indices2 = this.indices;
        int upperBound1 = indices2.GetUpperBound(0);
        int upperBound2 = indices2.GetUpperBound(1);
        for (int lowerBound1 = indices2.GetLowerBound(0); lowerBound1 <= upperBound1; ++lowerBound1)
        {
          for (int lowerBound2 = indices2.GetLowerBound(1); lowerBound2 <= upperBound2; ++lowerBound2)
          {
            short num = (short) indices2[lowerBound1, lowerBound2];
            indices1.Add(num);
          }
        }
      }
    }
    else
      indices1 = new List<short>()
      {
        (short) (this.Selected_Base_Tile.X + this.Selected_Base_Tile.Y * this.Tileset_Width)
      };
    List<short> shortList = (this.Owner as FE_Map_Creator_Form).tileset_tiles_without_redundant(indices1);
    Tile_Data data;
    if (shortList.Count > 1)
    {
      if (!this.Multiple_Edit_Disclaimer_Accepted && MessageBox.Show("This will set the same map generation rules to\r\nall selected tiles. If they are not effectively\r\nidentical and interchangable in usage, there\r\ncould be mistakes in map generation.", "Editing multiple tiles", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation) != DialogResult.OK)
        return;
      this.Multiple_Edit_Disclaimer_Accepted = true;
      data = new Tile_Data(shortList.Select<short, Tile_Data>((Func<short, Tile_Data>) (index => (this.Owner as FE_Map_Creator_Form).tile_data((int) index))));
    }
    else
    {
      short index = shortList.First<short>();
      data = (this.Owner as FE_Map_Creator_Form).tile_data((int) index);
      shortList = new List<short>() { index };
    }
    shortList.Sort();
    Tile_Edit_Form tileEditForm = new Tile_Edit_Form();
    if (!this.Edit_Form_Size.IsEmpty)
      tileEditForm.Size = this.Edit_Form_Size;
    tileEditForm.setup(new Bitmap(this.pictureBox1.Image), shortList, data, (this.Owner as FE_Map_Creator_Form).identical_tiles);
    if (tileEditForm.ShowDialog() == DialogResult.OK)
    {
      foreach (short index in shortList)
        (this.Owner as FE_Map_Creator_Form).set_tile_data(index, tileEditForm.data);
    }
    this.Edit_Form_Size = tileEditForm.Size;
    tileEditForm.Close();
  }

  private void TerrainCheckBox_CheckedChanged(object sender, EventArgs e)
  {
    this.pictureBox1.Invalidate();
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing && this.components != null)
      this.components.Dispose();
    base.Dispose(disposing);
  }

  private void InitializeComponent()
  {
    this.TilesetPaletteTable = new TableLayoutPanel();
    this.flowLayoutPanel1 = new FlowLayoutPanel();
    this.EditTileButton = new Button();
    this.TerrainCheckBox = new CheckBox();
    this.TilesetPanel = new Panel();
    this.pictureBox1 = new PictureBox();
    this.TilesetPaletteTable.SuspendLayout();
    this.flowLayoutPanel1.SuspendLayout();
    this.TilesetPanel.SuspendLayout();
    ((ISupportInitialize) this.pictureBox1).BeginInit();
    this.SuspendLayout();
    this.TilesetPaletteTable.ColumnCount = 1;
    this.TilesetPaletteTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
    this.TilesetPaletteTable.Controls.Add((Control) this.flowLayoutPanel1, 0, 1);
    this.TilesetPaletteTable.Controls.Add((Control) this.TilesetPanel, 0, 0);
    this.TilesetPaletteTable.Dock = DockStyle.Fill;
    this.TilesetPaletteTable.Location = new Point(0, 0);
    this.TilesetPaletteTable.Name = "TilesetPaletteTable";
    this.TilesetPaletteTable.RowCount = 2;
    this.TilesetPaletteTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
    this.TilesetPaletteTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
    this.TilesetPaletteTable.Size = new Size(382, 259);
    this.TilesetPaletteTable.TabIndex = 1;
    this.flowLayoutPanel1.Controls.Add((Control) this.EditTileButton);
    this.flowLayoutPanel1.Controls.Add((Control) this.TerrainCheckBox);
    this.flowLayoutPanel1.Dock = DockStyle.Fill;
    this.flowLayoutPanel1.Location = new Point(3, 214);
    this.flowLayoutPanel1.Name = "flowLayoutPanel1";
    this.flowLayoutPanel1.Size = new Size(376, 42);
    this.flowLayoutPanel1.TabIndex = 1;
    this.EditTileButton.Enabled = false;
    this.EditTileButton.Location = new Point(3, 3);
    this.EditTileButton.Name = "EditTileButton";
    this.EditTileButton.Size = new Size(97, 23);
    this.EditTileButton.TabIndex = 2;
    this.EditTileButton.Text = "Edit Tile";
    this.EditTileButton.UseVisualStyleBackColor = true;
    this.EditTileButton.Click += new EventHandler(this.EditTileButton_Click);
    this.TerrainCheckBox.AutoSize = true;
    this.TerrainCheckBox.Location = new Point(106, 6);
    this.TerrainCheckBox.Margin = new Padding(3, 6, 3, 3);
    this.TerrainCheckBox.Name = "TerrainCheckBox";
    this.TerrainCheckBox.Size = new Size(119, 17);
    this.TerrainCheckBox.TabIndex = 3;
    this.TerrainCheckBox.Text = "Show terrain types?";
    this.TerrainCheckBox.UseVisualStyleBackColor = true;
    this.TerrainCheckBox.CheckedChanged += new EventHandler(this.TerrainCheckBox_CheckedChanged);
    this.TilesetPanel.AutoScroll = true;
    this.TilesetPanel.Controls.Add((Control) this.pictureBox1);
    this.TilesetPanel.Dock = DockStyle.Fill;
    this.TilesetPanel.Location = new Point(0, 0);
    this.TilesetPanel.Margin = new Padding(0);
    this.TilesetPanel.Name = "TilesetPanel";
    this.TilesetPanel.Size = new Size(382, 211);
    this.TilesetPanel.TabIndex = 1;
    this.pictureBox1.Location = new Point(0, 0);
    this.pictureBox1.Margin = new Padding(0);
    this.pictureBox1.Name = "pictureBox1";
    this.pictureBox1.Size = new Size(200, 200);
    this.pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
    this.pictureBox1.TabIndex = 0;
    this.pictureBox1.TabStop = false;
    this.AutoScaleDimensions = new SizeF(6f, 13f);
    this.AutoScaleMode = AutoScaleMode.Font;
    this.ClientSize = new Size(382, 259);
    this.Controls.Add((Control) this.TilesetPaletteTable);
    this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
    this.MaximizeBox = false;
    this.MinimizeBox = false;
    this.Name = nameof (Tileset_Palette_Form);
    this.ShowInTaskbar = false;
    this.StartPosition = FormStartPosition.CenterParent;
    this.Text = "Tileset";
    this.TilesetPaletteTable.ResumeLayout(false);
    this.flowLayoutPanel1.ResumeLayout(false);
    this.flowLayoutPanel1.PerformLayout();
    this.TilesetPanel.ResumeLayout(false);
    this.TilesetPanel.PerformLayout();
    ((ISupportInitialize) this.pictureBox1).EndInit();
    this.ResumeLayout(false);
  }
}
