namespace FE_Map_Creator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CliBatchIntegrationTests
{
  [TestMethod]
  public async Task GenerateCountUsesDeterministicSeedsAndNameTemplates()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string default_first = directory.path("default-first");
    string default_second = directory.path("default-second");
    string custom = directory.path("custom");
    string explicit_format = directory.path("explicit-format");

    Cli_Process_Result first = await Cli_Test_Helpers.run_cli_async(
      "generate",
      "--width", "2",
      "--height", "1",
      "--tileset", "01020304",
      "--count", "3",
      "--output-dir", default_first,
      "--seed", "12345");
    Cli_Process_Result second = await Cli_Test_Helpers.run_cli_async(
      "generate",
      "--width", "2",
      "--height", "1",
      "--tileset", "01020304",
      "--count", "3",
      "--output-dir", default_second,
      "--seed", "12345");
    Cli_Process_Result custom_template = await Cli_Test_Helpers.run_cli_async(
      "generate",
      "--width", "2",
      "--height", "1",
      "--tileset", "01020304",
      "--count", "2",
      "--output-dir", custom,
      "--seed", "12345",
      "--name-template", "job-{index}-{seed}.tmx");
    Cli_Process_Result explicit_mar = await Cli_Test_Helpers.run_cli_async(
      "generate",
      "--width", "1",
      "--height", "1",
      "--tileset", "01020304",
      "--count", "1",
      "--output-dir", explicit_format,
      "--seed", "12345",
      "--format", "mar",
      "--name-template", "custom-{index}");

    Cli_Test_Helpers.assert_success(first);
    Cli_Test_Helpers.assert_success(second);
    Cli_Test_Helpers.assert_success(custom_template);
    Cli_Test_Helpers.assert_success(explicit_mar);
    StringAssert.Contains(first.Standard_Error, "Generate progress:");
    Assert.IsFalse(first.Standard_Output.Contains("progress:", StringComparison.OrdinalIgnoreCase), first.describe());

    CollectionAssert.AreEqual(
      new[] { "map-1.map", "map-2.map", "map-3.map" },
      Directory.GetFiles(default_first).Select(Path.GetFileName).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray());
    CollectionAssert.AreEqual(
      Directory.GetFiles(default_first).Select(Path.GetFileName).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray(),
      Directory.GetFiles(default_second).Select(Path.GetFileName).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray());

    string[] default_names = Directory.GetFiles(default_first)
      .Select(Path.GetFileName)
      .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
      .ToArray()!;
    foreach (string name in default_names)
    {
      CollectionAssert.AreEqual(
        File.ReadAllBytes(Path.Combine(default_first, name)),
        File.ReadAllBytes(Path.Combine(default_second, name)),
        $"Batch output {name} was not deterministic across repeated runs.");
    }

    int[] first_seeds = Cli_Test_Helpers.extract_reported_seeds(first.Standard_Output);
    int[] second_seeds = Cli_Test_Helpers.extract_reported_seeds(second.Standard_Output);
    CollectionAssert.AreEqual(first_seeds, second_seeds);
    Assert.HasCount(3, first_seeds);
    Assert.AreEqual(3, first_seeds.Distinct().Count(), first.describe());

    string[] custom_names = Directory.GetFiles(custom)
      .Select(Path.GetFileName)
      .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
      .ToArray()!;
    Assert.HasCount(2, custom_names);
    int[] custom_seeds = Cli_Test_Helpers.extract_reported_seeds(custom_template.Standard_Output);
    for (int index = 0; index < custom_names.Length; ++index)
    {
      StringAssert.EndsWith(custom_names[index], ".tmx");
      StringAssert.Contains(custom_names[index], $"job-{index + 1}-{custom_seeds[index]}");
      Map_Document document = new Tmx_Map_Codec().read(Path.Combine(custom, custom_names[index]));
      Assert.AreEqual(2, document.Width);
      Assert.AreEqual(1, document.Height);
    }

    string mar_output = Path.Combine(explicit_format, "custom-1.mar");
    Assert.IsTrue(File.Exists(mar_output), "Expected the batch name template to pick up the explicit MAR extension.");
    Assert.AreEqual(2L, new FileInfo(mar_output).Length);
    StringAssert.Contains(explicit_mar.Standard_Output, "custom-1.mar");
  }

  [TestMethod]
  public async Task GenerateCountRejectsDuplicateOutputCollisionsBeforeWriting()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string output_dir = directory.path("out");

    Cli_Process_Result result = await Cli_Test_Helpers.run_cli_async(
      "generate",
      "--width", "1",
      "--height", "1",
      "--tileset", "01020304",
      "--count", "2",
      "--output-dir", output_dir,
      "--name-template", "same");

    Cli_Test_Helpers.assert_error(result, "produced duplicate output path");
    Assert.IsTrue(!Directory.Exists(output_dir) || !Directory.EnumerateFileSystemEntries(output_dir).Any(), result.describe());
  }

  [TestMethod]
  public async Task GenerateCountAggregatesIncompleteJobsAsBatchFailure()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string spec_path = directory.path("spec.json");
    string output_dir = directory.path("out");
    Map_Job_Spec spec = new Map_Job_Spec()
    {
      Version = 1,
      Operation = "generate",
      Width = 1,
      Height = 1,
      Tileset = "01020304",
      Terrain = new int[][]
      {
        new int[] { 999 }
      }
    };
    new Map_Job_Spec_Reader().write_job(spec_path, spec);

    Cli_Process_Result result = await Cli_Test_Helpers.run_cli_async(
      "generate",
      "--count", "2",
      "--output-dir", output_dir,
      "--spec", spec_path,
      "--seed", "7");

    Assert.AreEqual(3, result.Exit_Code, result.describe());
    StringAssert.Contains(result.All_Output, "Generation: 0 succeeded, 2 incomplete, 0 failed of 2 job(s).");
    foreach (string output in Directory.GetFiles(output_dir, "*.map"))
      Cli_Test_Helpers.assert_all_tiles(new Text_Map_Codec().read(output), 0);
  }

  [TestMethod]
  public async Task RepairDirectoryPreservesRelativePathsSupportsMarSidecarAndExcludesNestedOutputDirOnRerun()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string input_dir = directory.path("input");
    string nested_dir = directory.path("input", "sub");
    string output_dir = directory.path("input", "repaired");
    Directory.CreateDirectory(nested_dir);

    string expected_text = directory.path("expected.map");
    string expected_mar = directory.path("expected.mar");
    await generate_fixture_async(expected_text, "map");
    await generate_fixture_async(expected_mar, "mar");

    string root_input = directory.path("input", "a.map");
    string nested_input = directory.path("input", "sub", "b.map");
    string mar_input = directory.path("input", "sub", "c.mar");
    Cli_Test_Helpers.create_hole_map(expected_text, root_input, 2, 0);
    Cli_Test_Helpers.create_hole_map(expected_text, nested_input, 2, 0);
    Cli_Test_Helpers.create_hole_mar(expected_mar, mar_input, 4, 1, "01020304", 2, 0);
    new Map_Job_Spec_Reader().write_job(directory.path("input", "sub", "c.mapgen.json"), new Map_Job_Spec()
    {
      Version = 1,
      Operation = "repair",
      Width = 4,
      Height = 1,
      Tileset = "01020304"
    });

    Cli_Process_Result first = await Cli_Test_Helpers.run_cli_async(
      "repair",
      "--input-dir", input_dir,
      "--output-dir", output_dir,
      "--pattern", "*.*",
      "--recursive",
      "--seed", "12345");
    Cli_Process_Result rerun = await Cli_Test_Helpers.run_cli_async(
      "repair",
      "--input-dir", input_dir,
      "--output-dir", output_dir,
      "--pattern", "*.*",
      "--recursive",
      "--seed", "12345",
      "--force");

    Cli_Test_Helpers.assert_success(first);
    Cli_Test_Helpers.assert_success(rerun);
    StringAssert.Contains(first.Standard_Error, "Repair progress:");
    Assert.IsFalse(first.Standard_Output.Contains("progress:", StringComparison.OrdinalIgnoreCase), first.describe());
    StringAssert.Contains(first.Standard_Output, "Repair: 3 succeeded, 0 incomplete, 0 failed of 3 job(s).");
    StringAssert.Contains(rerun.Standard_Output, "Repair: 3 succeeded, 0 incomplete, 0 failed of 3 job(s).");

    CollectionAssert.AreEqual(File.ReadAllBytes(expected_text), File.ReadAllBytes(directory.path("input", "repaired", "a.map")));
    CollectionAssert.AreEqual(File.ReadAllBytes(expected_text), File.ReadAllBytes(directory.path("input", "repaired", "sub", "b.map")));
    CollectionAssert.AreEqual(File.ReadAllBytes(expected_mar), File.ReadAllBytes(directory.path("input", "repaired", "sub", "c.mar")));
    Assert.IsFalse(rerun.All_Output.Contains("repaired\\repaired", StringComparison.OrdinalIgnoreCase), rerun.describe());
  }

  [TestMethod]
  public async Task RepairDirectorySupportsContinueOnErrorAndFailFast()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string input_dir = directory.path("input");
    Directory.CreateDirectory(input_dir);
    string expected = directory.path("expected.map");
    await generate_fixture_async(expected, "map");

    File.WriteAllText(directory.path("input", "01-bad.map"),
      """
      01020304
      """);
    Cli_Test_Helpers.create_hole_map(expected, directory.path("input", "02-good.map"), 2, 0);
    Cli_Test_Helpers.create_hole_map(expected, directory.path("input", "03-good.map"), 2, 0);

    Cli_Process_Result continued = await Cli_Test_Helpers.run_cli_async(
      "repair",
      "--input-dir", input_dir,
      "--output-dir", directory.path("continued"),
      "--seed", "12345");
    Cli_Process_Result fail_fast = await Cli_Test_Helpers.run_cli_async(
      "repair",
      "--input-dir", input_dir,
      "--output-dir", directory.path("fail-fast"),
      "--seed", "12345",
      "--fail-fast");

    Assert.AreEqual(3, continued.Exit_Code, continued.describe());
    StringAssert.Contains(continued.All_Output, "Repair: 2 succeeded, 0 incomplete, 1 failed of 3 job(s).");
    Assert.IsTrue(File.Exists(directory.path("continued", "02-good.map")));
    Assert.IsTrue(File.Exists(directory.path("continued", "03-good.map")));

    Assert.AreEqual(3, fail_fast.Exit_Code, fail_fast.describe());
    StringAssert.Contains(fail_fast.All_Output, "Repair: 0 succeeded, 0 incomplete, 1 failed, 2 not attempted of 3 job(s).");
    Assert.IsFalse(Directory.EnumerateFiles(directory.path("fail-fast"), "*", SearchOption.AllDirectories).Any(), fail_fast.describe());
  }

  [TestMethod]
  public async Task ManifestBatchSupportsMixedGenerateAndRepairWithManifestRelativePaths()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string original = directory.path("original.map");
    string template = directory.path("template.map");
    string hole = directory.path("hole.map");
    string mar = directory.path("input.mar");
    string manifest = directory.path("manifest.json");
    await generate_fixture_async(original, "map");
    await generate_fixture_async(mar, "mar");
    Cli_Test_Helpers.create_hole_map(original, template, 2, 0);
    Cli_Test_Helpers.create_hole_map(original, hole, 2, 0);

    Map_Job_Manifest batch = new Map_Job_Manifest()
    {
      Version = 1,
      Jobs = new[]
      {
        new Map_Job_Spec()
        {
          Version = 1,
          Operation = "generate",
          Template = "template.map",
          Output = Path.Combine("generated", "out.map"),
          Tileset = "01020304",
          Seed = 12345,
          Drawn = new[]
          {
            new[] { true, true, false, true }
          },
          Locked = new[]
          {
            new[] { true, false, false, false }
          }
        },
        new Map_Job_Spec()
        {
          Version = 1,
          Operation = "repair",
          Input = "hole.map",
          Output = Path.Combine("repaired", "out.map"),
          Seed = 12345
        },
        new Map_Job_Spec()
        {
          Version = 1,
          Operation = "repair",
          Input = "input.mar",
          Output = Path.Combine("repaired", "out.mar"),
          Width = 4,
          Height = 1,
          Tileset = "01020304",
          Seed = 12345
        }
      }
    };
    new Map_Job_Spec_Reader().write_manifest(manifest, batch);

    Cli_Process_Result result = await Cli_Test_Helpers.run_cli_async(
      "batch",
      "--manifest", manifest);

    Cli_Test_Helpers.assert_success(result);
    StringAssert.Contains(result.Standard_Output, "Batch: 3 succeeded, 0 incomplete, 0 failed of 3 job(s).");
    StringAssert.Contains(result.Standard_Error, "Generate progress:");
    StringAssert.Contains(result.Standard_Error, "Repair progress:");
    Assert.IsFalse(result.Standard_Output.Contains("progress:", StringComparison.OrdinalIgnoreCase), result.describe());
    CollectionAssert.AreEqual(File.ReadAllBytes(original), File.ReadAllBytes(directory.path("generated", "out.map")));
    CollectionAssert.AreEqual(File.ReadAllBytes(original), File.ReadAllBytes(directory.path("repaired", "out.map")));
    CollectionAssert.AreEqual(File.ReadAllBytes(mar), File.ReadAllBytes(directory.path("repaired", "out.mar")));
  }

  [TestMethod]
  public async Task MarBatchPreflightPreventsDirectoryAndManifestPartialOutputs()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string expected_map = directory.path("expected.map");
    string mar = directory.path("missing-metadata.mar");
    await generate_fixture_async(expected_map, "map");
    await generate_fixture_async(mar, "mar");

    string input_dir = directory.path("input");
    Directory.CreateDirectory(input_dir);
    Cli_Test_Helpers.create_hole_map(expected_map, directory.path("input", "01-good.map"), 2, 0);
    File.Copy(mar, directory.path("input", "02-bad.mar"));
    string output_dir = directory.path("directory-output");

    Cli_Process_Result directory_result = await Cli_Test_Helpers.run_cli_async(
      "repair",
      "--input-dir", input_dir,
      "--output-dir", output_dir,
      "--pattern", "*.*",
      "--seed", "12345");

    Cli_Test_Helpers.assert_error(directory_result, "requires positive width, positive height, and a tileset identifier");
    Assert.IsFalse(Directory.Exists(output_dir), directory_result.describe());

    string manifest = directory.path("manifest.json");
    string generated_output = directory.path("manifest-output", "generated.map");
    new Map_Job_Spec_Reader().write_manifest(manifest, new Map_Job_Manifest()
    {
      Version = 1,
      Jobs = new[]
      {
        new Map_Job_Spec()
        {
          Version = 1,
          Operation = "generate",
          Width = 2,
          Height = 1,
          Tileset = "01020304",
          Output = Path.Combine("manifest-output", "generated.map"),
          Seed = 1
        },
        new Map_Job_Spec()
        {
          Version = 1,
          Operation = "repair",
          Input = "missing-metadata.mar",
          Output = Path.Combine("manifest-output", "repaired.mar")
        }
      }
    });

    Cli_Process_Result manifest_result = await Cli_Test_Helpers.run_cli_async(
      "batch",
      "--manifest", manifest);

    Cli_Test_Helpers.assert_error(manifest_result, "requires positive width, positive height, and a tileset identifier");
    StringAssert.Contains(manifest_result.All_Output, "batch manifest job");
    Assert.IsFalse(File.Exists(generated_output), manifest_result.describe());
  }

  [TestMethod]
  public async Task ManifestBatchAggregatesInvalidOperationsAndHonorsFailFast()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string hole = directory.path("hole.map");
    string manifest = directory.path("manifest.json");
    Cli_Test_Helpers.create_hole_map(await create_expected_map_async(directory), hole, 2, 0);

    Map_Job_Manifest batch = new Map_Job_Manifest()
    {
      Version = 1,
      Jobs = new[]
      {
        new Map_Job_Spec()
        {
          Version = 1,
          Operation = "batch"
        },
        new Map_Job_Spec()
        {
          Version = 1,
          Operation = "nope"
        },
        new Map_Job_Spec()
        {
          Version = 1,
          Operation = "generate",
          Width = 4,
          Height = 1,
          Tileset = "01020304",
          Output = Path.Combine("good", "generated.map"),
          Seed = 12345
        },
        new Map_Job_Spec()
        {
          Version = 1,
          Operation = "repair",
          Input = "hole.map",
          Output = Path.Combine("good", "repaired.map"),
          Seed = 12345
        }
      }
    };
    new Map_Job_Spec_Reader().write_manifest(manifest, batch);

    Cli_Process_Result fail_fast = await Cli_Test_Helpers.run_cli_async(
      "batch",
      "--manifest", manifest,
      "--fail-fast");

    Assert.AreEqual(3, fail_fast.Exit_Code, fail_fast.describe());
    StringAssert.Contains(fail_fast.All_Output, "nested batch operations are not supported");
    StringAssert.Contains(fail_fast.All_Output, "Batch: 0 succeeded, 0 incomplete, 1 failed, 3 not attempted of 4 job(s).");
    Assert.IsFalse(File.Exists(directory.path("good", "generated.map")), fail_fast.describe());
    Assert.IsFalse(File.Exists(directory.path("good", "repaired.map")), fail_fast.describe());

    Cli_Process_Result continued = await Cli_Test_Helpers.run_cli_async(
      "batch",
      "--manifest", manifest);

    Assert.AreEqual(3, continued.Exit_Code, continued.describe());
    StringAssert.Contains(continued.All_Output, "nested batch operations are not supported");
    StringAssert.Contains(continued.All_Output, "unsupported operation \"nope\"");
    StringAssert.Contains(continued.All_Output, "Batch: 2 succeeded, 0 incomplete, 2 failed of 4 job(s).");
    Assert.IsTrue(File.Exists(directory.path("good", "generated.map")));
    Assert.IsTrue(File.Exists(directory.path("good", "repaired.map")));
  }

  private static async Task generate_fixture_async(string output, string format)
  {
    Cli_Process_Result result = await Cli_Test_Helpers.run_cli_async(
      "generate",
      "--width", "4",
      "--height", "1",
      "--tileset", "01020304",
      "--output", output,
      "--seed", "12345",
      "--format", format);
    Cli_Test_Helpers.assert_success(result);
  }

  private static async Task<string> create_expected_map_async(Cli_Temporary_Directory directory)
  {
    string original = directory.path("original.map");
    await generate_fixture_async(original, "map");
    return original;
  }
}
