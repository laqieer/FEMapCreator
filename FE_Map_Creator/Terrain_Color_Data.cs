// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Terrain_Color_Data
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

#nullable disable
namespace FE_Map_Creator;

internal class Terrain_Color_Data
{
  private const int ZOOM = 2;
  private static readonly Color ERROR_COLOR = Color.FromArgb((int) byte.MaxValue, 0, (int) byte.MaxValue);
  private static Bitmap Terrain_Graphic;
  private static Bitmap Terrain_Text_Graphic;
  private static Bitmap Terrain_Text_Graphic_Zoom;
  internal static readonly Color[] Terrain_Average_Color;
  private static object graphic_lock = new object();
  internal static readonly Dictionary<int, Terrain_Color_Data> TERRAIN_COLORS = new Dictionary<int, Terrain_Color_Data>()
  {
    {
      0,
      new Terrain_Color_Data("Debug", Terrain_Color_Data.ERROR_COLOR, Terrain_Color_Data.ERROR_COLOR)
    },
    {
      1,
      new Terrain_Color_Data("Plains", Color.FromArgb(80 /*0x50*/, 240 /*0xF0*/, 48 /*0x30*/), Color.FromArgb(224 /*0xE0*/, 224 /*0xE0*/, 48 /*0x30*/))
    },
    {
      2,
      new Terrain_Color_Data("Road", Color.FromArgb(224 /*0xE0*/, 204, 32 /*0x20*/), Color.FromArgb(224 /*0xE0*/, 224 /*0xE0*/, 48 /*0x30*/))
    },
    {
      3,
      new Terrain_Color_Data("Village", Color.FromArgb(224 /*0xE0*/, 24, 24))
    },
    {
      4,
      new Terrain_Color_Data("Village (Closed)", Color.FromArgb(160 /*0xA0*/, 24, 24))
    },
    {
      5,
      new Terrain_Color_Data("House", Color.FromArgb(208 /*0xD0*/, 192 /*0xC0*/, 48 /*0x30*/))
    },
    {
      6,
      new Terrain_Color_Data("Armory", Color.FromArgb(176 /*0xB0*/, 120, 80 /*0x50*/), Color.FromArgb(96 /*0x60*/, 64 /*0x40*/, 16 /*0x10*/))
    },
    {
      7,
      new Terrain_Color_Data("Vendor", Color.FromArgb(200, 48 /*0x30*/, 32 /*0x20*/), Color.FromArgb(96 /*0x60*/, 64 /*0x40*/, 16 /*0x10*/))
    },
    {
      8,
      new Terrain_Color_Data("Arena", Color.FromArgb(192 /*0xC0*/, 184, 120), Color.FromArgb(96 /*0x60*/, 64 /*0x40*/, 16 /*0x10*/))
    },
    {
      9,
      new Terrain_Color_Data("C. Room???", Terrain_Color_Data.ERROR_COLOR, Terrain_Color_Data.ERROR_COLOR)
    },
    {
      10,
      new Terrain_Color_Data("Fortress", Color.FromArgb(184, 160 /*0xA0*/, 48 /*0x30*/), Color.FromArgb(96 /*0x60*/, 64 /*0x40*/, 16 /*0x10*/))
    },
    {
      11,
      new Terrain_Color_Data("Gate (Castle)", Color.FromArgb(160 /*0xA0*/, 136, 80 /*0x50*/))
    },
    {
      12,
      new Terrain_Color_Data("Forest", Color.FromArgb(0, 160 /*0xA0*/, 16 /*0x10*/))
    },
    {
      13,
      new Terrain_Color_Data("Thicket", Color.FromArgb(0, 120, 32 /*0x20*/))
    },
    {
      14,
      new Terrain_Color_Data("Sand", Color.FromArgb(240 /*0xF0*/, 240 /*0xF0*/, 176 /*0xB0*/))
    },
    {
      15,
      new Terrain_Color_Data("Desert", Color.FromArgb(240 /*0xF0*/, 240 /*0xF0*/, 136))
    },
    {
      16 /*0x10*/,
      new Terrain_Color_Data("River", Color.FromArgb(48 /*0x30*/, 120, 240 /*0xF0*/))
    },
    {
      17,
      new Terrain_Color_Data("Hill", Color.FromArgb(48 /*0x30*/, 200, 120))
    },
    {
      18,
      new Terrain_Color_Data("Peak", Color.FromArgb(160 /*0xA0*/, 120, 64 /*0x40*/))
    },
    {
      19,
      new Terrain_Color_Data("Bridge", Color.FromArgb(184, 152, 24), Color.FromArgb(120, 88, 16 /*0x10*/))
    },
    {
      20,
      new Terrain_Color_Data("Bridge (Broken)", Color.FromArgb(136, 104, 16 /*0x10*/))
    },
    {
      21,
      new Terrain_Color_Data("Sea", Color.FromArgb(24, 40, 200))
    },
    {
      22,
      new Terrain_Color_Data("Lake", Color.FromArgb(24, 80 /*0x50*/, 224 /*0xE0*/))
    },
    {
      23,
      new Terrain_Color_Data("Floor", Color.FromArgb(120, 104, 168), Color.FromArgb(160 /*0xA0*/, 152, 112 /*0x70*/))
    },
    {
      24,
      new Terrain_Color_Data("Floor (Ward)", Color.FromArgb(248, 128 /*0x80*/, 248))
    },
    {
      25,
      new Terrain_Color_Data("Fence", Color.FromArgb(120, 120, 80 /*0x50*/), Color.FromArgb(80 /*0x50*/, 80 /*0x50*/, 80 /*0x50*/))
    },
    {
      26,
      new Terrain_Color_Data("Wall", Color.FromArgb(96 /*0x60*/, 96 /*0x60*/, 96 /*0x60*/))
    },
    {
      27,
      new Terrain_Color_Data("Wall (Weak)", Color.FromArgb(104, 104, 104), Color.FromArgb(96 /*0x60*/, 96 /*0x60*/, 96 /*0x60*/))
    },
    {
      28,
      new Terrain_Color_Data("Rubble???", Terrain_Color_Data.ERROR_COLOR, Terrain_Color_Data.ERROR_COLOR)
    },
    {
      29,
      new Terrain_Color_Data("Pillar", Color.FromArgb(96 /*0x60*/, 64 /*0x40*/, 88))
    },
    {
      30,
      new Terrain_Color_Data("Door", Color.FromArgb(120, 120, 112 /*0x70*/))
    },
    {
      31 /*0x1F*/,
      new Terrain_Color_Data("Throne", Color.FromArgb(208 /*0xD0*/, 56, 72), Color.FromArgb(192 /*0xC0*/, 216, 72))
    },
    {
      32 /*0x20*/,
      new Terrain_Color_Data("Chest (Opened)", Color.FromArgb(176 /*0xB0*/, 64 /*0x40*/, 24), Color.FromArgb(168, 168, 40))
    },
    {
      33,
      new Terrain_Color_Data("Chest", Color.FromArgb(240 /*0xF0*/, 64 /*0x40*/, 24), Color.FromArgb(224 /*0xE0*/, 224 /*0xE0*/, 48 /*0x30*/))
    },
    {
      34,
      new Terrain_Color_Data("Roof", Color.FromArgb(144 /*0x90*/, 112 /*0x70*/, 128 /*0x80*/), Color.FromArgb(128 /*0x80*/, 112 /*0x70*/, 64 /*0x40*/))
    },
    {
      35,
      new Terrain_Color_Data("Gate (Fort)", Color.FromArgb(176 /*0xB0*/, 144 /*0x90*/, 80 /*0x50*/))
    },
    {
      36,
      new Terrain_Color_Data("Church???", Terrain_Color_Data.ERROR_COLOR, Terrain_Color_Data.ERROR_COLOR)
    },
    {
      37,
      new Terrain_Color_Data("Ruins (Destroyed)", Color.FromArgb(80 /*0x50*/, 72, 56))
    },
    {
      38,
      new Terrain_Color_Data("Cliff", Color.FromArgb(120, 104, 8))
    },
    {
      39,
      new Terrain_Color_Data("Ballista", Color.White)
    },
    {
      40,
      new Terrain_Color_Data("Heavy Ballista", Color.White)
    },
    {
      41,
      new Terrain_Color_Data("Killer Ballista", Color.White)
    },
    {
      42,
      new Terrain_Color_Data("Flat", Color.FromArgb(192 /*0xC0*/, 112 /*0x70*/, 96 /*0x60*/), Color.FromArgb(144 /*0x90*/, 96 /*0x60*/, 80 /*0x50*/))
    },
    {
      43,
      new Terrain_Color_Data("Wreck Tile???")
    },
    {
      44,
      new Terrain_Color_Data("-- (Castle)", Color.FromArgb(40, 40, 40), Color.FromArgb(40, 40, 40))
    },
    {
      45,
      new Terrain_Color_Data("Stairs", Color.FromArgb(200, 200, 112 /*0x70*/), Color.FromArgb(248, 248, 208 /*0xD0*/))
    },
    {
      46,
      new Terrain_Color_Data("-- (Village)", Color.FromArgb(56, 56, 56), Color.FromArgb(40, 40, 40))
    },
    {
      47,
      new Terrain_Color_Data("Glacier", Color.FromArgb(200, 240 /*0xF0*/, 224 /*0xE0*/), Color.FromArgb(224 /*0xE0*/, 248, 248))
    },
    {
      48 /*0x30*/,
      new Terrain_Color_Data("Arena???")
    },
    {
      49,
      new Terrain_Color_Data("Valley", Color.FromArgb(200, 104, 48 /*0x30*/), Color.FromArgb(88, 24, 32 /*0x20*/))
    },
    {
      50,
      new Terrain_Color_Data("Fence???")
    },
    {
      51,
      new Terrain_Color_Data("Snag", Color.FromArgb(64 /*0x40*/, 136, 16 /*0x10*/), Color.FromArgb(176 /*0xB0*/, 176 /*0xB0*/, 24))
    },
    {
      52,
      new Terrain_Color_Data("Bridge (Snag)", Color.FromArgb(80 /*0x50*/, 88, 16 /*0x10*/), Color.FromArgb(176 /*0xB0*/, 176 /*0xB0*/, 24))
    },
    {
      53,
      new Terrain_Color_Data("Sky", Color.FromArgb(248, 248, 248), Color.FromArgb(216, 224 /*0xE0*/, 240 /*0xF0*/))
    },
    {
      54,
      new Terrain_Color_Data("Deeps", Color.FromArgb(24, 40, 104))
    },
    {
      55,
      new Terrain_Color_Data("Ruins (Visit)", Color.FromArgb(160 /*0xA0*/, 24, 24), Color.FromArgb(144 /*0x90*/, 136, 72))
    },
    {
      56,
      new Terrain_Color_Data("Inn", Color.FromArgb(168, 160 /*0xA0*/, 72), Color.FromArgb(136, 120, 144 /*0x90*/))
    },
    {
      57,
      new Terrain_Color_Data("Barrel", Color.FromArgb(144 /*0x90*/, 120, 56), Color.FromArgb(248, 248, 208 /*0xD0*/))
    },
    {
      58,
      new Terrain_Color_Data("Bone", Color.FromArgb(248, 248, 208 /*0xD0*/), Color.FromArgb(208 /*0xD0*/, 200, 144 /*0x90*/))
    },
    {
      59,
      new Terrain_Color_Data("Dark", Color.FromArgb(224 /*0xE0*/, 192 /*0xC0*/, 128 /*0x80*/), Color.FromArgb(152, 104, 40))
    },
    {
      60,
      new Terrain_Color_Data("Water", Color.FromArgb(24, 120, 224 /*0xE0*/), Color.FromArgb(16 /*0x10*/, 192 /*0xC0*/, 160 /*0xA0*/))
    },
    {
      61,
      new Terrain_Color_Data("Gunnel", Color.FromArgb(184, 176 /*0xB0*/, 104), Color.FromArgb(112 /*0x70*/, 120, 200))
    },
    {
      62,
      new Terrain_Color_Data("Deck", Color.FromArgb(184, 144 /*0x90*/, 144 /*0x90*/), Color.FromArgb(128 /*0x80*/, 136, 184))
    },
    {
      63 /*0x3F*/,
      new Terrain_Color_Data("Brace", Color.FromArgb(104, 128 /*0x80*/, 120), Color.FromArgb(96 /*0x60*/, 96 /*0x60*/, 96 /*0x60*/))
    },
    {
      64 /*0x40*/,
      new Terrain_Color_Data("Mast", Color.FromArgb(184, 176 /*0xB0*/, 104), Color.FromArgb(144 /*0x90*/, 120, 56))
    },
    {
      65,
      new Terrain_Color_Data("Crenel", Color.FromArgb(160 /*0xA0*/, 144 /*0x90*/, 120), Color.FromArgb(96 /*0x60*/, 96 /*0x60*/, 96 /*0x60*/))
    }
  };
  private string Name;

  public Color Color1 { get; private set; }

  public bool Two_Colors { get; private set; }

  public Color Color2 { get; private set; }

  public Color avg_color
  {
    get
    {
      if (Terrain_Color_Data.Terrain_Average_Color != null && Terrain_Color_Data.TERRAIN_COLORS.ContainsValue(this))
      {
        int key = Terrain_Color_Data.TERRAIN_COLORS.First<KeyValuePair<int, Terrain_Color_Data>>((Func<KeyValuePair<int, Terrain_Color_Data>, bool>) (terrain => terrain.Value == this)).Key;
        if (key >= 0 && key < Terrain_Color_Data.Terrain_Average_Color.Length)
          return Terrain_Color_Data.Terrain_Average_Color[key];
      }
      return this.Color1;
    }
  }

  static Terrain_Color_Data()
  {
    Terrain_Color_Data.Terrain_Graphic = (Bitmap) null;
    try
    {
      if (File.Exists("Terrain_Images.png"))
        Terrain_Color_Data.Terrain_Graphic = new Bitmap("Terrain_Images.png");
    }
    catch
    {
    }
    if (Terrain_Color_Data.Terrain_Graphic != null)
    {
      using (Bitmap bmp = new Bitmap(Terrain_Color_Data.Terrain_Graphic.Width, Terrain_Color_Data.Terrain_Graphic.Height, PixelFormat.Format32bppArgb))
      {
        using (Graphics graphics = Graphics.FromImage((Image) bmp))
          graphics.DrawImage((Image) Terrain_Color_Data.Terrain_Graphic, new Rectangle(0, 0, bmp.Width, bmp.Height), new Rectangle(0, 0, Terrain_Color_Data.Terrain_Graphic.Width, Terrain_Color_Data.Terrain_Graphic.Height), GraphicsUnit.Pixel);
        Terrain_Color_Data.Terrain_Average_Color = new Color[bmp.Width / 16 /*0x10*/];
        HashSet<Color> colorSet = new HashSet<Color>();
        for (int key = 0; key < Terrain_Color_Data.Terrain_Average_Color.Length; ++key)
        {
          Color color1 = FE_Map_Creator_Form.rms_color(bmp, new Rectangle(key * 16 /*0x10*/, 0, 16 /*0x10*/, 16 /*0x10*/));
          if (Terrain_Color_Data.TERRAIN_COLORS.ContainsKey(key))
            color1 = Color.FromArgb(((int) Terrain_Color_Data.TERRAIN_COLORS[key].Color1.R + (int) color1.R) / 2, ((int) Terrain_Color_Data.TERRAIN_COLORS[key].Color1.G + (int) color1.G) / 2, ((int) Terrain_Color_Data.TERRAIN_COLORS[key].Color1.B + (int) color1.B) / 2);
          Color color2 = color1;
          if (colorSet.Contains(color2))
          {
            for (int index = 1; index <= 765; ++index)
            {
              int num1 = (index + 2) / 3;
              int num2 = (index + 1) / 3;
              int num3 = index / 3;
              color2 = Color.FromArgb(Math.Max(0, Math.Min((int) byte.MaxValue, (int) color1.R + num2)), Math.Max(0, Math.Min((int) byte.MaxValue, (int) color1.G + num3)), Math.Max(0, Math.Min((int) byte.MaxValue, (int) color1.B + num1)));
              if (colorSet.Contains(color2))
              {
                color2 = Color.FromArgb(Math.Max(0, Math.Min((int) byte.MaxValue, (int) color1.R - num2)), Math.Max(0, Math.Min((int) byte.MaxValue, (int) color1.G - num3)), Math.Max(0, Math.Min((int) byte.MaxValue, (int) color1.B - num1)));
                if (!colorSet.Contains(color2))
                  break;
              }
              else
                break;
            }
          }
          Terrain_Color_Data.Terrain_Average_Color[key] = color2;
          colorSet.Add(Terrain_Color_Data.Terrain_Average_Color[key]);
        }
      }
    }
    Terrain_Color_Data.generate_terrain_text_graphic();
  }

  private static void generate_terrain_text_graphic()
  {
    if (Terrain_Color_Data.Terrain_Text_Graphic != null)
      return;
    int num = 16 /*0x10*/;
    Terrain_Color_Data.Terrain_Text_Graphic = new Bitmap((Terrain_Color_Data.TERRAIN_COLORS.Keys.Max() + 1) * num, num);
    Terrain_Color_Data.Terrain_Text_Graphic_Zoom = new Bitmap(Terrain_Color_Data.Terrain_Text_Graphic.Width * 2, Terrain_Color_Data.Terrain_Text_Graphic.Height * 2);
    for (int key = 0; key <= Terrain_Color_Data.TERRAIN_COLORS.Keys.Max(); ++key)
    {
      using (Graphics graphics = Graphics.FromImage((Image) Terrain_Color_Data.Terrain_Text_Graphic_Zoom))
      {
        using (Font font = new Font("Lucida Console", 18f))
        {
          graphics.SetClip(new Rectangle(key * num * 2, 0, num * 2, num * 2));
          string s = Terrain_Color_Data.TERRAIN_COLORS[key].terrain_name(true);
          using (Brush brush = (Brush) new SolidBrush(Color.FromArgb(64 /*0x40*/, Color.Black)))
            graphics.FillRectangle(brush, new Rectangle(new Point(key * 2 * num, 0), new Size(2 * num, 2 * num)));
          using (Brush brush = (Brush) new SolidBrush(Color.FromArgb(64 /*0x40*/, Terrain_Color_Data.TERRAIN_COLORS[key].Color1)))
            graphics.FillRectangle(brush, new Rectangle(new Point(key * 2 * num, 0), new Size(2 * num, 2 * num)));
          using (GraphicsPath path = new GraphicsPath())
          {
            path.AddString(s, font.FontFamily, (int) font.Style, (float) ((double) graphics.DpiY * (double) font.Size / 72.0), new Point((key * num - num / 16 /*0x10*/) * 2, num * 3 / 16 /*0x10*/ * 2), StringFormat.GenericDefault);
            using (Pen pen = new Pen(!Terrain_Color_Data.TERRAIN_COLORS.ContainsKey(key) || !Terrain_Color_Data.TERRAIN_COLORS[key].Two_Colors ? Color.Black : Terrain_Color_Data.TERRAIN_COLORS[key].Color2, 4f))
              graphics.DrawPath(pen, path);
            using (Brush brush = (Brush) new SolidBrush(Terrain_Color_Data.TERRAIN_COLORS.ContainsKey(key) ? Terrain_Color_Data.TERRAIN_COLORS[key].Color1 : Color.White))
              graphics.FillPath(brush, path);
          }
        }
      }
      using (Graphics graphics = Graphics.FromImage((Image) Terrain_Color_Data.Terrain_Text_Graphic))
        graphics.DrawImage((Image) Terrain_Color_Data.Terrain_Text_Graphic_Zoom, new Rectangle(new Point(key * num, 0), new Size(num, num)), new Rectangle(new Point(key * num * 2, 0), new Size(num * 2, num * 2)), GraphicsUnit.Pixel);
    }
  }

  public override string ToString()
  {
    return this.Two_Colors ? $"Terrain: {this.Name}, colors {this.Color1} {this.Color2}" : $"Terrain: {this.Name}, color {this.Color1}";
  }

  private Terrain_Color_Data(string name)
    : this(name, Terrain_Color_Data.ERROR_COLOR, Terrain_Color_Data.ERROR_COLOR)
  {
  }

  private Terrain_Color_Data(string name, Color color1)
  {
    this.Name = name;
    this.Color1 = color1;
    this.Two_Colors = false;
  }

  private Terrain_Color_Data(string name, Color color1, Color color2)
  {
    this.Name = name;
    this.Color1 = color1;
    this.Two_Colors = true;
    this.Color2 = color2;
  }

  internal string terrain_name() => this.terrain_name(false);

  private string terrain_name(bool shorten)
  {
    return shorten ? this.Name.Substring(0, Math.Min(2, this.Name.Length)) : this.Name;
  }

  internal static void draw(
    Graphics g,
    int x,
    int y,
    int terrain_id,
    int tile_size,
    bool active,
    bool draw_tile_image = false,
    int zoom = 1)
  {
    if (terrain_id == 0 && !draw_tile_image)
      return;
    int key = terrain_id > 0 ? terrain_id : -terrain_id;
    if (!Terrain_Color_Data.TERRAIN_COLORS.ContainsKey(key))
      return;
    Rectangle rectangle = new Rectangle(new Point(x * tile_size * zoom, y * tile_size * zoom), new Size(tile_size * zoom, tile_size * zoom));
    if (!g.ClipBounds.IntersectsWith((RectangleF) rectangle))
      return;
    lock (Terrain_Color_Data.graphic_lock)
    {
      if (draw_tile_image && Terrain_Color_Data.Terrain_Graphic != null)
        g.DrawImage((Image) Terrain_Color_Data.Terrain_Graphic, rectangle, new Rectangle(new Point(key * tile_size, 0), new Size(tile_size, tile_size)), GraphicsUnit.Pixel);
      else if (active)
      {
        if (zoom == 1)
          g.DrawImage((Image) Terrain_Color_Data.Terrain_Text_Graphic, rectangle, new Rectangle(new Point(key * tile_size, 0), new Size(tile_size, tile_size)), GraphicsUnit.Pixel);
        else
          g.DrawImage((Image) Terrain_Color_Data.Terrain_Text_Graphic_Zoom, rectangle, new Rectangle(new Point(key * tile_size * 2, 0), new Size(tile_size * 2, tile_size * 2)), GraphicsUnit.Pixel);
      }
      else
      {
        using (Brush brush = (Brush) new SolidBrush(Color.FromArgb(32 /*0x20*/, Color.Black)))
          g.FillRectangle(brush, rectangle);
        using (Brush brush = (Brush) new SolidBrush(Color.FromArgb(16 /*0x10*/, Terrain_Color_Data.TERRAIN_COLORS[key].Color1)))
          g.FillRectangle(brush, rectangle);
      }
      if (!active || terrain_id >= 0)
        return;
      using (Brush brush = (Brush) new SolidBrush(Color.FromArgb(64 /*0x40*/, Color.Red)))
        g.FillRectangle(brush, rectangle);
      using (Pen pen = new Pen(Color.Red, (float) zoom))
        g.DrawRectangle(pen, (float) (x * zoom * tile_size) + 0.5f * (float) zoom, (float) (y * zoom * tile_size) + 0.5f * (float) zoom, (float) zoom * ((float) tile_size - 1f), (float) zoom * ((float) tile_size - 1f));
    }
  }

  internal static void dispose()
  {
    lock (Terrain_Color_Data.graphic_lock)
    {
      if (Terrain_Color_Data.Terrain_Graphic != null)
      {
        Terrain_Color_Data.Terrain_Graphic.Dispose();
        Terrain_Color_Data.Terrain_Graphic = (Bitmap) null;
      }
      if (Terrain_Color_Data.Terrain_Text_Graphic != null)
      {
        Terrain_Color_Data.Terrain_Text_Graphic.Dispose();
        Terrain_Color_Data.Terrain_Text_Graphic = (Bitmap) null;
      }
      if (Terrain_Color_Data.Terrain_Text_Graphic_Zoom == null)
        return;
      Terrain_Color_Data.Terrain_Text_Graphic_Zoom.Dispose();
      Terrain_Color_Data.Terrain_Text_Graphic_Zoom = (Bitmap) null;
    }
  }
}
