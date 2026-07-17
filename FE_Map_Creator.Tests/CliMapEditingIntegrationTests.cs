using System.Text.Json;
using FE_Map_Creator.Editing;

namespace FE_Map_Creator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CliMapEditingIntegrationTests
{
  [TestMethod]
  public async Task MapEditCreatesResizesAndLetsDirectOptionsOverrideSpec()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string spec_path = directory.path("edit.json");
    string spec_output = directory.path("spec.map");
    string direct_output = directory.path("direct.map");
    new Map_Job_Spec_Reader().write_job(spec_path, new Map_Job_Spec
    {
      Version = 1,
      Operation = "edit",
      Width = 1,
      Height = 1,
      Tileset = "unused",
      Output = spec_output,
      Edits =
      [
        new Map_Edit_Operation
        {
          Action = "set-tile",
          Shape = "rectangle",
          X = 0,
          Y = 0,
          EndX = 1,
          EndY = 1,
          Tile = 7
        },
        new Map_Edit_Operation { Action = "resize", Width = 3, Height = 2 },
        new Map_Edit_Operation { Action = "set-tile", X = 2, Y = 1, Tile = 9 }
      ]
    });

    Cli_Process_Result result = await Cli_Test_Helpers.run_cli_async(
      "map", "edit",
      "--spec", spec_path,
      "--width", "2",
      "--height", "2",
      "--tileset", "01020304",
      "--output", direct_output);

    Cli_Test_Helpers.assert_success(result);
    Assert.IsFalse(File.Exists(spec_output), "The direct output must override the spec output.");
    Map_Document document = new Text_Map_Codec().read(direct_output);
    Assert.AreEqual(3, document.Width);
    Assert.AreEqual(2, document.Height);
    Assert.AreEqual("01020304", document.Tileset);
    Assert.AreEqual(7, document.Tiles[0, 0]);
    Assert.AreEqual(7, document.Tiles[1, 1]);
    Assert.AreEqual(0, document.Tiles[2, 0]);
    Assert.AreEqual(9, document.Tiles[2, 1]);
  }

  [TestMethod]
  public async Task MapEditConvertsMapMarAndTmxWithoutLosingTilesetMetadata()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string input = directory.path("input.map");
    string mar = directory.path("converted.mar");
    string tmx = directory.path("converted.tmx");
    string round_trip = directory.path("round-trip.map");
    write_map(input, 2, 2, (x, y) => 1 + x + y * 2);

    Cli_Process_Result to_mar = await Cli_Test_Helpers.run_cli_async(
      "map", "edit",
      "--input", input,
      "--output", mar,
      "--format", "mar");
    Cli_Process_Result inspect_mar = await Cli_Test_Helpers.run_cli_async(
      "map", "inspect",
      "--input", mar,
      "--width", "2",
      "--height", "2",
      "--tileset", "01020304",
      "--json");
    Cli_Process_Result to_tmx = await Cli_Test_Helpers.run_cli_async(
      "map", "edit",
      "--input", mar,
      "--width", "2",
      "--height", "2",
      "--tileset", "01020304",
      "--assets-dir", Cli_Test_Helpers.repository_asset_root(),
      "--output", tmx);
    Cli_Process_Result inspect_tmx = await Cli_Test_Helpers.run_cli_async(
      "map", "inspect", "--input", tmx, "--json");
    Cli_Process_Result to_map = await Cli_Test_Helpers.run_cli_async(
      "map", "edit",
      "--input", tmx,
      "--assets-dir", Cli_Test_Helpers.repository_asset_root(),
      "--output", round_trip);

    Cli_Test_Helpers.assert_success(to_mar);
    Cli_Test_Helpers.assert_success(inspect_mar);
    Cli_Test_Helpers.assert_success(to_tmx);
    Cli_Test_Helpers.assert_success(inspect_tmx);
    Cli_Test_Helpers.assert_success(to_map);
    using JsonDocument mar_json = JsonDocument.Parse(inspect_mar.Standard_Output);
    Assert.AreEqual("mar", mar_json.RootElement.GetProperty("format").GetString());
    Assert.AreEqual(4, mar_json.RootElement.GetProperty("tiles")[1][1].GetInt32());
    using JsonDocument tmx_json = JsonDocument.Parse(inspect_tmx.Standard_Output);
    Assert.AreEqual("tmx", tmx_json.RootElement.GetProperty("format").GetString());
    StringAssert.EndsWith(
      tmx_json.RootElement.GetProperty("tilesetImageSource").GetString() ?? "",
      ".png");
    Map_Document tmx_document = new Tmx_Map_Codec().read(tmx);
    StringAssert.Contains(tmx_document.Tileset, "Fields");
    StringAssert.EndsWith(tmx_document.Tileset_Image_Source, ".png");
    string tmx_image = resolve_tmx_image(tmx, tmx_document.Tileset_Image_Source);
    Assert.IsTrue(File.Exists(tmx_image), $"TMX image source did not resolve: {tmx_image}");

    string nested_directory = directory.path("nested");
    Directory.CreateDirectory(nested_directory);
    string rebased_tmx = Path.Combine(nested_directory, "rebased.tmx");
    Cli_Process_Result rebase = await Cli_Test_Helpers.run_cli_async(
      "map", "edit",
      "--input", tmx,
      "--output", rebased_tmx);
    Cli_Test_Helpers.assert_success(rebase);
    Map_Document rebased_document = new Tmx_Map_Codec().read(rebased_tmx);
    Assert.AreEqual(
      Path.GetFullPath(tmx_image),
      Path.GetFullPath(resolve_tmx_image(rebased_tmx, rebased_document.Tileset_Image_Source)));

    string uri_tmx = directory.path("uri-source.tmx");
    Map_Document uri_document = tmx_document.clone();
    uri_document.Tileset_Image_Source = new Uri(tmx_image).AbsoluteUri;
    new Tmx_Map_Codec().write(uri_tmx, uri_document);
    string rebased_uri_tmx = Path.Combine(nested_directory, "uri-rebased.tmx");
    Cli_Process_Result rebase_uri = await Cli_Test_Helpers.run_cli_async(
      "map", "edit",
      "--input", uri_tmx,
      "--output", rebased_uri_tmx);
    Cli_Test_Helpers.assert_success(rebase_uri);
    Map_Document rebased_uri_document = new Tmx_Map_Codec().read(rebased_uri_tmx);
    Assert.AreEqual(
      Path.GetFullPath(tmx_image),
      Path.GetFullPath(resolve_tmx_image(
        rebased_uri_tmx,
        rebased_uri_document.Tileset_Image_Source)));

    Map_Document map_document = new Text_Map_Codec().read(round_trip);
    Assert.AreEqual("01020304", map_document.Tileset);
    CollectionAssert.AreEqual(File.ReadAllBytes(input), File.ReadAllBytes(round_trip));
  }

  [TestMethod]
  public async Task MapEditInPlaceIsAtomicAndInvalidEditsDoNotMutateInput()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string input = directory.path("input.map");
    string invalid_spec = directory.path("invalid.json");
    string valid_spec = directory.path("valid.json");
    write_map(input, 2, 1, (x, _) => x + 1);
    byte[] original = File.ReadAllBytes(input);
    new Map_Job_Spec_Reader().write_job(invalid_spec, new Map_Job_Spec
    {
      Version = 1,
      Operation = "edit",
      Edits =
      [
        new Map_Edit_Operation { Action = "set-tile", X = 0, Y = 0, Tile = 8 },
        new Map_Edit_Operation { Action = "set-tile", X = 2, Y = 0, Tile = 9 }
      ]
    });

    Cli_Process_Result invalid = await Cli_Test_Helpers.run_cli_async(
      "map", "edit",
      "--input", input,
      "--in-place",
      "--spec", invalid_spec);

    Cli_Test_Helpers.assert_error(invalid, "outside the 2x1 map");
    CollectionAssert.AreEqual(original, File.ReadAllBytes(input));
    Assert.IsFalse(
      Directory.GetFiles(directory.Root, $".{Path.GetFileName(input)}.*.tmp").Any(),
      "Failed in-place edits must not leave a temporary file.");

    new Map_Job_Spec_Reader().write_job(valid_spec, new Map_Job_Spec
    {
      Version = 1,
      Operation = "edit",
      Edits = [new Map_Edit_Operation { Action = "set-tile", X = 0, Y = 0, Tile = 8 }]
    });
    Cli_Process_Result valid = await Cli_Test_Helpers.run_cli_async(
      "map", "edit",
      "--input", input,
      "--in-place",
      "--spec", valid_spec);

    Cli_Test_Helpers.assert_success(valid);
    Assert.AreEqual(8, new Text_Map_Codec().read(input).Tiles[0, 0]);
  }

  [TestMethod]
  public async Task StandaloneMapEditRejectsTransientStateOnlyChanges()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string lock_spec = directory.path("lock.json");
    string lock_output = directory.path("lock-output.map");
    new Map_Job_Spec_Reader().write_job(lock_spec, new Map_Job_Spec
    {
      Version = 1,
      Operation = "edit",
      Width = 1,
      Height = 1,
      Tileset = "01020304",
      Output = lock_output,
      Edits = [new Map_Edit_Operation { Action = "lock", X = 0, Y = 0 }]
    });

    Cli_Process_Result lock_result = await Cli_Test_Helpers.run_cli_async(
      "map", "edit", "--spec", lock_spec);

    Cli_Test_Helpers.assert_error(lock_result, "standalone map edit cannot persist");
    Assert.IsFalse(File.Exists(lock_output));

    string erase_spec = directory.path("erase.json");
    string erase_output = directory.path("erase-output.map");
    new Map_Job_Spec_Reader().write_job(erase_spec, new Map_Job_Spec
    {
      Version = 1,
      Operation = "edit",
      Width = 1,
      Height = 1,
      Tileset = "01020304",
      Output = erase_output,
      Edits = [new Map_Edit_Operation { Action = "erase", X = 0, Y = 0 }]
    });

    Cli_Process_Result erase = await Cli_Test_Helpers.run_cli_async(
      "map", "edit", "--spec", erase_spec);

    Cli_Test_Helpers.assert_error(erase, "standalone map edit cannot persist");
    Assert.IsFalse(File.Exists(erase_output));
  }

  [TestMethod]
  public async Task InspectAndTilesetDiscoveryHaveStableJson()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string input = directory.path("inspect.map");
    write_map(input, 2, 2, (x, y) => x + y * 2);
    string assets = directory.path("assets");
    string tilesets_dir = Path.Combine(assets, "Tilesets");
    Directory.CreateDirectory(tilesets_dir);
    Tileset_Asset bundled = new Tileset_Catalog(Cli_Test_Helpers.repository_asset_root()).resolve(
      "01020304",
      require_image: true,
      require_generation_data: false);
    File.Copy(bundled.Image_Path, Path.Combine(tilesets_dir, Path.GetFileName(bundled.Image_Path)));
    File.Copy(
      Path.Combine(Cli_Test_Helpers.repository_asset_root(), "Tileset_Data.xml"),
      Path.Combine(assets, "Tileset_Data.xml"));
    File.Copy(
      Path.Combine(Cli_Test_Helpers.repository_asset_root(), "Terrain_Data.xml"),
      Path.Combine(assets, "Terrain_Data.xml"));

    Cli_Process_Result inspect = await Cli_Test_Helpers.run_cli_async(
      "map", "inspect", "--input", input, "--json");
    Cli_Process_Result list = await Cli_Test_Helpers.run_cli_async(
      "tilesets", "list", "--assets-dir", assets, "--json");
    Cli_Process_Result terrain = await Cli_Test_Helpers.run_cli_async(
      "tilesets", "terrain",
      "--tileset", "01020304",
      "--assets-dir", assets,
      "--json");

    Cli_Test_Helpers.assert_success(inspect);
    Cli_Test_Helpers.assert_success(list);
    Cli_Test_Helpers.assert_success(terrain);
    using JsonDocument inspect_json = JsonDocument.Parse(inspect.Standard_Output);
    Assert.AreEqual("map", inspect_json.RootElement.GetProperty("format").GetString());
    Assert.AreEqual(2, inspect_json.RootElement.GetProperty("width").GetInt32());
    Assert.AreEqual(3, inspect_json.RootElement.GetProperty("tiles")[1][1].GetInt32());
    using JsonDocument list_json = JsonDocument.Parse(list.Standard_Output);
    Assert.IsGreaterThan(0, list_json.RootElement.GetProperty("tilesets").GetArrayLength());
    JsonElement fields = list_json.RootElement.GetProperty("tilesets")
      .EnumerateArray()
      .Single(item => item.GetProperty("name").GetString()!.Contains("01020304", StringComparison.Ordinal));
    Assert.IsTrue(fields.GetProperty("hasImage").GetBoolean());
    Assert.IsFalse(fields.GetProperty("hasGenerationData").GetBoolean());
    using JsonDocument terrain_json = JsonDocument.Parse(terrain.Standard_Output);
    Assert.AreEqual(1024, terrain_json.RootElement.GetProperty("tileCount").GetInt32());
    Assert.AreEqual(1024, terrain_json.RootElement.GetProperty("tileTerrainTags").GetArrayLength());
    Assert.IsTrue(
      terrain_json.RootElement.GetProperty("terrains")
        .EnumerateArray()
        .Any(item => item.GetProperty("name").GetString() == "Plains"));
  }

  [TestMethod]
  public async Task GenerateRepairAndBatchComposeOrderedEdits()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string generate_spec = directory.path("generate.json");
    string generated = directory.path("generated.map");
    new Map_Job_Spec_Reader().write_job(generate_spec, new Map_Job_Spec
    {
      Version = 1,
      Operation = "generate",
      Width = 1,
      Height = 1,
      Tileset = "01020304",
      Output = generated,
      Seed = 4,
      Edits =
      [
        new Map_Edit_Operation { Action = "resize", Width = 2, Height = 1 },
        new Map_Edit_Operation
        {
          Action = "set-tile",
          Shape = "rectangle",
          X = 0,
          Y = 0,
          EndX = 1,
          EndY = 0,
          Tile = 7
        }
      ]
    });

    Cli_Process_Result generate = await Cli_Test_Helpers.run_cli_async(
      "generate", "--spec", generate_spec);

    Cli_Test_Helpers.assert_success(generate);
    Map_Document generated_document = new Text_Map_Codec().read(generated);
    Assert.AreEqual(2, generated_document.Width);
    Assert.AreEqual(7, generated_document.Tiles[0, 0]);
    Assert.AreEqual(7, generated_document.Tiles[1, 0]);

    string repair_input = directory.path("repair-input.map");
    string repair_output = directory.path("repair-output.map");
    write_map(repair_input, 1, 1, (_, _) => 7);
    string repair_spec = directory.path("repair.json");
    new Map_Job_Spec_Reader().write_job(repair_spec, new Map_Job_Spec
    {
      Version = 1,
      Operation = "repair",
      Input = repair_input,
      Output = repair_output,
      Tileset = "01020304",
      Algorithm = "experimental",
      Seed = 4,
      Edits =
      [
        new Map_Edit_Operation { Action = "resize", Width = 2, Height = 1 },
        new Map_Edit_Operation { Action = "set-tile", X = 1, Y = 0, Tile = 7 },
        new Map_Edit_Operation { Action = "set-tile", X = 0, Y = 0, Tile = 0 }
      ]
    });

    Cli_Process_Result repair = await Cli_Test_Helpers.run_cli_async(
      "repair", "--spec", repair_spec);

    Cli_Test_Helpers.assert_success(repair);
    Map_Document repaired_document = new Text_Map_Codec().read(repair_output);
    Assert.AreEqual(2, repaired_document.Width);
    Assert.AreEqual(
      0,
      repaired_document.Tiles[0, 0],
      "An explicitly edited tile 0 must stay drawn after imported zero holes are reopened.");
    Assert.AreEqual(7, repaired_document.Tiles[1, 0]);

    string manifest = directory.path("manifest.json");
    string batch_output = directory.path("batch.map");
    new Map_Job_Spec_Reader().write_manifest(manifest, new Map_Job_Manifest
    {
      Version = 1,
      Jobs =
      [
        new Map_Job_Spec
        {
          Version = 1,
          Operation = "edit",
          Width = 2,
          Height = 1,
          Tileset = "01020304",
          Output = batch_output,
          Edits =
          [
            new Map_Edit_Operation
            {
              Action = "set-tile",
              Shape = "rectangle",
              X = 0,
              Y = 0,
              EndX = 1,
              EndY = 0,
              Tile = 6
            }
          ]
        }
      ]
    });

    Cli_Process_Result batch = await Cli_Test_Helpers.run_cli_async(
      "batch", "--manifest", manifest);

    Cli_Test_Helpers.assert_success(batch);
    StringAssert.Contains(batch.Standard_Output, "Batch: 1 succeeded");
    Cli_Test_Helpers.assert_all_tiles(new Text_Map_Codec().read(batch_output), 6);
  }

  private static void write_map(
    string path,
    int width,
    int height,
    Func<int, int, int> tile)
  {
    int[,] tiles = new int[width, height];
    for (int y = 0; y < height; ++y)
    {
      for (int x = 0; x < width; ++x)
        tiles[x, y] = tile(x, y);
    }
    new Text_Map_Codec().write(path, new Map_Document(tiles, "01020304"));
  }

  private static string resolve_tmx_image(string tmx_path, string image_source)
  {
    string filesystem_source = image_source.Replace('/', Path.DirectorySeparatorChar);
    return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(tmx_path)!, filesystem_source));
  }
}
