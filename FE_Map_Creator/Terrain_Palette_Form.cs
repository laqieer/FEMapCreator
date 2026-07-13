// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Terrain_Palette_Form
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

#nullable disable
namespace FE_Map_Creator;

public class Terrain_Palette_Form : Form
{
  private const int TERRAIN_COLUMNS = 16 /*0x10*/;
  private const int ZOOM = 1;
  protected int Terrain_Width;
  protected int Terrain_Height;
  protected bool Selecting_Tile;
  protected Point Selected_Base_Tile;
  private IContainer components = new Container();
  private TableLayoutPanel TilesetPaletteTable;
  private PictureBox pictureBox1;
  private Panel TilesetPanel;
  private FlowLayoutPanel flowLayoutPanel1;
  private Label label1;

  [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
  public int active_terrain
  {
    get => this.Selected_Base_Tile.Y * this.Terrain_Width + this.Selected_Base_Tile.X;
    set
    {
      this.Selected_Base_Tile = Terrain_Color_Data.TERRAIN_COLORS.ContainsKey(value) ? new Point(value % this.Terrain_Width, value / this.Terrain_Width) : new Point(0, 0);
      this.refresh_label();
      this.pictureBox1.Invalidate();
    }
  }

  public Terrain_Palette_Form()
  {
    this.InitializeComponent();
    this.Terrain_Width = 16 /*0x10*/;
    this.Terrain_Height = (int) Math.Ceiling((double) (Terrain_Color_Data.TERRAIN_COLORS.Keys.Max() + 1) / 16.0);
    this.pictureBox1.Size = new Size(this.Terrain_Width * 16 /*0x10*/, this.Terrain_Height * 16 /*0x10*/);
    this.update_form_size(this.pictureBox1.Size.Width);
    this.pictureBox1.MouseDown += new MouseEventHandler(this.pictureBox1_MouseDown);
    this.pictureBox1.MouseMove += new MouseEventHandler(this.pictureBox1_MouseMove);
    this.pictureBox1.MouseUp += new MouseEventHandler(this.pictureBox1_MouseUp);
    this.pictureBox1.Paint += new PaintEventHandler(this.pictureBox1_Paint);
    this.active_terrain = 1;
  }

  protected void update_form_size(int width)
  {
    this.MaximumSize = new Size(width + (SystemInformation.FrameBorderSize.Width * 2 + SystemInformation.VerticalScrollBarWidth), Terrain_Palette_Form.all_screen_height());
    this.MinimumSize = new Size(this.MaximumSize.Width, this.pictureBox1.Size.Height + this.flowLayoutPanel1.Height + 6 + SystemInformation.FrameBorderSize.Width * 2 + SystemInformation.CaptionHeight);
    this.Size = new Size(Math.Min(width + (SystemInformation.FrameBorderSize.Width * 2 + SystemInformation.VerticalScrollBarWidth), Screen.GetBounds((Control) this).Width), this.Size.Height);
  }

  public static int all_screen_height()
  {
    return ((IEnumerable<Screen>) Screen.AllScreens).Sum<Screen>((Func<Screen, int>) (s => s.Bounds.Height));
  }

  private void refresh_label()
  {
    this.label1.Text = Terrain_Color_Data.TERRAIN_COLORS[this.active_terrain].terrain_name();
  }

  private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
  {
    this.Selecting_Tile = true;
    Point point = new Point(e.X / 16 /*0x10*/, e.Y / 16 /*0x10*/);
    if (point.X >= 0 && point.Y >= 0 && point.X < this.Terrain_Width && point.Y < this.Terrain_Height && Terrain_Color_Data.TERRAIN_COLORS.ContainsKey(point.Y * this.Terrain_Width + point.X))
    {
      this.Selected_Base_Tile = point;
      this.refresh_label();
    }
    this.pictureBox1.Invalidate();
  }

  private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
  {
    if (!this.Selecting_Tile)
      return;
    Point point = new Point(e.X / 16 /*0x10*/, e.Y / 16 /*0x10*/);
    if (point.X >= 0 && point.Y >= 0 && point.X < this.Terrain_Width && point.Y < this.Terrain_Height && Terrain_Color_Data.TERRAIN_COLORS.ContainsKey(point.Y * this.Terrain_Width + point.X))
    {
      this.Selected_Base_Tile = point;
      this.refresh_label();
    }
    this.pictureBox1.Invalidate();
  }

  private void pictureBox1_MouseUp(object sender, MouseEventArgs e) => this.tile_select();

  public event EventHandler Terrain_Selected;

  protected void tile_select()
  {
    this.Selecting_Tile = false;
    this.Terrain_Selected((object) this, new EventArgs());
  }

  private void pictureBox1_Paint(object sender, PaintEventArgs e)
  {
    e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
    e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
    e.Graphics.SmoothingMode = SmoothingMode.None;
    int x = this.Selected_Base_Tile.X * 16 /*0x10*/;
    int y = this.Selected_Base_Tile.Y * 16 /*0x10*/;
    int width = 16 /*0x10*/;
    int height = 16 /*0x10*/;
    this.draw_terrain_colors(e.Graphics);
    using (Pen pen = new Pen(Brushes.Black, 4f))
      e.Graphics.DrawRectangles(pen, new Rectangle[1]
      {
        new Rectangle(x, y, width, height)
      });
    using (Pen pen = new Pen(Terrain_Color_Data.TERRAIN_COLORS[this.active_terrain].Color1, 2f))
      e.Graphics.DrawRectangles(pen, new Rectangle[1]
      {
        new Rectangle(x, y, width, height)
      });
  }

  private void draw_terrain_colors(Graphics g)
  {
    for (int y = 0; y < this.Terrain_Height; ++y)
    {
      for (int x = 0; x < this.Terrain_Width; ++x)
        Terrain_Color_Data.draw(g, x, y, y * this.Terrain_Width + x, 16 /*0x10*/, true, true);
    }
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
    this.label1 = new Label();
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
    this.TilesetPaletteTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
    this.TilesetPaletteTable.Size = new Size(273, 106);
    this.TilesetPaletteTable.TabIndex = 1;
    this.flowLayoutPanel1.Controls.Add((Control) this.label1);
    this.flowLayoutPanel1.Dock = DockStyle.Fill;
    this.flowLayoutPanel1.Location = new Point(0, 82);
    this.flowLayoutPanel1.Margin = new Padding(0);
    this.flowLayoutPanel1.Name = "flowLayoutPanel1";
    this.flowLayoutPanel1.Size = new Size(273, 24);
    this.flowLayoutPanel1.TabIndex = 1;
    this.label1.AutoSize = true;
    this.label1.Location = new Point(3, 3);
    this.label1.Margin = new Padding(3, 3, 3, 0);
    this.label1.Name = "label1";
    this.label1.Size = new Size(35, 13);
    this.label1.TabIndex = 0;
    this.label1.Text = "Plains";
    this.TilesetPanel.AutoScroll = true;
    this.TilesetPanel.Controls.Add((Control) this.pictureBox1);
    this.TilesetPanel.Dock = DockStyle.Fill;
    this.TilesetPanel.Location = new Point(0, 0);
    this.TilesetPanel.Margin = new Padding(0);
    this.TilesetPanel.Name = "TilesetPanel";
    this.TilesetPanel.Size = new Size(273, 82);
    this.TilesetPanel.TabIndex = 1;
    this.pictureBox1.Location = new Point(0, 0);
    this.pictureBox1.Margin = new Padding(0);
    this.pictureBox1.Name = "pictureBox1";
    this.pictureBox1.Size = new Size(256 /*0x0100*/, 80 /*0x50*/);
    this.pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
    this.pictureBox1.TabIndex = 0;
    this.pictureBox1.TabStop = false;
    this.AutoScaleDimensions = new SizeF(6f, 13f);
    this.AutoScaleMode = AutoScaleMode.Font;
    this.ClientSize = new Size(273, 106);
    this.Controls.Add((Control) this.TilesetPaletteTable);
    this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
    this.MaximizeBox = false;
    this.MinimizeBox = false;
    this.Name = nameof (Terrain_Palette_Form);
    this.ShowInTaskbar = false;
    this.StartPosition = FormStartPosition.CenterParent;
    this.Text = "Terrain";
    this.TilesetPaletteTable.ResumeLayout(false);
    this.flowLayoutPanel1.ResumeLayout(false);
    this.flowLayoutPanel1.PerformLayout();
    this.TilesetPanel.ResumeLayout(false);
    this.TilesetPanel.PerformLayout();
    ((ISupportInitialize) this.pictureBox1).EndInit();
    this.ResumeLayout(false);
  }
}
