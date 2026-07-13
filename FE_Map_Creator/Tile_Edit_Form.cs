// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Tile_Edit_Form
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

#nullable disable
namespace FE_Map_Creator;

public class Tile_Edit_Form : Form
{
  private List<short> Selected_Tiles;
  private short Selected_Tile_Index;
  private short Actual_Target_Tile;
  private short Target_Tile_Index;
  private Dictionary<short, short> Identical_Tiles;
  private int Tileset_Width;
  private int Tileset_Height;
  private bool Selecting_Tile;
  private Tile_Data Data;
  private bool Updating;
  private Thread Tile_Update_Thread;
  private CancellationTokenSource Tile_Update_Cancellation;
  private object thread_lock = new object();
  private IContainer components = new Container();
  private TableLayoutPanel tableLayoutPanel1;
  private Panel TilesetPanel;
  private PictureBox pictureBox1;
  private FlowLayoutPanel flowLayoutPanel1;
  private Button Accept_Button;
  private Button Cancel_Button;
  private CheckBox checkBox1;
  private CheckBox checkBox2;
  private CheckBox checkBox3;
  private CheckBox checkBox4;
  private Panel TileSurroundingsPanel;
  private NumericUpDown PriorityUpDown;
  private Label PriorityLabel;
  private NumericUpDown numericUpDown1;
  private NumericUpDown numericUpDown2;
  private NumericUpDown numericUpDown3;
  private NumericUpDown numericUpDown4;

  private short selected_tile => this.Selected_Tiles[(int) this.Selected_Tile_Index];

  private short target_tile_index
  {
    set
    {
      if ((int) this.Actual_Target_Tile == (int) value)
        return;
      this.Actual_Target_Tile = value;
      this.Target_Tile_Index = this.Identical_Tiles.ContainsKey(value) ? this.Identical_Tiles[value] : value;
      this.Updating = true;
      this.refresh_priority_spinner(ref this.numericUpDown1, ref this.checkBox1, this.Target_Tile_Index, this.Data, (byte) 8);
      this.refresh_priority_spinner(ref this.numericUpDown2, ref this.checkBox2, this.Target_Tile_Index, this.Data, (byte) 4);
      this.refresh_priority_spinner(ref this.numericUpDown3, ref this.checkBox3, this.Target_Tile_Index, this.Data, (byte) 6);
      this.refresh_priority_spinner(ref this.numericUpDown4, ref this.checkBox4, this.Target_Tile_Index, this.Data, (byte) 2);
      this.Updating = false;
      this.pictureBox1.Invalidate();
      this.TileSurroundingsPanel.Invalidate();
    }
  }

  private void refresh_priority_spinner(
    ref NumericUpDown updown,
    ref CheckBox checkbox,
    short tile,
    Tile_Data data,
    byte dir)
  {
    checkbox.Checked = data.Valid_Tile_Priority[dir].ContainsKey(tile);
    short val2 = data.Valid_Tile_Priority[dir].ContainsKey(tile) ? data.Valid_Tile_Priority[dir][tile] : (short) 0;
    updown.Minimum = (Decimal) (data.Valid_Tile_Priority[dir].ContainsKey(tile) ? 1 : 0);
    updown.Value = Math.Max(updown.Minimum, (Decimal) val2);
    updown.Enabled = updown.Value > 0M;
  }

  public Tile_Data data => this.Data;

  public Tile_Edit_Form()
  {
    this.InitializeComponent();
    this.pictureBox1.MouseDown += new MouseEventHandler(this.pictureBox1_MouseDown);
    this.pictureBox1.MouseMove += new MouseEventHandler(this.pictureBox1_MouseMove);
    this.pictureBox1.MouseUp += new MouseEventHandler(this.pictureBox1_MouseUp);
    this.pictureBox1.Paint += new PaintEventHandler(this.pictureBox1_Paint);
    this.TileSurroundingsPanel.Paint += new PaintEventHandler(this.TileSurroundingsPanel_Paint);
    this.TileSurroundingsPanel.Size = new Size(48 /*0x30*/, 48 /*0x30*/);
    int num1 = (200 - this.TileSurroundingsPanel.Size.Width - this.checkBox2.Width * 2) / 2 - 3;
    int num2 = (this.TileSurroundingsPanel.Size.Height - this.checkBox2.Height) / 2;
    this.checkBox2.Margin = new Padding(num1, num2, 0, num2);
    this.checkBox3.Margin = new Padding(0, num2, num1, num2);
    this.PriorityUpDown.Select();
    this.FormClosing += new FormClosingEventHandler(this.Tile_Edit_Form_FormClosing);
  }

  public void setup(
    Bitmap bmp,
    List<short> indices,
    Tile_Data data,
    Dictionary<short, short> identical_tiles)
  {
    this.Selected_Tiles = indices;
    this.Selected_Tile_Index = (short) 0;
    this.Identical_Tiles = identical_tiles;
    lock (this.thread_lock)
    {
      this.Tile_Update_Cancellation = new CancellationTokenSource();
      CancellationToken cancellationToken = this.Tile_Update_Cancellation.Token;
      this.Tile_Update_Thread = new Thread((ThreadStart) (() => this.update_displayed_tile(cancellationToken)))
      {
        IsBackground = true
      };
      this.Tile_Update_Thread.Start();
    }
    this.load_tileset(bmp);
    this.target_tile_index = (short) 0;
    this.Data = data;
    this.Updating = true;
    this.PriorityUpDown.Value = (Decimal) this.Data.Priority;
    this.Updating = false;
  }

  private void load_tileset(Bitmap bmp)
  {
    this.pictureBox1.Image = (Image) new Bitmap((Image) bmp);
    this.Tileset_Width = this.pictureBox1.Image.Width / 16 /*0x10*/;
    this.Tileset_Height = this.pictureBox1.Image.Height / 16 /*0x10*/;
    this.update_form_size(this.pictureBox1.Image.Width);
  }

  private void update_form_size(int width)
  {
    this.MaximumSize = new Size(width + 200 + (SystemInformation.FrameBorderSize.Width * 2 + SystemInformation.VerticalScrollBarWidth), Tileset_Palette_Form.all_screen_height());
    this.Size = new Size(Math.Min(width + 200 + (SystemInformation.FrameBorderSize.Width * 2 + SystemInformation.VerticalScrollBarWidth), Screen.GetBounds((Control) this).Width), this.Size.Height);
  }

  private void update_displayed_tile(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      while (this.Selecting_Tile)
      {
        if (cancellationToken.WaitHandle.WaitOne(100))
          return;
      }
      if (cancellationToken.WaitHandle.WaitOne(500))
        return;
      lock (this.thread_lock)
      {
        if (!this.Selecting_Tile)
          this.Selected_Tile_Index = (short) (((int) this.Selected_Tile_Index + 1) % this.Selected_Tiles.Count);
      }
      this.pictureBox1.Invalidate();
      this.TileSurroundingsPanel.Invalidate();
    }
  }

  private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
  {
    if (this.pictureBox1.Image == null)
      return;
    this.Selecting_Tile = true;
    Point point = new Point(e.X / 16 /*0x10*/, e.Y / 16 /*0x10*/);
    short num = (short) (point.X + point.Y * this.Tileset_Width);
    if (e.Button == MouseButtons.Left)
    {
      this.target_tile_index = num;
    }
    else
    {
      lock (this.thread_lock)
      {
        if (this.Selected_Tiles.Contains(num))
        {
          this.Selected_Tile_Index = (short) this.Selected_Tiles.IndexOf(num);
          this.pictureBox1.Invalidate();
          this.TileSurroundingsPanel.Invalidate();
        }
      }
    }
    this.pictureBox1.Invalidate();
  }

  private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
  {
    if (!this.Selecting_Tile)
      return;
    Point point = new Point(Math.Min(this.Tileset_Width - 1, Math.Max(0, e.X / 16 /*0x10*/)), Math.Min(this.Tileset_Height - 1, Math.Max(0, e.Y / 16 /*0x10*/)));
    short num = (short) (point.X + point.Y * this.Tileset_Width);
    if (e.Button == MouseButtons.Left)
    {
      this.target_tile_index = num;
    }
    else
    {
      lock (this.thread_lock)
      {
        if (!this.Selected_Tiles.Contains(num))
          return;
        this.Selected_Tile_Index = (short) this.Selected_Tiles.IndexOf(num);
        this.pictureBox1.Invalidate();
        this.TileSurroundingsPanel.Invalidate();
      }
    }
  }

  private void pictureBox1_MouseUp(object sender, MouseEventArgs e) => this.Selecting_Tile = false;

  private void pictureBox1_Paint(object sender, PaintEventArgs e)
  {
    if (this.pictureBox1.Image == null)
      return;
    int width1 = 16 /*0x10*/;
    int height1 = 16 /*0x10*/;
    foreach (short selectedTile in this.Selected_Tiles)
    {
      int x = (int) selectedTile % this.Tileset_Width * 16 /*0x10*/;
      int y = (int) selectedTile / this.Tileset_Width * 16 /*0x10*/;
      using (Pen pen = new Pen(Color.Black, 4f))
        e.Graphics.DrawRectangles(pen, new Rectangle[1]
        {
          new Rectangle(x, y, width1, height1)
        });
    }
    foreach (short selectedTile in this.Selected_Tiles)
    {
      int x = (int) selectedTile % this.Tileset_Width * 16 /*0x10*/;
      int y = (int) selectedTile / this.Tileset_Width * 16 /*0x10*/;
      using (Pen pen = new Pen(Color.FromArgb(80 /*0x50*/, 80 /*0x50*/, 80 /*0x50*/), 2f))
        e.Graphics.DrawRectangles(pen, new Rectangle[1]
        {
          new Rectangle(x, y, width1, height1)
        });
    }
    short selectedTile1;
    lock (this.thread_lock)
      selectedTile1 = this.selected_tile;
    int x1 = (int) selectedTile1 % this.Tileset_Width * 16 /*0x10*/;
    int y1 = (int) selectedTile1 / this.Tileset_Width * 16 /*0x10*/;
    using (Pen pen = new Pen(Color.FromArgb(160 /*0xA0*/, 160 /*0xA0*/, 160 /*0xA0*/), 2f))
      e.Graphics.DrawRectangles(pen, new Rectangle[1]
      {
        new Rectangle(x1, y1, width1, height1)
      });
    if (this.Identical_Tiles.ContainsKey(this.Actual_Target_Tile))
    {
      IEnumerable<short> shorts = this.Identical_Tiles.Keys.Where<short>((Func<short, bool>) (i => (int) i != (int) this.Actual_Target_Tile && (int) this.Identical_Tiles[i] == (int) this.Target_Tile_Index));
      foreach (short num in shorts)
      {
        int x2 = (int) num % this.Tileset_Width * 16 /*0x10*/;
        int y2 = (int) num / this.Tileset_Width * 16 /*0x10*/;
        using (Pen pen = new Pen(Color.Black, 4f))
          e.Graphics.DrawRectangles(pen, new Rectangle[1]
          {
            new Rectangle(x2, y2, width1, height1)
          });
      }
      foreach (short num in shorts)
      {
        int x3 = (int) num % this.Tileset_Width * 16 /*0x10*/;
        int y3 = (int) num / this.Tileset_Width * 16 /*0x10*/;
        using (Pen pen = new Pen(Color.FromArgb(208 /*0xD0*/, 208 /*0xD0*/, 208 /*0xD0*/), 2f))
          e.Graphics.DrawRectangles(pen, new Rectangle[1]
          {
            new Rectangle(x3, y3, width1, height1)
          });
      }
    }
    int x4 = (int) this.Actual_Target_Tile % this.Tileset_Width * 16 /*0x10*/;
    int y4 = (int) this.Actual_Target_Tile / this.Tileset_Width * 16 /*0x10*/;
    using (Pen pen = new Pen(Color.Black, 4f))
      e.Graphics.DrawRectangles(pen, new Rectangle[1]
      {
        new Rectangle(x4, y4, width1, height1)
      });
    using (Pen pen = new Pen(Color.White, 2f))
      e.Graphics.DrawRectangles(pen, new Rectangle[1]
      {
        new Rectangle(x4, y4, width1, height1)
      });
    int width2 = 1;
    int height2 = 1;
    using (Pen pen1 = new Pen(Color.White, 1f))
    {
      using (Pen pen2 = new Pen(Color.Black, 3f))
      {
        using (Pen pen3 = new Pen(Color.Black, 1f))
        {
          int num = 5;
          for (short key1 = 0; (int) key1 < this.Tileset_Width * this.Tileset_Height; ++key1)
          {
            short key2 = this.Identical_Tiles.ContainsKey(key1) ? this.Identical_Tiles[key1] : key1;
            int x5 = (int) key1 % this.Tileset_Width * 16 /*0x10*/ + 2;
            int y5 = (int) key1 / this.Tileset_Width * 16 /*0x10*/ + 2;
            if (this.Data.Valid_Tile_Priority[(byte) 8].ContainsKey(key2))
            {
              e.Graphics.DrawRectangles(pen2, new Rectangle[1]
              {
                new Rectangle(x5 + num, y5 + num * 2, width2, height2)
              });
              e.Graphics.DrawRectangles(pen1, new Rectangle[1]
              {
                new Rectangle(x5 + num, y5 + num * 2, width2, height2)
              });
            }
            else
              e.Graphics.DrawRectangles(pen3, new Rectangle[1]
              {
                new Rectangle(x5 + num, y5 + num * 2, width2, height2)
              });
            if (this.Data.Valid_Tile_Priority[(byte) 4].ContainsKey(key2))
            {
              e.Graphics.DrawRectangles(pen2, new Rectangle[1]
              {
                new Rectangle(x5 + num * 2, y5 + num, width2, height2)
              });
              e.Graphics.DrawRectangles(pen1, new Rectangle[1]
              {
                new Rectangle(x5 + num * 2, y5 + num, width2, height2)
              });
            }
            else
              e.Graphics.DrawRectangles(pen3, new Rectangle[1]
              {
                new Rectangle(x5 + num * 2, y5 + num, width2, height2)
              });
            if (this.Data.Valid_Tile_Priority[(byte) 6].ContainsKey(key2))
            {
              e.Graphics.DrawRectangles(pen2, new Rectangle[1]
              {
                new Rectangle(x5, y5 + num, width2, height2)
              });
              e.Graphics.DrawRectangles(pen1, new Rectangle[1]
              {
                new Rectangle(x5, y5 + num, width2, height2)
              });
            }
            else
              e.Graphics.DrawRectangles(pen3, new Rectangle[1]
              {
                new Rectangle(x5, y5 + num, width2, height2)
              });
            if (this.Data.Valid_Tile_Priority[(byte) 2].ContainsKey(key2))
            {
              e.Graphics.DrawRectangles(pen2, new Rectangle[1]
              {
                new Rectangle(x5 + num, y5, width2, height2)
              });
              e.Graphics.DrawRectangles(pen1, new Rectangle[1]
              {
                new Rectangle(x5 + num, y5, width2, height2)
              });
            }
            else
              e.Graphics.DrawRectangles(pen3, new Rectangle[1]
              {
                new Rectangle(x5 + num, y5, width2, height2)
              });
          }
        }
      }
    }
  }

  private void TileSurroundingsPanel_Paint(object sender, PaintEventArgs e)
  {
    if (this.pictureBox1.Image == null)
      return;
    Rectangle destRect = new Rectangle(16 /*0x10*/, 16 /*0x10*/, 16 /*0x10*/, 16 /*0x10*/);
    Rectangle srcRect;
    lock (this.thread_lock)
      srcRect = new Rectangle((int) this.selected_tile % this.Tileset_Width * 16 /*0x10*/, (int) this.selected_tile / this.Tileset_Width * 16 /*0x10*/, 16 /*0x10*/, 16 /*0x10*/);
    e.Graphics.DrawImage(this.pictureBox1.Image, destRect, srcRect, GraphicsUnit.Pixel);
    srcRect = new Rectangle((int) this.Actual_Target_Tile % this.Tileset_Width * 16 /*0x10*/, (int) this.Actual_Target_Tile / this.Tileset_Width * 16 /*0x10*/, 16 /*0x10*/, 16 /*0x10*/);
    destRect = new Rectangle(16 /*0x10*/, 0, 16 /*0x10*/, 16 /*0x10*/);
    e.Graphics.DrawImage(this.pictureBox1.Image, destRect, srcRect, GraphicsUnit.Pixel);
    destRect = new Rectangle(0, 16 /*0x10*/, 16 /*0x10*/, 16 /*0x10*/);
    e.Graphics.DrawImage(this.pictureBox1.Image, destRect, srcRect, GraphicsUnit.Pixel);
    destRect = new Rectangle(32 /*0x20*/, 16 /*0x10*/, 16 /*0x10*/, 16 /*0x10*/);
    e.Graphics.DrawImage(this.pictureBox1.Image, destRect, srcRect, GraphicsUnit.Pixel);
    destRect = new Rectangle(16 /*0x10*/, 32 /*0x20*/, 16 /*0x10*/, 16 /*0x10*/);
    e.Graphics.DrawImage(this.pictureBox1.Image, destRect, srcRect, GraphicsUnit.Pixel);
  }

  private void PriorityUpDown_ValueChanged(object sender, EventArgs e)
  {
    if (this.Updating)
      return;
    this.Data.Priority = (short) ((NumericUpDown) sender).Value;
  }

  private bool change_valid_tile(
    bool is_checked,
    byte dir,
    ref NumericUpDown updown,
    ref Tile_Data data)
  {
    if (!is_checked)
    {
      data.Valid_Tile_Priority[dir].Remove(this.Target_Tile_Index);
      updown.Minimum = 0M;
      updown.Value = 0M;
      updown.Enabled = false;
    }
    else
    {
      if (data.Valid_Tile_Priority[dir].ContainsKey(this.Target_Tile_Index))
        return false;
      updown.Minimum = 1M;
      updown.Value = (Decimal) (data.Valid_Tile_Priority[dir][this.Target_Tile_Index] = (short) 1);
      updown.Enabled = true;
    }
    return true;
  }

  private void checkBox1_CheckedChanged(object sender, EventArgs e)
  {
    if (this.Updating || !this.change_valid_tile((sender as CheckBox).Checked, (byte) 8, ref this.numericUpDown1, ref this.Data))
      return;
    this.pictureBox1.Invalidate();
  }

  private void checkBox2_CheckedChanged(object sender, EventArgs e)
  {
    if (this.Updating || !this.change_valid_tile((sender as CheckBox).Checked, (byte) 4, ref this.numericUpDown2, ref this.Data))
      return;
    this.pictureBox1.Invalidate();
  }

  private void checkBox3_CheckedChanged(object sender, EventArgs e)
  {
    if (this.Updating || !this.change_valid_tile((sender as CheckBox).Checked, (byte) 6, ref this.numericUpDown3, ref this.Data))
      return;
    this.pictureBox1.Invalidate();
  }

  private void checkBox4_CheckedChanged(object sender, EventArgs e)
  {
    if (this.Updating || !this.change_valid_tile((sender as CheckBox).Checked, (byte) 2, ref this.numericUpDown4, ref this.Data))
      return;
    this.pictureBox1.Invalidate();
  }

  private void numericUpDown1_ValueChanged(object sender, EventArgs e)
  {
    if (!this.Data.Valid_Tile_Priority[(byte) 8].ContainsKey(this.Target_Tile_Index))
      return;
    this.Data.Valid_Tile_Priority[(byte) 8][this.Target_Tile_Index] = (short) (sender as NumericUpDown).Value;
  }

  private void numericUpDown2_ValueChanged(object sender, EventArgs e)
  {
    if (!this.Data.Valid_Tile_Priority[(byte) 4].ContainsKey(this.Target_Tile_Index))
      return;
    this.Data.Valid_Tile_Priority[(byte) 4][this.Target_Tile_Index] = (short) (sender as NumericUpDown).Value;
  }

  private void numericUpDown3_ValueChanged(object sender, EventArgs e)
  {
    if (!this.Data.Valid_Tile_Priority[(byte) 6].ContainsKey(this.Target_Tile_Index))
      return;
    this.Data.Valid_Tile_Priority[(byte) 6][this.Target_Tile_Index] = (short) (sender as NumericUpDown).Value;
  }

  private void numericUpDown4_ValueChanged(object sender, EventArgs e)
  {
    if (!this.Data.Valid_Tile_Priority[(byte) 2].ContainsKey(this.Target_Tile_Index))
      return;
    this.Data.Valid_Tile_Priority[(byte) 2][this.Target_Tile_Index] = (short) (sender as NumericUpDown).Value;
  }

  private void Tile_Edit_Form_FormClosing(object sender, FormClosingEventArgs e)
  {
    Thread tileUpdateThread;
    CancellationTokenSource tileUpdateCancellation;
    lock (this.thread_lock)
    {
      tileUpdateThread = this.Tile_Update_Thread;
      tileUpdateCancellation = this.Tile_Update_Cancellation;
      this.Tile_Update_Thread = (Thread) null;
      this.Tile_Update_Cancellation = (CancellationTokenSource) null;
    }
    tileUpdateCancellation?.Cancel();
    if (tileUpdateThread != null && tileUpdateThread.IsAlive)
      tileUpdateThread.Join(1000);
    tileUpdateCancellation?.Dispose();
    if (this.pictureBox1.Image == null)
      return;
    this.pictureBox1.Image.Dispose();
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing && this.components != null)
      this.components.Dispose();
    base.Dispose(disposing);
  }

  private void InitializeComponent()
  {
    this.tableLayoutPanel1 = new TableLayoutPanel();
    this.TilesetPanel = new Panel();
    this.pictureBox1 = new PictureBox();
    this.flowLayoutPanel1 = new FlowLayoutPanel();
    this.PriorityUpDown = new NumericUpDown();
    this.PriorityLabel = new Label();
    this.checkBox1 = new CheckBox();
    this.checkBox2 = new CheckBox();
    this.TileSurroundingsPanel = new Panel();
    this.checkBox3 = new CheckBox();
    this.checkBox4 = new CheckBox();
    this.numericUpDown1 = new NumericUpDown();
    this.numericUpDown2 = new NumericUpDown();
    this.numericUpDown3 = new NumericUpDown();
    this.numericUpDown4 = new NumericUpDown();
    this.Accept_Button = new Button();
    this.Cancel_Button = new Button();
    this.tableLayoutPanel1.SuspendLayout();
    this.TilesetPanel.SuspendLayout();
    ((ISupportInitialize) this.pictureBox1).BeginInit();
    this.flowLayoutPanel1.SuspendLayout();
    this.PriorityUpDown.BeginInit();
    this.numericUpDown1.BeginInit();
    this.numericUpDown2.BeginInit();
    this.numericUpDown3.BeginInit();
    this.numericUpDown4.BeginInit();
    this.SuspendLayout();
    this.tableLayoutPanel1.ColumnCount = 2;
    this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
    this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
    this.tableLayoutPanel1.Controls.Add((Control) this.TilesetPanel, 0, 0);
    this.tableLayoutPanel1.Controls.Add((Control) this.flowLayoutPanel1, 1, 0);
    this.tableLayoutPanel1.Dock = DockStyle.Fill;
    this.tableLayoutPanel1.Location = new Point(0, 0);
    this.tableLayoutPanel1.Name = "tableLayoutPanel1";
    this.tableLayoutPanel1.RowCount = 1;
    this.tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
    this.tableLayoutPanel1.Size = new Size(504, 262);
    this.tableLayoutPanel1.TabIndex = 0;
    this.TilesetPanel.AutoScroll = true;
    this.TilesetPanel.Controls.Add((Control) this.pictureBox1);
    this.TilesetPanel.Dock = DockStyle.Fill;
    this.TilesetPanel.Location = new Point(0, 0);
    this.TilesetPanel.Margin = new Padding(0);
    this.TilesetPanel.Name = "TilesetPanel";
    this.TilesetPanel.Size = new Size(304, 262);
    this.TilesetPanel.TabIndex = 0;
    this.pictureBox1.Location = new Point(0, 0);
    this.pictureBox1.Name = "pictureBox1";
    this.pictureBox1.Size = new Size(100, 50);
    this.pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
    this.pictureBox1.TabIndex = 0;
    this.pictureBox1.TabStop = false;
    this.flowLayoutPanel1.Controls.Add((Control) this.PriorityUpDown);
    this.flowLayoutPanel1.Controls.Add((Control) this.PriorityLabel);
    this.flowLayoutPanel1.Controls.Add((Control) this.checkBox1);
    this.flowLayoutPanel1.Controls.Add((Control) this.checkBox2);
    this.flowLayoutPanel1.Controls.Add((Control) this.TileSurroundingsPanel);
    this.flowLayoutPanel1.Controls.Add((Control) this.checkBox3);
    this.flowLayoutPanel1.Controls.Add((Control) this.checkBox4);
    this.flowLayoutPanel1.Controls.Add((Control) this.numericUpDown1);
    this.flowLayoutPanel1.Controls.Add((Control) this.numericUpDown2);
    this.flowLayoutPanel1.Controls.Add((Control) this.numericUpDown3);
    this.flowLayoutPanel1.Controls.Add((Control) this.numericUpDown4);
    this.flowLayoutPanel1.Controls.Add((Control) this.Accept_Button);
    this.flowLayoutPanel1.Controls.Add((Control) this.Cancel_Button);
    this.flowLayoutPanel1.Dock = DockStyle.Fill;
    this.flowLayoutPanel1.Location = new Point(307, 3);
    this.flowLayoutPanel1.Name = "flowLayoutPanel1";
    this.flowLayoutPanel1.Size = new Size(194, 256 /*0x0100*/);
    this.flowLayoutPanel1.TabIndex = 1;
    this.PriorityUpDown.Location = new Point(3, 3);
    this.PriorityUpDown.Name = "PriorityUpDown";
    this.PriorityUpDown.Size = new Size(55, 20);
    this.PriorityUpDown.TabIndex = 7;
    this.PriorityUpDown.ValueChanged += new EventHandler(this.PriorityUpDown_ValueChanged);
    this.PriorityLabel.AutoSize = true;
    this.PriorityLabel.Location = new Point(64 /*0x40*/, 5);
    this.PriorityLabel.Margin = new Padding(3, 5, 3, 5);
    this.PriorityLabel.Name = "PriorityLabel";
    this.PriorityLabel.Size = new Size(38, 13);
    this.PriorityLabel.TabIndex = 8;
    this.PriorityLabel.Text = "Priority";
    this.checkBox1.AutoSize = true;
    this.checkBox1.Location = new Point(92, 29);
    this.checkBox1.Margin = new Padding(92, 3, 92, 3);
    this.checkBox1.Name = "checkBox1";
    this.checkBox1.Size = new Size(15, 14);
    this.checkBox1.TabIndex = 2;
    this.checkBox1.UseVisualStyleBackColor = true;
    this.checkBox1.CheckedChanged += new EventHandler(this.checkBox1_CheckedChanged);
    this.checkBox2.AutoSize = true;
    this.checkBox2.Location = new Point(8, 49);
    this.checkBox2.Margin = new Padding(8, 3, 8, 3);
    this.checkBox2.Name = "checkBox2";
    this.checkBox2.Size = new Size(15, 14);
    this.checkBox2.TabIndex = 3;
    this.checkBox2.UseVisualStyleBackColor = true;
    this.checkBox2.CheckedChanged += new EventHandler(this.checkBox2_CheckedChanged);
    this.TileSurroundingsPanel.Location = new Point(31 /*0x1F*/, 46);
    this.TileSurroundingsPanel.Margin = new Padding(0);
    this.TileSurroundingsPanel.Name = "TileSurroundingsPanel";
    this.TileSurroundingsPanel.Size = new Size(48 /*0x30*/, 48 /*0x30*/);
    this.TileSurroundingsPanel.TabIndex = 6;
    this.checkBox3.AutoSize = true;
    this.checkBox3.Location = new Point(87, 49);
    this.checkBox3.Margin = new Padding(8, 3, 8, 3);
    this.checkBox3.Name = "checkBox3";
    this.checkBox3.Size = new Size(15, 14);
    this.checkBox3.TabIndex = 4;
    this.checkBox3.UseVisualStyleBackColor = true;
    this.checkBox3.CheckedChanged += new EventHandler(this.checkBox3_CheckedChanged);
    this.checkBox4.AutoSize = true;
    this.checkBox4.Location = new Point(92, 97);
    this.checkBox4.Margin = new Padding(92, 3, 92, 3);
    this.checkBox4.Name = "checkBox4";
    this.checkBox4.Size = new Size(15, 14);
    this.checkBox4.TabIndex = 5;
    this.checkBox4.UseVisualStyleBackColor = true;
    this.checkBox4.CheckedChanged += new EventHandler(this.checkBox4_CheckedChanged);
    this.numericUpDown1.Location = new Point(2, 117);
    this.numericUpDown1.Margin = new Padding(2, 3, 2, 3);
    this.numericUpDown1.Maximum = new Decimal(new int[4]
    {
      9999,
      0,
      0,
      0
    });
    this.numericUpDown1.Name = "numericUpDown1";
    this.numericUpDown1.Size = new Size(44, 20);
    this.numericUpDown1.TabIndex = 9;
    this.numericUpDown1.ValueChanged += new EventHandler(this.numericUpDown1_ValueChanged);
    this.numericUpDown2.Location = new Point(50, 117);
    this.numericUpDown2.Margin = new Padding(2, 3, 2, 3);
    this.numericUpDown2.Maximum = new Decimal(new int[4]
    {
      9999,
      0,
      0,
      0
    });
    this.numericUpDown2.Name = "numericUpDown2";
    this.numericUpDown2.Size = new Size(44, 20);
    this.numericUpDown2.TabIndex = 10;
    this.numericUpDown2.ValueChanged += new EventHandler(this.numericUpDown2_ValueChanged);
    this.numericUpDown3.Location = new Point(98, 117);
    this.numericUpDown3.Margin = new Padding(2, 3, 2, 3);
    this.numericUpDown3.Maximum = new Decimal(new int[4]
    {
      9999,
      0,
      0,
      0
    });
    this.numericUpDown3.Name = "numericUpDown3";
    this.numericUpDown3.Size = new Size(44, 20);
    this.numericUpDown3.TabIndex = 11;
    this.numericUpDown3.ValueChanged += new EventHandler(this.numericUpDown3_ValueChanged);
    this.numericUpDown4.Location = new Point(146, 117);
    this.numericUpDown4.Margin = new Padding(2, 3, 2, 3);
    this.numericUpDown4.Maximum = new Decimal(new int[4]
    {
      9999,
      0,
      0,
      0
    });
    this.numericUpDown4.Name = "numericUpDown4";
    this.numericUpDown4.Size = new Size(44, 20);
    this.numericUpDown4.TabIndex = 12;
    this.numericUpDown4.ValueChanged += new EventHandler(this.numericUpDown4_ValueChanged);
    this.Accept_Button.DialogResult = DialogResult.OK;
    this.Accept_Button.Location = new Point(3, 143);
    this.Accept_Button.Name = "Accept_Button";
    this.Accept_Button.Size = new Size(75, 23);
    this.Accept_Button.TabIndex = 0;
    this.Accept_Button.Text = "Accept";
    this.Accept_Button.UseVisualStyleBackColor = true;
    this.Cancel_Button.DialogResult = DialogResult.Cancel;
    this.Cancel_Button.Location = new Point(84, 143);
    this.Cancel_Button.Name = "Cancel_Button";
    this.Cancel_Button.Size = new Size(75, 23);
    this.Cancel_Button.TabIndex = 1;
    this.Cancel_Button.Text = "Cancel";
    this.Cancel_Button.UseVisualStyleBackColor = true;
    this.AcceptButton = (IButtonControl) this.Accept_Button;
    this.AutoScaleDimensions = new SizeF(6f, 13f);
    this.AutoScaleMode = AutoScaleMode.Font;
    this.CancelButton = (IButtonControl) this.Cancel_Button;
    this.ClientSize = new Size(504, 262);
    this.Controls.Add((Control) this.tableLayoutPanel1);
    this.MaximizeBox = false;
    this.MinimizeBox = false;
    this.Name = nameof (Tile_Edit_Form);
    this.ShowInTaskbar = false;
    this.Text = "Edit Tile";
    this.tableLayoutPanel1.ResumeLayout(false);
    this.TilesetPanel.ResumeLayout(false);
    this.TilesetPanel.PerformLayout();
    ((ISupportInitialize) this.pictureBox1).EndInit();
    this.flowLayoutPanel1.ResumeLayout(false);
    this.flowLayoutPanel1.PerformLayout();
    this.PriorityUpDown.EndInit();
    this.numericUpDown1.EndInit();
    this.numericUpDown2.EndInit();
    this.numericUpDown3.EndInit();
    this.numericUpDown4.EndInit();
    this.ResumeLayout(false);
  }
}
