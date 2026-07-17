using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FE_Map_Creator.Generation;
using FE_Map_Creator.Gui.Assets;
using FE_Map_Creator.Gui.Controls;
using FE_Map_Creator.Gui.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

#nullable disable
namespace FE_Map_Creator.Gui.Views;

public partial class MainView : UserControl
{
  private const string REPOSITORY_URL = "https://github.com/laqieer/FEMapCreator";
  private const string USER_GUIDE_URL =
    "https://github.com/laqieer/FEMapCreator/blob/main/docs/user-guide/Home.md";

  private static readonly FilePickerFileType All_Map_Files = new FilePickerFileType("Supported maps")
  {
    Patterns = new[] { "*.map", "*.mar", "*.tmx" },
  };

  private static readonly FilePickerFileType Text_Map_Files = new FilePickerFileType("FE text map")
  {
    Patterns = new[] { "*.map" },
  };

  private static readonly FilePickerFileType Mar_Map_Files = new FilePickerFileType("MAR map")
  {
    Patterns = new[] { "*.mar" },
  };

  private static readonly FilePickerFileType Tmx_Map_Files = new FilePickerFileType("TMX map")
  {
    Patterns = new[] { "*.tmx" },
  };

  private readonly Editor_Session _session = new Editor_Session();
  private Bundled_Asset_Catalog _catalog;
  private Bundled_Tileset _tileset;
  private IStorageFile _current_file;
  private CancellationTokenSource _operation_cancellation;
  private TaskCompletionSource<bool> _confirmation;
  private Map_Cell _drag_start;
  private Map_Cell _last_cell;
  private bool _drawing;
  private bool? _stroke_lock_value;
  private bool _initialized;
  private bool _allow_window_close;

  public MainView()
  {
    this.InitializeComponent();
    this.MapCanvas.Session = this._session;
    this.MapCanvas.Cell_Pressed += this.map_cell_pressed;
    this.MapCanvas.Cell_Moved += this.map_cell_moved;
    this.MapCanvas.Cell_Released += this.map_cell_released;
    this.MapCanvas.Cell_Hovered += this.map_cell_hovered;
    this.MapCanvas.Stroke_Cancelled += this.map_stroke_cancelled;
    this.TilePalette.Selected_Tile_Changed += this.tile_palette_selection_changed;
    this.Loaded += this.main_view_loaded;
  }

  private void main_view_loaded(object sender, RoutedEventArgs e)
  {
    if (this._initialized)
      return;
    this._initialized = true;
    try
    {
      this._catalog = new Bundled_Asset_Catalog();
      this.TilesetComboBox.ItemsSource = this._catalog.Tilesets;
      this.BrushComboBox.ItemsSource = new[]
      {
        new Choice<Editor_Brush>("Brush", Editor_Brush.Brush),
        new Choice<Editor_Brush>("Rectangle", Editor_Brush.Rectangle),
        new Choice<Editor_Brush>("Flood fill", Editor_Brush.Flood_Fill),
      };
      this.ModeComboBox.ItemsSource = new[]
      {
        new Choice<Editor_Mode>("Paint tile", Editor_Mode.Tile),
        new Choice<Editor_Mode>("Erase / reopen", Editor_Mode.Erase),
        new Choice<Editor_Mode>("Lock cells", Editor_Mode.Lock),
        new Choice<Editor_Mode>("Require terrain", Editor_Mode.Terrain_Required),
        new Choice<Editor_Mode>("Forbid terrain", Editor_Mode.Terrain_Forbidden),
      };
      this.ZoomComboBox.ItemsSource = new[]
      {
        new Choice<double>("50%", 0.5),
        new Choice<double>("100%", 1),
        new Choice<double>("200%", 2),
        new Choice<double>("300%", 3),
        new Choice<double>("400%", 4),
      };
      this.AlgorithmComboBox.ItemsSource = new[]
      {
        new Choice<Map_Generation_Algorithm>(
          "Constraint solver", Map_Generation_Algorithm.Experimental_Constraint),
        new Choice<Map_Generation_Algorithm>(
          "Hybrid legacy + constraint", Map_Generation_Algorithm.Experimental_Hybrid),
        new Choice<Map_Generation_Algorithm>("Legacy frontier", Map_Generation_Algorithm.Legacy),
      };
      this.BrushComboBox.SelectedIndex = 0;
      this.ModeComboBox.SelectedIndex = 0;
      this.ZoomComboBox.SelectedIndex = 2;
      this.AlgorithmComboBox.SelectedIndex = 0;
      Bundled_Tileset_Descriptor initial =
        this._catalog.find("FE6 - Fields - 01020304") ?? this._catalog.Tilesets.First();
      this.TilesetComboBox.SelectedItem = initial;
      if (this.VisualRoot is Window window)
        window.Closing += this.window_closing;
      this.refresh_editor("Ready");
    }
    catch (Exception ex) when (is_expected_ui_exception(ex))
    {
      this.show_error("Could not initialize bundled assets.", ex);
    }
  }

  private async void window_closing(object sender, WindowClosingEventArgs e)
  {
    if (this._allow_window_close || !this._session.Is_Dirty)
      return;
    e.Cancel = true;
    if (await this.confirm_discard_changes())
    {
      this._allow_window_close = true;
      ((Window) sender).Close();
    }
  }

  private void tileset_selection_changed(object sender, SelectionChangedEventArgs e)
  {
    if (this.TilesetComboBox.SelectedItem is not Bundled_Tileset_Descriptor descriptor)
      return;
    try
    {
      Bundled_Tileset loaded = descriptor.load();
      Bundled_Tileset old = this._tileset;
      this._tileset = loaded;
      this.MapCanvas.Tileset = loaded.Image;
      this.TilePalette.Tileset = loaded.Image;
      old?.Dispose();
      this.update_terrain_choices();
      this.refresh_editor($"Loaded tileset {loaded.Name} ({loaded.Tile_Count} tiles).");
    }
    catch (Exception ex) when (is_expected_ui_exception(ex))
    {
      this.show_error($"Could not load tileset \"{descriptor.Name}\".", ex);
    }
  }

  private void update_terrain_choices()
  {
    List<Terrain_Choice> choices = new List<Terrain_Choice>();
    if (this._tileset?.Metadata != null)
    {
      foreach (int id in this._tileset.Metadata.Terrain_Tags
        .Where(id => id > 0)
        .Distinct()
        .OrderBy(id => id))
      {
        string name = this._catalog.Terrains.TryGetValue(id, out FEXNA_Library.Data_Terrain terrain)
          ? terrain.Name
          : $"Terrain {id}";
        choices.Add(new Terrain_Choice(id, name));
      }
    }
    this.TerrainComboBox.ItemsSource = choices;
    this.TerrainComboBox.SelectedIndex = choices.Count > 0 ? 0 : -1;
    this.update_terrain_enabled();
  }

  private void mode_selection_changed(object sender, SelectionChangedEventArgs e)
  {
    this.update_terrain_enabled();
  }

  private void update_terrain_enabled()
  {
    Editor_Mode mode = this.current_mode;
    this.TerrainComboBox.IsEnabled =
      this.TerrainComboBox.ItemCount > 0 &&
      (mode == Editor_Mode.Terrain_Required || mode == Editor_Mode.Terrain_Forbidden);
  }

  private void zoom_selection_changed(object sender, SelectionChangedEventArgs e)
  {
    if (this.ZoomComboBox.SelectedItem is Choice<double> zoom)
      this.MapCanvas.Zoom = zoom.Value;
  }

  private void tile_palette_selection_changed(object sender, EventArgs e)
  {
    this.TileSelectionText.Text = $"Tile {this.TilePalette.Selected_Tile}";
  }

  private void map_cell_hovered(object sender, Map_Cell_Event_Args e)
  {
    int tile = this._session.Tiles[e.Cell.X, e.Cell.Y];
    int terrain = this._session.Terrain[e.Cell.X, e.Cell.Y];
    string terrain_text = terrain == 0 ? "" : $", terrain {terrain}";
    string lock_text = this._session.Locked[e.Cell.X, e.Cell.Y] ? ", locked" : "";
    string open_text = this._session.Drawn[e.Cell.X, e.Cell.Y] ? "" : ", open";
    this.CursorText.Text =
      $"x: {e.Cell.X}, y: {e.Cell.Y}, tile: {tile}{terrain_text}{lock_text}{open_text}";
  }

  private void map_cell_pressed(object sender, Map_Cell_Event_Args e)
  {
    if (this._operation_cancellation != null || this._tileset == null)
      return;
    if ((this.current_mode == Editor_Mode.Terrain_Required ||
         this.current_mode == Editor_Mode.Terrain_Forbidden) &&
        this.current_terrain <= 0)
    {
      this.show_error("This tileset has no terrain metadata available for terrain drawing.");
      return;
    }
    this._drawing = true;
    this._drag_start = e.Cell;
    this._last_cell = e.Cell;
    this._stroke_lock_value = this.current_mode == Editor_Mode.Lock
      ? !this._session.Locked[e.Cell.X, e.Cell.Y]
      : null;
    this._session.begin_edit();
    if (this.current_brush == Editor_Brush.Flood_Fill)
    {
      this._session.flood_fill(
        e.Cell.X,
        e.Cell.Y,
        this.current_mode,
        this.TilePalette.Selected_Tile,
        this.current_terrain,
        this._stroke_lock_value);
      this._session.end_edit();
      this._drawing = false;
      this.refresh_editor("Flood fill applied.");
      return;
    }
    if (this.current_brush == Editor_Brush.Rectangle)
    {
      this.MapCanvas.set_selection(e.Cell, e.Cell);
      return;
    }
    this.apply_line(e.Cell, e.Cell);
    this.refresh_editor();
  }

  private void map_cell_moved(object sender, Map_Cell_Event_Args e)
  {
    if (!this._drawing)
      return;
    if (this.current_brush == Editor_Brush.Rectangle)
    {
      this.MapCanvas.set_selection(this._drag_start, e.Cell);
      return;
    }
    if (this.current_brush == Editor_Brush.Brush)
    {
      this.apply_line(this._last_cell, e.Cell);
      this._last_cell = e.Cell;
      this.refresh_editor();
    }
  }

  private void map_cell_released(object sender, Map_Cell_Event_Args e)
  {
    if (!this._drawing)
      return;
    if (this.current_brush == Editor_Brush.Rectangle)
    {
      this._session.apply_rectangle(
        this._drag_start,
        e.Cell,
        this.current_mode,
        this.TilePalette.Selected_Tile,
        this.current_terrain,
        this._stroke_lock_value);
      this.MapCanvas.set_selection(null, null);
    }
    this._session.end_edit();
    this._drawing = false;
    this.refresh_editor("Map updated.");
  }

  private void map_stroke_cancelled(object sender, EventArgs e)
  {
    if (!this._drawing)
      return;
    this._session.end_edit();
    this._drawing = false;
    this.MapCanvas.set_selection(null, null);
    this.refresh_editor("Drawing stroke ended.");
  }

  private void apply_line(Map_Cell start, Map_Cell end)
  {
    int x0 = start.X;
    int y0 = start.Y;
    int x1 = end.X;
    int y1 = end.Y;
    int dx = Math.Abs(x1 - x0);
    int sx = x0 < x1 ? 1 : -1;
    int dy = -Math.Abs(y1 - y0);
    int sy = y0 < y1 ? 1 : -1;
    int error = dx + dy;
    while (true)
    {
      this._session.apply_cell(
        x0,
        y0,
        this.current_mode,
        this.TilePalette.Selected_Tile,
        this.current_terrain,
        this._stroke_lock_value);
      if (x0 == x1 && y0 == y1)
        break;
      int doubled = 2 * error;
      if (doubled >= dy)
      {
        error += dy;
        x0 += sx;
      }
      if (doubled <= dx)
      {
        error += dx;
        y0 += sy;
      }
    }
  }

  private async void new_map_click(object sender, RoutedEventArgs e)
  {
    if (!await this.confirm_discard_changes())
      return;
    this._session.new_map(this.map_width, this.map_height);
    this._current_file = null;
    this.refresh_editor("Created a new map.");
  }

  private async void open_map_click(object sender, RoutedEventArgs e)
  {
    if (!await this.confirm_discard_changes())
      return;
    TopLevel top_level = TopLevel.GetTopLevel(this);
    if (top_level?.StorageProvider == null)
    {
      this.show_error("File opening is not available on this platform.");
      return;
    }
    try
    {
      IReadOnlyList<IStorageFile> files = await top_level.StorageProvider.OpenFilePickerAsync(
        new FilePickerOpenOptions
        {
          Title = "Open map",
          AllowMultiple = false,
          FileTypeFilter = new[] { All_Map_Files, Text_Map_Files, Mar_Map_Files, Tmx_Map_Files },
        });
      if (files.Count == 0)
        return;
      IStorageFile file = files[0];
      Map_Codec_Registry registry = new Map_Codec_Registry();
      Map_Format format = registry.format_from_path(file.Name);
      Map_Read_Options options = format == Map_Format.Mar
        ? new Map_Read_Options
        {
          Width = this.map_width,
          Height = this.map_height,
          Tileset = this._tileset?.Name,
        }
        : null;
      Map_Document document;
      await using (Stream stream = await file.OpenReadAsync())
        document = await registry.read_async(stream, format, options);

      if (format != Map_Format.Mar)
      {
        Bundled_Tileset_Descriptor descriptor =
          this._catalog.find(document.Tileset_Image_Source) ??
          this._catalog.find(document.Tileset);
        if (descriptor == null)
        {
          throw new InvalidDataException(
            $"No bundled tileset uniquely matches \"{document.Tileset}\".");
        }
        this.TilesetComboBox.SelectedItem = descriptor;
      }
      this._session.load_map(document);
      this._current_file = file;
      this.refresh_editor($"Opened {file.Name}.");
    }
    catch (Exception ex) when (is_expected_ui_exception(ex))
    {
      this.show_error("Could not open the map.", ex);
    }
  }

  private async void save_map_click(object sender, RoutedEventArgs e)
  {
    if (this._current_file == null)
      await this.save_map_as();
    else
      await this.save_to_file(this._current_file);
  }

  private async void save_map_as_click(object sender, RoutedEventArgs e)
  {
    await this.save_map_as();
  }

  private async Task save_map_as()
  {
    TopLevel top_level = TopLevel.GetTopLevel(this);
    if (top_level?.StorageProvider == null)
    {
      this.show_error("File saving is not available on this platform.");
      return;
    }
    try
    {
      IStorageFile file = await top_level.StorageProvider.SaveFilePickerAsync(
        new FilePickerSaveOptions
        {
          Title = "Save map",
          SuggestedFileName = this._current_file?.Name ?? "map.map",
          DefaultExtension = "map",
          FileTypeChoices = new[] { Text_Map_Files, Mar_Map_Files, Tmx_Map_Files },
        });
      if (file == null)
        return;
      await this.save_to_file(file);
    }
    catch (Exception ex) when (is_expected_ui_exception(ex))
    {
      this.show_error("Could not choose a save destination.", ex);
    }
  }

  private async Task save_to_file(IStorageFile file)
  {
    if (this._tileset == null)
    {
      this.show_error("Select a tileset before saving.");
      return;
    }
    try
    {
      Map_Codec_Registry registry = new Map_Codec_Registry();
      Map_Format format = registry.format_from_path(file.Name);
      string tileset_name = format == Map_Format.Text
        ? Tileset_Asset_Naming.identifier(this._tileset.Name)
        : this._tileset.Name;
      string image_source = format == Map_Format.Tmx ? $"{this._tileset.Name}.png" : "";
      Map_Document document = this._session.create_document(tileset_name);
      document.Tileset_Image_Source = image_source;
      Map_Write_Options options = new Map_Write_Options
      {
        Tileset = tileset_name,
        Tileset_Image_Source = image_source,
      };
      await using (Stream stream = await file.OpenWriteAsync())
      {
        if (stream.CanSeek)
          stream.SetLength(0);
        await registry.write_async(stream, format, document, options);
        await stream.FlushAsync();
      }
      this._current_file = file;
      this._session.mark_saved();
      this.refresh_editor($"Saved {file.Name}.");
    }
    catch (Exception ex) when (is_expected_ui_exception(ex))
    {
      this.show_error($"Could not save {file.Name}.", ex);
    }
  }

  private void undo_click(object sender, RoutedEventArgs e)
  {
    if (this._session.undo())
      this.refresh_editor("Undo.");
  }

  private void redo_click(object sender, RoutedEventArgs e)
  {
    if (this._session.redo())
      this.refresh_editor("Redo.");
  }

  private void resize_map_click(object sender, RoutedEventArgs e)
  {
    try
    {
      this._session.resize(this.map_width, this.map_height);
      this.refresh_editor("Map resized.");
    }
    catch (ArgumentOutOfRangeException ex)
    {
      this.show_error("Could not resize the map.", ex);
    }
  }

  private void clear_locks_click(object sender, RoutedEventArgs e)
  {
    this._session.clear_locks();
    this.refresh_editor("Cleared all locked cells.");
  }

  private void clear_terrain_click(object sender, RoutedEventArgs e)
  {
    this._session.clear_terrain();
    this.refresh_editor("Cleared all terrain constraints.");
  }

  private async void generate_click(object sender, RoutedEventArgs e)
  {
    await this.run_generation(repair: false);
  }

  private async void repair_click(object sender, RoutedEventArgs e)
  {
    await this.run_generation(repair: true);
  }

  private async Task run_generation(bool repair)
  {
    if (this._tileset == null || this._operation_cancellation != null)
      return;
    int? seed;
    try
    {
      seed = this.read_seed();
    }
    catch (FormatException ex)
    {
      this.show_error("The seed must be a 32-bit integer.", ex);
      return;
    }

    this._operation_cancellation = new CancellationTokenSource();
    this.set_busy(true);
    this._session.begin_edit();
    try
    {
      Map_Generation_Engine engine =
        new Map_Generation_Engine(this._tileset.Generation_Data, this._tileset.Metadata);
      IProgress<int> progress = new Progress<int>(
        value => this.OperationProgress.Value = Math.Clamp(value, 0, 100));
      CancellationToken token = this._operation_cancellation.Token;
      Func<Map_Generation_Result> operation = repair
        ? () => engine.repair(
          this._session.create_map_state(),
          this.create_repair_options(seed),
          token,
          progress: progress)
        : () => engine.generate(
          this._session.create_map_state(),
          this.create_generation_options(seed),
          token,
          progress: progress);
      Map_Generation_Result result;
      if (OperatingSystem.IsBrowser())
      {
        this.StatusText.Text =
          $"{(repair ? "Repairing" : "Generating")} map; browser execution is single-threaded.";
        await Task.Yield();
        result = operation();
      }
      else
      {
        result = await Task.Run(operation, token);
      }
      this._session.commit_external_edit();
      string completeness = result.Is_Complete
        ? "complete"
        : $"{result.Unresolved_Tile_Count} unresolved cells";
      this.refresh_editor(
        $"{(repair ? "Repair" : "Generation")} {completeness}; seed {result.Seed}.");
    }
    catch (OperationCanceledException)
    {
      this._session.rollback_edit();
      this.refresh_editor("Operation cancelled; map restored.");
    }
    catch (Exception ex) when (
      ex is ArgumentException ||
      ex is InvalidOperationException ||
      ex is InvalidDataException)
    {
      this._session.rollback_edit();
      this.show_error($"{(repair ? "Repair" : "Generation")} failed.", ex);
    }
    finally
    {
      this._operation_cancellation.Dispose();
      this._operation_cancellation = null;
      this.set_busy(false);
    }
  }

  private Map_Generation_Options create_generation_options(int? seed)
  {
    return new Map_Generation_Options
    {
      Algorithm = this.current_algorithm,
      Depth = int_value(this.DepthUpDown),
      Seed = seed,
      Experimental_Search_Node_Limit = int_value(this.SearchLimitUpDown),
      Experimental_Restart_Count = int_value(this.RestartUpDown),
      Experimental_Nogood_Limit = int_value(this.NogoodUpDown),
      Experimental_Enable_Conflict_Learning = this.ConflictLearningCheckBox.IsChecked == true,
      Experimental_Enable_Branch_Arc_Consistency = this.BranchConsistencyCheckBox.IsChecked == true,
      Hybrid_Initial_Halo = int_value(this.InitialHaloUpDown),
      Hybrid_Max_Halo = int_value(this.MaxHaloUpDown),
    };
  }

  private Map_Repair_Options create_repair_options(int? seed)
  {
    return new Map_Repair_Options
    {
      Algorithm = this.current_algorithm,
      Depth = int_value(this.DepthUpDown),
      Radius = int_value(this.RadiusUpDown),
      Seed = seed,
      Experimental_Search_Node_Limit = int_value(this.SearchLimitUpDown),
      Experimental_Restart_Count = int_value(this.RestartUpDown),
      Experimental_Nogood_Limit = int_value(this.NogoodUpDown),
      Experimental_Enable_Conflict_Learning = this.ConflictLearningCheckBox.IsChecked == true,
      Experimental_Enable_Branch_Arc_Consistency = this.BranchConsistencyCheckBox.IsChecked == true,
      Hybrid_Initial_Halo = int_value(this.InitialHaloUpDown),
      Hybrid_Max_Halo = int_value(this.MaxHaloUpDown),
    };
  }

  private void cancel_operation_click(object sender, RoutedEventArgs e)
  {
    this._operation_cancellation?.Cancel();
  }

  private void set_busy(bool busy)
  {
    bool can_cancel = busy && !OperatingSystem.IsBrowser();
    this.OperationControls.IsEnabled = !busy;
    this.MapCanvas.IsEnabled = !busy;
    this.CancelOperationButton.IsEnabled = can_cancel;
    this.CancelOperationMenuItem.IsEnabled = can_cancel;
    this.OperationProgress.IsVisible = can_cancel;
    this.OperationProgress.Value = 0;
  }

  private async void repository_link_click(object sender, RoutedEventArgs e) =>
    await this.launch_uri(REPOSITORY_URL);

  private async void issues_link_click(object sender, RoutedEventArgs e) =>
    await this.launch_uri($"{REPOSITORY_URL}/issues");

  private async void discussions_link_click(object sender, RoutedEventArgs e) =>
    await this.launch_uri($"{REPOSITORY_URL}/discussions");

  private async void user_guide_link_click(object sender, RoutedEventArgs e) =>
    await this.launch_uri(USER_GUIDE_URL);

  private async void release_link_click(object sender, RoutedEventArgs e) =>
    await this.launch_uri($"{REPOSITORY_URL}/releases/latest");

  private async Task launch_uri(string url)
  {
    try
    {
      TopLevel top_level = TopLevel.GetTopLevel(this);
      if (top_level?.Launcher == null || !await top_level.Launcher.LaunchUriAsync(new Uri(url)))
        this.show_error($"Could not open {url}.");
    }
    catch (Exception ex) when (
      ex is InvalidOperationException ||
      ex is NotSupportedException ||
      ex is ArgumentException)
    {
      this.show_error($"Could not open {url}.", ex);
    }
  }

  private async Task<bool> confirm_discard_changes()
  {
    if (!this._session.Is_Dirty)
      return true;
    if (this._confirmation != null)
      return false;
    this.ConfirmationText.Text =
      "This map has unsaved changes. Discard them and continue?";
    this.ConfirmationOverlay.IsVisible = true;
    this._confirmation = new TaskCompletionSource<bool>();
    return await this._confirmation.Task;
  }

  private void confirmation_accept_click(object sender, RoutedEventArgs e)
  {
    this.complete_confirmation(true);
  }

  private void confirmation_cancel_click(object sender, RoutedEventArgs e)
  {
    this.complete_confirmation(false);
  }

  private void complete_confirmation(bool result)
  {
    TaskCompletionSource<bool> confirmation = this._confirmation;
    if (confirmation == null)
      return;
    this._confirmation = null;
    this.ConfirmationOverlay.IsVisible = false;
    confirmation.SetResult(result);
  }

  private void dismiss_error_click(object sender, RoutedEventArgs e)
  {
    this.ErrorBanner.IsVisible = false;
  }

  private void show_error(string message, Exception exception = null)
  {
    string detail = exception == null ? "" : $" {exception.Message}";
    this.ErrorText.Text = message + detail;
    this.ErrorBanner.IsVisible = true;
    this.StatusText.Text = message;
  }

  private void refresh_editor(string status = null)
  {
    this.WidthUpDown.Value = this._session.Width;
    this.HeightUpDown.Value = this._session.Height;
    this.MapCanvas.InvalidateMeasure();
    this.MapCanvas.InvalidateVisual();
    this.UndoButton.IsEnabled = this._session.Can_Undo;
    this.RedoButton.IsEnabled = this._session.Can_Redo;
    this.UndoMenuItem.IsEnabled = this._session.Can_Undo;
    this.RedoMenuItem.IsEnabled = this._session.Can_Redo;
    if (!string.IsNullOrWhiteSpace(status))
      this.StatusText.Text = status;
    string filename = this._current_file?.Name ?? "Untitled";
    string dirty = this._session.Is_Dirty ? "*" : "";
    if (this.VisualRoot is Window window)
      window.Title = $"FE Map Creator - {filename}{dirty}";
  }

  private int? read_seed()
  {
    string text = this.SeedTextBox.Text?.Trim();
    if (string.IsNullOrEmpty(text))
      return null;
    if (!int.TryParse(text, out int seed))
      throw new FormatException($"\"{text}\" is not a valid 32-bit integer.");
    return seed;
  }

  private int map_width => int_value(this.WidthUpDown);

  private int map_height => int_value(this.HeightUpDown);

  private Editor_Brush current_brush =>
    (this.BrushComboBox.SelectedItem as Choice<Editor_Brush>)?.Value ?? Editor_Brush.Brush;

  private Editor_Mode current_mode =>
    (this.ModeComboBox.SelectedItem as Choice<Editor_Mode>)?.Value ?? Editor_Mode.Tile;

  private Map_Generation_Algorithm current_algorithm =>
    (this.AlgorithmComboBox.SelectedItem as Choice<Map_Generation_Algorithm>)?.Value
    ?? Map_Generation_Algorithm.Experimental_Constraint;

  private int current_terrain =>
    (this.TerrainComboBox.SelectedItem as Terrain_Choice)?.Id ?? 0;

  private static int int_value(NumericUpDown control)
  {
    return decimal.ToInt32(control.Value ?? 0);
  }

  private static bool is_expected_ui_exception(Exception exception)
  {
    return exception is IOException ||
      exception is InvalidDataException ||
      exception is InvalidOperationException ||
      exception is NotSupportedException ||
      exception is UnauthorizedAccessException ||
      exception is ArgumentException ||
      exception is FormatException ||
      (OperatingSystem.IsBrowser() && exception is JSException);
  }

  private sealed class Choice<T>
  {
    internal string Label { get; }

    internal T Value { get; }

    internal Choice(string label, T value)
    {
      this.Label = label;
      this.Value = value;
    }

    public override string ToString() => this.Label;
  }

  private sealed class Terrain_Choice
  {
    internal int Id { get; }

    private string Name { get; }

    internal Terrain_Choice(int id, string name)
    {
      this.Id = id;
      this.Name = name;
    }

    public override string ToString() => $"{this.Id}: {this.Name}";
  }
}
