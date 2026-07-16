// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.FE_Map_Creator_Form
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using FE_Map_Creator.Generation;
using FEXNA_Library;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;

#nullable disable
namespace FE_Map_Creator;

public class FE_Map_Creator_Form : Form
{
  public const int TILE_SIZE = 16 /*0x10*/;
  private const int DEPTH = 1;
  private const int MAX_DEPTH = 2;
  private static readonly Color TRANSPARENT_COLOR = Color.FromArgb((int) byte.MaxValue, 0, 0, 0);
  private Tileset_Generation_Data Tileset_Generator_Data;
  protected Dictionary<int, Data_Tileset> Tileset_Data = new Dictionary<int, Data_Tileset>();
  protected Dictionary<int, Data_Terrain> Terrain_Data = new Dictionary<int, Data_Terrain>();
  private Tile_Matching_Data Tile_Matches;
  private bool Changed;
  protected string Tileset_Filename = "";
  protected string Tileset_Name = "";
  private int Tileset_Id = -1;
  protected string Map_Directory;
  protected string Tileset_Directory;
  protected Bitmap Tileset_Image;
  protected int Map_Width = 20;
  protected int Map_Height = 20;
  private Dictionary<string, int[,]> Maps = new Dictionary<string, int[,]>();
  private int[,] Map_Tiles;
  private int[,] Undo_Map_Tiles;
  private bool[,] Drawn_Tiles;
  private bool[,] Locked_Tiles;
  private int[,] Terrain_Types;
  private HashSet<int> Open_Tiles;
  protected int Zoom = 1;
  private HashSet<int> Blocked_Terrain_Types = new HashSet<int>();
  private static Random rand = new Random();
  protected bool Updating;
  private CancellationTokenSource Map_Generation_Cancellation;
  protected bool Drawing;
  protected bool Selecting;
  protected Point Drawing_Mouse_Loc;
  protected Point Base_Drawing_Mouse_Loc;
  private bool Box_Pin_Value;
  private int Box_Terrain_Value;
  private Tileset_Palette_Form Tileset_Palette;
  private Terrain_Palette_Form Terrain_Palette;
  private static readonly Dictionary<byte, Size> REVERSE_DIRS = new Dictionary<byte, Size>()
  {
    {
      (byte) 2,
      new Size(0, -1)
    },
    {
      (byte) 4,
      new Size(1, 0)
    },
    {
      (byte) 6,
      new Size(-1, 0)
    },
    {
      (byte) 8,
      new Size(0, 1)
    }
  };
  private IContainer components;
  private TableLayoutPanel tableLayoutPanel1;
  private PictureBox MapPicture;
  private FlowLayoutPanel flowLayoutPanel1;
  private NumericUpDown WidthUpDown;
  private NumericUpDown HeightUpDown;
  private NumericUpDown DepthUpDown;
  private NumericUpDown DistUpDown;
  private Button CancelOperationButton;
  private Label label1;
  private Label label2;
  private Button NewMapButton;
  private Label label3;
  private NumericUpDown ZoomUpDown;
  private Panel PictureBoxPanel;
  private OpenFileDialog MapImageImportDialog;
  private ToolStripMenuItem fileToolStripMenuItem;
  private ToolStripMenuItem loadTilesetToolStripMenuItem;
  private ToolStripMenuItem importMapImageToolStripMenuItem;
  private MenuStrip menuStrip1;
  private Panel MainPanel;
  private ToolStripMenuItem saveMapAsToolStripMenuItem;
  private ToolStripMenuItem loadMapToolStripMenuItem;
  private ToolStripMenuItem exitToolStripMenuItem;
  private SaveFileDialog SaveMapDialog;
  private OpenFileDialog OpenMapDialog;
  private OpenFileDialog LoadTilesetDialog;
  private ToolStripMenuItem saveTilesetSettingsToolStripMenuItem;
  private ToolStripMenuItem editToolStripMenuItem;
  private ToolStripMenuItem resizeMapToolStripMenuItem;
  private ToolStripMenuItem importClipboardImageToolStripMenuItem;
  private ToolStripMenuItem copyMapImageToolStripMenuItem;
  private FolderBrowserDialog folderBrowserDialog1;
  private ToolStripMenuItem mapGenerationToolStripMenuItem;
  private ToolStripMenuItem processTilesetFromMapsToolStripMenuItem;
  private ToolStripMenuItem generateMapToolStripMenuItem;
  private ComboBox DrawingModeComboBox;
  private ToolStripMenuItem undoToolStripMenuItem;
  private ToolStripMenuItem clearPinnedTilesToolStripMenuItem;
  private ToolStripMenuItem repairMapToolStripMenuItem;
  private ToolStripMenuItem experimentalConstraintSolverToolStripMenuItem;
  private ToolStripMenuItem hybridSolverToolStripMenuItem;
  private ToolStripMenuItem experimentalBranchArcConsistencyToolStripMenuItem;
  private ToolStripSeparator toolStripMenuItem1;
  private ToolStripMenuItem clearTerrainTagsToolStripMenuItem;
  private ToolStripMenuItem convertMapToTerrainTagsToolStripMenuItem;
  private ToolStripMenuItem prepareTilesetForEditsToolStripMenuItem;
  private StatusStrip statusStrip1;
  private ToolStripStatusLabel StatusbarSpacerLabel;
  private ToolStripProgressBar progressBar1;
  private ToolStripSeparator toolStripMenuItem2;
  private FlowLayoutPanel ToolsFlowPanel;
  private RadioButton BrushRadioButton;
  private RadioButton BoxRadioButton;
  private RadioButton FloodFillRadioButton;
  private ToolTip toolTip1;
  private Label WidthLabel;
  private Label HeightLabel;
  private ToolStripMenuItem importTerrainTagsFromClipboardImageToolStripMenuItem;
  private ToolStripMenuItem copyTerrainTagsImageToolStripMenuItem;
  private ToolStripStatusLabel CursorPositionStatusLabel;

  protected bool no_tileset => this.Tileset_Filename == "";

  private Dictionary<int, Tile_Data> tileset_config_data
  {
    get
    {
      return this.Tileset_Generator_Data == null ? (Dictionary<int, Tile_Data>) null : this.Tileset_Generator_Data.generation_data;
    }
  }

  public Dictionary<short, short> identical_tiles => this.Tileset_Generator_Data.identical_tiles;

  private Data_Tileset tileset_data
  {
    get
    {
      return this.Tileset_Id != -1 && this.Tileset_Data != null ? this.Tileset_Data[this.Tileset_Id] : (Data_Tileset) null;
    }
  }

  public bool can_edit_tileset => this.Tile_Matches != null;

  private Tools active_tool
  {
    get
    {
      if (this.BrushRadioButton.Checked)
        return Tools.Brush;
      if (this.BoxRadioButton.Checked)
        return Tools.Box;
      return this.FloodFillRadioButton.Checked ? Tools.Bucket : Tools.Brush;
    }
  }

  private bool pinning_tiles => this.DrawingModeComboBox.SelectedIndex == 2;

  private bool painting_terrain => this.DrawingModeComboBox.SelectedIndex == 1;

  public FE_Map_Creator_Form()
  {
    this.Load += new EventHandler(this.FE_Map_Creator_Form_Load);
    this.InitializeComponent();
    this.MapPicture.MouseDown += new MouseEventHandler(this.MapPicture_MouseDown);
    this.MapPicture.MouseMove += new MouseEventHandler(this.MapPicture_MouseMove);
    this.MapPicture.MouseUp += new MouseEventHandler(this.MapPicture_MouseUp);
    this.MapPicture.Paint += new PaintEventHandler(this.MapPicture_Paint);
    this.MapPicture.MouseLeave += new EventHandler(this.MapPicture_MouseLeave);
    this.load_tilesets();
    string directoryName = App_Paths.Base_Directory;
    this.Map_Directory = directoryName;
    if (Directory.Exists(App_Paths.Tilesets_Directory))
      directoryName = App_Paths.Tilesets_Directory;
    this.Tileset_Directory = directoryName;
    this.Tileset_Palette = new Tileset_Palette_Form();
    this.Tileset_Palette.FormClosing += new FormClosingEventHandler(this.Tileset_Palette_FormClosing);
    this.Tileset_Palette.Tile_Selected += new EventHandler(this.Tileset_Palette_Tile_Selected);
    this.Terrain_Palette = new Terrain_Palette_Form();
    this.Terrain_Palette.FormClosing += new FormClosingEventHandler(this.Terrain_Palette_FormClosing);
    this.Terrain_Palette.Terrain_Selected += new EventHandler(this.Terrain_Palette_Terrain_Selected);
    this.Shown += new EventHandler(this.FE_Map_Creator_Form_Shown);
    this.FormClosed += new FormClosedEventHandler(this.FE_Map_Creator_Form_FormClosed);
    this.DrawingModeComboBox.SelectedIndex = 0;
    this.Blocked_Terrain_Types.Clear();
    this.DepthUpDown.Maximum = 2M;
  }

  private void FE_Map_Creator_Form_Load(object sender, EventArgs e)
  {
    Assembly executingAssembly = Assembly.GetExecutingAssembly();
    this.BrushRadioButton.Text = "";
    this.BrushRadioButton.Image = (Image) this.load_icon(executingAssembly, "FE_Map_Creator.Icons.Brush.png");
    this.BoxRadioButton.Text = "";
    this.BoxRadioButton.Image = (Image) this.load_icon(executingAssembly, "FE_Map_Creator.Icons.Rectangle.png");
    this.FloodFillRadioButton.Text = "";
    this.FloodFillRadioButton.Image = (Image) this.load_icon(executingAssembly, "FE_Map_Creator.Icons.Bucket.png");
    this.BrushRadioButton.Checked = true;
  }

  private Bitmap load_icon(Assembly asm, string resource)
  {
    using (Stream manifestResourceStream = asm.GetManifestResourceStream(resource))
    {
      if (manifestResourceStream == null)
        return (Bitmap) null;
      Bitmap bitmap = new Bitmap(manifestResourceStream);
      bitmap.MakeTransparent(Color.FromArgb((int) byte.MaxValue, 0, 128 /*0x80*/, (int) byte.MaxValue));
      return bitmap;
    }
  }

  private void FE_Map_Creator_Form_Shown(object sender, EventArgs e)
  {
    this.Tileset_Palette.Show((IWin32Window) this);
    this.Tileset_Palette.Location = Point.Add(this.Tileset_Palette.Location, new Size(this.Size.Width, 0));
    this.Terrain_Palette.Show((IWin32Window) this);
    this.Terrain_Palette.Location = Point.Add(this.Terrain_Palette.Location, new Size(this.Size.Width, this.Tileset_Palette.Height + 16 /*0x10*/));
  }

  private void FE_Map_Creator_Form_FormClosed(object sender, FormClosedEventArgs e)
  {
    Terrain_Color_Data.dispose();
  }

  protected void load_tilesets()
  {
    string str1 = App_Paths.Tileset_Data_File;
    if (File.Exists(str1))
      this.Tileset_Data = this.read_tileset_xml(str1);
    string str2 = App_Paths.Terrain_Data_File;
    if (!File.Exists(str2))
      return;
    this.Terrain_Data = this.read_terrain_xml(str2);
  }

  private Dictionary<int, Data_Tileset> read_tileset_xml(string filename)
  {
    Dictionary<int, Data_Tileset> dictionary = new Dictionary<int, Data_Tileset>();
    try
    {
      XElement xelement1 = XDocument.Load(filename).Root.Element((XName) "Asset");
      if (xelement1 != null && xelement1.Attribute((XName) "Type").Value == "Generic:Dictionary[int,FEXNA_Library.Data_Tileset]" && xelement1.Element((XName) "Item") != null)
      {
        foreach (XElement xelement2 in xelement1.Elements((XName) "Item").Select<XElement, XElement>((Func<XElement, XElement>) (x => x.Element((XName) "Value"))))
        {
          if (xelement2 != null)
          {
            Data_Tileset dataTileset = new Data_Tileset();
            int result;
            if (xelement2.Element((XName) "Id") != null && int.TryParse(xelement2.Element((XName) "Id").Value, out result))
              dataTileset.Id = Convert.ToInt32(xelement2.Element((XName) "Id").Value);
            if (xelement2.Element((XName) "Name") != null)
              dataTileset.Name = xelement2.Element((XName) "Name").Value;
            if (xelement2.Element((XName) "Graphic_Name") != null)
              dataTileset.Graphic_Name = xelement2.Element((XName) "Graphic_Name").Value;
            dataTileset.Terrain_Tags = new List<int>();
            if (xelement2.Element((XName) "Terrain_Tags") != null)
            {
              string str = xelement2.Element((XName) "Terrain_Tags").Value;
              char[] chArray = new char[1]{ ' ' };
              foreach (string s in str.Split(chArray))
              {
                if (int.TryParse(s, out result))
                  dataTileset.Terrain_Tags.Add(Convert.ToInt32(s));
              }
            }
            dictionary[dataTileset.Id] = dataTileset;
          }
        }
      }
      return dictionary;
    }
    catch (Exception)
    {
    }
    return (Dictionary<int, Data_Tileset>) null;
  }

  private Dictionary<int, Data_Terrain> read_terrain_xml(string filename)
  {
    Dictionary<int, Data_Terrain> dictionary = new Dictionary<int, Data_Terrain>();
    try
    {
      XElement xelement1 = XDocument.Load(filename).Root.Element((XName) "Asset");
      if (xelement1 != null && xelement1.Attribute((XName) "Type").Value == "Generic:Dictionary[int,FEXNA_Library.Data_Terrain]" && xelement1.Element((XName) "Item") != null)
      {
        foreach (XElement xelement2 in xelement1.Elements((XName) "Item").Select<XElement, XElement>((Func<XElement, XElement>) (x => x.Element((XName) "Value"))))
        {
          if (xelement2 != null)
          {
            Data_Terrain dataTerrain = new Data_Terrain();
            int result;
            if (xelement2.Element((XName) "Id") != null && int.TryParse(xelement2.Element((XName) "Id").Value, out result))
              dataTerrain.Id = Convert.ToInt32(xelement2.Element((XName) "Id").Value);
            if (xelement2.Element((XName) "Name") != null)
              dataTerrain.Name = xelement2.Element((XName) "Name").Value;
            if (xelement2.Element((XName) "Move_Costs") != null && xelement2.Element((XName) "Move_Costs").Elements((XName) "Item").Any<XElement>((Func<XElement, bool>) (x => x != null)))
            {
              int index1 = 0;
              foreach (XElement element in xelement2.Element((XName) "Move_Costs").Elements((XName) "Item"))
              {
                if (element != null)
                {
                  string[] strArray = element.Value.Split(' ');
                  for (int index2 = 0; index2 < strArray.Length; ++index2)
                  {
                    if (int.TryParse(strArray[index2], out result))
                      dataTerrain.Move_Costs[index1][index2] = Convert.ToInt32(strArray[index2]);
                  }
                }
                ++index1;
              }
            }
            dictionary[dataTerrain.Id] = dataTerrain;
          }
        }
      }
      return dictionary;
    }
    catch (Exception)
    {
    }
    return (Dictionary<int, Data_Terrain>) null;
  }

  protected void save_tilesets()
  {
    this.save_tilesets(App_Paths.tileset_generation_file(this.Tileset_Filename));
  }

  protected void save_tilesets(string filename)
  {
    if (this.Tileset_Generator_Data == null)
      return;
    Directory.CreateDirectory(Path.GetDirectoryName(filename));
    using (FileStream output = new FileStream(filename, FileMode.Create))
    {
      using (BinaryWriter writer = new BinaryWriter((Stream) output))
        this.Tileset_Generator_Data.write(writer);
    }
  }

  protected void save_map(string filename, int filter)
  {
    this.save_map(filename, this.Map_Tiles, this.Tileset_Name, filter);
    this.Text = $"Map Editor - {Path.GetFileNameWithoutExtension(filename)}";
  }

  protected void save_map(string filename, int[,] map_tiles, string tileset_filename, int filter)
  {
    Map_Codec_Registry registry = new Map_Codec_Registry();
    Map_Document document = new Map_Document(map_tiles, tileset_filename);
    Map_Write_Options write_options = new Map_Write_Options
    {
      Tileset = tileset_filename,
      Tileset_Image_Source = tileset_filename
    };
    Map_Format? format = null;
    switch (filter)
    {
      case 1:
        format = Map_Format.Text;
        break;
      case 2:
        format = Map_Format.Mar;
        break;
      case 3:
        format = Map_Format.Tmx;
        break;
      default:
        try { format = registry.format_from_path(filename); }
        catch (NotSupportedException ex)
        {
          MessageBox.Show(this, ex.Message, "Unsupported Format", MessageBoxButtons.OK, MessageBoxIcon.Hand);
          return;
        }
        break;
    }
    registry.write(filename, document, write_options, format);
  }

  protected void load_map(int[,] map_tiles)
  {
    this.WidthUpDown.Value = (Decimal) (this.Map_Width = map_tiles.GetLength(0));
    this.HeightUpDown.Value = (Decimal) (this.Map_Height = map_tiles.GetLength(1));
    this.Map_Tiles = map_tiles;
    this.copy_map_to_undo(false);
    this.reset_metadata();
    for (int index1 = 0; index1 < this.Map_Height; ++index1)
    {
      for (int index2 = 0; index2 < this.Map_Width; ++index2)
        this.Drawn_Tiles[index2, index1] = true;
    }
    this.Open_Tiles = new HashSet<int>();
    this.refresh_panel_size();
    if (this.Tileset_Image == null)
      return;
    this.refresh_map();
  }

  protected int[,] load_text_map(string filename, bool just_return_data = false)
  {
    try
    {
      Map_Codec_Registry registry = new Map_Codec_Registry();
      Map_Document document = registry.read(filename, null, Map_Format.Text);
      if (!just_return_data)
      {
        this.Text = $"Map Editor - {Path.GetFileNameWithoutExtension(filename)}";
        if (this.load_tileset(document.Tileset))
        {
          using (new Bitmap(this.LoadTilesetDialog.FileName))
            this.load_tileset_image();
        }
      }
      return document.Tiles;
    }
    catch (Exception ex) when (ex is InvalidDataException || ex is FormatException || ex is IOException)
    {
      if (!just_return_data)
        MessageBox.Show(this, ex.Message, "Invalid map", MessageBoxButtons.OK, MessageBoxIcon.Hand);
    }
    return (int[,]) null;
  }

  protected int[,] load_bytewise_map(string filename)
  {
    try
    {
      int width = (int) this.WidthUpDown.Value;
      int height = (int) this.HeightUpDown.Value;
      Map_Codec_Registry registry = new Map_Codec_Registry();
      Map_Read_Options read_options = new Map_Read_Options
      {
        Width = width,
        Height = height,
        Tileset = this.Tileset_Name
      };
      Map_Document document = registry.read(filename, read_options, Map_Format.Mar);
      this.Text = $"Map Editor - {Path.GetFileNameWithoutExtension(filename)}";
      return document.Tiles;
    }
    catch (Exception ex) when (ex is InvalidDataException || ex is FormatException || ex is IOException)
    {
      MessageBox.Show(this, ex.Message, "Invalid map", MessageBoxButtons.OK, MessageBoxIcon.Hand);
    }
    return (int[,]) null;
  }

  private List<int[,]> read_xml_map(string filename, bool just_return_data = false)
  {
    try
    {
      Map_Codec_Registry registry = new Map_Codec_Registry();
      Map_Document document = registry.read(filename, null, Map_Format.Tmx);
      List<int[,]> result = new List<int[,]>();
      result.Add(document.Tiles);
      if (!just_return_data)
      {
        this.Text = $"Map Editor - {Path.GetFileNameWithoutExtension(filename)}";
        string tileset_source = !string.IsNullOrWhiteSpace(document.Tileset_Image_Source)
          ? document.Tileset_Image_Source : document.Tileset;
        if (this.load_tileset(tileset_source))
        {
          using (new Bitmap(this.LoadTilesetDialog.FileName))
            this.load_tileset_image();
        }
      }
      return result;
    }
    catch (Exception ex) when (ex is InvalidDataException || ex is NotSupportedException || ex is FormatException || ex is IOException)
    {
      if (!just_return_data)
        MessageBox.Show(this, ex.Message, "Error loading file", MessageBoxButtons.OK, MessageBoxIcon.Hand);
    }
    return (List<int[,]>) null;
  }

  protected bool load_tileset() => this.load_tileset("");

  protected bool load_tileset(string filename)
  {
    if (!string.IsNullOrEmpty(this.Tileset_Directory))
      this.LoadTilesetDialog.InitialDirectory = this.Tileset_Directory;
    this.LoadTilesetDialog.FileName = filename;
    if (this.LoadTilesetDialog.ShowDialog() != DialogResult.OK)
      return false;
    try
    {
      using (new Bitmap(this.LoadTilesetDialog.FileName))
      {
      }
    }
    catch (ArgumentException)
    {
      int num = (int) MessageBox.Show("Not an image file", "Invalid image", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      return false;
    }
    this.Tileset_Filename = this.LoadTilesetDialog.FileName;
    this.Tileset_Name = Path.GetFileNameWithoutExtension(this.Tileset_Filename);
    this.Tileset_Id = this.Tileset_Data != null ? (this.Tileset_Data.Any<KeyValuePair<int, Data_Tileset>>((Func<KeyValuePair<int, Data_Tileset>, bool>) (data_tileset => data_tileset.Value.Graphic_Name == this.Tileset_Name)) ? this.Tileset_Data.First<KeyValuePair<int, Data_Tileset>>((Func<KeyValuePair<int, Data_Tileset>, bool>) (data_tileset => data_tileset.Value.Graphic_Name == this.Tileset_Name)).Key : -1) : -1;
    this.Tileset_Directory = Path.GetDirectoryName(this.Tileset_Filename);
    this.ZoomUpDown.Enabled = true;
    this.saveMapAsToolStripMenuItem.Enabled = true;
    this.importMapImageToolStripMenuItem.Enabled = true;
    this.importClipboardImageToolStripMenuItem.Enabled = true;
    return true;
  }

  private void refresh_panel_size()
  {
    this.MapPicture.Size = new Size(this.Map_Width * 16 /*0x10*/ * this.Zoom, this.Map_Height * 16 /*0x10*/ * this.Zoom);
  }

  private void undo()
  {
    if (this.Undo_Map_Tiles == null)
      return;
    if (this.Map_Tiles.GetLength(0) != this.Undo_Map_Tiles.GetLength(0) || this.Map_Tiles.GetLength(1) != this.Undo_Map_Tiles.GetLength(1))
    {
      this.reset_metadata(this.Undo_Map_Tiles.GetLength(0), this.Undo_Map_Tiles.GetLength(1));
      for (int index1 = 0; index1 < this.Undo_Map_Tiles.GetLength(1); ++index1)
      {
        for (int index2 = 0; index2 < this.Undo_Map_Tiles.GetLength(0); ++index2)
          this.Drawn_Tiles[index2, index1] = true;
      }
    }
    int[,] undoMapTiles = this.Undo_Map_Tiles;
    this.copy_map_to_undo();
    this.Map_Tiles = new int[undoMapTiles.GetLength(0), undoMapTiles.GetLength(1)];
    Array.Copy((Array) undoMapTiles, (Array) this.Map_Tiles, undoMapTiles.Length);
    this.Map_Width = this.Map_Tiles.GetLength(0);
    this.Map_Height = this.Map_Tiles.GetLength(1);
    this.Changed = true;
  }

  private void copy_map_to_undo(bool changed = true)
  {
    this.Undo_Map_Tiles = new int[this.Map_Tiles.GetLength(0), this.Map_Tiles.GetLength(1)];
    Array.Copy((Array) this.Map_Tiles, (Array) this.Undo_Map_Tiles, this.Map_Tiles.Length);
    this.Changed = changed;
  }

  private void reset_metadata(int width = -1, int height = -1)
  {
    if (width == -1)
      width = this.Map_Width;
    if (height == -1)
      height = this.Map_Height;
    this.Drawn_Tiles = new bool[width, height];
    this.Locked_Tiles = new bool[width, height];
    this.Terrain_Types = new int[width, height];
  }

  private bool ready_to_draw => this.Tileset_Image != null && this.Map_Tiles != null;

  protected void test_mode()
  {
    this.refresh_panel_size();
    this.Map_Tiles = new int[this.Map_Width, this.Map_Height];
    this.copy_map_to_undo();
    this.reset_metadata();
    this.load_tileset_image();
    this.Open_Tiles = new HashSet<int>();
    this.draw_random_tile();
    CancellationToken cancellation_token = this.start_map_operation("Generating map...");
    this.generate_map(1, this.Zoom, cancellation_token, Map_Generation_Algorithm.Legacy);
    this.refresh_panel_size();
    this.MapPicture.Invalidate();
  }

  protected bool load_tileset_image()
  {
    if (this.Tileset_Filename == "")
      return false;
    using (Bitmap bitmap = new Bitmap(this.Tileset_Filename))
    {
      int num1 = bitmap.Width / 16 /*0x10*/ * (bitmap.Height / 16 /*0x10*/);
      if (num1 > (int) short.MaxValue)
      {
        int num2 = (int) MessageBox.Show($"The tileset is too large.\nThe maximum size is {(ValueType) short.MaxValue} tiles.\nThis tileset has {num1}.", "Tileset too large", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        return false;
      }
    }
    if (this.Tileset_Image != null)
    {
      this.Tileset_Image.Dispose();
      this.Tileset_Image = (Bitmap) null;
    }
    this.Tileset_Image = new Bitmap(this.Tileset_Filename);
    this.Tileset_Generator_Data = (Tileset_Generation_Data) null;
    string path = App_Paths.tileset_generation_file(this.Tileset_Filename);
    if (File.Exists(path))
    {
      using (FileStream input = new FileStream(path, FileMode.Open))
      {
        using (BinaryReader reader = new BinaryReader((Stream) input))
          this.Tileset_Generator_Data = Tileset_Generation_Data.read(reader);
      }
    }
    if (this.Tileset_Generator_Data == null)
    {
      this.generateMapToolStripMenuItem.Enabled = false;
      this.repairMapToolStripMenuItem.Enabled = false;
    }
    else
    {
      this.generateMapToolStripMenuItem.Enabled = true;
      this.repairMapToolStripMenuItem.Enabled = true;
    }
    this.Blocked_Terrain_Types.Clear();
    if (this.tileset_data != null)
    {
      this.Blocked_Terrain_Types.UnionWith((IEnumerable<int>) Terrain_Color_Data.TERRAIN_COLORS.Keys);
      foreach (int terrainTag in this.tileset_data.Terrain_Tags)
        this.Blocked_Terrain_Types.Remove(terrainTag);
      this.Blocked_Terrain_Types.Remove(0);
    }
    this.Tile_Matches = (Tile_Matching_Data) null;
    this.prepareTilesetForEditsToolStripMenuItem.Enabled = true;
    this.copyTerrainTagsImageToolStripMenuItem.Enabled = this.tileset_data != null;
    this.importTerrainTagsFromClipboardImageToolStripMenuItem.Enabled = true;
    this.Tileset_Palette.load_tileset(this.Tileset_Filename);
    this.refresh_map();
    return true;
  }

  protected void refresh_map()
  {
    if (this.Map_Tiles == null)
      return;
    this.refresh_panel_size();
    this.MapPicture.Invalidate();
  }

  protected void test_map_reader(string tileset_name) => throw new Exception();

  protected bool setup_tileset(string tileset_name, string maps_folder)
  {
    this.Maps = this.load_maps_from_dir(maps_folder);
    if (this.Maps.Count <= 0)
      return false;
    this.progressBar1.Visible = true;
    this.setup_tileset_data(tileset_name, this.Maps);
    this.generateMapToolStripMenuItem.Enabled = true;
    this.repairMapToolStripMenuItem.Enabled = true;
    return true;
  }

  private Dictionary<string, int[,]> load_maps_from_dir(string maps_folder)
  {
    Dictionary<string, int[,]> dictionary = new Dictionary<string, int[,]>();
    foreach (string file in Directory.GetFiles(maps_folder, "*.map"))
    {
      int[,] numArray = this.load_text_map(file, true);
      if (numArray != null)
        dictionary.Add(Path.GetFileNameWithoutExtension(file), numArray);
    }
    return dictionary;
  }

  protected void setup_tileset_data(string tileset_name, Dictionary<string, int[,]> maps)
  {
    this.Tileset_Name = tileset_name;
    this.Tileset_Id = this.Tileset_Data != null ? (this.Tileset_Data.Any<KeyValuePair<int, Data_Tileset>>((Func<KeyValuePair<int, Data_Tileset>, bool>) (data_tileset => data_tileset.Value.Graphic_Name == this.Tileset_Name)) ? this.Tileset_Data.First<KeyValuePair<int, Data_Tileset>>((Func<KeyValuePair<int, Data_Tileset>, bool>) (data_tileset => data_tileset.Value.Graphic_Name == this.Tileset_Name)).Key : -1) : -1;
    this.Tileset_Generator_Data = new Tileset_Generation_Data(this.Tileset_Image.Width / 16 /*0x10*/ * (this.Tileset_Image.Height / 16 /*0x10*/), this.Tile_Matches);
    List<string> list = maps.Keys.ToList<string>();
    for (int index1 = 0; index1 < maps.Count; ++index1)
    {
      int[,] map = maps[list[index1]];
      int length1 = map.GetLength(0);
      int length2 = map.GetLength(1);
      for (int index2 = 0; index2 < map.Length; ++index2)
      {
        int x = index2 % map.GetLength(0);
        int y = index2 / map.GetLength(0);
        short identicalTile = (short) map[x, y];
        if (this.Tileset_Generator_Data.identical_tiles.ContainsKey(identicalTile))
          identicalTile = this.Tileset_Generator_Data.identical_tiles[identicalTile];
        if (!this.tileset_config_data.ContainsKey((int) identicalTile))
          this.tileset_config_data.Add((int) identicalTile, new Tile_Data());
        for (byte index3 = 2; index3 <= (byte) 8; index3 += (byte) 2)
        {
          short num = (short) this.surrounding_tile(x, y, (int) index3, length1, length2, map);
          if (num != (short) -1)
          {
            if (this.Tileset_Generator_Data.identical_tiles.ContainsKey(num))
              num = this.Tileset_Generator_Data.identical_tiles[num];
            if (!this.tileset_config_data[(int) identicalTile].Valid_Tile_Priority[index3].ContainsKey(num))
            {
              List<short> shortList = this.same_side(num, (byte) (10U - (uint) index3));
              foreach (short key1 in this.same_side(identicalTile, index3))
              {
                if (!this.tileset_config_data.ContainsKey((int) key1))
                  this.tileset_config_data.Add((int) key1, new Tile_Data());
                foreach (short key2 in shortList)
                {
                  if (!this.tileset_config_data[(int) key1].Valid_Tile_Priority[index3].ContainsKey(key2))
                    this.tileset_config_data[(int) key1].Valid_Tile_Priority[index3][key2] = (short) 1;
                }
              }
            }
            this.tileset_config_data[(int) identicalTile].increment_valid_tile_priority(index3, num);
          }
        }
        this.progressBar1.Value = this.progressBar1.Maximum * (index1 * map.Length + index2 + 1) / (maps.Count * map.Length);
      }
    }
  }

  protected void update_tileset_same_corners()
  {
    this.progressBar1.Visible = true;
    HashSet<Tile_Directions> tileDirectionsSet = new HashSet<Tile_Directions>()
    {
      Tile_Directions.SW,
      Tile_Directions.SE,
      Tile_Directions.NW,
      Tile_Directions.NE
    };
    this.Tile_Matches = new Tile_Matching_Data(tileDirectionsSet);
    Dictionary<Tile_Directions, int> dictionary1 = tileDirectionsSet.ToDictionary<Tile_Directions, Tile_Directions, int>((Func<Tile_Directions, Tile_Directions>) (p => p), (Func<Tile_Directions, int>) (p => 0));
    using (Bitmap render = new Bitmap((Image) this.Tileset_Image))
    {
      BitmapData bitmapdata1 = this.Tileset_Image.LockBits(new Rectangle(0, 0, this.Tileset_Image.Width, this.Tileset_Image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
      BitmapData bitmapdata2 = render.LockBits(new Rectangle(0, 0, render.Width, render.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
      int stride1 = bitmapdata1.Stride;
      int stride2 = bitmapdata2.Stride;
      IntPtr scan0_1 = bitmapdata1.Scan0;
      IntPtr scan0_2 = bitmapdata2.Scan0;
      int num1 = this.Tileset_Image.Width / 16 /*0x10*/;
      for (short index1 = 1; (int) index1 < this.Tileset_Image.Width / 16 /*0x10*/ * (this.Tileset_Image.Height / 16 /*0x10*/); ++index1)
      {
        int num2 = (int) index1 % num1;
        int num3 = (int) index1 / num1;
        foreach (Tile_Directions dir in tileDirectionsSet)
        {
          if (!this.Tile_Matches.has_index(dir, (int) index1))
          {
            int num5 = dir == Tile_Directions.SE || dir == Tile_Directions.NE ? 8 : 0;
            int num6 = dir == Tile_Directions.SW || dir == Tile_Directions.SE ? 8 : 0;
            int width = 8;
            int height = 8;
            List<short> same = new List<short>();
            for (short index2 = (short) ((int) index1 + 1); (int) index2 < this.Tileset_Image.Width / 16 /*0x10*/ * (this.Tileset_Image.Height / 16 /*0x10*/); ++index2)
            {
              if (!this.Tile_Matches.has_index(dir, (int) index2))
              {
                int num7 = (int) index2 % (this.Tileset_Image.Width / 16 /*0x10*/) * 16 /*0x10*/;
                int num8 = (int) index2 / (this.Tileset_Image.Width / 16 /*0x10*/) * 16 /*0x10*/;
                if (this.compare_pixels(this.Tileset_Image, render, new Rectangle(num7 + num5, num8 + num6, width, height), new Point(num2 * 16 /*0x10*/ + num5, num3 * 16 /*0x10*/ + num6), stride1, stride2, scan0_1, scan0_2) == 0)
                  same.Add(index2);
              }
            }
            if (same.Count > 0)
            {
              same.Insert(0, index1);
              this.Tile_Matches.add(dir, same);
              Dictionary<Tile_Directions, int> dictionary2;
              Tile_Directions key;
              (dictionary2 = dictionary1)[key = dir] = dictionary2[key] + 1;
            }
          }
        }
        this.progressBar1.Value = this.progressBar1.Maximum * ((int) index1 + 1) / (this.Tileset_Image.Width / 16 /*0x10*/ * (this.Tileset_Image.Height / 16 /*0x10*/));
      }
      this.Tileset_Image.UnlockBits(bitmapdata1);
      render.UnlockBits(bitmapdata2);
    }
    this.Tile_Matches.refresh_identical(this.Tileset_Image.Width / 16 /*0x10*/ * (this.Tileset_Image.Height / 16 /*0x10*/));
  }

  protected void draw_random_tile()
  {
    if (this.tileset_config_data.Count == 0)
      return;
    int num = 0;
    int index;
    int x;
    int y;
    do
    {
      ++num;
      if (num >= 10000)
        return;
      x = FE_Map_Creator_Form.rand.Next(this.Map_Width);
      y = FE_Map_Creator_Form.rand.Next(this.Map_Height);
      index = -1;
      if (this.Terrain_Types[x, y] != 0 && this.tileset_data != null)
      {
        List<int> intList = this.Terrain_Types[x, y] <= 0 ? this.tileset_config_data.Keys.Where<int>((Func<int, bool>) (tile => this.tileset_data.Terrain_Tags.Count > tile && this.tileset_data.Terrain_Tags[tile] != -this.Terrain_Types[x, y])).ToList<int>() : this.tileset_config_data.Keys.Where<int>((Func<int, bool>) (tile => this.tileset_data.Terrain_Tags.Count > tile && this.tileset_data.Terrain_Tags[tile] == this.Terrain_Types[x, y])).ToList<int>();
        if (intList.Count > 0)
          index = intList[FE_Map_Creator_Form.rand.Next(intList.Count)];
      }
      else
        index = this.tileset_config_data.Keys.ToList<int>()[FE_Map_Creator_Form.rand.Next(this.tileset_config_data.Count)];
    }
    while (index <= -1);
    this.draw_tile(x, y, index);
  }

  protected void draw_tile(int x, int y, int index)
  {
    this.Drawn_Tiles[x, y] = true;
    this.Map_Tiles[x, y] = index;
    if (!this.is_open_tile(x, y))
      return;
    this.Open_Tiles.Add(x + y * this.Map_Width);
  }

  protected void draw_tile(int x, int y, int index, Graphics g, Bitmap tileset, int zoom = 1)
  {
    Rectangle destRect = new Rectangle(new Point(x * 16 /*0x10*/ * zoom, y * 16 /*0x10*/ * zoom), new Size(16 /*0x10*/ * zoom, 16 /*0x10*/ * zoom));
    g.DrawImage((Image) tileset, destRect, new Rectangle(new Point(index % (tileset.Width / 16 /*0x10*/) * 16 /*0x10*/, index / (tileset.Width / 16 /*0x10*/) * 16 /*0x10*/), new Size(16 /*0x10*/, 16 /*0x10*/)), GraphicsUnit.Pixel);
  }

  protected void draw_map(Graphics g, int zoom = 1, int[,] map_tiles = null, Bitmap tileset = null)
  {
    if (map_tiles == null)
      map_tiles = this.Map_Tiles;
    if (tileset == null)
      tileset = this.Tileset_Image;
    int num1 = Math.Max(0, Math.Min(map_tiles.GetLength(0) - 1, (int) ((double) g.ClipBounds.Left / (double) zoom - 2.0) / 16 /*0x10*/));
    int num2 = Math.Max(0, Math.Min(map_tiles.GetLength(0) - 1, (int) ((double) g.ClipBounds.Right / (double) zoom) / 16 /*0x10*/));
    int num3 = Math.Max(0, Math.Min(map_tiles.GetLength(1) - 1, (int) ((double) g.ClipBounds.Top / (double) zoom - 2.0) / 16 /*0x10*/));
    int num4 = Math.Max(0, Math.Min(map_tiles.GetLength(1) - 1, (int) ((double) g.ClipBounds.Bottom / (double) zoom) / 16 /*0x10*/));
    for (int x = num1; x <= num2; ++x)
    {
      for (int y = num3; y <= num4; ++y)
        this.draw_tile(x, y, map_tiles[x, y], g, tileset, zoom);
    }
  }

  public static List<Point> bresenham(int x0, int y0, int x1, int y1)
  {
    List<Point> pointList = new List<Point>();
    if (y0 == y1)
    {
      if (x0 > x1)
        Additional_Math.swap(ref x0, ref x1);
      for (int x = x0; x < x1; ++x)
        pointList.Add(new Point(x, y0));
      pointList.Add(new Point(x1, y1));
    }
    else if (x0 == x1)
    {
      if (y0 > y1)
        Additional_Math.swap(ref y0, ref y1);
      for (int y = y0; y < y1; ++y)
        pointList.Add(new Point(x0, y));
      pointList.Add(new Point(x1, y1));
    }
    else
    {
      bool flag = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
      if (flag)
      {
        Additional_Math.swap(ref x0, ref y0);
        Additional_Math.swap(ref x1, ref y1);
      }
      if (x0 > x1)
      {
        Additional_Math.swap(ref x0, ref x1);
        Additional_Math.swap(ref y0, ref y1);
      }
      int num1 = x1 - x0;
      int num2 = Math.Abs(y1 - y0);
      int num3 = num1 / 2;
      int num4 = y0 < y1 ? 1 : -1;
      int num5 = y0;
      for (int index = x0; index <= x1; ++index)
      {
        pointList.Add(flag ? new Point(num5, index) : new Point(index, num5));
        num3 -= num2;
        if (num3 < 0)
        {
          num5 += num4;
          num3 += num1;
        }
      }
    }
    return pointList;
  }

  private void invoke_on_ui(Action action)
  {
    try
    {
      if (this.IsDisposed || !this.IsHandleCreated)
        return;
      if (this.InvokeRequired)
        this.BeginInvoke(action);
      else
        action();
    }
    catch (ObjectDisposedException) { }
    catch (InvalidOperationException) { }
  }

  private CancellationToken start_map_operation(string status)
  {
    this.Map_Generation_Cancellation = new CancellationTokenSource();
    this.Updating = true;
    this.editToolStripMenuItem.Enabled = false;
    this.mapGenerationToolStripMenuItem.Enabled = false;
    this.NewMapButton.Enabled = false;
    this.CancelOperationButton.Enabled = true;
    this.StatusbarSpacerLabel.Text = status;
    return this.Map_Generation_Cancellation.Token;
  }

  private void finish_map_operation(CancellationToken cancellation_token, string status)
  {
    if (this.Map_Generation_Cancellation == null || this.Map_Generation_Cancellation.Token != cancellation_token)
      return;
    CancellationTokenSource cancellation = this.Map_Generation_Cancellation;
    this.Map_Generation_Cancellation = null;
    this.Updating = false;
    this.editToolStripMenuItem.Enabled = true;
    this.mapGenerationToolStripMenuItem.Enabled = true;
    this.NewMapButton.Enabled = true;
    this.CancelOperationButton.Enabled = false;
    this.StatusbarSpacerLabel.Text = status;
    cancellation.Dispose();
  }

  private void show_map_generation_result(string operation, Map_Generation_Result result)
  {
    if (result.Unresolved_Tile_Count <= 0)
      return;
    string experimental = result.Algorithm switch
    {
      Map_Generation_Algorithm.Experimental_Constraint => result.Search_Budget_Exhausted
        ? "\r\nExperimental constraint solver returned the best result found within its search budget."
        : "\r\nExperimental constraint solver minimized the unresolved count.",
      Map_Generation_Algorithm.Experimental_Hybrid =>
        result.Hybrid_Halo >= 0
          ? $"\r\nHybrid solver reduced the legacy result from " +
            $"{result.Hybrid_Legacy_Unresolved_Tile_Count} unresolved tile(s) " +
            $"using halo {result.Hybrid_Halo}."
          : "\r\nHybrid regional attempts did not improve the legacy result.",
      _ => "",
    };
    if (result.Search_Budget_Exhausted)
    {
      experimental +=
        $"\r\nSearch budget exhausted after {result.Search_Node_Count} node(s); " +
        "the unresolved count may not be optimal.";
    }
    MessageBox.Show(this, $"{operation} completed with {result.Unresolved_Tile_Count} unresolved tiles.\r\nSeed: {result.Seed}.{experimental}", $"{operation} Incomplete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
  }

  protected void repair_map(int depth, int radius, int zoom, CancellationToken cancellation_token)
  {
    this.repair_map(depth, radius, zoom, cancellation_token, Map_Generation_Algorithm.Legacy);
  }

  protected void repair_map(
    int depth,
    int radius,
    int zoom,
    CancellationToken cancellation_token,
    Map_Generation_Algorithm algorithm)
  {
    Map_Generation_Result result = null;
    Exception operation_error = null;
    string status = "Map repair failed.";
    try
    {
      Map_State state = new Map_State(this.Map_Tiles, this.Drawn_Tiles, this.Locked_Tiles, this.Terrain_Types);
      Map_Generation_Engine engine = new Map_Generation_Engine(this.Tileset_Generator_Data, this.tileset_data);
      Map_Repair_Options options = new Map_Repair_Options { Algorithm = algorithm, Depth = depth, Radius = radius };
      Tile_Drawn_Callback callback = (int x, int y, int tile_index) =>
      {
        Rectangle rect = new Rectangle(x * 16 /*0x10*/ * zoom, y * 16 /*0x10*/ * zoom, 16 /*0x10*/ * zoom, 16 /*0x10*/ * zoom);
        this.invoke_on_ui(() => this.MapPicture.Invalidate(rect));
      };
      this.invoke_on_ui(() =>
      {
        this.refresh_panel_size();
        this.MapPicture.Invalidate();
      });
      Thread.Sleep(25);
      cancellation_token.ThrowIfCancellationRequested();
      result = engine.repair(state, options, cancellation_token, callback);
      status = $"Map repair complete{algorithm_status_suffix(result.Algorithm)}. Seed: {result.Seed}.";
    }
    catch (OperationCanceledException) when (cancellation_token.IsCancellationRequested)
    {
      status = "Map repair cancelled.";
    }
    catch (ArgumentException ex)
    {
      operation_error = ex;
    }
    catch (InvalidOperationException ex)
    {
      operation_error = ex;
    }
    catch (KeyNotFoundException ex)
    {
      operation_error = ex;
    }
    catch (IndexOutOfRangeException ex)
    {
      operation_error = ex;
    }
    finally
    {
      this.invoke_on_ui(() =>
      {
        this.MapPicture.Invalidate();
        this.finish_map_operation(cancellation_token, status);
        if (operation_error != null)
          MessageBox.Show(this, operation_error.Message, "Map Repair Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else if (result != null)
          this.show_map_generation_result("Map Repair", result);
      });
    }
  }

  protected void generate_map(int zoom, CancellationToken cancellation_token)
  {
    this.generate_map(1, zoom, cancellation_token, Map_Generation_Algorithm.Legacy);
  }

  protected void generate_map(
    int depth,
    int zoom,
    CancellationToken cancellation_token,
    Map_Generation_Algorithm algorithm)
  {
    Map_Generation_Result result = null;
    Exception operation_error = null;
    string status = "Map generation failed.";
    try
    {
      Map_State state = new Map_State(this.Map_Tiles, this.Drawn_Tiles, this.Locked_Tiles, this.Terrain_Types);
      Map_Generation_Engine engine = new Map_Generation_Engine(this.Tileset_Generator_Data, this.tileset_data);
      Map_Generation_Options options = new Map_Generation_Options { Algorithm = algorithm, Depth = depth };
      Tile_Drawn_Callback callback = (int x, int y, int tile_index) =>
      {
        Rectangle rect = new Rectangle(x * 16 /*0x10*/ * zoom, y * 16 /*0x10*/ * zoom, 16 /*0x10*/ * zoom, 16 /*0x10*/ * zoom);
        this.invoke_on_ui(() => this.MapPicture.Invalidate(rect));
      };
      this.invoke_on_ui(() =>
      {
        this.refresh_panel_size();
        this.MapPicture.Invalidate();
      });
      cancellation_token.ThrowIfCancellationRequested();
      result = engine.generate(state, options, cancellation_token, callback);
      status = $"Map generation complete{algorithm_status_suffix(result.Algorithm)}. Seed: {result.Seed}.";
    }

    catch (OperationCanceledException) when (cancellation_token.IsCancellationRequested)
    {
      status = "Map generation cancelled.";
    }
    catch (ArgumentException ex)
    {
      operation_error = ex;
    }
    catch (InvalidOperationException ex)
    {
      operation_error = ex;
    }
    catch (KeyNotFoundException ex)
    {
      operation_error = ex;
    }
    catch (IndexOutOfRangeException ex)
    {
      operation_error = ex;
    }
    finally
    {
      this.invoke_on_ui(() =>
      {
        this.MapPicture.Invalidate();
        this.finish_map_operation(cancellation_token, status);
        if (operation_error != null)
          MessageBox.Show(this, operation_error.Message, "Map Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else if (result != null)
          this.show_map_generation_result("Map Generation", result);
      });
    }
  }

  private static string algorithm_status_suffix(Map_Generation_Algorithm algorithm)
  {
    return algorithm switch
    {
      Map_Generation_Algorithm.Experimental_Constraint => " (experimental)",
      Map_Generation_Algorithm.Experimental_Hybrid => " (hybrid)",
      _ => "",
    };
  }

  private Map_State create_experimental_generation_state(int width, int height)
  {
    int[,] tiles = new int[width, height];
    bool[,] drawn = new bool[width, height];
    bool[,] locked = this.Locked_Tiles != null
      && this.Locked_Tiles.GetLength(0) == width
      && this.Locked_Tiles.GetLength(1) == height
      ? (bool[,]) this.Locked_Tiles.Clone()
      : new bool[width, height];
    int[,] terrain = this.Terrain_Types != null
      && this.Terrain_Types.GetLength(0) == width
      && this.Terrain_Types.GetLength(1) == height
      ? (int[,]) this.Terrain_Types.Clone()
      : new int[width, height];

    if (this.Map_Tiles != null
      && this.Map_Tiles.GetLength(0) == width
      && this.Map_Tiles.GetLength(1) == height)
    {
      for (int y = 0; y < height; ++y)
      {
        for (int x = 0; x < width; ++x)
        {
          if (!locked[x, y])
            continue;
          tiles[x, y] = this.Map_Tiles[x, y];
          drawn[x, y] = true;
        }
      }
    }

    return new Map_State(tiles, drawn, locked, terrain);
  }

  private Map_State create_experimental_repair_state()
  {
    Map_State state = new Map_State(
      (int[,]) this.Map_Tiles.Clone(),
      (bool[,]) this.Drawn_Tiles.Clone(),
      (bool[,]) this.Locked_Tiles.Clone(),
      (int[,]) this.Terrain_Types.Clone());
    for (int y = 0; y < state.Height; ++y)
    {
      for (int x = 0; x < state.Width; ++x)
      {
        if (state.Tiles[x, y] == 0)
          state.Drawn[x, y] = false;
      }
    }
    return state;
  }

  private Map_State create_hybrid_repair_state()
  {
    return new Map_State(
      (int[,]) this.Map_Tiles.Clone(),
      (bool[,]) this.Drawn_Tiles.Clone(),
      (bool[,]) this.Locked_Tiles.Clone(),
      (int[,]) this.Terrain_Types.Clone());
  }

  private void generate_map_experimental(
    Map_State state,
    int depth,
    int zoom,
    CancellationToken cancellation_token,
    Map_Generation_Algorithm algorithm,
    bool enable_branch_arc_consistency)
  {
    Map_Generation_Result result = null;
    Exception operation_error = null;
    string status = "Map generation failed.";
    try
    {
      Map_Generation_Engine engine = new Map_Generation_Engine(this.Tileset_Generator_Data, this.tileset_data);
      result = engine.generate(
        state,
        new Map_Generation_Options()
        {
          Algorithm = algorithm,
          Depth = depth,
          Experimental_Enable_Branch_Arc_Consistency = enable_branch_arc_consistency
        },
        cancellation_token);
      status = $"Map generation complete{algorithm_status_suffix(result.Algorithm)}. Seed: {result.Seed}.";
    }
    catch (OperationCanceledException) when (cancellation_token.IsCancellationRequested)
    {
      status = "Map generation cancelled.";
    }
    catch (ArgumentException ex)
    {
      operation_error = ex;
    }
    catch (InvalidOperationException ex)
    {
      operation_error = ex;
    }
    catch (KeyNotFoundException ex)
    {
      operation_error = ex;
    }
    catch (IndexOutOfRangeException ex)
    {
      operation_error = ex;
    }
    finally
    {
      this.invoke_on_ui(() =>
      {
        if (operation_error == null && result != null)
        {
          if (this.Map_Tiles == null)
            this.Undo_Map_Tiles = new int[state.Width, state.Height];
          else
            this.copy_map_to_undo();
          this.Text = "Map Editor - New Map";
          this.WidthUpDown.Value = (Decimal) (this.Map_Width = state.Width);
          this.HeightUpDown.Value = (Decimal) (this.Map_Height = state.Height);
          this.Map_Tiles = state.Tiles;
          this.Drawn_Tiles = state.Drawn;
          this.Locked_Tiles = state.Locked;
          this.Terrain_Types = state.Terrain;
          this.Open_Tiles = new HashSet<int>();
          this.Changed = true;
          this.refresh_panel_size();
        }
        this.MapPicture.Invalidate();
        this.finish_map_operation(cancellation_token, status);
        if (operation_error != null)
          MessageBox.Show(this, operation_error.Message, "Map Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else if (result != null)
          this.show_map_generation_result("Map Generation", result);
      });
    }
  }

  private void repair_map_experimental(
    Map_State state,
    int depth,
    int radius,
    int zoom,
    CancellationToken cancellation_token,
    Map_Generation_Algorithm algorithm,
    bool enable_branch_arc_consistency)
  {
    Map_Generation_Result result = null;
    Exception operation_error = null;
    string status = "Map repair failed.";
    try
    {
      Map_Generation_Engine engine = new Map_Generation_Engine(this.Tileset_Generator_Data, this.tileset_data);
      result = engine.repair(
        state,
        new Map_Repair_Options()
        {
          Algorithm = algorithm,
          Depth = depth,
          Radius = radius,
          Experimental_Enable_Branch_Arc_Consistency = enable_branch_arc_consistency
        },
        cancellation_token);
      status = $"Map repair complete{algorithm_status_suffix(result.Algorithm)}. Seed: {result.Seed}.";
    }
    catch (OperationCanceledException) when (cancellation_token.IsCancellationRequested)
    {
      status = "Map repair cancelled.";
    }
    catch (ArgumentException ex)
    {
      operation_error = ex;
    }
    catch (InvalidOperationException ex)
    {
      operation_error = ex;
    }
    catch (KeyNotFoundException ex)
    {
      operation_error = ex;
    }
    catch (IndexOutOfRangeException ex)
    {
      operation_error = ex;
    }
    finally
    {
      this.invoke_on_ui(() =>
      {
        if (operation_error == null && result != null)
        {
          this.copy_map_to_undo();
          this.Map_Tiles = state.Tiles;
          this.Drawn_Tiles = state.Drawn;
          this.Locked_Tiles = state.Locked;
          this.Terrain_Types = state.Terrain;
          this.Changed = true;
          this.refresh_panel_size();
        }
        this.MapPicture.Invalidate();
        this.finish_map_operation(cancellation_token, status);
        if (operation_error != null)
          MessageBox.Show(this, operation_error.Message, "Map Repair Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        else if (result != null)
          this.show_map_generation_result("Map Repair", result);
      });
    }
  }

  private void invalidate_map(HashSet<Rectangle> rects)
  {
    if (this.MapPicture.InvokeRequired)
    {
      this.BeginInvoke((Delegate) new FE_Map_Creator_Form.SetRectHashCallback(this.invalidate_map), (object) rects);
    }
    else
    {
      using (Region region = new Region())
      {
        foreach (Rectangle rect in rects)
          region.Union(rect);
        this.MapPicture.Invalidate(region);
      }
    }
  }

  protected void resize_map(int x1, int y1, int x2, int y2)
  {
    int length1 = (int) Math.Max(Math.Min((Decimal) (this.Map_Height + y1 + y2), this.HeightUpDown.Maximum), this.HeightUpDown.Minimum);
    int length2 = (int) Math.Max(Math.Min((Decimal) (this.Map_Width + x1 + x2), this.WidthUpDown.Maximum), this.WidthUpDown.Minimum);
    if (length2 < 1 || length1 < 1)
      return;
    int[,] numArray1 = new int[length2, length1];
    bool[,] flagArray = new bool[length2, length1];
    int[,] numArray2 = new int[length2, length1];
    for (int index1 = 0; index1 < length1; ++index1)
    {
      for (int index2 = 0; index2 < length2; ++index2)
      {
        if (index2 - x1 >= 0 && index2 - x1 < this.Map_Width && index1 - y1 >= 0 && index1 - y1 < this.Map_Height)
        {
          numArray1[index2, index1] = this.Map_Tiles[index2 - x1, index1 - y1];
          flagArray[index2, index1] = this.Locked_Tiles[index2 - x1, index1 - y1];
          numArray2[index2, index1] = this.Terrain_Types[index2 - x1, index1 - y1];
        }
      }
    }
    this.WidthUpDown.Value = (Decimal) (this.Map_Width = length2);
    this.HeightUpDown.Value = (Decimal) (this.Map_Height = length1);
    this.copy_map_to_undo();
    this.Map_Tiles = numArray1;
    this.Locked_Tiles = flagArray;
    this.Terrain_Types = numArray2;
    this.refresh_panel_size();
    this.Drawn_Tiles = new bool[this.Map_Width, this.Map_Height];
    for (int index3 = 0; index3 < length1; ++index3)
    {
      for (int index4 = 0; index4 < length2; ++index4)
        this.Drawn_Tiles[index4, index3] = true;
    }
    this.Open_Tiles = new HashSet<int>();
    this.refresh_panel_size();
    if (this.Tileset_Image == null)
      return;
    this.refresh_map();
  }

  protected bool is_open_tile(int x, int y) => this.open_dirs(x, y).Count > 0;

  protected List<byte> open_dirs(int x, int y)
  {
    List<byte> byteList = new List<byte>();
    if (y + 1 < this.Map_Height && !this.Drawn_Tiles[x, y + 1] && !this.Locked_Tiles[x, y + 1])
      byteList.Add((byte) 2);
    if (x - 1 >= 0 && !this.Drawn_Tiles[x - 1, y] && !this.Locked_Tiles[x - 1, y])
      byteList.Add((byte) 4);
    if (x + 1 < this.Map_Width && !this.Drawn_Tiles[x + 1, y] && !this.Locked_Tiles[x + 1, y])
      byteList.Add((byte) 6);
    if (y - 1 >= 0 && !this.Drawn_Tiles[x, y - 1] && !this.Locked_Tiles[x, y - 1])
      byteList.Add((byte) 8);
    return byteList;
  }

  protected List<short> test_valid_tiles(int base_x, int base_y, int depth)
  {
    int[,] numArray1 = new int[this.Map_Width, this.Map_Height];
    Array.Copy((Array) this.Map_Tiles, (Array) numArray1, this.Map_Tiles.Length);
    List<Point> locs = new List<Point>();
    for (int index1 = 0; index1 <= depth; ++index1)
    {
      for (int index2 = -index1; index2 <= index1; ++index2)
      {
        for (int index3 = Math.Abs(index2) - index1; index3 <= index1 - Math.Abs(index2); ++index3)
        {
          if (Math.Abs(index3) + Math.Abs(index2) == index1 && !this.is_off_map(base_x + index3, base_y + index2) && (!this.Drawn_Tiles[base_x + index3, base_y + index2] || this.Terrain_Types[base_x + index3, base_y + index2] != 0 && this.tileset_data != null && this.Map_Tiles[base_x + index3, base_y + index2] == 0))
            locs.Add(new Point(base_x + index3, base_y + index2));
        }
      }
    }
    int[] numArray2 = new int[locs.Count];
    int index4 = 0;
    List<short>[] tiles = new List<short>[numArray2.Length];
    int[] numArray3 = new int[numArray2.Length];
    tiles[index4] = new List<short>((IEnumerable<short>) this.valid_tiles(locs[index4].X, locs[index4].Y, numArray1));
    if (tiles[index4].Count == 0 || index4 + 1 >= numArray2.Length || tiles[index4].Count == 1 && tiles[index4].First<short>() == (short) 0)
      return tiles[index4];
    numArray3[index4] = 0;
    this.Drawn_Tiles[locs[index4].X, locs[index4].Y] = true;
    numArray1[locs[index4].X, locs[index4].Y] = (int) tiles[index4][numArray3[index4]];
    while (index4 != -1)
    {
      if (this.valid_surrounding_tiles(index4, numArray2.Length, numArray1, locs, ref tiles))
      {
        if (index4 + 2 == numArray2.Length)
        {
          for (int index5 = index4; index5 > 0; --index5)
          {
            this.Drawn_Tiles[locs[index5].X, locs[index5].Y] = false;
            numArray1[locs[index5].X, locs[index5].Y] = 0;
          }
          index4 = 0;
          ++numArray3[index4];
          if (numArray3[index4] < tiles[index4].Count)
            numArray1[locs[index4].X, locs[index4].Y] = (int) tiles[index4][numArray3[index4]];
          else
            break;
        }
        else
        {
          ++index4;
          numArray3[index4] = 0;
          this.Drawn_Tiles[locs[index4].X, locs[index4].Y] = true;
          numArray1[locs[index4].X, locs[index4].Y] = (int) tiles[index4][numArray3[index4]];
        }
      }
      else
      {
        do
        {
          ++numArray3[index4];
          if (index4 == 0)
          {
            --numArray3[index4];
            tiles[index4].RemoveAt(numArray3[index4]);
          }
          if (numArray3[index4] >= tiles[index4].Count)
          {
            this.Drawn_Tiles[locs[index4].X, locs[index4].Y] = false;
            numArray1[locs[index4].X, locs[index4].Y] = 0;
            --index4;
          }
          else
            goto label_27;
        }
        while (index4 != -1);
        continue;
label_27:
        numArray1[locs[index4].X, locs[index4].Y] = (int) tiles[index4][numArray3[index4]];
      }
    }
    return tiles[0];
  }

  protected HashSet<short> valid_tiles(int x, int y, int[,] map_tiles)
  {
    if (map_tiles[x, y] == 0 && this.Terrain_Types[x, y] != 0 && this.Drawn_Tiles[x, y] && this.tileset_data != null)
      return this.Terrain_Types[x, y] > 0 ? new HashSet<short>(Enumerable.Range(0, this.tileset_data.Terrain_Tags.Count).Select<int, short>((Func<int, short>) (i => (short) i)).Where<short>((Func<short, bool>) (tile => this.tileset_data.Terrain_Tags[(int) tile] == this.Terrain_Types[x, y]))) : new HashSet<short>(Enumerable.Range(0, this.tileset_data.Terrain_Tags.Count).Select<int, short>((Func<int, short>) (i => (short) i)).Where<short>((Func<short, bool>) (tile => this.tileset_data.Terrain_Tags[(int) tile] != -this.Terrain_Types[x, y])));
    IEnumerable<byte> source1 = Enumerable.Range(0, 4).Select<int, byte>((Func<int, byte>) (dir => (byte) ((dir + 1) * 2))).Where<byte>((Func<byte, bool>) (dir =>
    {
      if (x + FE_Map_Creator_Form.REVERSE_DIRS[dir].Width < 0 || x + FE_Map_Creator_Form.REVERSE_DIRS[dir].Width >= this.Map_Width || y + FE_Map_Creator_Form.REVERSE_DIRS[dir].Height < 0 || y + FE_Map_Creator_Form.REVERSE_DIRS[dir].Height >= this.Map_Height || !this.Drawn_Tiles[x + FE_Map_Creator_Form.REVERSE_DIRS[dir].Width, y + FE_Map_Creator_Form.REVERSE_DIRS[dir].Height])
        return false;
      short key = (short) map_tiles[x + FE_Map_Creator_Form.REVERSE_DIRS[dir].Width, y + FE_Map_Creator_Form.REVERSE_DIRS[dir].Height];
      if (this.Tileset_Generator_Data.identical_tiles.ContainsKey(key))
        key = this.Tileset_Generator_Data.identical_tiles[key];
      return this.tileset_config_data.ContainsKey((int) key);
    }));
    if (source1.Count<byte>() == 0)
      return new HashSet<short>() { (short) 0 };
    byte key1 = source1.First<byte>();
    short key2 = (short) map_tiles[x + FE_Map_Creator_Form.REVERSE_DIRS[key1].Width, y + FE_Map_Creator_Form.REVERSE_DIRS[key1].Height];
    if (this.Tileset_Generator_Data.identical_tiles.ContainsKey(key2))
      key2 = this.Tileset_Generator_Data.identical_tiles[key2];
    HashSet<short> source2 = new HashSet<short>((IEnumerable<short>) this.tileset_config_data[(int) key2].Valid_Tile_Priority[key1].Keys);
    foreach (byte key3 in source1)
    {
      if ((int) key3 != (int) key1)
      {
        short key4 = (short) map_tiles[x + FE_Map_Creator_Form.REVERSE_DIRS[key3].Width, y + FE_Map_Creator_Form.REVERSE_DIRS[key3].Height];
        if (this.Tileset_Generator_Data.identical_tiles.ContainsKey(key4))
          key4 = this.Tileset_Generator_Data.identical_tiles[key4];
        source2.IntersectWith((IEnumerable<short>) this.tileset_config_data[(int) key4].Valid_Tile_Priority[key3].Keys);
      }
    }
    if (source2.Count > 0 && this.Terrain_Types[x, y] != 0 && this.tileset_data != null)
      source2 = this.Terrain_Types[x, y] <= 0 ? new HashSet<short>(source2.Where<short>((Func<short, bool>) (tile => this.tileset_data.Terrain_Tags.Count > (int) tile && this.tileset_data.Terrain_Tags[(int) tile] != -this.Terrain_Types[x, y]))) : new HashSet<short>(source2.Where<short>((Func<short, bool>) (tile => this.tileset_data.Terrain_Tags.Count > (int) tile && this.tileset_data.Terrain_Tags[(int) tile] == this.Terrain_Types[x, y])));
    return source2;
  }

  protected int surrounding_tile(int x, int y, int dir, int width = 0, int height = 0, int[,] map_tiles = null)
  {
    if (width == 0)
      width = this.Map_Width;
    if (height == 0)
      height = this.Map_Height;
    if (map_tiles == null)
      map_tiles = this.Map_Tiles;
    switch (dir)
    {
      case 2:
        if (y + 1 < height)
          return map_tiles[x, y + 1];
        break;
      case 4:
        if (x - 1 >= 0)
          return map_tiles[x - 1, y];
        break;
      case 6:
        if (x + 1 < width)
          return map_tiles[x + 1, y];
        break;
      case 8:
        if (y - 1 >= 0)
          return map_tiles[x, y - 1];
        break;
    }
    return -1;
  }

  protected bool valid_surrounding_tiles(
    int index,
    int length,
    int[,] map_tiles,
    List<Point> locs,
    ref List<short>[] tiles)
  {
    for (int index1 = index + 1; index1 < length; ++index1)
    {
      tiles[index1] = new List<short>((IEnumerable<short>) this.valid_tiles(locs[index1].X, locs[index1].Y, map_tiles));
      if (tiles[index1].Count <= 0)
        return false;
    }
    return true;
  }

  private short[,] tile_priorities(int[,] map_tiles)
  {
    int length1 = map_tiles.GetLength(0);
    int length2 = map_tiles.GetLength(1);
    short[,] numArray = new short[length1, length2];
    for (int index1 = 0; index1 < length2; ++index1)
    {
      for (int index2 = 0; index2 < length1; ++index2)
        numArray[index2, index1] = this.tile_priority(map_tiles[index2, index1]);
    }
    return numArray;
  }

  private short tile_priority(int index)
  {
    return this.tileset_config_data.ContainsKey(index) ? this.tileset_config_data[index].Priority : (short) -1;
  }

  protected int first_open_tile(short[,] tile_priorities)
  {
    int priority = (int) this.Open_Tiles.Select<int, short>((Func<int, short>) (open_tile => tile_priorities[open_tile % this.Map_Width, open_tile / this.Map_Width])).Max<short>();
    HashSet<int> source = new HashSet<int>(this.Open_Tiles.Where<int>((Func<int, bool>) (open_tile => (int) tile_priorities[open_tile % this.Map_Width, open_tile / this.Map_Width] == priority)));
    return source.ElementAt<int>(FE_Map_Creator_Form.rand.Next(source.Count<int>()));
  }

  protected bool is_off_map(int x, int y)
  {
    return x < 0 || y < 0 || x >= this.Map_Width || y >= this.Map_Height;
  }

  protected void import_image_map(Bitmap bmp)
  {
    this.WidthUpDown.Value = (Decimal) (this.Map_Width = bmp.Width / 16 /*0x10*/);
    this.HeightUpDown.Value = (Decimal) (this.Map_Height = bmp.Height / 16 /*0x10*/);
    this.Map_Tiles = new int[this.Map_Width, this.Map_Height];
    this.copy_map_to_undo(false);
    this.reset_metadata();
    this.Open_Tiles = new HashSet<int>();
    this.compare_tiles(bmp);
    this.refresh_panel_size();
    this.MapPicture.Invalidate();
  }

  public Tile_Data tile_data(int index)
  {
    return this.tileset_config_data != null && this.tileset_config_data.ContainsKey(index) ? new Tile_Data(this.tileset_config_data[index]) : new Tile_Data();
  }

  public List<short> tileset_tiles_without_redundant(List<short> indices)
  {
    HashSet<short> source = new HashSet<short>();
    foreach (short index in indices)
      source.Add(this.Tileset_Generator_Data.identical_tiles.ContainsKey(index) ? this.Tileset_Generator_Data.identical_tiles[index] : index);
    return source.ToList<short>();
  }

  public void set_tile_data(short index, Tile_Data data)
  {
    if (this.tileset_config_data == null)
      return;
    Tile_Data tileData = this.tileset_config_data.ContainsKey((int) index) ? new Tile_Data(this.tileset_config_data[(int) index]) : new Tile_Data();
    for (byte index1 = 2; index1 <= (byte) 8; index1 += (byte) 2)
    {
      byte num1 = (byte) (10U - (uint) index1);
      HashSet<short> shortSet1 = new HashSet<short>((IEnumerable<short>) data.Valid_Tile_Priority[index1].Keys);
      shortSet1.ExceptWith((IEnumerable<short>) tileData.Valid_Tile_Priority[index1].Keys);
      HashSet<short> shortSet2 = new HashSet<short>((IEnumerable<short>) tileData.Valid_Tile_Priority[index1].Keys);
      shortSet2.ExceptWith((IEnumerable<short>) data.Valid_Tile_Priority[index1].Keys);
      List<short> shortList1 = this.Tile_Matches.matched_tiles((Tile_Directions) index1, index);
      foreach (short key in shortList1)
      {
        if (!this.tileset_config_data.ContainsKey((int) key))
          this.tileset_config_data[(int) key] = new Tile_Data();
        foreach (short num2 in shortSet1)
        {
          foreach (short matchedTile in this.Tile_Matches.matched_tiles((Tile_Directions) num1, num2))
          {
            if (!this.tileset_config_data[(int) key].Valid_Tile_Priority[index1].ContainsKey(matchedTile))
              this.tileset_config_data[(int) key].Valid_Tile_Priority[index1].Add(matchedTile, data.Valid_Tile_Priority[index1][num2]);
          }
        }
        foreach (short index2 in shortSet2)
        {
          foreach (short matchedTile in this.Tile_Matches.matched_tiles((Tile_Directions) num1, index2))
            this.tileset_config_data[(int) key].Valid_Tile_Priority[index1].Remove(matchedTile);
        }
      }
      foreach (short index3 in shortSet2)
      {
        List<short> shortList2 = this.Tile_Matches.matched_tiles((Tile_Directions) num1, index3);
        for (int index4 = 0; index4 < shortList2.Count; ++index4)
        {
          if (!this.tileset_config_data.ContainsKey((int) shortList2[index4]))
            this.tileset_config_data[(int) shortList2[index4]] = new Tile_Data();
          foreach (short key in shortList1)
            this.tileset_config_data[(int) shortList2[index4]].Valid_Tile_Priority[num1].Remove(key);
        }
      }
      foreach (short num3 in shortSet1)
      {
        List<short> shortList3 = this.Tile_Matches.matched_tiles((Tile_Directions) num1, num3);
        for (int index5 = 0; index5 < shortList3.Count; ++index5)
        {
          if (!this.tileset_config_data.ContainsKey((int) shortList3[index5]))
            this.tileset_config_data[(int) shortList3[index5]] = new Tile_Data();
          foreach (short key in shortList1)
          {
            if (!this.tileset_config_data[(int) shortList3[index5]].Valid_Tile_Priority[num1].ContainsKey(key))
              this.tileset_config_data[(int) shortList3[index5]].Valid_Tile_Priority[num1].Add(key, data.Valid_Tile_Priority[index1][num3]);
          }
        }
      }
    }
    this.tileset_config_data[(int) index] = data;
  }

  public List<short> same_side(short tile, byte dir)
  {
    if (this.Tile_Matches != null)
      return this.Tile_Matches.matched_tiles((Tile_Directions) dir, tile);
    return new List<short>() { tile };
  }

  protected int valid_tile_priority(int index, byte dir, short other_tile)
  {
    if (this.tileset_config_data != null && this.tileset_config_data.ContainsKey(index))
    {
      Tile_Data tileData = this.tileset_config_data[index];
      if (tileData.Valid_Tile_Priority[dir].ContainsKey(other_tile))
        return (int) tileData.Valid_Tile_Priority[dir][other_tile];
    }
    return 1;
  }

  private string terrain_name(int terrain_id, bool shorten = false)
  {
    if (!this.Terrain_Data.ContainsKey(terrain_id))
      return terrain_id.ToString();
    return shorten ? this.Terrain_Data[terrain_id].Name.Substring(0, Math.Min(2, this.Terrain_Data[terrain_id].Name.Length)) : this.Terrain_Data[terrain_id].Name;
  }

  private void copy_color(Bitmap render)
  {
    this.copy_color(render, new Rectangle(0, 0, render.Width, render.Height));
  }

  private void copy_color(Bitmap render, Color bg_color)
  {
    this.copy_color(render, new Rectangle(0, 0, render.Width, render.Height), bg_color);
  }

  private void copy_color(Bitmap render, Rectangle rect)
  {
    this.copy_color(render, rect, FE_Map_Creator_Form.TRANSPARENT_COLOR);
  }

  private unsafe void copy_color(Bitmap render, Rectangle rect, Color bg_color)
  {
    BitmapData bitmapdata = render.LockBits(new Rectangle(0, 0, render.Width, render.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
    int stride = bitmapdata.Stride;
    IntPtr scan0 = bitmapdata.Scan0;
    int num1 = Math.Min(render.Width - rect.X, rect.Width);
    int num2 = Math.Min(render.Height - rect.Y, rect.Height);
    int num3 = stride - render.Width * 4;
    for (int index1 = 0; index1 < num2; ++index1)
    {
      byte* numPtr = (byte*) ((IntPtr) (void*) scan0 + (num3 + render.Width * 4 * (index1 + rect.Y) + rect.X * 4));
      for (int index2 = 0; index2 < num1; ++index2)
      {
        *numPtr = (byte) Math.Min((int) byte.MaxValue, ((int) *numPtr * ((int) byte.MaxValue - (int) bg_color.A) + (int) bg_color.B * (int) bg_color.A) / (int) byte.MaxValue);
        numPtr[1] = (byte) Math.Min((int) byte.MaxValue, ((int) numPtr[1] * ((int) byte.MaxValue - (int) bg_color.A) + (int) bg_color.G * (int) bg_color.A) / (int) byte.MaxValue);
        numPtr[2] = (byte) Math.Min((int) byte.MaxValue, ((int) numPtr[2] * ((int) byte.MaxValue - (int) bg_color.A) + (int) bg_color.R * (int) bg_color.A) / (int) byte.MaxValue);
        numPtr[3] = byte.MaxValue;
        numPtr += 4;
      }
    }
    render.UnlockBits(bitmapdata);
  }

  private void copy_pixels(Bitmap source, Bitmap render, Rectangle src_rect, Point loc)
  {
    this.copy_pixels(source, render, src_rect, loc, FE_Map_Creator_Form.TRANSPARENT_COLOR);
  }

  private void copy_pixels(
    Bitmap source,
    Bitmap render,
    Rectangle src_rect,
    Point loc,
    Color bg_color)
  {
    this.copy_pixels(source, render, src_rect, loc, bg_color, (int) byte.MaxValue, 0, false);
  }

  private void copy_pixels(
    Bitmap source,
    Bitmap render,
    Rectangle src_rect,
    Point loc,
    bool mirrored)
  {
    this.copy_pixels(source, render, src_rect, loc, FE_Map_Creator_Form.TRANSPARENT_COLOR, (int) byte.MaxValue, 0, mirrored);
  }

  private void copy_pixels(
    Bitmap source,
    Bitmap render,
    Rectangle src_rect,
    Point loc,
    int opacity)
  {
    this.copy_pixels(source, render, src_rect, loc, opacity, 0);
  }

  private void copy_pixels(
    Bitmap source,
    Bitmap render,
    Rectangle src_rect,
    Point loc,
    int opacity,
    int blend_mode)
  {
    this.copy_pixels(source, render, src_rect, loc, opacity, blend_mode, false);
  }

  private void copy_pixels(
    Bitmap source,
    Bitmap render,
    Rectangle src_rect,
    Point loc,
    int opacity,
    int blend_mode,
    bool mirrored)
  {
    this.copy_pixels(source, render, src_rect, loc, FE_Map_Creator_Form.TRANSPARENT_COLOR, opacity, blend_mode, mirrored);
  }

  private unsafe void copy_pixels(
    Bitmap source,
    Bitmap render,
    Rectangle src_rect,
    Point loc,
    Color bg_color,
    int opacity,
    int blend_mode,
    bool mirrored)
  {
    BitmapData bitmapdata1 = source.LockBits(new Rectangle(0, 0, source.Width, source.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    BitmapData bitmapdata2 = render.LockBits(new Rectangle(0, 0, render.Width, render.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
    int stride1 = bitmapdata1.Stride;
    int stride2 = bitmapdata2.Stride;
    IntPtr scan0_1 = bitmapdata1.Scan0;
    IntPtr scan0_2 = bitmapdata2.Scan0;
    int num1;
    int num2;
    if (mirrored)
    {
      if (loc.X < 0)
      {
        src_rect.Width += loc.X;
        loc.X = 0;
      }
      if (loc.Y < 0)
      {
        src_rect.Y -= loc.Y;
        src_rect.Height += loc.Y;
        loc.Y = 0;
      }
      num1 = Math.Min(Math.Min(source.Width - src_rect.X, render.Width - loc.X), src_rect.Width);
      num2 = Math.Min(Math.Min(source.Height - src_rect.Y, render.Height - loc.Y), src_rect.Height);
    }
    else
    {
      if (loc.X < 0)
      {
        src_rect.X -= loc.X;
        src_rect.Width += loc.X;
        loc.X = 0;
      }
      if (loc.Y < 0)
      {
        src_rect.Y -= loc.Y;
        src_rect.Height += loc.Y;
        loc.Y = 0;
      }
      num1 = Math.Min(Math.Min(source.Width - src_rect.X, render.Width - loc.X), src_rect.Width);
      num2 = Math.Min(Math.Min(source.Height - src_rect.Y, render.Height - loc.Y), src_rect.Height);
    }
    float num3 = (float) opacity / (float) byte.MaxValue;
    int num4 = stride1 - source.Width * 4;
    int num5 = stride2 - render.Width * 4;
    for (int index1 = 0; index1 < num2; ++index1)
    {
      byte* numPtr1 = !mirrored ? (byte*) ((IntPtr) (void*) scan0_1 + (num4 + source.Width * 4 * (index1 + src_rect.Y) + src_rect.X * 4)) : (byte*) ((IntPtr) (void*) scan0_1 + (num4 + source.Width * 4 * (index1 + src_rect.Y) + (src_rect.Width + src_rect.X - 1) * 4));
      byte* numPtr2 = (byte*) ((IntPtr) (void*) scan0_2 + (num5 + render.Width * 4 * (index1 + loc.Y) + loc.X * 4));
      for (int index2 = 0; index2 < num1; ++index2)
      {
        switch (blend_mode)
        {
          case 0:
            *numPtr2 = (byte) Math.Min((float) byte.MaxValue, (float) (((double) *numPtr2 * ((double) byte.MaxValue - (double) numPtr1[3] * (double) num3) + (double) ((int) *numPtr1 * (int) numPtr1[3]) * (double) num3) / (double) byte.MaxValue));
            numPtr2[1] = (byte) Math.Min((float) byte.MaxValue, (float) (((double) numPtr2[1] * ((double) byte.MaxValue - (double) numPtr1[3] * (double) num3) + (double) ((int) numPtr1[1] * (int) numPtr1[3]) * (double) num3) / (double) byte.MaxValue));
            numPtr2[2] = (byte) Math.Min((float) byte.MaxValue, (float) (((double) numPtr2[2] * ((double) byte.MaxValue - (double) numPtr1[3] * (double) num3) + (double) ((int) numPtr1[2] * (int) numPtr1[3]) * (double) num3) / (double) byte.MaxValue));
            break;
          case 1:
            *numPtr2 = (byte) Math.Min((float) byte.MaxValue, (float) (((double) ((int) *numPtr2 * (int) byte.MaxValue) + (double) ((int) *numPtr1 * (int) numPtr1[3]) * (double) num3) / (double) byte.MaxValue));
            numPtr2[1] = (byte) Math.Min((float) byte.MaxValue, (float) (((double) ((int) numPtr2[1] * (int) byte.MaxValue) + (double) ((int) numPtr1[1] * (int) numPtr1[3]) * (double) num3) / (double) byte.MaxValue));
            numPtr2[2] = (byte) Math.Min((float) byte.MaxValue, (float) (((double) ((int) numPtr2[2] * (int) byte.MaxValue) + (double) ((int) numPtr1[2] * (int) numPtr1[3]) * (double) num3) / (double) byte.MaxValue));
            break;
        }
        numPtr2[3] = byte.MaxValue;
        if (mirrored)
          numPtr1 -= 4;
        else
          numPtr1 += 4;
        numPtr2 += 4;
      }
    }
    source.UnlockBits(bitmapdata1);
    render.UnlockBits(bitmapdata2);
  }

  internal static unsafe Color average_color(Bitmap bmp, Rectangle rect)
  {
    long[] numArray = new long[3];
    BitmapData bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
    int stride = bitmapdata.Stride;
    IntPtr scan0 = bitmapdata.Scan0;
    int num1 = Math.Min(bmp.Width - rect.X, rect.Width);
    int num2 = Math.Min(bmp.Height - rect.Y, rect.Height);
    int num3 = stride - bmp.Width * 4;
    for (int index1 = 0; index1 < num2; ++index1)
    {
      byte* numPtr = (byte*) ((IntPtr) (void*) scan0 + (num3 + bmp.Width * 4 * (index1 + rect.Y) + rect.X * 4));
      for (int index2 = 0; index2 < num1; ++index2)
      {
        numArray[2] += (long) *numPtr;
        numArray[1] += (long) numPtr[1];
        numArray[0] += (long) numPtr[2];
        numPtr += 4;
      }
    }
    bmp.UnlockBits(bitmapdata);
    return Color.FromArgb((int) (numArray[0] / (long) (num1 * num2)), (int) (numArray[1] / (long) (num1 * num2)), (int) (numArray[2] / (long) (num1 * num2)));
  }

  internal static unsafe Color rms_color(Bitmap bmp, Rectangle rect)
  {
    long[] numArray = new long[3];
    BitmapData bitmapdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
    int stride = bitmapdata.Stride;
    IntPtr scan0 = bitmapdata.Scan0;
    int num1 = Math.Min(bmp.Width - rect.X, rect.Width);
    int num2 = Math.Min(bmp.Height - rect.Y, rect.Height);
    int num3 = stride - bmp.Width * 4;
    for (int index1 = 0; index1 < num2; ++index1)
    {
      byte* numPtr = (byte*) ((IntPtr) (void*) scan0 + (num3 + bmp.Width * 4 * (index1 + rect.Y) + rect.X * 4));
      for (int index2 = 0; index2 < num1; ++index2)
      {
        int num4 = (int) numPtr[2];
        int num5 = (int) numPtr[1];
        int num6 = (int) *numPtr;
        numArray[2] += (long) Math.Pow((double) *numPtr, 2.0);
        numArray[1] += (long) Math.Pow((double) numPtr[1], 2.0);
        numArray[0] += (long) Math.Pow((double) numPtr[2], 2.0);
        numPtr += 4;
      }
    }
    bmp.UnlockBits(bitmapdata);
    return Color.FromArgb((int) Math.Sqrt((double) (numArray[0] / (long) (num1 * num2))), (int) Math.Sqrt((double) (numArray[1] / (long) (num1 * num2))), (int) Math.Sqrt((double) (numArray[2] / (long) (num1 * num2))));
  }

  protected void compare_tiles(Bitmap map)
  {
    BitmapData bitmapdata1 = this.Tileset_Image.LockBits(new Rectangle(0, 0, this.Tileset_Image.Width, this.Tileset_Image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    using (Bitmap render = new Bitmap((Image) map))
    {
      BitmapData bitmapdata2 = render.LockBits(new Rectangle(0, 0, map.Width, map.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
      int stride1 = bitmapdata1.Stride;
      int stride2 = bitmapdata2.Stride;
      IntPtr scan0_1 = bitmapdata1.Scan0;
      IntPtr scan0_2 = bitmapdata2.Scan0;
      for (int index1 = 0; index1 < this.Map_Width * this.Map_Height; ++index1)
      {
        int index2 = index1 % this.Map_Width;
        int index3 = index1 / this.Map_Width;
        int num1 = -1;
        int num2 = 0;
        for (int index4 = 0; index4 < this.Tileset_Image.Width / 16 /*0x10*/ * (this.Tileset_Image.Height / 16 /*0x10*/); ++index4)
        {
          int x = index4 % (this.Tileset_Image.Width / 16 /*0x10*/) * 16 /*0x10*/;
          int y = index4 / (this.Tileset_Image.Width / 16 /*0x10*/) * 16 /*0x10*/;
          int num3 = this.compare_pixels(this.Tileset_Image, render, new Rectangle(x, y, 16 /*0x10*/, 16 /*0x10*/), new Point(index2 * 16 /*0x10*/, index3 * 16 /*0x10*/), stride1, stride2, scan0_1, scan0_2);
          if (num3 == 0)
          {
            num2 = index4;
            break;
          }
          if (num3 < num1 || num1 == -1)
          {
            num1 = num3;
            num2 = index4;
          }
        }
        this.Map_Tiles[index2, index3] = num2;
      }
      this.Tileset_Image.UnlockBits(bitmapdata1);
      render.UnlockBits(bitmapdata2);
    }
  }

  private int compare_pixels(
    Bitmap source,
    Bitmap render,
    Rectangle src_rect,
    Point loc,
    int stride1,
    int stride2,
    IntPtr scan1,
    IntPtr scan2)
  {
    return this.compare_pixels(source, render, src_rect, loc, stride1, stride2, scan1, scan2, Modes.Normal);
  }

  private unsafe int compare_pixels(
    Bitmap source,
    Bitmap render,
    Rectangle src_rect,
    Point loc,
    int stride1,
    int stride2,
    IntPtr scan1,
    IntPtr scan2,
    Modes mode)
  {
    if (loc.X < 0)
    {
      src_rect.X -= loc.X;
      src_rect.Width += loc.X;
      loc.X = 0;
    }
    if (loc.Y < 0)
    {
      src_rect.Y -= loc.Y;
      src_rect.Height += loc.Y;
      loc.Y = 0;
    }
    int num1 = Math.Min(Math.Min(source.Width - src_rect.X, render.Width - loc.X), src_rect.Width);
    int num2 = Math.Min(Math.Min(source.Height - src_rect.Y, render.Height - loc.Y), src_rect.Height);
    int num3 = 0;
    int num4 = 0;
    bool flag = false;
    int num5 = stride1 - source.Width * 4;
    int num6 = stride2 - render.Width * 4;
    for (int index1 = 0; index1 < num2; ++index1)
    {
      byte* numPtr1 = (byte*) ((IntPtr) (void*) scan1 + (num5 + source.Width * 4 * (index1 + src_rect.Y) + src_rect.X * 4));
      byte* numPtr2 = (byte*) ((IntPtr) (void*) scan2 + (num6 + render.Width * 4 * (index1 + loc.Y) + loc.X * 4));
      for (int index2 = 0; index2 < num1; ++index2)
      {
        int num7 = 0;
        if (mode == Modes.Mode1 || mode == Modes.Mode2)
          num3 = 0;
        for (int index3 = 0; index3 < 4; ++index3)
        {
          int x = Math.Abs((int) numPtr1[index3] - (int) numPtr2[index3]);
          if (x > 0 && mode == Modes.Exact)
          {
            int num8 = (int) Math.Pow((double) byte.MaxValue, 2.0) * 3;
            flag = true;
            break;
          }
          if (mode == Modes.Mode1 || mode == Modes.Mode2)
            num3 += (int) Math.Pow((double) x, 2.0);
          num7 += x;
        }
        if (!flag)
        {
          if (num7 != 0 && mode == Modes.Mode1)
            num3 /= num7;
          int num9 = num7 / 4;
          if (mode == Modes.Mode1 || mode == Modes.Mode2)
            num4 += num3;
          else
            num4 += num9;
          numPtr1 += 4;
          numPtr2 += 4;
        }
        else
          break;
      }
      if (flag)
        break;
    }
    return num4;
  }

  private void NewMapButton_Click(object sender, EventArgs e)
  {
    if (string.IsNullOrEmpty(this.Tileset_Filename))
    {
      int num = (int) MessageBox.Show("A tileset must be loaded before creating a map.", "Load a Tileset First", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
    }
    else
    {
      if (!(this.WidthUpDown.Value > 0M) || !(this.HeightUpDown.Value > 0M) || this.Updating)
        return;
      this.Updating = true;
      this.Text = string.Format("Map Editor - New Map");
      this.Map_Width = (int) this.WidthUpDown.Value;
      this.Map_Height = (int) this.HeightUpDown.Value;
      if (this.Map_Tiles == null)
        this.Undo_Map_Tiles = new int[this.Map_Width, this.Map_Height];
      else
        this.copy_map_to_undo();
      this.Map_Tiles = new int[this.Map_Width, this.Map_Height];
      this.Drawn_Tiles = new bool[this.Map_Width, this.Map_Height];
      this.Locked_Tiles = new bool[this.Map_Width, this.Map_Height];
      if (this.Terrain_Types == null || this.Terrain_Types.GetLength(0) != this.Map_Width || this.Terrain_Types.GetLength(1) != this.Map_Height)
        this.Terrain_Types = new int[this.Map_Width, this.Map_Height];
      this.Open_Tiles = new HashSet<int>();
      this.refresh_panel_size();
      this.MapPicture.Invalidate();
      this.Updating = false;
    }
  }

  private void ZoomUpDown_ValueChanged(object sender, EventArgs e)
  {
    if (((NumericUpDown) sender).Value == (Decimal) this.Zoom)
      return;
    if (!this.Updating)
    {
      this.Zoom = (int) ((NumericUpDown) sender).Value;
      if (!this.ready_to_draw)
        return;
      this.refresh_panel_size();
      this.MapPicture.Invalidate();
    }
    else
      ((NumericUpDown) sender).Value = (Decimal) this.Zoom;
  }

  private void MapPicture_MouseDown(object sender, MouseEventArgs e)
  {
    if (this.Drawing || this.Selecting)
    {
      this.Drawing = false;
      this.Selecting = false;
      this.MapPicture.Invalidate();
    }
    else
    {
      if (!this.ready_to_draw || this.Updating)
        return;
      int index1 = e.X / (16 /*0x10*/ * this.Zoom);
      int index2 = e.Y / (16 /*0x10*/ * this.Zoom);
      this.Drawing_Mouse_Loc = new Point(index1, index2);
      this.Base_Drawing_Mouse_Loc = new Point(index1, index2);
      using (Region region = new Region())
      {
        region.MakeEmpty();
        Size size = new Size(Math.Max(1, this.Tileset_Palette.indices.GetLength(0)) * 16 /*0x10*/ * this.Zoom + 8, Math.Max(1, this.Tileset_Palette.indices.GetLength(1)) * 16 /*0x10*/ * this.Zoom + 8);
        region.Union(new Rectangle(new Point(this.Drawing_Mouse_Loc.X * 16 /*0x10*/ * this.Zoom - 4, this.Drawing_Mouse_Loc.Y * 16 /*0x10*/ * this.Zoom - 4), size));
        int num = this.painting_terrain ? 1 : 0;
        if (!this.pinning_tiles && !this.painting_terrain && e.Button == MouseButtons.Right)
        {
          this.Drawing = false;
          this.Selecting = true;
          this.MapPicture.Invalidate(region);
        }
        else
        {
          switch (this.active_tool)
          {
            case Tools.Brush:
              this.Drawing = true;
              if (this.pinning_tiles)
              {
                if (index1 < 0 || index1 >= this.Map_Width || index2 < 0 || index2 >= this.Map_Height)
                  break;
                this.Locked_Tiles[index1, index2] = e.Button == MouseButtons.Left;
                if (this.Locked_Tiles[index1, index2])
                  this.Terrain_Types[index1, index2] = 0;
                this.MapPicture.Invalidate(region);
                break;
              }
              if (this.painting_terrain)
              {
                if (index1 < 0 || index1 >= this.Map_Width || index2 < 0 || index2 >= this.Map_Height)
                  break;
                this.Terrain_Types[index1, index2] = (e.Button == MouseButtons.Right ? -1 : 1) * this.Terrain_Palette.active_terrain;
                this.Locked_Tiles[index1, index2] = false;
                this.MapPicture.Invalidate(region);
                break;
              }
              if (this.Tileset_Palette.indices.Length > 0)
              {
                this.copy_map_to_undo();
                if (!this.mouse_draw(index1, index2, this.Tileset_Palette.indices))
                  break;
                this.MapPicture.Invalidate(region);
                break;
              }
              this.Drawing = false;
              break;
            case Tools.Box:
              if (index1 < 0 || index1 >= this.Map_Width || index2 < 0 || index2 >= this.Map_Height)
                break;
              this.Drawing = true;
              if (!this.pinning_tiles && !this.painting_terrain)
              {
                if (this.Tileset_Palette.indices.Length > 0)
                  this.copy_map_to_undo();
                else
                  this.Drawing = false;
              }
              if (!this.Drawing)
                break;
              if (this.pinning_tiles)
                this.Box_Pin_Value = e.Button == MouseButtons.Left;
              else if (this.painting_terrain)
                this.Box_Terrain_Value = (e.Button == MouseButtons.Right ? -1 : 1) * this.Terrain_Palette.active_terrain;
              this.MapPicture.Invalidate(region);
              break;
            case Tools.Bucket:
              if (index1 < 0 || index1 >= this.Map_Width || index2 < 0 || index2 >= this.Map_Height)
                break;
              bool flag = false;
              if (this.pinning_tiles)
                flag = this.pin_flood_fill(index1, index2, e.Button == MouseButtons.Left);
              else if (this.painting_terrain)
                flag = this.terrain_flood_fill(index1, index2, (e.Button == MouseButtons.Right ? -1 : 1) * this.Terrain_Palette.active_terrain);
              else if (this.Tileset_Palette.indices.Length > 0)
              {
                this.copy_map_to_undo();
                flag = this.mouse_flood_fill(index1, index2, this.Tileset_Palette.indices);
              }
              if (!flag)
                break;
              this.Drawing = true;
              this.MapPicture.Invalidate();
              break;
          }
        }
      }
    }
  }

  private void MapPicture_MouseMove(object sender, MouseEventArgs e)
  {
    int num1 = e.X / (16 /*0x10*/ * this.Zoom);
    int num2 = e.Y / (16 /*0x10*/ * this.Zoom);
    this.CursorPositionStatusLabel.Text = $"{num1},{num2}";
    Point drawingMouseLoc = this.Drawing_Mouse_Loc;
    if (this.Drawing)
    {
      if (!(this.Drawing_Mouse_Loc != new Point(num1, num2)))
        return;
      this.Drawing_Mouse_Loc = new Point(num1, num2);
      using (Region region = new Region())
      {
        region.MakeEmpty();
        switch (this.active_tool)
        {
          case Tools.Brush:
            Size size1 = new Size(Math.Max(1, this.Tileset_Palette.indices.GetLength(0)) * 16 /*0x10*/ * this.Zoom + 8, Math.Max(1, this.Tileset_Palette.indices.GetLength(1)) * 16 /*0x10*/ * this.Zoom + 8);
            region.Union(new Rectangle(new Point(drawingMouseLoc.X * 16 /*0x10*/ * this.Zoom - 4, drawingMouseLoc.Y * 16 /*0x10*/ * this.Zoom - 4), size1));
            region.Union(new Rectangle(new Point(this.Drawing_Mouse_Loc.X * 16 /*0x10*/ * this.Zoom - 4, this.Drawing_Mouse_Loc.Y * 16 /*0x10*/ * this.Zoom - 4), size1));
            List<Point> pointList = FE_Map_Creator_Form.bresenham(drawingMouseLoc.X, drawingMouseLoc.Y, num1, num2);
            region.Union(new Rectangle(new Point(Math.Min(drawingMouseLoc.X, num1) * 16 /*0x10*/ * this.Zoom - 4, Math.Min(drawingMouseLoc.Y, num2) * 16 /*0x10*/ * this.Zoom - 4), new Size((Math.Abs(drawingMouseLoc.X - num1) + 1) * 16 /*0x10*/ * this.Zoom + 8, (Math.Abs(drawingMouseLoc.Y - num2) + 1) * 16 /*0x10*/ * this.Zoom + 8)));
            this.Drawing = true;
            if (this.pinning_tiles)
            {
              using (List<Point>.Enumerator enumerator = pointList.GetEnumerator())
              {
                while (enumerator.MoveNext())
                {
                  Point current = enumerator.Current;
                  if (current.X >= 0 && current.X < this.Map_Width && current.Y >= 0 && current.Y < this.Map_Height)
                  {
                    this.Locked_Tiles[current.X, current.Y] = e.Button == MouseButtons.Left;
                    if (this.Locked_Tiles[current.X, current.Y])
                      this.Terrain_Types[current.X, current.Y] = 0;
                  }
                }
                break;
              }
            }
            if (this.painting_terrain)
            {
              using (List<Point>.Enumerator enumerator = pointList.GetEnumerator())
              {
                while (enumerator.MoveNext())
                {
                  Point current = enumerator.Current;
                  if (current.X >= 0 && current.X < this.Map_Width && current.Y >= 0 && current.Y < this.Map_Height)
                  {
                    this.Terrain_Types[current.X, current.Y] = (e.Button == MouseButtons.Right ? -1 : 1) * this.Terrain_Palette.active_terrain;
                    this.Locked_Tiles[current.X, current.Y] = false;
                  }
                }
                break;
              }
            }
            using (List<Point>.Enumerator enumerator = pointList.GetEnumerator())
            {
              while (enumerator.MoveNext())
              {
                Point current = enumerator.Current;
                this.mouse_draw(current.X, current.Y, this.Tileset_Palette.indices);
              }
              break;
            }
          case Tools.Box:
            region.Union(new Rectangle(new Point(Math.Min(drawingMouseLoc.X, this.Base_Drawing_Mouse_Loc.X) * 16 /*0x10*/ * this.Zoom - 4, Math.Min(drawingMouseLoc.Y, this.Base_Drawing_Mouse_Loc.Y) * 16 /*0x10*/ * this.Zoom - 4), new Size((Math.Abs(drawingMouseLoc.X - this.Base_Drawing_Mouse_Loc.X) + 1) * 16 /*0x10*/ * this.Zoom + 8, (Math.Abs(drawingMouseLoc.Y - this.Base_Drawing_Mouse_Loc.Y) + 1) * 16 /*0x10*/ * this.Zoom + 8)));
            region.Union(new Rectangle(new Point(Math.Min(this.Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.X) * 16 /*0x10*/ * this.Zoom - 4, Math.Min(this.Drawing_Mouse_Loc.Y, this.Base_Drawing_Mouse_Loc.Y) * 16 /*0x10*/ * this.Zoom - 4), new Size((Math.Abs(this.Drawing_Mouse_Loc.X - this.Base_Drawing_Mouse_Loc.X) + 1) * 16 /*0x10*/ * this.Zoom + 8, (Math.Abs(this.Drawing_Mouse_Loc.Y - this.Base_Drawing_Mouse_Loc.Y) + 1) * 16 /*0x10*/ * this.Zoom + 8)));
            break;
          case Tools.Bucket:
            Size size2 = new Size(Math.Max(1, this.Tileset_Palette.indices.GetLength(0)) * 16 /*0x10*/ * this.Zoom + 8, Math.Max(1, this.Tileset_Palette.indices.GetLength(1)) * 16 /*0x10*/ * this.Zoom + 8);
            region.Union(new Rectangle(new Point(drawingMouseLoc.X * 16 /*0x10*/ * this.Zoom - 4, drawingMouseLoc.Y * 16 /*0x10*/ * this.Zoom - 4), size2));
            region.Union(new Rectangle(new Point(this.Drawing_Mouse_Loc.X * 16 /*0x10*/ * this.Zoom - 4, this.Drawing_Mouse_Loc.Y * 16 /*0x10*/ * this.Zoom - 4), size2));
            break;
        }
        this.MapPicture.Invalidate(region);
      }
    }
    else if (this.Selecting)
    {
      if (!(this.Drawing_Mouse_Loc != new Point(num1, num2)))
        return;
      this.Drawing_Mouse_Loc = new Point(num1, num2);
      using (Region region = new Region())
      {
        region.MakeEmpty();
        region.Union(new Rectangle(new Point(Math.Min(drawingMouseLoc.X, this.Base_Drawing_Mouse_Loc.X) * 16 /*0x10*/ * this.Zoom - 4, Math.Min(drawingMouseLoc.Y, this.Base_Drawing_Mouse_Loc.Y) * 16 /*0x10*/ * this.Zoom - 4), new Size((Math.Abs(drawingMouseLoc.X - this.Base_Drawing_Mouse_Loc.X) + 1) * 16 /*0x10*/ * this.Zoom + 8, (Math.Abs(drawingMouseLoc.Y - this.Base_Drawing_Mouse_Loc.Y) + 1) * 16 /*0x10*/ * this.Zoom + 8)));
        region.Union(new Rectangle(new Point(Math.Min(this.Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.X) * 16 /*0x10*/ * this.Zoom - 4, Math.Min(this.Drawing_Mouse_Loc.Y, this.Base_Drawing_Mouse_Loc.Y) * 16 /*0x10*/ * this.Zoom - 4), new Size((Math.Abs(this.Drawing_Mouse_Loc.X - this.Base_Drawing_Mouse_Loc.X) + 1) * 16 /*0x10*/ * this.Zoom + 8, (Math.Abs(this.Drawing_Mouse_Loc.Y - this.Base_Drawing_Mouse_Loc.Y) + 1) * 16 /*0x10*/ * this.Zoom + 8)));
        this.MapPicture.Invalidate(region);
      }
    }
    else
    {
      if (!this.ready_to_draw || !(this.Drawing_Mouse_Loc != new Point(num1, num2)))
        return;
      this.Drawing_Mouse_Loc = new Point(num1, num2);
      using (Region region = new Region())
      {
        region.MakeEmpty();
        Size size = new Size(Math.Max(1, this.Tileset_Palette.indices.GetLength(0)) * 16 /*0x10*/ * this.Zoom + 8, Math.Max(1, this.Tileset_Palette.indices.GetLength(1)) * 16 /*0x10*/ * this.Zoom + 8);
        region.Union(new Rectangle(new Point(drawingMouseLoc.X * 16 /*0x10*/ * this.Zoom - 4, drawingMouseLoc.Y * 16 /*0x10*/ * this.Zoom - 4), size));
        region.Union(new Rectangle(new Point(this.Drawing_Mouse_Loc.X * 16 /*0x10*/ * this.Zoom - 4, this.Drawing_Mouse_Loc.Y * 16 /*0x10*/ * this.Zoom - 4), size));
        this.MapPicture.Invalidate(region);
      }
    }
  }

  private void MapPicture_MouseUp(object sender, MouseEventArgs e)
  {
    if (this.Drawing)
    {
      if (this.active_tool == Tools.Box)
      {
        int x = e.X / (16 /*0x10*/ * this.Zoom);
        int y = e.Y / (16 /*0x10*/ * this.Zoom);
        Point drawingMouseLoc = this.Drawing_Mouse_Loc;
        this.Drawing_Mouse_Loc = new Point(x, y);
        if (this.pinning_tiles)
          this.pin_box_fill(this.Base_Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.Y, this.Drawing_Mouse_Loc.X, this.Drawing_Mouse_Loc.Y, e.Button == MouseButtons.Left);
        else if (this.painting_terrain)
          this.terrain_box_fill(this.Base_Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.Y, this.Drawing_Mouse_Loc.X, this.Drawing_Mouse_Loc.Y, (e.Button == MouseButtons.Right ? -1 : 1) * this.Terrain_Palette.active_terrain);
        else
          this.mouse_box_fill(this.Base_Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.Y, this.Drawing_Mouse_Loc.X, this.Drawing_Mouse_Loc.Y, this.Tileset_Palette.indices);
        using (Region region = new Region())
        {
          region.MakeEmpty();
          region.Union(new Rectangle(new Point(Math.Min(drawingMouseLoc.X, this.Base_Drawing_Mouse_Loc.X) * 16 /*0x10*/ * this.Zoom - 4, Math.Min(drawingMouseLoc.Y, this.Base_Drawing_Mouse_Loc.Y) * 16 /*0x10*/ * this.Zoom - 4), new Size((Math.Abs(drawingMouseLoc.X - this.Base_Drawing_Mouse_Loc.X) + 1) * 16 /*0x10*/ * this.Zoom + 8, (Math.Abs(drawingMouseLoc.Y - this.Base_Drawing_Mouse_Loc.Y) + 1) * 16 /*0x10*/ * this.Zoom + 8)));
          region.Union(new Rectangle(new Point(Math.Min(this.Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.X) * 16 /*0x10*/ * this.Zoom - 4, Math.Min(this.Drawing_Mouse_Loc.Y, this.Base_Drawing_Mouse_Loc.Y) * 16 /*0x10*/ * this.Zoom - 4), new Size((Math.Abs(this.Drawing_Mouse_Loc.X - this.Base_Drawing_Mouse_Loc.X) + 1) * 16 /*0x10*/ * this.Zoom + 8, (Math.Abs(this.Drawing_Mouse_Loc.Y - this.Base_Drawing_Mouse_Loc.Y) + 1) * 16 /*0x10*/ * this.Zoom + 8)));
          this.MapPicture.Invalidate(region);
        }
      }
    }
    else if (this.Selecting)
    {
      int x = Math.Max(0, Math.Min(this.Map_Width - 1, e.X / (16 /*0x10*/ * this.Zoom)));
      int y = Math.Max(0, Math.Min(this.Map_Height - 1, e.Y / (16 /*0x10*/ * this.Zoom)));
      Point drawingMouseLoc = this.Drawing_Mouse_Loc;
      this.Drawing_Mouse_Loc = new Point(x, y);
      int[,] numArray = new int[Math.Abs(this.Drawing_Mouse_Loc.X - this.Base_Drawing_Mouse_Loc.X) + 1, Math.Abs(this.Drawing_Mouse_Loc.Y - this.Base_Drawing_Mouse_Loc.Y) + 1];
      for (int index1 = 0; index1 < numArray.GetLength(1); ++index1)
      {
        for (int index2 = 0; index2 < numArray.GetLength(0); ++index2)
          numArray[index2, index1] = this.Map_Tiles[Math.Min(this.Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.X) + index2, Math.Min(this.Drawing_Mouse_Loc.Y, this.Base_Drawing_Mouse_Loc.Y) + index1];
      }
      this.Tileset_Palette.indices = numArray;
      using (Region region = new Region())
      {
        region.MakeEmpty();
        region.Union(new Rectangle(new Point(Math.Min(drawingMouseLoc.X, this.Base_Drawing_Mouse_Loc.X) * 16 /*0x10*/ * this.Zoom - 4, Math.Min(drawingMouseLoc.Y, this.Base_Drawing_Mouse_Loc.Y) * 16 /*0x10*/ * this.Zoom - 4), new Size((Math.Abs(drawingMouseLoc.X - this.Base_Drawing_Mouse_Loc.X) + numArray.GetLength(0)) * 16 /*0x10*/ * this.Zoom + 8, (Math.Abs(drawingMouseLoc.Y - this.Base_Drawing_Mouse_Loc.Y) + numArray.GetLength(1)) * 16 /*0x10*/ * this.Zoom + 8)));
        region.Union(new Rectangle(new Point(Math.Min(this.Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.X) * 16 /*0x10*/ * this.Zoom - 4, Math.Min(this.Drawing_Mouse_Loc.Y, this.Base_Drawing_Mouse_Loc.Y) * 16 /*0x10*/ * this.Zoom - 4), new Size((Math.Abs(this.Drawing_Mouse_Loc.X - this.Base_Drawing_Mouse_Loc.X) + numArray.GetLength(0)) * 16 /*0x10*/ * this.Zoom + 8, (Math.Abs(this.Drawing_Mouse_Loc.Y - this.Base_Drawing_Mouse_Loc.Y) + numArray.GetLength(1)) * 16 /*0x10*/ * this.Zoom + 8)));
        this.MapPicture.Invalidate(region);
      }
    }
    this.Drawing = false;
    this.Selecting = false;
  }

  private void MapPicture_MouseLeave(object sender, EventArgs e)
  {
    if (!this.ready_to_draw || this.Drawing && this.active_tool == Tools.Box)
      return;
    this.Drawing_Mouse_Loc = new Point(-1, -1);
    this.MapPicture.Invalidate();
  }

  protected bool mouse_draw(int base_x, int base_y, int[,] indices)
  {
    bool flag = false;
    for (int index1 = 0; index1 < indices.GetLength(1); ++index1)
    {
      for (int index2 = 0; index2 < indices.GetLength(0); ++index2)
      {
        if (!this.is_off_map(base_x + index2, base_y + index1) && this.Map_Tiles[base_x + index2, base_y + index1] != indices[index2, index1])
        {
          if (!flag)
            flag = true;
          this.draw_tile(base_x + index2, base_y + index1, indices[index2, index1]);
          if (this.Map_Tiles[base_x + index2, base_y + index1] != 0)
            this.Terrain_Types[base_x + index2, base_y + index1] = 0;
        }
      }
    }
    return flag;
  }

  private void mouse_box_fill(int base_x, int base_y, int target_x, int target_y, int[,] indices)
  {
    int[,] numArray = new int[this.Map_Tiles.GetLength(0), this.Map_Tiles.GetLength(1)];
    Array.Copy((Array) this.Map_Tiles, (Array) numArray, this.Map_Tiles.Length);
    this.box_fill<int>(numArray, base_x, base_y, target_x, target_y, indices[0, 0]);
    int num1 = Math.Max(0, Math.Min(base_x, target_x));
    int num2 = Math.Max(0, Math.Min(base_y, target_y));
    target_x = Math.Min(this.Map_Tiles.GetLength(0) - 1, Math.Max(base_x, target_x));
    target_y = Math.Min(this.Map_Tiles.GetLength(1) - 1, Math.Max(base_y, target_y));
    base_x = num1;
    base_y = num2;
    for (int y = base_y; y <= target_y; ++y)
    {
      for (int x = base_x; x <= target_x; ++x)
        this.draw_tile(x, y, numArray[x, y]);
    }
  }

  private void pin_box_fill(int base_x, int base_y, int target_x, int target_y, bool value)
  {
    bool[,] flagArray = new bool[this.Locked_Tiles.GetLength(0), this.Locked_Tiles.GetLength(1)];
    Array.Copy((Array) this.Locked_Tiles, (Array) flagArray, this.Locked_Tiles.Length);
    this.box_fill<bool>(flagArray, base_x, base_y, target_x, target_y, value);
    int num1 = Math.Max(0, Math.Min(base_x, target_x));
    int num2 = Math.Max(0, Math.Min(base_y, target_y));
    target_x = Math.Min(this.Locked_Tiles.GetLength(0) - 1, Math.Max(base_x, target_x));
    target_y = Math.Min(this.Locked_Tiles.GetLength(1) - 1, Math.Max(base_y, target_y));
    base_x = num1;
    base_y = num2;
    for (int index1 = base_y; index1 <= target_y; ++index1)
    {
      for (int index2 = base_x; index2 <= target_x; ++index2)
        this.Locked_Tiles[index2, index1] = value;
    }
  }

  private void terrain_box_fill(int base_x, int base_y, int target_x, int target_y, int value)
  {
    int[,] numArray = new int[this.Terrain_Types.GetLength(0), this.Terrain_Types.GetLength(1)];
    Array.Copy((Array) this.Terrain_Types, (Array) numArray, this.Terrain_Types.Length);
    this.box_fill<int>(numArray, base_x, base_y, target_x, target_y, value);
    int num1 = Math.Max(0, Math.Min(base_x, target_x));
    int num2 = Math.Max(0, Math.Min(base_y, target_y));
    target_x = Math.Min(this.Terrain_Types.GetLength(0) - 1, Math.Max(base_x, target_x));
    target_y = Math.Min(this.Terrain_Types.GetLength(1) - 1, Math.Max(base_y, target_y));
    base_x = num1;
    base_y = num2;
    for (int index1 = base_y; index1 <= target_y; ++index1)
    {
      for (int index2 = base_x; index2 <= target_x; ++index2)
        this.Terrain_Types[index2, index1] = numArray[index2, index1];
    }
  }

  private void box_fill<T>(
    T[,] array,
    int base_x,
    int base_y,
    int target_x,
    int target_y,
    T value)
  {
    bool flag1 = target_x >= base_x;
    bool flag2 = target_y >= base_y;
    int index1 = base_y;
    while (index1 >= 0 && index1 < array.GetLength(1) && (flag2 ? (index1 <= target_y ? 1 : 0) : (index1 >= target_y ? 1 : 0)) != 0)
    {
      int index2 = base_x;
      while (index2 >= 0 && index2 < array.GetLength(0) && (flag1 ? (index2 <= target_x ? 1 : 0) : (index2 >= target_x ? 1 : 0)) != 0)
      {
        array[index2, index1] = value;
        if (flag1)
          ++index2;
        else
          --index2;
      }
      if (flag2)
        ++index1;
      else
        --index1;
    }
  }

  private bool mouse_flood_fill(int base_x, int base_y, int[,] indices)
  {
    int[,] numArray = new int[this.Map_Tiles.GetLength(0), this.Map_Tiles.GetLength(1)];
    Array.Copy((Array) this.Map_Tiles, (Array) numArray, this.Map_Tiles.Length);
    bool flag = Flood_Fill.flood_fill<int>(this.Map_Tiles, numArray, base_x, base_y, indices[0, 0]);
    if (flag)
    {
      for (int y = 0; y < this.Map_Tiles.GetLength(1); ++y)
      {
        for (int x = 0; x < this.Map_Tiles.GetLength(0); ++x)
        {
          if (this.Map_Tiles[x, y] != numArray[x, y])
            this.draw_tile(x, y, numArray[x, y]);
        }
      }
    }
    return flag;
  }

  private bool pin_flood_fill(int base_x, int base_y, bool value)
  {
    bool[,] flagArray = new bool[this.Locked_Tiles.GetLength(0), this.Locked_Tiles.GetLength(1)];
    Array.Copy((Array) this.Locked_Tiles, (Array) flagArray, this.Locked_Tiles.Length);
    bool flag = Flood_Fill.flood_fill<bool>(this.Locked_Tiles, flagArray, base_x, base_y, value);
    if (flag)
    {
      for (int index1 = 0; index1 < this.Locked_Tiles.GetLength(1); ++index1)
      {
        for (int index2 = 0; index2 < this.Locked_Tiles.GetLength(0); ++index2)
        {
          if (this.Locked_Tiles[index2, index1] != flagArray[index2, index1])
            this.Locked_Tiles[index2, index1] = value;
        }
      }
    }
    return flag;
  }

  private bool terrain_flood_fill(int base_x, int base_y, int value)
  {
    int[,] numArray = new int[this.Terrain_Types.GetLength(0), this.Terrain_Types.GetLength(1)];
    Array.Copy((Array) this.Terrain_Types, (Array) numArray, this.Terrain_Types.Length);
    bool flag = Flood_Fill.flood_fill<int>(this.Terrain_Types, numArray, base_x, base_y, value);
    if (flag)
    {
      for (int index1 = 0; index1 < this.Terrain_Types.GetLength(1); ++index1)
      {
        for (int index2 = 0; index2 < this.Terrain_Types.GetLength(0); ++index2)
        {
          if (this.Terrain_Types[index2, index1] != numArray[index2, index1])
            this.Terrain_Types[index2, index1] = numArray[index2, index1];
        }
      }
    }
    return flag;
  }

  private void MapPicture_Paint(object sender, PaintEventArgs e)
  {
    e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
    e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
    e.Graphics.SmoothingMode = SmoothingMode.None;
    if (!this.ready_to_draw)
      return;
    bool paintingTerrain = this.painting_terrain;
    int[,] numArray1;
    if (this.active_tool == Tools.Box && this.Drawing && !this.pinning_tiles && !paintingTerrain)
    {
      numArray1 = new int[this.Map_Tiles.GetLength(0), this.Map_Tiles.GetLength(1)];
      Array.Copy((Array) this.Map_Tiles, (Array) numArray1, this.Map_Tiles.Length);
      this.box_fill<int>(numArray1, this.Base_Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.Y, this.Drawing_Mouse_Loc.X, this.Drawing_Mouse_Loc.Y, this.Tileset_Palette.indices[0, 0]);
    }
    else
      numArray1 = this.Map_Tiles;
    this.draw_map(e.Graphics, this.Zoom, numArray1);
    bool[,] flagArray;
    if (this.active_tool == Tools.Box && this.Drawing && this.pinning_tiles)
    {
      flagArray = new bool[this.Locked_Tiles.GetLength(0), this.Locked_Tiles.GetLength(1)];
      Array.Copy((Array) this.Locked_Tiles, (Array) flagArray, this.Locked_Tiles.Length);
      this.box_fill<bool>(flagArray, this.Base_Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.Y, this.Drawing_Mouse_Loc.X, this.Drawing_Mouse_Loc.Y, this.Box_Pin_Value);
    }
    else
      flagArray = this.Locked_Tiles;
    for (int index1 = 0; index1 < this.Map_Height; ++index1)
    {
      for (int index2 = 0; index2 < this.Map_Width; ++index2)
      {
        if (flagArray[index2, index1])
        {
          Rectangle rect = new Rectangle(new Point(index2 * this.Zoom * 16 /*0x10*/, index1 * this.Zoom * 16 /*0x10*/), new Size(this.Zoom * 16 /*0x10*/, this.Zoom * 16 /*0x10*/));
          if (e.Graphics.ClipBounds.IntersectsWith((RectangleF) rect))
          {
            using (Brush brush = (Brush) new SolidBrush(Color.FromArgb(this.pinning_tiles ? 64 /*0x40*/ : 16 /*0x10*/, (int) byte.MaxValue, 0, 0)))
              e.Graphics.FillRectangle(brush, rect);
          }
        }
      }
    }
    int[,] numArray2;
    if (this.active_tool == Tools.Box && this.Drawing && paintingTerrain)
    {
      numArray2 = new int[this.Terrain_Types.GetLength(0), this.Terrain_Types.GetLength(1)];
      Array.Copy((Array) this.Terrain_Types, (Array) numArray2, this.Terrain_Types.Length);
      this.box_fill<int>(numArray2, this.Base_Drawing_Mouse_Loc.X, this.Base_Drawing_Mouse_Loc.Y, this.Drawing_Mouse_Loc.X, this.Drawing_Mouse_Loc.Y, this.Box_Terrain_Value);
    }
    else
      numArray2 = this.Terrain_Types;
    if (paintingTerrain)
      e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
    for (int y = 0; y < this.Map_Height; ++y)
    {
      for (int x = 0; x < this.Map_Width; ++x)
        Terrain_Color_Data.draw(e.Graphics, x, y, numArray2[x, y], 16 /*0x10*/, paintingTerrain, zoom: this.Zoom);
    }
    if (paintingTerrain)
      e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
    Color color = !this.pinning_tiles ? (!paintingTerrain ? Color.White : Terrain_Color_Data.TERRAIN_COLORS[this.Terrain_Palette.active_terrain].Color1) : Color.FromArgb((int) byte.MaxValue, 200, 200);
    if (this.Drawing && this.active_tool == Tools.Box || this.Selecting)
    {
      int x = Math.Min(this.Base_Drawing_Mouse_Loc.X, this.Drawing_Mouse_Loc.X) * 16 /*0x10*/ * this.Zoom;
      int y = Math.Min(this.Base_Drawing_Mouse_Loc.Y, this.Drawing_Mouse_Loc.Y) * 16 /*0x10*/ * this.Zoom;
      int width = (Math.Abs(this.Base_Drawing_Mouse_Loc.X - this.Drawing_Mouse_Loc.X) + 1) * 16 /*0x10*/ * this.Zoom;
      int height = (Math.Abs(this.Base_Drawing_Mouse_Loc.Y - this.Drawing_Mouse_Loc.Y) + 1) * 16 /*0x10*/ * this.Zoom;
      using (Pen pen = new Pen(Brushes.Black, 4f))
        e.Graphics.DrawRectangles(pen, new Rectangle[1]
        {
          new Rectangle(x, y, width, height)
        });
      using (Pen pen = new Pen(color, 2f))
        e.Graphics.DrawRectangles(pen, new Rectangle[1]
        {
          new Rectangle(x, y, width, height)
        });
    }
    else
    {
      if (this.Drawing_Mouse_Loc.X < 0 || this.Drawing_Mouse_Loc.Y < 0)
        return;
      int num1 = 0;
      int num2 = 0;
      if (this.pinning_tiles || paintingTerrain)
      {
        num1 = 1;
        num2 = 1;
      }
      else if (this.Tileset_Palette.indices.Length > 0)
      {
        num1 = this.active_tool == Tools.Brush ? this.Tileset_Palette.indices.GetLength(0) : 1;
        num2 = this.active_tool == Tools.Brush ? this.Tileset_Palette.indices.GetLength(1) : 1;
      }
      if (num1 <= 0 || num2 <= 0)
        return;
      int x = this.Drawing_Mouse_Loc.X * 16 /*0x10*/ * this.Zoom;
      int y = this.Drawing_Mouse_Loc.Y * 16 /*0x10*/ * this.Zoom;
      int width = num1 * 16 /*0x10*/ * this.Zoom;
      int height = num2 * 16 /*0x10*/ * this.Zoom;
      using (Pen pen = new Pen(Brushes.Black, 4f))
        e.Graphics.DrawRectangles(pen, new Rectangle[1]
        {
          new Rectangle(x, y, width, height)
        });
      using (Pen pen = new Pen(color, 2f))
        e.Graphics.DrawRectangles(pen, new Rectangle[1]
        {
          new Rectangle(x, y, width, height)
        });
    }
  }

  private void saveMapAsToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Updating)
      return;
    this.try_save_map();
  }

  private bool try_save_map()
  {
    if (!string.IsNullOrEmpty(this.Map_Directory))
      this.SaveMapDialog.InitialDirectory = this.Map_Directory;
    if (this.Map_Tiles == null || this.no_tileset || this.SaveMapDialog.ShowDialog() != DialogResult.OK)
      return false;
    this.Map_Directory = Path.GetDirectoryName(this.SaveMapDialog.FileName);
    if ("*" + Path.GetExtension(this.SaveMapDialog.FileName) == this.SaveMapDialog.Filter.Split('|')[1])
      this.SaveMapDialog.FilterIndex = 1;
    else if ("*" + Path.GetExtension(this.SaveMapDialog.FileName) == this.SaveMapDialog.Filter.Split('|')[3])
      this.SaveMapDialog.FilterIndex = 2;
    else if ("*" + Path.GetExtension(this.SaveMapDialog.FileName) == this.SaveMapDialog.Filter.Split('|')[5])
      this.SaveMapDialog.FilterIndex = 3;
    if (this.SaveMapDialog.FilterIndex == 2)
    {
      int num1 = this.Tileset_Image == null ? ((IEnumerable) this.Map_Tiles).Cast<int>().Max<int>((Func<int, int>) (x => x)) + 1 : this.Tileset_Image.Width / 16 /*0x10*/ * (this.Tileset_Image.Height / 16 /*0x10*/);
      if (num1 > 1024 /*0x0400*/)
      {
        int num2 = (int) MessageBox.Show($"MAR files can only have 1024 unique tiles.\n(as far as I can tell)\nThe current {(this.Tileset_Image == null ? (object) "map" : (object) "tileset")} has {num1} {(num1 == 1 ? (object) "tile" : (object) "tiles")},\nso it can't be saved as a MAR.", "Tileset too large", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        return false;
      }
    }
    this.save_map(this.SaveMapDialog.FileName, this.SaveMapDialog.FilterIndex);
    this.Changed = false;
    return true;
  }

  public DialogResult save_check()
  {
    if (this.Map_Tiles == null || this.no_tileset || !this.Changed)
      return DialogResult.Yes;
    DialogResult dialogResult = MessageBox.Show(string.Format("Do you want to save changes to the map?"), "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
    return dialogResult == DialogResult.Yes && !this.try_save_map() ? DialogResult.Cancel : dialogResult;
  }

  private void loadMapToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Updating)
      return;
    switch (this.save_check())
    {
      case DialogResult.Cancel:
        return;
      case DialogResult.Yes:
        this.OpenMapDialog.FileName = "";
        break;
    }
    if (!string.IsNullOrEmpty(this.Map_Directory))
      this.OpenMapDialog.InitialDirectory = this.Map_Directory;
    if (this.OpenMapDialog.ShowDialog() != DialogResult.OK)
      return;
    this.Map_Directory = Path.GetDirectoryName(this.OpenMapDialog.FileName);
    if (this.OpenMapDialog.FilterIndex == 1 || this.OpenMapDialog.FilterIndex == 4 && Path.GetExtension(this.OpenMapDialog.FileName).ToLowerInvariant() == ".map")
    {
      int[,] map_tiles = this.load_text_map(this.OpenMapDialog.FileName);
      if (map_tiles != null)
        this.load_map(map_tiles);
    }
    else if (this.OpenMapDialog.FilterIndex == 3 || this.OpenMapDialog.FilterIndex == 4 && Path.GetExtension(this.OpenMapDialog.FileName).ToLowerInvariant() == ".tmx")
    {
      List<int[,]> numArrayList = this.read_xml_map(this.OpenMapDialog.FileName);
      if (numArrayList != null)
        this.load_map(numArrayList[0]);
    }
    else if (this.OpenMapDialog.FilterIndex == 2 || this.OpenMapDialog.FilterIndex == 4 && Path.GetExtension(this.OpenMapDialog.FileName).ToLowerInvariant() == ".mar")
    {
      long size;
      using (StreamReader streamReader = new StreamReader(this.OpenMapDialog.FileName))
      {
        if (streamReader.BaseStream.Length % 2L == 1L)
          return;
        size = streamReader.BaseStream.Length / 2L;
      }
      if (size > 1000000L && MessageBox.Show("This file is abnormally large for a map.\nAre you sure you want to load it?", "Large Filesize", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) != DialogResult.Yes)
        return;
      Mar_Import_Form marImportForm = new Mar_Import_Form((int) size, (int) this.WidthUpDown.Value);
      if (marImportForm.ShowDialog() == DialogResult.OK)
      {
        this.WidthUpDown.Value = (Decimal) marImportForm.map_width;
        this.HeightUpDown.Value = (Decimal) marImportForm.map_height;
        int[,] map_tiles = this.load_bytewise_map(this.OpenMapDialog.FileName);
        if (map_tiles != null)
          this.load_map(map_tiles);
      }
    }
    this.SaveMapDialog.InitialDirectory = this.OpenMapDialog.InitialDirectory = Path.GetDirectoryName(this.OpenMapDialog.FileName);
    this.OpenMapDialog.FileName = Path.GetFileName(this.OpenMapDialog.FileName);
    this.SaveMapDialog.FileName = this.OpenMapDialog.FileName;
  }

  private void loadTilesetToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Updating || !this.load_tileset())
      return;
    using (new Bitmap(this.LoadTilesetDialog.FileName))
      this.load_tileset_image();
  }

  private void saveTilesetSettingsToolStripMenuItem_Click(object sender, EventArgs e)
  {
    this.save_tilesets();
  }

  private void importMapImageToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Updating || this.save_check() == DialogResult.Cancel || this.MapImageImportDialog.ShowDialog() != DialogResult.OK)
      return;
    using (Bitmap bmp = new Bitmap(this.MapImageImportDialog.FileName))
    {
      if (bmp != null && bmp.Width >= 16 /*0x10*/ && bmp.Height >= 16 /*0x10*/)
      {
        this.Updating = true;
        this.import_image_map(bmp);
        this.Updating = false;
      }
      else
      {
        int num = (int) MessageBox.Show("File is too small or not an image", "Invalid image", MessageBoxButtons.OK, MessageBoxIcon.Hand);
      }
    }
  }

  private void importClipboardImageToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Updating || this.save_check() == DialogResult.Cancel)
      return;
    if (Clipboard.ContainsImage())
    {
      using (Bitmap image = Clipboard.GetImage() as Bitmap)
      {
        if (image != null && image.Width >= 16 /*0x10*/ && image.Height >= 16 /*0x10*/)
        {
          this.Updating = true;
          this.import_image_map(image);
          this.Updating = false;
        }
        else
        {
          int num = (int) MessageBox.Show("File is too small or not an image", "Invalid image", MessageBoxButtons.OK, MessageBoxIcon.Hand);
        }
      }
    }
    else
    {
      int num1 = (int) MessageBox.Show("Clipboard does not contain image data", "Invalid image", MessageBoxButtons.OK, MessageBoxIcon.Hand);
    }
  }

  private void exitToolStripMenuItem_Click(object sender, EventArgs e) => this.Close();

  private void editToolStripMenuItem_Click(object sender, EventArgs e)
  {
    this.resizeMapToolStripMenuItem.Enabled = this.Map_Tiles != null;
    this.copyMapImageToolStripMenuItem.Enabled = this.Map_Tiles != null && this.Tileset_Image != null;
    this.clearPinnedTilesToolStripMenuItem.Enabled = this.Map_Tiles != null && this.Tileset_Image != null;
  }

  private void undoToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Updating)
      return;
    this.undo();
    this.refresh_panel_size();
    this.MapPicture.Invalidate();
  }

  private void resizeMapToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Updating)
      return;
    Map_Resize_Form mapResizeForm = new Map_Resize_Form(this.Map_Width, this.Map_Height);
    if (mapResizeForm.ShowDialog() != DialogResult.OK)
      return;
    this.resize_map(mapResizeForm.left, mapResizeForm.up, mapResizeForm.right, mapResizeForm.down);
  }

  private void copyMapImageToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (!this.ready_to_draw)
      return;
    using (Bitmap bitmap = new Bitmap(this.Map_Width * 16 /*0x10*/, this.Map_Height * 16 /*0x10*/))
    {
      using (Graphics g = Graphics.FromImage((Image) bitmap))
      {
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        this.draw_map(g);
      }
      Clipboard.SetImage((Image) bitmap);
    }
  }

  private void clearPinnedTilesToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Updating)
      return;
    if (this.Locked_Tiles != null)
    {
      for (int index1 = 0; index1 < this.Locked_Tiles.GetLength(1); ++index1)
      {
        for (int index2 = 0; index2 < this.Locked_Tiles.GetLength(0); ++index2)
          this.Locked_Tiles[index2, index1] = false;
      }
    }
    this.MapPicture.Invalidate();
  }

  private void clearTerrainTagsToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Updating)
      return;
    if (this.Terrain_Types != null)
    {
      for (int index1 = 0; index1 < this.Terrain_Types.GetLength(1); ++index1)
      {
        for (int index2 = 0; index2 < this.Terrain_Types.GetLength(0); ++index2)
          this.Terrain_Types[index2, index1] = 0;
      }
    }
    this.MapPicture.Invalidate();
  }

  private void prepareTilesetForEditsToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Updating)
      return;
    this.prepareTilesetForEditsToolStripMenuItem.Enabled = false;
    this.update_tileset_same_corners();
    if (this.Tileset_Generator_Data == null)
    {
      this.Tileset_Generator_Data = new Tileset_Generation_Data(this.Tileset_Image.Width / 16 /*0x10*/ * (this.Tileset_Image.Height / 16 /*0x10*/), this.Tile_Matches);
      this.generateMapToolStripMenuItem.Enabled = true;
      this.repairMapToolStripMenuItem.Enabled = true;
    }
    else if (!this.Tileset_Generator_Data.fix_identical(this.Tile_Matches.identical_tiles))
    {
      int num1 = (int) MessageBox.Show("Matching tile data is inconsistent with the tileset\r\nimage, indicating the tileset image has been changed.\r\nAn automated attempt to account for the differences has\r\nbeen made, but the data should be reviewed manually.", "Tileset Changed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
    }
    int num2 = (int) MessageBox.Show("Tileset ready", "Ready", MessageBoxButtons.OK);
    this.progressBar1.Value = 0;
    this.progressBar1.Visible = false;
    this.Tileset_Palette.refresh_edit_ready();
    this.prepareTilesetForEditsToolStripMenuItem.Enabled = true;
  }

  private void generateMapToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (!(this.WidthUpDown.Value > 0M) || !(this.HeightUpDown.Value > 0M) || this.Updating)
      return;
    Map_Generation_Algorithm algorithm = this.selected_generation_algorithm;
    if (algorithm != Map_Generation_Algorithm.Legacy)
    {
      int width = (int) this.WidthUpDown.Value;
      int height = (int) this.HeightUpDown.Value;
      int depth = (int) this.DepthUpDown.Value;
      int experimental_zoom = this.Zoom;
      bool enable_branch_arc_consistency =
        this.experimentalBranchArcConsistencyToolStripMenuItem.Checked;
      Map_State state = this.create_experimental_generation_state(width, height);
      CancellationToken experimental_cancellation = this.start_map_operation(
        algorithm == Map_Generation_Algorithm.Experimental_Hybrid
          ? "Generating map (hybrid)..."
          : "Generating map (experimental)...");
      new Thread(() => this.generate_map_experimental(
        state,
        depth,
        experimental_zoom,
        experimental_cancellation,
        algorithm,
        enable_branch_arc_consistency)).Start();
      return;
    }
    this.Text = string.Format("Map Editor - New Map");
    this.Map_Width = (int) this.WidthUpDown.Value;
    this.Map_Height = (int) this.HeightUpDown.Value;
    this.refresh_panel_size();
    if (this.Map_Tiles == null)
      this.Undo_Map_Tiles = new int[this.Map_Width, this.Map_Height];
    else
      this.copy_map_to_undo();
    int[,] mapTiles = this.Map_Tiles;
    this.Map_Tiles = new int[this.Map_Width, this.Map_Height];
    this.Drawn_Tiles = new bool[this.Map_Width, this.Map_Height];
    this.Open_Tiles = new HashSet<int>();
    if (this.Locked_Tiles == null || this.Map_Width != this.Locked_Tiles.GetLength(0) || this.Map_Height != this.Locked_Tiles.GetLength(1))
    {
      this.Locked_Tiles = new bool[this.Map_Width, this.Map_Height];
    }
    else
    {
      for (int y = 0; y < this.Map_Height; ++y)
      {
        for (int x = 0; x < this.Map_Width; ++x)
        {
          if (this.Locked_Tiles[x, y])
          {
            this.Map_Tiles[x, y] = mapTiles[x, y];
            this.Drawn_Tiles[x, y] = true;
          }
        }
      }
    }
    if (this.Terrain_Types == null || this.Map_Width != this.Terrain_Types.GetLength(0) || this.Map_Height != this.Terrain_Types.GetLength(1))
      this.Terrain_Types = new int[this.Map_Width, this.Map_Height];
    this.MapPicture.Invalidate();
    int zoom = this.Zoom;
    CancellationToken cancellation_token = this.start_map_operation("Generating map...");
    new Thread(() => this.generate_map(zoom, cancellation_token)).Start();
  }

  private void repairMapToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.Map_Tiles == null || this.Updating)
      return;
    HashSet<Point> pointSet = new HashSet<Point>();
    for (int y = 0; y < this.Map_Height; ++y)
    {
      for (int x = 0; x < this.Map_Width; ++x)
      {
        bool repair_candidate = this.Map_Tiles[x, y] == 0;
        if (!repair_candidate && !this.Locked_Tiles[x, y] && this.Terrain_Types[x, y] != 0 && this.tileset_data != null && this.tileset_data.Terrain_Tags.Count > this.Map_Tiles[x, y])
          repair_candidate = this.Terrain_Types[x, y] > 0 ? this.tileset_data.Terrain_Tags[this.Map_Tiles[x, y]] != this.Terrain_Types[x, y] : this.tileset_data.Terrain_Tags[this.Map_Tiles[x, y]] == -this.Terrain_Types[x, y];
        if (repair_candidate)
          pointSet.Add(new Point(x, y));
      }
    }
    if (pointSet.Count == 0)
    {
      this.StatusbarSpacerLabel.Text = "Nothing to repair.";
      MessageBox.Show(this, "Nothing to repair.", "Map Repair", MessageBoxButtons.OK, MessageBoxIcon.Information);
      return;
    }
    lock (this.MapPicture)
      this.refresh_panel_size();
    this.refresh_panel_size();
    int depth = (int) this.DepthUpDown.Value;
    int radius = (int) this.DistUpDown.Value;
    Map_Generation_Algorithm algorithm = this.selected_generation_algorithm;
    int zoom = this.Zoom;
    if (algorithm != Map_Generation_Algorithm.Legacy)
    {
      bool enable_branch_arc_consistency =
        this.experimentalBranchArcConsistencyToolStripMenuItem.Checked;
      Map_State state = algorithm == Map_Generation_Algorithm.Experimental_Hybrid
        ? this.create_hybrid_repair_state()
        : this.create_experimental_repair_state();
      CancellationToken experimental_cancellation = this.start_map_operation(
        algorithm == Map_Generation_Algorithm.Experimental_Hybrid
          ? "Repairing map (hybrid)..."
          : "Repairing map (experimental)...");
      new Thread((ThreadStart) (() => this.repair_map_experimental(
        state,
        depth,
        radius,
        zoom,
        experimental_cancellation,
        algorithm,
        enable_branch_arc_consistency)))
      {
        IsBackground = false
      }.Start();
      return;
    }
    this.copy_map_to_undo();
    CancellationToken cancellation_token = this.start_map_operation("Repairing map...");
    new Thread((ThreadStart) (() => this.repair_map(depth, radius, zoom, cancellation_token)))
    {
      IsBackground = false
    }.Start();
  }

  private Map_Generation_Algorithm selected_generation_algorithm
  {
    get
    {
      if (this.hybridSolverToolStripMenuItem.Checked)
        return Map_Generation_Algorithm.Experimental_Hybrid;
      return this.experimentalConstraintSolverToolStripMenuItem.Checked
        ? Map_Generation_Algorithm.Experimental_Constraint
        : Map_Generation_Algorithm.Legacy;
    }
  }

  private void experimentalConstraintSolverToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.experimentalConstraintSolverToolStripMenuItem.Checked)
      this.hybridSolverToolStripMenuItem.Checked = false;
    this.update_experimental_settings_enabled();
  }

  private void hybridSolverToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.hybridSolverToolStripMenuItem.Checked)
      this.experimentalConstraintSolverToolStripMenuItem.Checked = false;
    this.update_experimental_settings_enabled();
  }

  private void update_experimental_settings_enabled()
  {
    this.experimentalBranchArcConsistencyToolStripMenuItem.Enabled =
      this.experimentalConstraintSolverToolStripMenuItem.Checked
        || this.hybridSolverToolStripMenuItem.Checked;
  }

  private void CancelOperationButton_Click(object sender, EventArgs e)
  {
    if (this.Map_Generation_Cancellation == null || this.Map_Generation_Cancellation.IsCancellationRequested)
      return;
    this.Map_Generation_Cancellation.Cancel();
    this.CancelOperationButton.Enabled = false;
    this.StatusbarSpacerLabel.Text = "Cancelling...";
  }

  private void convertMapToTerrainTagsToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (!this.ready_to_draw)
      return;
    int[] terrainTypes = this.get_terrain_types();
    if (terrainTypes == null)
      return;
    for (int index1 = 0; index1 < this.Map_Height; ++index1)
    {
      for (int index2 = 0; index2 < this.Map_Width; ++index2)
        this.Terrain_Types[index2, index1] = terrainTypes[this.Map_Tiles[index2, index1]];
    }
    this.MapPicture.Invalidate();
  }

  internal int[] get_terrain_types()
  {
    if (this.tileset_data == null)
      return (int[]) null;
    int[] terrainTypes = new int[this.tileset_data.Terrain_Tags.Count];
    for (int index = 0; index < terrainTypes.Length; ++index)
      terrainTypes[index] = this.get_terrain_type(index, this.tileset_data);
    return terrainTypes;
  }

  private int get_terrain_type(int index, Data_Tileset tileset_data)
  {
    return index < tileset_data.Terrain_Tags.Count && Terrain_Color_Data.TERRAIN_COLORS.ContainsKey(tileset_data.Terrain_Tags[index]) ? tileset_data.Terrain_Tags[index] : 0;
  }

  private void copyTerrainTagsImageToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (this.tileset_data == null || !this.ready_to_draw)
      return;
    int[] terrainTypes = this.get_terrain_types();
    using (Bitmap bitmap = new Bitmap(this.Map_Width, this.Map_Height))
    {
      for (int y = 0; y < this.Map_Height; ++y)
      {
        for (int x = 0; x < this.Map_Width; ++x)
        {
          if (this.Terrain_Types[x, y] != 0)
            bitmap.SetPixel(x, y, Terrain_Color_Data.TERRAIN_COLORS[this.Terrain_Types[x, y]].avg_color);
          else
            bitmap.SetPixel(x, y, Terrain_Color_Data.TERRAIN_COLORS[terrainTypes[this.Map_Tiles[x, y]]].avg_color);
        }
      }
      Clipboard.SetImage((Image) bitmap);
    }
  }

  private void importTerrainTagsFromClipboardImageToolStripMenuItem_Click(
    object sender,
    EventArgs e)
  {
    if (!this.ready_to_draw)
      return;
    if (!Clipboard.ContainsImage())
    {
      int num1 = (int) MessageBox.Show("The clipboard does not contain an image.", "No image found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
    }
    else
    {
      using (Bitmap image = Clipboard.GetImage() as Bitmap)
      {
        if (image == null)
        {
          int num2 = (int) MessageBox.Show("The clipboard does not contain an image.", "No image found", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
        if (image.Width < this.Map_Width || image.Height < this.Map_Height || image.Width % this.Map_Width != 0 || image.Height % this.Map_Height != 0 || image.Width / this.Map_Width != image.Height / this.Map_Height)
        {
          int num3 = (int) MessageBox.Show("The image does not match the size of the map.", "Invalid image", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }
        else
        {
          int num4 = image.Width / this.Map_Width;
          for (int index1 = 0; index1 < this.Map_Height; ++index1)
          {
            for (int index2 = 0; index2 < this.Map_Width; ++index2)
            {
              Color color = FE_Map_Creator_Form.rms_color(image, new Rectangle(index2 * num4, index1 * num4, num4, num4));
              Dictionary<int, int> differences = Terrain_Color_Data.TERRAIN_COLORS.Where<KeyValuePair<int, Terrain_Color_Data>>((Func<KeyValuePair<int, Terrain_Color_Data>, bool>) (terrain => !this.Blocked_Terrain_Types.Contains(terrain.Key))).Select<KeyValuePair<int, Terrain_Color_Data>, KeyValuePair<int, int>>((Func<KeyValuePair<int, Terrain_Color_Data>, KeyValuePair<int, int>>) (terrain =>
              {
                int num5 = (int) Math.Sqrt(Math.Pow((double) ((int) color.R - (int) terrain.Value.avg_color.R), 2.0) + Math.Pow((double) ((int) color.G - (int) terrain.Value.avg_color.G), 2.0) + Math.Pow((double) ((int) color.B - (int) terrain.Value.avg_color.B), 2.0));
                return new KeyValuePair<int, int>(terrain.Key, num5);
              })).ToDictionary<KeyValuePair<int, int>, int, int>((Func<KeyValuePair<int, int>, int>) (p => p.Key), (Func<KeyValuePair<int, int>, int>) (p => p.Value));
              this.Terrain_Types[index2, index1] = differences.First<KeyValuePair<int, int>>((Func<KeyValuePair<int, int>, bool>) (terrain => terrain.Value == differences.Values.Min())).Key;
            }
          }
          this.DrawingModeComboBox.SelectedIndex = 1;
          this.MapPicture.Invalidate();
        }
      }
    }
  }

  private void processTilesetFromMapsToolStripMenuItem_Click(object sender, EventArgs e)
  {
    if (!this.load_tileset())
      return;
    using (new Bitmap(this.LoadTilesetDialog.FileName))
      this.load_tileset_image();
    if (this.Tileset_Image == null)
      return;
    this.update_tileset_same_corners();
    this.progressBar1.Value = 0;
    this.Tileset_Palette.refresh_edit_ready();
    if (string.IsNullOrEmpty(this.folderBrowserDialog1.SelectedPath))
      this.folderBrowserDialog1.SelectedPath = Path.GetDirectoryName(this.LoadTilesetDialog.FileName);
    if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK && this.setup_tileset(this.Tileset_Name, this.folderBrowserDialog1.SelectedPath))
    {
      int num = (int) MessageBox.Show("Tileset ready", "Ready", MessageBoxButtons.OK);
      this.progressBar1.Value = 0;
    }
    this.progressBar1.Visible = false;
  }

  private void Tileset_Palette_FormClosing(object sender, FormClosingEventArgs e)
  {
    if (this.close_checking())
      return;
    e.Cancel = true;
  }

  private void Terrain_Palette_FormClosing(object sender, FormClosingEventArgs e)
  {
    if (this.close_checking())
      return;
    e.Cancel = true;
  }

  private bool close_checking()
  {
    if (this.Updating)
      Thread.Sleep(1000);
    if (this.Updating || this.save_check() == DialogResult.Cancel)
      return false;
    this.Changed = false;
    Application.Exit();
    return true;
  }

  private void Tileset_Palette_Tile_Selected(object sender, EventArgs e)
  {
    this.DrawingModeComboBox.SelectedIndex = 0;
  }

  private void Terrain_Palette_Terrain_Selected(object sender, EventArgs e)
  {
    this.DrawingModeComboBox.SelectedIndex = 1;
  }

  private void DrawingModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
  {
    this.MapPicture.Invalidate();
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing)
    {
      if (this.Map_Generation_Cancellation != null)
      {
        this.Map_Generation_Cancellation.Cancel();
        this.Map_Generation_Cancellation.Dispose();
        this.Map_Generation_Cancellation = null;
      }
      if (this.components != null)
        this.components.Dispose();
    }
    base.Dispose(disposing);
  }

  private void InitializeComponent()
  {
    this.components = (IContainer) new System.ComponentModel.Container();
    this.tableLayoutPanel1 = new TableLayoutPanel();
    this.flowLayoutPanel1 = new FlowLayoutPanel();
    this.WidthLabel = new Label();
    this.WidthUpDown = new NumericUpDown();
    this.HeightLabel = new Label();
    this.HeightUpDown = new NumericUpDown();
    this.NewMapButton = new Button();
    this.label1 = new Label();
    this.DepthUpDown = new NumericUpDown();
    this.label2 = new Label();
    this.DistUpDown = new NumericUpDown();
    this.CancelOperationButton = new Button();
    this.PictureBoxPanel = new Panel();
    this.MapPicture = new PictureBox();
    this.ToolsFlowPanel = new FlowLayoutPanel();
    this.BrushRadioButton = new RadioButton();
    this.BoxRadioButton = new RadioButton();
    this.FloodFillRadioButton = new RadioButton();
    this.DrawingModeComboBox = new ComboBox();
    this.label3 = new Label();
    this.ZoomUpDown = new NumericUpDown();
    this.MapImageImportDialog = new OpenFileDialog();
    this.fileToolStripMenuItem = new ToolStripMenuItem();
    this.saveMapAsToolStripMenuItem = new ToolStripMenuItem();
    this.loadMapToolStripMenuItem = new ToolStripMenuItem();
    this.loadTilesetToolStripMenuItem = new ToolStripMenuItem();
    this.saveTilesetSettingsToolStripMenuItem = new ToolStripMenuItem();
    this.importMapImageToolStripMenuItem = new ToolStripMenuItem();
    this.importClipboardImageToolStripMenuItem = new ToolStripMenuItem();
    this.exitToolStripMenuItem = new ToolStripMenuItem();
    this.menuStrip1 = new MenuStrip();
    this.editToolStripMenuItem = new ToolStripMenuItem();
    this.undoToolStripMenuItem = new ToolStripMenuItem();
    this.resizeMapToolStripMenuItem = new ToolStripMenuItem();
    this.copyMapImageToolStripMenuItem = new ToolStripMenuItem();
    this.toolStripMenuItem1 = new ToolStripSeparator();
    this.clearPinnedTilesToolStripMenuItem = new ToolStripMenuItem();
    this.clearTerrainTagsToolStripMenuItem = new ToolStripMenuItem();
    this.mapGenerationToolStripMenuItem = new ToolStripMenuItem();
    this.prepareTilesetForEditsToolStripMenuItem = new ToolStripMenuItem();
    this.generateMapToolStripMenuItem = new ToolStripMenuItem();
    this.repairMapToolStripMenuItem = new ToolStripMenuItem();
    this.experimentalConstraintSolverToolStripMenuItem = new ToolStripMenuItem();
    this.hybridSolverToolStripMenuItem = new ToolStripMenuItem();
    this.experimentalBranchArcConsistencyToolStripMenuItem = new ToolStripMenuItem();
    this.convertMapToTerrainTagsToolStripMenuItem = new ToolStripMenuItem();
    this.copyTerrainTagsImageToolStripMenuItem = new ToolStripMenuItem();
    this.importTerrainTagsFromClipboardImageToolStripMenuItem = new ToolStripMenuItem();
    this.toolStripMenuItem2 = new ToolStripSeparator();
    this.processTilesetFromMapsToolStripMenuItem = new ToolStripMenuItem();
    this.MainPanel = new Panel();
    this.SaveMapDialog = new SaveFileDialog();
    this.OpenMapDialog = new OpenFileDialog();
    this.LoadTilesetDialog = new OpenFileDialog();
    this.folderBrowserDialog1 = new FolderBrowserDialog();
    this.statusStrip1 = new StatusStrip();
    this.StatusbarSpacerLabel = new ToolStripStatusLabel();
    this.progressBar1 = new ToolStripProgressBar();
    this.toolTip1 = new ToolTip(this.components);
    this.CursorPositionStatusLabel = new ToolStripStatusLabel();
    this.tableLayoutPanel1.SuspendLayout();
    this.flowLayoutPanel1.SuspendLayout();
    this.WidthUpDown.BeginInit();
    this.HeightUpDown.BeginInit();
    this.DepthUpDown.BeginInit();
    this.DistUpDown.BeginInit();
    this.PictureBoxPanel.SuspendLayout();
    ((ISupportInitialize) this.MapPicture).BeginInit();
    this.ToolsFlowPanel.SuspendLayout();
    this.ZoomUpDown.BeginInit();
    this.menuStrip1.SuspendLayout();
    this.MainPanel.SuspendLayout();
    this.statusStrip1.SuspendLayout();
    this.SuspendLayout();
    this.tableLayoutPanel1.ColumnCount = 1;
    this.tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
    this.tableLayoutPanel1.Controls.Add((Control) this.flowLayoutPanel1, 0, 2);
    this.tableLayoutPanel1.Controls.Add((Control) this.PictureBoxPanel, 0, 1);
    this.tableLayoutPanel1.Controls.Add((Control) this.ToolsFlowPanel, 0, 0);
    this.tableLayoutPanel1.Dock = DockStyle.Fill;
    this.tableLayoutPanel1.Location = new Point(0, 0);
    this.tableLayoutPanel1.Name = "tableLayoutPanel1";
    this.tableLayoutPanel1.RowCount = 3;
    this.tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));
    this.tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
    this.tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
    this.tableLayoutPanel1.Size = new Size(487, 440);
    this.tableLayoutPanel1.TabIndex = 0;
    this.flowLayoutPanel1.Controls.Add((Control) this.WidthLabel);
    this.flowLayoutPanel1.Controls.Add((Control) this.WidthUpDown);
    this.flowLayoutPanel1.Controls.Add((Control) this.HeightLabel);
    this.flowLayoutPanel1.Controls.Add((Control) this.HeightUpDown);
    this.flowLayoutPanel1.Controls.Add((Control) this.NewMapButton);
    this.flowLayoutPanel1.Controls.Add((Control) this.label1);
    this.flowLayoutPanel1.Controls.Add((Control) this.DepthUpDown);
    this.flowLayoutPanel1.Controls.Add((Control) this.label2);
    this.flowLayoutPanel1.Controls.Add((Control) this.DistUpDown);
    this.flowLayoutPanel1.Controls.Add((Control) this.CancelOperationButton);
    this.flowLayoutPanel1.Dock = DockStyle.Fill;
    this.flowLayoutPanel1.Location = new Point(3, 379);
    this.flowLayoutPanel1.Name = "flowLayoutPanel1";
    this.flowLayoutPanel1.Size = new Size(481, 58);
    this.flowLayoutPanel1.TabIndex = 1;
    this.WidthLabel.AutoSize = true;
    this.WidthLabel.Location = new Point(3, 6);
    this.WidthLabel.Margin = new Padding(3, 6, 3, 0);
    this.WidthLabel.Name = "WidthLabel";
    this.WidthLabel.Size = new Size(38, 13);
    this.WidthLabel.TabIndex = 14;
    this.WidthLabel.Text = "Width:";
    this.WidthUpDown.Location = new Point(47, 3);
    this.WidthUpDown.Maximum = new Decimal(new int[4]
    {
      9999,
      0,
      0,
      0
    });
    this.WidthUpDown.Minimum = new Decimal(new int[4]
    {
      1,
      0,
      0,
      0
    });
    this.WidthUpDown.Name = "WidthUpDown";
    this.WidthUpDown.Size = new Size(48 /*0x30*/, 20);
    this.WidthUpDown.TabIndex = 4;
    this.WidthUpDown.Value = new Decimal(new int[4]
    {
      15,
      0,
      0,
      0
    });
    this.HeightLabel.AutoSize = true;
    this.HeightLabel.Location = new Point(101, 6);
    this.HeightLabel.Margin = new Padding(3, 6, 3, 0);
    this.HeightLabel.Name = "HeightLabel";
    this.HeightLabel.Size = new Size(41, 13);
    this.HeightLabel.TabIndex = 15;
    this.HeightLabel.Text = "Height:";
    this.HeightUpDown.Location = new Point(148, 3);
    this.HeightUpDown.Maximum = new Decimal(new int[4]
    {
      9999,
      0,
      0,
      0
    });
    this.HeightUpDown.Minimum = new Decimal(new int[4]
    {
      1,
      0,
      0,
      0
    });
    this.HeightUpDown.Name = "HeightUpDown";
    this.HeightUpDown.Size = new Size(48 /*0x30*/, 20);
    this.HeightUpDown.TabIndex = 5;
    this.HeightUpDown.Value = new Decimal(new int[4]
    {
      10,
      0,
      0,
      0
    });
    this.flowLayoutPanel1.SetFlowBreak((Control) this.NewMapButton, true);
    this.NewMapButton.Location = new Point(202, 3);
    this.NewMapButton.Name = "NewMapButton";
    this.NewMapButton.Size = new Size(75, 23);
    this.NewMapButton.TabIndex = 13;
    this.NewMapButton.Text = "New Map";
    this.toolTip1.SetToolTip((Control) this.NewMapButton, "Creates a blank map with the chosen width\r\nand height. A tileset must be loaded first.");
    this.NewMapButton.UseVisualStyleBackColor = true;
    this.NewMapButton.Click += new EventHandler(this.NewMapButton_Click);
    this.label1.AutoSize = true;
    this.label1.Location = new Point(3, 35);
    this.label1.Margin = new Padding(3, 6, 3, 0);
    this.label1.Name = "label1";
    this.label1.Size = new Size(39, 13);
    this.label1.TabIndex = 11;
    this.label1.Text = "Depth:";
    this.toolTip1.SetToolTip((Control) this.label1, "Distance around tiles to\r\nlook when generating a\r\nmap. Map generation is\r\nslower at higher values.");
    this.DepthUpDown.Location = new Point(48 /*0x30*/, 32 /*0x20*/);
    this.DepthUpDown.Maximum = new Decimal(new int[4]
    {
      2,
      0,
      0,
      0
    });
    this.DepthUpDown.Minimum = new Decimal(new int[4]
    {
      1,
      0,
      0,
      0
    });
    this.DepthUpDown.Name = "DepthUpDown";
    this.DepthUpDown.Size = new Size(29, 20);
    this.DepthUpDown.TabIndex = 8;
    this.toolTip1.SetToolTip((Control) this.DepthUpDown, "Distance around tiles to\r\nlook when generating a\r\nmap. Map generation is\r\nslower at higher values.");
    this.DepthUpDown.Value = new Decimal(new int[4]
    {
      1,
      0,
      0,
      0
    });
    this.label2.AutoSize = true;
    this.label2.Location = new Point(83, 35);
    this.label2.Margin = new Padding(3, 6, 3, 0);
    this.label2.Name = "label2";
    this.label2.Size = new Size(77, 13);
    this.label2.TabIndex = 12;
    this.label2.Text = "Repair Radius:";
    this.toolTip1.SetToolTip((Control) this.label2, "Distance around tiles to\r\nexpand blank tiles when\r\nrepairing, to try and redo\r\nerror sections of the map.");
    this.DistUpDown.Location = new Point(166, 32 /*0x20*/);
    this.DistUpDown.Name = "DistUpDown";
    this.DistUpDown.Size = new Size(37, 20);
    this.DistUpDown.TabIndex = 9;
    this.toolTip1.SetToolTip((Control) this.DistUpDown, "Distance around tiles to\r\nexpand blank tiles when\r\nrepairing, to try and redo\r\nerror sections of the map.");
    this.DistUpDown.Value = new Decimal(new int[4]
    {
      1,
      0,
      0,
      0
    });
    this.CancelOperationButton.Enabled = false;
    this.CancelOperationButton.Location = new Point(209, 32 /*0x20*/);
    this.CancelOperationButton.Name = "CancelOperationButton";
    this.CancelOperationButton.Size = new Size(75, 23);
    this.CancelOperationButton.TabIndex = 16 /*0x10*/;
    this.CancelOperationButton.Text = "Cancel";
    this.toolTip1.SetToolTip((Control) this.CancelOperationButton, "Cancels the active map generation or repair operation.");
    this.CancelOperationButton.UseVisualStyleBackColor = true;
    this.CancelOperationButton.Click += new EventHandler(this.CancelOperationButton_Click);
    this.PictureBoxPanel.AutoScroll = true;
    this.PictureBoxPanel.Controls.Add((Control) this.MapPicture);
    this.PictureBoxPanel.Dock = DockStyle.Fill;
    this.PictureBoxPanel.Location = new Point(3, 35);
    this.PictureBoxPanel.Name = "PictureBoxPanel";
    this.PictureBoxPanel.Size = new Size(481, 338);
    this.PictureBoxPanel.TabIndex = 15;
    this.MapPicture.Location = new Point(0, 0);
    this.MapPicture.Name = "MapPicture";
    this.MapPicture.Size = new Size(225, 220);
    this.MapPicture.SizeMode = PictureBoxSizeMode.StretchImage;
    this.MapPicture.TabIndex = 0;
    this.MapPicture.TabStop = false;
    this.ToolsFlowPanel.Controls.Add((Control) this.BrushRadioButton);
    this.ToolsFlowPanel.Controls.Add((Control) this.BoxRadioButton);
    this.ToolsFlowPanel.Controls.Add((Control) this.FloodFillRadioButton);
    this.ToolsFlowPanel.Controls.Add((Control) this.DrawingModeComboBox);
    this.ToolsFlowPanel.Controls.Add((Control) this.label3);
    this.ToolsFlowPanel.Controls.Add((Control) this.ZoomUpDown);
    this.ToolsFlowPanel.Dock = DockStyle.Fill;
    this.ToolsFlowPanel.Location = new Point(3, 0);
    this.ToolsFlowPanel.Margin = new Padding(3, 0, 3, 0);
    this.ToolsFlowPanel.Name = "ToolsFlowPanel";
    this.ToolsFlowPanel.Size = new Size(481, 32 /*0x20*/);
    this.ToolsFlowPanel.TabIndex = 16 /*0x10*/;
    this.BrushRadioButton.Appearance = Appearance.Button;
    this.BrushRadioButton.AutoSize = true;
    this.BrushRadioButton.Location = new Point(0, 0);
    this.BrushRadioButton.Margin = new Padding(0);
    this.BrushRadioButton.MinimumSize = new Size(30, 30);
    this.BrushRadioButton.Name = "BrushRadioButton";
    this.BrushRadioButton.Size = new Size(43, 30);
    this.BrushRadioButton.TabIndex = 1;
    this.BrushRadioButton.Text = "brush";
    this.BrushRadioButton.UseVisualStyleBackColor = true;
    this.BoxRadioButton.Appearance = Appearance.Button;
    this.BoxRadioButton.AutoSize = true;
    this.BoxRadioButton.Location = new Point(43, 0);
    this.BoxRadioButton.Margin = new Padding(0);
    this.BoxRadioButton.MinimumSize = new Size(30, 30);
    this.BoxRadioButton.Name = "BoxRadioButton";
    this.BoxRadioButton.Size = new Size(34, 30);
    this.BoxRadioButton.TabIndex = 2;
    this.BoxRadioButton.Text = "box";
    this.BoxRadioButton.UseVisualStyleBackColor = true;
    this.FloodFillRadioButton.Appearance = Appearance.Button;
    this.FloodFillRadioButton.AutoSize = true;
    this.FloodFillRadioButton.Location = new Point(77, 0);
    this.FloodFillRadioButton.Margin = new Padding(0);
    this.FloodFillRadioButton.MinimumSize = new Size(30, 30);
    this.FloodFillRadioButton.Name = "FloodFillRadioButton";
    this.FloodFillRadioButton.Size = new Size(30, 30);
    this.FloodFillRadioButton.TabIndex = 3;
    this.FloodFillRadioButton.Text = "fill";
    this.FloodFillRadioButton.UseVisualStyleBackColor = true;
    this.DrawingModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
    this.DrawingModeComboBox.FormattingEnabled = true;
    this.DrawingModeComboBox.Items.AddRange(new object[3]
    {
      (object) "Drawing",
      (object) "Terrain",
      (object) "Pin Tiles"
    });
    this.DrawingModeComboBox.Location = new Point(122, 3);
    this.DrawingModeComboBox.Margin = new Padding(15, 3, 3, 3);
    this.DrawingModeComboBox.Name = "DrawingModeComboBox";
    this.DrawingModeComboBox.Size = new Size(121, 21);
    this.DrawingModeComboBox.TabIndex = 17;
    this.toolTip1.SetToolTip((Control) this.DrawingModeComboBox, "Active drawing mode.\r\n\"Terrain\" forces tiles to use a specific\r\nterrain when generating a map.\r\n\"Pin Tiles\" locks tiles so they will not\r\nbe overwritten by map generation.");
    this.DrawingModeComboBox.SelectedIndexChanged += new EventHandler(this.DrawingModeComboBox_SelectedIndexChanged);
    this.label3.AutoSize = true;
    this.label3.Location = new Point(249, 6);
    this.label3.Margin = new Padding(3, 6, 3, 0);
    this.label3.Name = "label3";
    this.label3.Size = new Size(37, 13);
    this.label3.TabIndex = 12;
    this.label3.Text = "Zoom:";
    this.ZoomUpDown.Enabled = false;
    this.ZoomUpDown.Location = new Point(292, 3);
    this.ZoomUpDown.Maximum = new Decimal(new int[4]
    {
      8,
      0,
      0,
      0
    });
    this.ZoomUpDown.Minimum = new Decimal(new int[4]
    {
      1,
      0,
      0,
      0
    });
    this.ZoomUpDown.Name = "ZoomUpDown";
    this.ZoomUpDown.Size = new Size(38, 20);
    this.ZoomUpDown.TabIndex = 14;
    this.ZoomUpDown.Value = new Decimal(new int[4]
    {
      1,
      0,
      0,
      0
    });
    this.ZoomUpDown.ValueChanged += new EventHandler(this.ZoomUpDown_ValueChanged);
    this.MapImageImportDialog.Filter = "Png Files (.png)|*.png|Gif Files (.gif)|*.gif|All files|*.*";
    this.MapImageImportDialog.Title = "Import Map";
    this.fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[7]
    {
      (ToolStripItem) this.saveMapAsToolStripMenuItem,
      (ToolStripItem) this.loadMapToolStripMenuItem,
      (ToolStripItem) this.loadTilesetToolStripMenuItem,
      (ToolStripItem) this.saveTilesetSettingsToolStripMenuItem,
      (ToolStripItem) this.importMapImageToolStripMenuItem,
      (ToolStripItem) this.importClipboardImageToolStripMenuItem,
      (ToolStripItem) this.exitToolStripMenuItem
    });
    this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
    this.fileToolStripMenuItem.Size = new Size(31 /*0x1F*/, 20);
    this.fileToolStripMenuItem.Text = "File";
    this.saveMapAsToolStripMenuItem.Enabled = false;
    this.saveMapAsToolStripMenuItem.Name = "saveMapAsToolStripMenuItem";
    this.saveMapAsToolStripMenuItem.Size = new Size(223, 22);
    this.saveMapAsToolStripMenuItem.Text = "Save Map As...";
    this.saveMapAsToolStripMenuItem.Click += new EventHandler(this.saveMapAsToolStripMenuItem_Click);
    this.loadMapToolStripMenuItem.Name = "loadMapToolStripMenuItem";
    this.loadMapToolStripMenuItem.Size = new Size(223, 22);
    this.loadMapToolStripMenuItem.Text = "Load Map...";
    this.loadMapToolStripMenuItem.Click += new EventHandler(this.loadMapToolStripMenuItem_Click);
    this.loadTilesetToolStripMenuItem.Name = "loadTilesetToolStripMenuItem";
    this.loadTilesetToolStripMenuItem.Size = new Size(223, 22);
    this.loadTilesetToolStripMenuItem.Text = "Load Tileset...";
    this.loadTilesetToolStripMenuItem.Click += new EventHandler(this.loadTilesetToolStripMenuItem_Click);
    this.saveTilesetSettingsToolStripMenuItem.Name = "saveTilesetSettingsToolStripMenuItem";
    this.saveTilesetSettingsToolStripMenuItem.Size = new Size(223, 22);
    this.saveTilesetSettingsToolStripMenuItem.Text = "Save Tileset Settings";
    this.saveTilesetSettingsToolStripMenuItem.Click += new EventHandler(this.saveTilesetSettingsToolStripMenuItem_Click);
    this.importMapImageToolStripMenuItem.Enabled = false;
    this.importMapImageToolStripMenuItem.Name = "importMapImageToolStripMenuItem";
    this.importMapImageToolStripMenuItem.Size = new Size(223, 22);
    this.importMapImageToolStripMenuItem.Text = "Import Map Image...";
    this.importMapImageToolStripMenuItem.Click += new EventHandler(this.importMapImageToolStripMenuItem_Click);
    this.importClipboardImageToolStripMenuItem.Enabled = false;
    this.importClipboardImageToolStripMenuItem.Name = "importClipboardImageToolStripMenuItem";
    this.importClipboardImageToolStripMenuItem.Size = new Size(223, 22);
    this.importClipboardImageToolStripMenuItem.Text = "Import Map Image from Clipboard";
    this.importClipboardImageToolStripMenuItem.Click += new EventHandler(this.importClipboardImageToolStripMenuItem_Click);
    this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
    this.exitToolStripMenuItem.Size = new Size(223, 22);
    this.exitToolStripMenuItem.Text = "Exit";
    this.exitToolStripMenuItem.Click += new EventHandler(this.exitToolStripMenuItem_Click);
    this.menuStrip1.Items.AddRange(new ToolStripItem[3]
    {
      (ToolStripItem) this.fileToolStripMenuItem,
      (ToolStripItem) this.editToolStripMenuItem,
      (ToolStripItem) this.mapGenerationToolStripMenuItem
    });
    this.menuStrip1.Location = new Point(0, 0);
    this.menuStrip1.Name = "menuStrip1";
    this.menuStrip1.Size = new Size(487, 24);
    this.menuStrip1.TabIndex = 2;
    this.menuStrip1.Text = "menuStrip1";
    this.editToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[6]
    {
      (ToolStripItem) this.undoToolStripMenuItem,
      (ToolStripItem) this.resizeMapToolStripMenuItem,
      (ToolStripItem) this.copyMapImageToolStripMenuItem,
      (ToolStripItem) this.toolStripMenuItem1,
      (ToolStripItem) this.clearPinnedTilesToolStripMenuItem,
      (ToolStripItem) this.clearTerrainTagsToolStripMenuItem
    });
    this.editToolStripMenuItem.Name = "editToolStripMenuItem";
    this.editToolStripMenuItem.Size = new Size(33, 20);
    this.editToolStripMenuItem.Text = "Edit";
    this.editToolStripMenuItem.Click += new EventHandler(this.editToolStripMenuItem_Click);
    this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
    this.undoToolStripMenuItem.ShortcutKeys = Keys.Z | Keys.Control;
    this.undoToolStripMenuItem.Size = new Size(147, 22);
    this.undoToolStripMenuItem.Text = "Undo";
    this.undoToolStripMenuItem.Click += new EventHandler(this.undoToolStripMenuItem_Click);
    this.resizeMapToolStripMenuItem.Name = "resizeMapToolStripMenuItem";
    this.resizeMapToolStripMenuItem.Size = new Size(147, 22);
    this.resizeMapToolStripMenuItem.Text = "Resize Map";
    this.resizeMapToolStripMenuItem.Click += new EventHandler(this.resizeMapToolStripMenuItem_Click);
    this.copyMapImageToolStripMenuItem.Name = "copyMapImageToolStripMenuItem";
    this.copyMapImageToolStripMenuItem.Size = new Size(147, 22);
    this.copyMapImageToolStripMenuItem.Text = "Copy Map Image";
    this.copyMapImageToolStripMenuItem.Click += new EventHandler(this.copyMapImageToolStripMenuItem_Click);
    this.toolStripMenuItem1.Name = "toolStripMenuItem1";
    this.toolStripMenuItem1.Size = new Size(144 /*0x90*/, 6);
    this.clearPinnedTilesToolStripMenuItem.Name = "clearPinnedTilesToolStripMenuItem";
    this.clearPinnedTilesToolStripMenuItem.Size = new Size(147, 22);
    this.clearPinnedTilesToolStripMenuItem.Text = "Clear Pinned Tiles";
    this.clearPinnedTilesToolStripMenuItem.Click += new EventHandler(this.clearPinnedTilesToolStripMenuItem_Click);
    this.clearTerrainTagsToolStripMenuItem.Name = "clearTerrainTagsToolStripMenuItem";
    this.clearTerrainTagsToolStripMenuItem.Size = new Size(147, 22);
    this.clearTerrainTagsToolStripMenuItem.Text = "Clear Terrain Tags";
    this.clearTerrainTagsToolStripMenuItem.Click += new EventHandler(this.clearTerrainTagsToolStripMenuItem_Click);
    this.mapGenerationToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[11]
    {
      (ToolStripItem) this.prepareTilesetForEditsToolStripMenuItem,
      (ToolStripItem) this.generateMapToolStripMenuItem,
      (ToolStripItem) this.repairMapToolStripMenuItem,
      (ToolStripItem) this.experimentalConstraintSolverToolStripMenuItem,
      (ToolStripItem) this.hybridSolverToolStripMenuItem,
      (ToolStripItem) this.experimentalBranchArcConsistencyToolStripMenuItem,
      (ToolStripItem) this.convertMapToTerrainTagsToolStripMenuItem,
      (ToolStripItem) this.copyTerrainTagsImageToolStripMenuItem,
      (ToolStripItem) this.importTerrainTagsFromClipboardImageToolStripMenuItem,
      (ToolStripItem) this.toolStripMenuItem2,
      (ToolStripItem) this.processTilesetFromMapsToolStripMenuItem
    });
    this.mapGenerationToolStripMenuItem.Name = "mapGenerationToolStripMenuItem";
    this.mapGenerationToolStripMenuItem.Size = new Size(88, 20);
    this.mapGenerationToolStripMenuItem.Text = "Map Generation";
    this.prepareTilesetForEditsToolStripMenuItem.Enabled = false;
    this.prepareTilesetForEditsToolStripMenuItem.Name = "prepareTilesetForEditsToolStripMenuItem";
    this.prepareTilesetForEditsToolStripMenuItem.Size = new Size((int) byte.MaxValue, 22);
    this.prepareTilesetForEditsToolStripMenuItem.Text = "Prepare Tileset for Edits";
    this.prepareTilesetForEditsToolStripMenuItem.Click += new EventHandler(this.prepareTilesetForEditsToolStripMenuItem_Click);
    this.generateMapToolStripMenuItem.Enabled = false;
    this.generateMapToolStripMenuItem.Name = "generateMapToolStripMenuItem";
    this.generateMapToolStripMenuItem.ShortcutKeys = Keys.N | Keys.Control;
    this.generateMapToolStripMenuItem.Size = new Size((int) byte.MaxValue, 22);
    this.generateMapToolStripMenuItem.Text = "Generate Map";
    this.generateMapToolStripMenuItem.Click += new EventHandler(this.generateMapToolStripMenuItem_Click);
    this.repairMapToolStripMenuItem.Enabled = false;
    this.repairMapToolStripMenuItem.Name = "repairMapToolStripMenuItem";
    this.repairMapToolStripMenuItem.ShortcutKeys = Keys.R | Keys.Control;
    this.repairMapToolStripMenuItem.Size = new Size((int) byte.MaxValue, 22);
    this.repairMapToolStripMenuItem.Text = "Repair Map";
    this.repairMapToolStripMenuItem.Click += new EventHandler(this.repairMapToolStripMenuItem_Click);
    this.experimentalConstraintSolverToolStripMenuItem.CheckOnClick = true;
    this.experimentalConstraintSolverToolStripMenuItem.Checked = true;
    this.experimentalConstraintSolverToolStripMenuItem.Name = "experimentalConstraintSolverToolStripMenuItem";
    this.experimentalConstraintSolverToolStripMenuItem.Size = new Size((int) byte.MaxValue, 22);
    this.experimentalConstraintSolverToolStripMenuItem.Text = "Experimental Constraint Solver";
    this.experimentalConstraintSolverToolStripMenuItem.ToolTipText = "Use the whole-map backtracking solver for generation and repair.";
    this.experimentalConstraintSolverToolStripMenuItem.Click += new EventHandler(this.experimentalConstraintSolverToolStripMenuItem_Click);
    this.hybridSolverToolStripMenuItem.CheckOnClick = true;
    this.hybridSolverToolStripMenuItem.Name = "hybridSolverToolStripMenuItem";
    this.hybridSolverToolStripMenuItem.Size = new Size((int) byte.MaxValue, 22);
    this.hybridSolverToolStripMenuItem.Text = "Hybrid Legacy + Constraint Solver";
    this.hybridSolverToolStripMenuItem.ToolTipText = "Run legacy first, then solve only unresolved regions with adaptive halos.";
    this.hybridSolverToolStripMenuItem.Click += new EventHandler(this.hybridSolverToolStripMenuItem_Click);
    this.experimentalBranchArcConsistencyToolStripMenuItem.CheckOnClick = true;
    this.experimentalBranchArcConsistencyToolStripMenuItem.Checked = false;
    this.experimentalBranchArcConsistencyToolStripMenuItem.Enabled = true;
    this.experimentalBranchArcConsistencyToolStripMenuItem.Name = "experimentalBranchArcConsistencyToolStripMenuItem";
    this.experimentalBranchArcConsistencyToolStripMenuItem.Size = new Size((int) byte.MaxValue, 22);
    this.experimentalBranchArcConsistencyToolStripMenuItem.Text = "Experimental Branch Arc Consistency";
    this.experimentalBranchArcConsistencyToolStripMenuItem.ToolTipText = "Propagate unresolved domains to a fixed point during complete-search branches.";
    this.convertMapToTerrainTagsToolStripMenuItem.Name = "convertMapToTerrainTagsToolStripMenuItem";
    this.convertMapToTerrainTagsToolStripMenuItem.Size = new Size((int) byte.MaxValue, 22);
    this.convertMapToTerrainTagsToolStripMenuItem.Text = "Convert Map to Terrain Tags";
    this.convertMapToTerrainTagsToolStripMenuItem.Click += new EventHandler(this.convertMapToTerrainTagsToolStripMenuItem_Click);
    this.copyTerrainTagsImageToolStripMenuItem.Enabled = false;
    this.copyTerrainTagsImageToolStripMenuItem.Name = "copyTerrainTagsImageToolStripMenuItem";
    this.copyTerrainTagsImageToolStripMenuItem.Size = new Size((int) byte.MaxValue, 22);
    this.copyTerrainTagsImageToolStripMenuItem.Text = "Copy Terrain Tags Image";
    this.copyTerrainTagsImageToolStripMenuItem.Click += new EventHandler(this.copyTerrainTagsImageToolStripMenuItem_Click);
    this.importTerrainTagsFromClipboardImageToolStripMenuItem.Enabled = false;
    this.importTerrainTagsFromClipboardImageToolStripMenuItem.Name = "importTerrainTagsFromClipboardImageToolStripMenuItem";
    this.importTerrainTagsFromClipboardImageToolStripMenuItem.Size = new Size((int) byte.MaxValue, 22);
    this.importTerrainTagsFromClipboardImageToolStripMenuItem.Text = "Import Terrain Tags Image from Clipboard";
    this.importTerrainTagsFromClipboardImageToolStripMenuItem.Click += new EventHandler(this.importTerrainTagsFromClipboardImageToolStripMenuItem_Click);
    this.toolStripMenuItem2.Name = "toolStripMenuItem2";
    this.toolStripMenuItem2.Size = new Size(252, 6);
    this.processTilesetFromMapsToolStripMenuItem.Name = "processTilesetFromMapsToolStripMenuItem";
    this.processTilesetFromMapsToolStripMenuItem.Size = new Size((int) byte.MaxValue, 22);
    this.processTilesetFromMapsToolStripMenuItem.Text = "Process Tileset from Maps";
    this.processTilesetFromMapsToolStripMenuItem.Click += new EventHandler(this.processTilesetFromMapsToolStripMenuItem_Click);
    this.MainPanel.Controls.Add((Control) this.tableLayoutPanel1);
    this.MainPanel.Dock = DockStyle.Fill;
    this.MainPanel.Location = new Point(0, 24);
    this.MainPanel.Margin = new Padding(0);
    this.MainPanel.Name = "MainPanel";
    this.MainPanel.Size = new Size(487, 440);
    this.MainPanel.TabIndex = 3;
    this.SaveMapDialog.Filter = "Map files - text|*.map|Mappy Array|*.mar|Tiled files|*.tmx|All files|*.*";
    this.SaveMapDialog.Title = "Save Map";
    this.OpenMapDialog.Filter = "Map files - text|*.map|Mappy Array|*.mar|Tiled files|*.tmx|All files|*.*";
    this.OpenMapDialog.Title = "Open Map";
    this.LoadTilesetDialog.Filter = "Png Files (.png)|*.png|Gif Files (.gif)|*.gif|All files|*.*";
    this.LoadTilesetDialog.Title = "Load Tileset";
    this.statusStrip1.Items.AddRange(new ToolStripItem[3]
    {
      (ToolStripItem) this.StatusbarSpacerLabel,
      (ToolStripItem) this.CursorPositionStatusLabel,
      (ToolStripItem) this.progressBar1
    });
    this.statusStrip1.Location = new Point(0, 464);
    this.statusStrip1.Name = "statusStrip1";
    this.statusStrip1.Size = new Size(487, 22);
    this.statusStrip1.TabIndex = 4;
    this.statusStrip1.Text = "statusStrip1";
    this.StatusbarSpacerLabel.Name = "StatusbarSpacerLabel";
    this.StatusbarSpacerLabel.Size = new Size(339, 17);
    this.StatusbarSpacerLabel.Spring = true;
    this.progressBar1.Name = "progressBar1";
    this.progressBar1.Size = new Size(100, 16 /*0x10*/);
    this.progressBar1.Visible = false;
    this.CursorPositionStatusLabel.Name = "CursorPositionStatusLabel";
    this.CursorPositionStatusLabel.Size = new Size(0, 17);
    this.AutoScaleDimensions = new SizeF(6f, 13f);
    this.AutoScaleMode = AutoScaleMode.Font;
    this.ClientSize = new Size(487, 486);
    this.Controls.Add((Control) this.MainPanel);
    this.Controls.Add((Control) this.menuStrip1);
    this.Controls.Add((Control) this.statusStrip1);
    this.Name = nameof (FE_Map_Creator_Form);
    this.Text = "FE Map Creator";
    this.tableLayoutPanel1.ResumeLayout(false);
    this.flowLayoutPanel1.ResumeLayout(false);
    this.flowLayoutPanel1.PerformLayout();
    this.WidthUpDown.EndInit();
    this.HeightUpDown.EndInit();
    this.DepthUpDown.EndInit();
    this.DistUpDown.EndInit();
    this.PictureBoxPanel.ResumeLayout(false);
    ((ISupportInitialize) this.MapPicture).EndInit();
    this.ToolsFlowPanel.ResumeLayout(false);
    this.ToolsFlowPanel.PerformLayout();
    this.ZoomUpDown.EndInit();
    this.menuStrip1.ResumeLayout(false);
    this.menuStrip1.PerformLayout();
    this.MainPanel.ResumeLayout(false);
    this.statusStrip1.ResumeLayout(false);
    this.statusStrip1.PerformLayout();
    this.ResumeLayout(false);
    this.PerformLayout();
  }

  private delegate void SetRectHashCallback(HashSet<Rectangle> rects);
}
