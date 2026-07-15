using System.Diagnostics;

namespace FE_Map_Creator.Tests;

[TestClass]
[DoNotParallelize]
public sealed class CliIntegrationTests
{
  [TestMethod]
  public async Task HelpAndBundledTilesetsListSucceed()
  {
    Cli_Process_Result help = await run_cli_async("--help");

    assert_success(help);
    StringAssert.Contains(help.Standard_Output, "FE Map Creator command-line tool for generating and repairing maps.");
    StringAssert.Contains(help.Standard_Output, "generate");
    StringAssert.Contains(help.Standard_Output, "repair");
    StringAssert.Contains(help.Standard_Output, "tilesets");

    Cli_Process_Result repair_help = await run_cli_async("repair", "--help");
    assert_success(repair_help);
    StringAssert.Contains(repair_help.Standard_Output, "MAR");
    StringAssert.Contains(repair_help.Standard_Output, "--width");
    StringAssert.Contains(repair_help.Standard_Output, "--algorithm");
    StringAssert.Contains(repair_help.Standard_Output, ".mapgen.json");
    string packaged_readme = Path.Combine(Path.GetDirectoryName(cli_dll_path())!, "README.md");
    Assert.IsTrue(File.Exists(packaged_readme), "The CLI output must include its README.");
    StringAssert.Contains(File.ReadAllText(packaged_readme), "MAR files contain only tile values");

    Cli_Process_Result list = await run_cli_async("tilesets", "list");

    assert_success(list);
    StringAssert.Contains(list.Standard_Output, "FE6 - Fields - 01020304");
    StringAssert.Contains(list.Standard_Output, "image:");
    StringAssert.Contains(list.Standard_Output, "generation-data:");
    StringAssert.Contains(list.Standard_Output, "Listed ");
  }

  [TestMethod]
  public async Task SeededTextMapGenerationIsDeterministicAndReportsSeed()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string first = directory.path("first.map");
    string second = directory.path("second.map");

    Cli_Process_Result first_result = await run_cli_async(
      "generate",
      "--width", "4",
      "--height", "3",
      "--tileset", "01020304",
      "--output", first,
      "--seed", "12345");
    Cli_Process_Result second_result = await run_cli_async(
      "generate",
      "--width", "4",
      "--height", "3",
      "--tileset", "01020304",
      "--output", second,
      "--seed", "12345");

    assert_success(first_result);
    assert_success(second_result);
    StringAssert.Contains(first_result.Standard_Output, "seed 12345");
    StringAssert.Contains(second_result.Standard_Output, "seed 12345");
    StringAssert.Contains(first_result.Standard_Error, "Generate progress:");
    Assert.IsFalse(first_result.Standard_Output.Contains("progress:", StringComparison.OrdinalIgnoreCase), first_result.describe());

    string[] lines = File.ReadAllLines(first);
    CollectionAssert.AreEqual(new[] { "01020304", "3 4" }, lines.Take(2).ToArray());
    CollectionAssert.AreEqual(File.ReadAllBytes(first), File.ReadAllBytes(second));

    Map_Document document = new Text_Map_Codec().read(first);
    Assert.AreEqual(4, document.Width);
    Assert.AreEqual(3, document.Height);
    Assert.AreEqual("01020304", document.Tileset);
  }

  [TestMethod]
  public async Task ExperimentalAlgorithmCanBeSelectedDirectlyAndFromSpec()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string direct_output = directory.path("direct.map");
    string spec_output = directory.path("spec.map");
    string override_output = directory.path("override.map");
    string spec_path = directory.path("experimental.json");

    Cli_Process_Result direct = await run_cli_async(
      "generate",
      "--width", "3",
      "--height", "2",
      "--tileset", "01020304",
      "--output", direct_output,
      "--algorithm", "experimental",
      "--experimental-search-node-limit", "10000",
      "--experimental-restarts", "5",
      "--experimental-nogood-limit", "64",
      "--seed", "77");

    new Map_Job_Spec_Reader().write_job(spec_path, new Map_Job_Spec()
    {
      Version = 1,
      Operation = "generate",
      Width = 3,
      Height = 2,
      Tileset = "01020304",
      Output = "spec.map",
      Algorithm = "experimental",
      ExperimentalSearchNodeLimit = 10000,
      ExperimentalRestartCount = 5,
      ExperimentalNogoodLimit = 64,
      Seed = 77
    });
    Cli_Process_Result from_spec = await run_cli_async("generate", "--spec", spec_path);
    Cli_Process_Result overridden = await run_cli_async(
      "generate",
      "--spec", spec_path,
      "--output", override_output,
      "--algorithm", "legacy");

    assert_success(direct);
    assert_success(from_spec);
    assert_success(overridden);
    StringAssert.Contains(direct.Standard_Output, "using experimental algorithm");
    StringAssert.Contains(from_spec.Standard_Output, "using experimental algorithm");
    StringAssert.Contains(direct.Standard_Output, "restart(s)");
    StringAssert.Contains(direct.Standard_Output, "nogood(s) learned");
    Assert.IsFalse(overridden.Standard_Output.Contains("experimental algorithm", StringComparison.OrdinalIgnoreCase), overridden.describe());
    CollectionAssert.AreEqual(File.ReadAllBytes(direct_output), File.ReadAllBytes(spec_output));
  }

  [TestMethod]
  public async Task ProgressReportsSeedOnlyGenerationAndNoOpRepairCompletion()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string generated = directory.path("generated.map");
    string complete = directory.path("complete.map");
    string repaired = directory.path("repaired.map");

    Cli_Process_Result generation = await run_cli_async(
      "generate",
      "--width", "1",
      "--height", "1",
      "--tileset", "01020304",
      "--output", generated,
      "--seed", "5");
    File.WriteAllText(complete,
      """
      01020304
      1 1
      1
      """);
    Cli_Process_Result repair = await run_cli_async(
      "repair",
      "--input", complete,
      "--output", repaired,
      "--seed", "5");

    assert_success(generation);
    assert_success(repair);
    StringAssert.Contains(generation.Standard_Error, "complete (1 cell(s) processed)");
    StringAssert.Contains(repair.Standard_Error, "complete (0 cell(s) processed)");
    Assert.IsFalse(generation.Standard_Output.Contains("progress:", StringComparison.OrdinalIgnoreCase), generation.describe());
    Assert.IsFalse(repair.Standard_Output.Contains("progress:", StringComparison.OrdinalIgnoreCase), repair.describe());
  }

  [TestMethod]
  public async Task GeneratesTmxAndMarAndRepairsTextHole()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string original = directory.path("original.map");
    string hole = directory.path("hole.map");
    string repaired = directory.path("repaired.map");
    string tmx = directory.path("generated.tmx");
    string mar = directory.path("generated.mar");

    Cli_Process_Result original_result = await run_cli_async(
      "generate",
      "--width", "4",
      "--height", "1",
      "--tileset", "01020304",
      "--output", original,
      "--seed", "12345");
    create_hole_map(original, hole, 2, 0);

    Cli_Process_Result tmx_result = await run_cli_async(
      "generate",
      "--width", "4",
      "--height", "1",
      "--tileset", "01020304",
      "--output", tmx,
      "--seed", "12345");
    Cli_Process_Result mar_result = await run_cli_async(
      "generate",
      "--width", "4",
      "--height", "1",
      "--tileset", "01020304",
      "--output", mar,
      "--seed", "12345");
    Cli_Process_Result repair_result = await run_cli_async(
      "repair",
      "--input", hole,
      "--output", repaired,
      "--seed", "12345");

    assert_success(original_result);
    assert_success(tmx_result);
    assert_success(mar_result);
    assert_success(repair_result);
    StringAssert.Contains(repair_result.Standard_Error, "Repair progress:");
    Assert.IsFalse(repair_result.Standard_Output.Contains("progress:", StringComparison.OrdinalIgnoreCase), repair_result.describe());

    Map_Document tmx_document = new Tmx_Map_Codec().read(tmx);
    Assert.AreEqual(4, tmx_document.Width);
    Assert.AreEqual(1, tmx_document.Height);
    Assert.AreEqual("FE6 - Fields - 01020304", tmx_document.Tileset);
    Assert.AreEqual("FE6 - Fields - 01020304.png", tmx_document.Tileset_Image_Source);
    string tmx_xml = File.ReadAllText(tmx);
    StringAssert.Contains(tmx_xml, "orientation=\"orthogonal\"");
    StringAssert.Contains(tmx_xml, "<tile gid=");

    Assert.AreEqual(8L, new FileInfo(mar).Length);
    Map_Document mar_document = new Mar_Map_Codec().read(mar, new Map_Read_Options()
    {
      Width = 4,
      Height = 1,
      Tileset = "01020304"
    });
    Assert.AreEqual(4, mar_document.Width);
    Assert.AreEqual(1, mar_document.Height);

    CollectionAssert.AreEqual(File.ReadAllBytes(original), File.ReadAllBytes(repaired));
  }

  [TestMethod]
  public async Task MarRepairSupportsOptionsAndSpecsAndPreflightsMissingMetadata()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string input = directory.path("input.mar");
    string direct_output = directory.path("direct.mar");
    string spec_output = directory.path("spec.mar");
    string valid_spec = directory.path("valid.json");
    string missing_spec = directory.path("missing.json");
    string protected_output = directory.path("protected.mar");

    Cli_Process_Result generated = await run_cli_async(
      "generate",
      "--width", "4",
      "--height", "1",
      "--tileset", "01020304",
      "--output", input,
      "--seed", "12345");
    assert_success(generated);

    Cli_Process_Result direct = await run_cli_async(
      "repair",
      "--input", input,
      "--output", direct_output,
      "--width", "4",
      "--height", "1",
      "--tileset", "01020304",
      "--seed", "12345");
    assert_success(direct);
    CollectionAssert.AreEqual(File.ReadAllBytes(input), File.ReadAllBytes(direct_output));

    new Map_Job_Spec_Reader().write_job(valid_spec, new Map_Job_Spec()
    {
      Version = 1,
      Operation = "repair",
      Input = "input.mar",
      Output = "spec.mar",
      Width = 4,
      Height = 1,
      Tileset = "01020304",
      Seed = 12345
    });
    Cli_Process_Result from_spec = await run_cli_async("repair", "--spec", valid_spec);
    assert_success(from_spec);
    CollectionAssert.AreEqual(File.ReadAllBytes(input), File.ReadAllBytes(spec_output));

    File.WriteAllText(protected_output, "unchanged");
    new Map_Job_Spec_Reader().write_job(missing_spec, new Map_Job_Spec()
    {
      Version = 1,
      Operation = "repair",
      Input = "input.mar",
      Output = "protected.mar"
    });
    Cli_Process_Result missing = await run_cli_async(
      "repair",
      "--spec", missing_spec,
      "--force");

    assert_error(missing, "requires positive width, positive height, and a tileset identifier");
    StringAssert.Contains(missing.All_Output, "never inferred");
    StringAssert.Contains(missing.All_Output, "--width");
    StringAssert.Contains(missing.All_Output, ".mapgen.json");
    Assert.AreEqual("unchanged", File.ReadAllText(protected_output));
  }

  [TestMethod]
  public async Task RepairReadsCsvTmxInput()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string input = directory.path("input.tmx");
    string output = directory.path("output.map");
    File.WriteAllText(input,
      """
      <map orientation="orthogonal" width="1" height="1">
        <tileset firstgid="7" name="01020304"><image source="FE6 - Fields - 01020304.png"/></tileset>
        <layer width="1" height="1"><data encoding="csv">8</data></layer>
      </map>
      """);

    Cli_Process_Result result = await run_cli_async(
      "repair",
      "--input", input,
      "--output", output,
      "--seed", "12345");

    assert_success(result);
    Map_Document document = new Text_Map_Codec().read(output);
    Assert.AreEqual(1, document.Width);
    Assert.AreEqual(1, document.Height);
    Assert.AreEqual(1, document.Tiles[0, 0]);
  }

  [TestMethod]
  public async Task RepairProtectsExistingOutputsAndSupportsForceAndInPlace()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string original = directory.path("original.map");
    string hole = directory.path("hole.map");
    string destination = directory.path("destination.map");
    string in_place = directory.path("in-place.map");

    Cli_Process_Result original_result = await run_cli_async(
      "generate",
      "--width", "4",
      "--height", "1",
      "--tileset", "01020304",
      "--output", original,
      "--seed", "12345");
    assert_success(original_result);

    create_hole_map(original, hole, 2, 0);
    create_hole_map(original, in_place, 2, 0);
    File.WriteAllText(destination, "existing");

    Cli_Process_Result blocked = await run_cli_async(
      "repair",
      "--input", hole,
      "--output", destination,
      "--seed", "12345");

    Assert.AreEqual(1, blocked.Exit_Code, blocked.describe());
    StringAssert.Contains(blocked.All_Output, "already exists");
    Assert.AreEqual("existing", File.ReadAllText(destination));

    Cli_Process_Result forced = await run_cli_async(
      "repair",
      "--input", hole,
      "--output", destination,
      "--seed", "12345",
      "--force");
    Cli_Process_Result in_place_result = await run_cli_async(
      "repair",
      "--input", in_place,
      "--in-place",
      "--seed", "12345");

    assert_success(forced);
    assert_success(in_place_result);
    CollectionAssert.AreEqual(File.ReadAllBytes(original), File.ReadAllBytes(destination));
    CollectionAssert.AreEqual(File.ReadAllBytes(original), File.ReadAllBytes(in_place));
  }

  [TestMethod]
  public async Task SpecPathsResolveRelativeToSpecAndMasksApply()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string original = directory.path("original.map");
    string template = directory.path("template.map");
    string spec = directory.path("job.json");
    string output = directory.path("nested", "result.map");

    Cli_Process_Result original_result = await run_cli_async(
      "generate",
      "--width", "4",
      "--height", "1",
      "--tileset", "01020304",
      "--output", original,
      "--seed", "12345");
    assert_success(original_result);

    create_hole_map(original, template, 2, 0);

    Map_Job_Spec job = new Map_Job_Spec()
    {
      Version = 1,
      Operation = "generate",
      Template = "template.map",
      Output = Path.Combine("nested", "result.map"),
      AssetsDir = Path.GetRelativePath(directory.Root, repository_asset_root()),
      Tileset = "01020304",
      Seed = 12345,
      Drawn = new bool[][]
      {
        new bool[] { true, true, false, true }
      },
      Locked = new bool[][]
      {
        new bool[] { true, false, false, false }
      }
    };
    new Map_Job_Spec_Reader().write_job(spec, job);

    Cli_Process_Result result = await run_cli_in_directory_async(
      repository_root(),
      "generate",
      "--spec", spec);

    assert_success(result);
    Assert.IsTrue(File.Exists(output), "Spec output was not written relative to the spec directory.");
    CollectionAssert.AreEqual(File.ReadAllBytes(original), File.ReadAllBytes(output));
  }

  [TestMethod]
  public async Task IncompletePoliciesReturnExpectedExitCodes()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string spec = directory.path("incomplete.json");
    string default_output = directory.path("default.map");
    string allow_output = directory.path("allow.map");
    string require_output = directory.path("require.map");

    Map_Job_Spec job = new Map_Job_Spec()
    {
      Version = 1,
      Operation = "generate",
      Width = 2,
      Height = 2,
      Tileset = "01020304",
      Output = "default.map",
      Seed = 99,
      Terrain = new int[][]
      {
        new int[] { 999, 999 },
        new int[] { 999, 999 }
      }
    };
    new Map_Job_Spec_Reader().write_job(spec, job);

    Cli_Process_Result incomplete = await run_cli_async("generate", "--spec", spec);
    Cli_Process_Result allowed = await run_cli_async(
      "generate",
      "--spec", spec,
      "--output", allow_output,
      "--allow-incomplete");
    Cli_Process_Result required = await run_cli_async(
      "generate",
      "--spec", spec,
      "--output", require_output,
      "--require-complete");

    Assert.AreEqual(2, incomplete.Exit_Code, incomplete.describe());
    StringAssert.Contains(incomplete.Standard_Output, "4 unresolved cell(s)");
    Assert.IsTrue(File.Exists(default_output));

    assert_success(allowed);
    StringAssert.Contains(allowed.Standard_Output, "4 unresolved cell(s)");
    Assert.IsTrue(File.Exists(allow_output));

    Assert.AreEqual(2, required.Exit_Code, required.describe());
    StringAssert.Contains(required.Standard_Output, "output was not written because --require-complete was specified");
    Assert.IsFalse(File.Exists(require_output));

    Map_Document incomplete_document = new Text_Map_Codec().read(default_output);
    assert_all_tiles(incomplete_document, 0);
  }

  [TestMethod]
  public async Task InvalidInputsReturnExitCode1()
  {
    using Cli_Temporary_Directory directory = new Cli_Temporary_Directory();
    string map = directory.path("map.map");
    string mar = directory.path("map.mar");
    string zstd_tmx = directory.path("zstd.tmx");

    Cli_Process_Result seed_map = await run_cli_async(
      "generate",
      "--width", "2",
      "--height", "1",
      "--tileset", "01020304",
      "--output", map,
      "--seed", "1");
    Cli_Process_Result seed_mar = await run_cli_async(
      "generate",
      "--width", "2",
      "--height", "1",
      "--tileset", "01020304",
      "--output", mar,
      "--seed", "1");
    assert_success(seed_map);
    assert_success(seed_mar);

    File.WriteAllText(zstd_tmx,
      """
      <map orientation="orthogonal" width="1" height="1">
        <tileset firstgid="1" name="test"><image source="test.png"/></tileset>
        <layer width="1" height="1">
          <data encoding="base64" compression="zstd">AQAAAA==</data>
        </layer>
      </map>
      """);

    Cli_Process_Result invalid_depth = await run_cli_async(
      "generate",
      "--width", "1",
      "--height", "1",
      "--tileset", "01020304",
      "--output", directory.path("bad-depth.map"),
      "--depth", "3");
    Cli_Process_Result invalid_radius = await run_cli_async(
      "repair",
      "--input", map,
      "--output", directory.path("bad-radius.map"),
      "--repair-radius=-1");
    Cli_Process_Result invalid_restarts = await run_cli_async(
      "generate",
      "--width", "1",
      "--height", "1",
      "--tileset", "01020304",
      "--output", directory.path("bad-restarts.map"),
      "--experimental-restarts", "0");
    Cli_Process_Result invalid_nogood_limit = await run_cli_async(
      "generate",
      "--width", "1",
      "--height", "1",
      "--tileset", "01020304",
      "--output", directory.path("bad-nogoods.map"),
      "--experimental-nogood-limit=-1");
    Cli_Process_Result conflicting_flags = await run_cli_async(
      "generate",
      "--width", "1",
      "--height", "1",
      "--tileset", "01020304",
      "--output", directory.path("bad-flags.map"),
      "--allow-incomplete",
      "--require-complete");
    Cli_Process_Result missing_mar_metadata = await run_cli_async(
      "repair",
      "--input", mar,
      "--output", directory.path("repaired.mar"));
    Cli_Process_Result unsupported_zstd_tmx = await run_cli_async(
      "repair",
      "--input", zstd_tmx,
      "--output", directory.path("out.tmx"));

    assert_error(invalid_depth, "--depth must be 1 or 2.");
    assert_error(invalid_radius, "--repair-radius must be zero or greater.");
    assert_error(invalid_restarts, "--experimental-restarts must be a positive number.");
    assert_error(invalid_nogood_limit, "--experimental-nogood-limit must be zero or greater.");
    assert_error(conflicting_flags, "--allow-incomplete and --require-complete cannot both be specified.");
    assert_error(missing_mar_metadata, "requires positive width, positive height, and a tileset identifier");
    assert_error(unsupported_zstd_tmx, "encoding \"base64\" with compression \"zstd\" is not supported.");
  }

  private static async Task<Cli_Process_Result> run_cli_async(params string[] arguments)
  {
    return await run_cli_in_directory_async(repository_root(), arguments);
  }

  private static async Task<Cli_Process_Result> run_cli_in_directory_async(string working_directory, params string[] arguments)
  {
    ProcessStartInfo start_info = new ProcessStartInfo()
    {
      FileName = "dotnet",
      WorkingDirectory = working_directory,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
      CreateNoWindow = true,
    };
    start_info.ArgumentList.Add(cli_dll_path());
    foreach (string argument in arguments)
      start_info.ArgumentList.Add(argument);

    using Process process = new Process()
    {
      StartInfo = start_info
    };
    Assert.IsTrue(process.Start(), "Failed to start the CLI child process.");

    Task<string> output_task = process.StandardOutput.ReadToEndAsync();
    Task<string> error_task = process.StandardError.ReadToEndAsync();

    using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
    try
    {
      await process.WaitForExitAsync(timeout.Token);
    }
    catch (OperationCanceledException)
    {
      try
      {
        if (!process.HasExited)
          process.Kill(true);
      }
      catch
      {
      }
      Assert.Fail($"CLI process timed out: dotnet {cli_dll_path()} {string.Join(" ", arguments)}");
      return null!;
    }

    return new Cli_Process_Result(
      process.ExitCode,
      await output_task,
      await error_task,
      arguments);
  }

  private static void create_hole_map(string source, string destination, int x, int y)
  {
    Map_Document document = new Text_Map_Codec().read(source).clone();
    document.Tiles[x, y] = 0;
    new Text_Map_Codec().write(destination, document);
  }

  private static void assert_success(Cli_Process_Result result)
  {
    Assert.AreEqual(0, result.Exit_Code, result.describe());
    string[] lines = result.Standard_Error
      .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    Assert.IsTrue(lines.All(line => line.Contains(" progress: ", StringComparison.Ordinal)), result.describe());
  }

  private static void assert_error(Cli_Process_Result result, string message)
  {
    Assert.AreEqual(1, result.Exit_Code, result.describe());
    StringAssert.Contains(result.All_Output, message);
  }

  private static void assert_all_tiles(Map_Document document, int expected)
  {
    for (int y = 0; y < document.Height; ++y)
    {
      for (int x = 0; x < document.Width; ++x)
        Assert.AreEqual(expected, document.Tiles[x, y], $"Unexpected tile at ({x},{y}).");
    }
  }

  private static string cli_dll_path()
  {
    string path = Path.Combine(
      repository_root(),
      "FE_Map_Creator.Cli",
      "bin",
      test_configuration(),
      "net10.0",
      "FE_Map_Creator.Cli.dll");
    if (!File.Exists(path))
    {
      throw new FileNotFoundException(
        "The built CLI DLL was not found. Ensure FE_Map_Creator.Cli is built as part of the test project.",
        path);
    }
    return path;
  }

  private static string repository_asset_root() => repository_root();

  private static string repository_root()
  {
    DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory != null)
    {
      if (File.Exists(Path.Combine(directory.FullName, "Tileset_Data.xml")) &&
          Directory.Exists(Path.Combine(directory.FullName, "FE_Map_Creator.Cli")))
      {
        return directory.FullName;
      }
      directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("Could not locate the repository root.");
  }

  private static string test_configuration()
  {
    DirectoryInfo framework_directory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
    string? configuration = framework_directory.Parent?.Name;
    if (string.IsNullOrWhiteSpace(configuration))
      throw new DirectoryNotFoundException("Could not determine the active test build configuration.");
    return configuration;
  }

  private sealed class Cli_Process_Result
  {
    public int Exit_Code { get; }

    public string Standard_Output { get; }

    public string Standard_Error { get; }

    public string All_Output => $"{this.Standard_Output}{this.Standard_Error}";

    private IReadOnlyList<string> Arguments { get; }

    public Cli_Process_Result(
      int exit_code,
      string standard_output,
      string standard_error,
      IReadOnlyList<string> arguments)
    {
      this.Exit_Code = exit_code;
      this.Standard_Output = standard_output ?? "";
      this.Standard_Error = standard_error ?? "";
      this.Arguments = arguments;
    }

    public string describe()
    {
      return
        $"Exit code: {this.Exit_Code}{Environment.NewLine}" +
        $"Arguments: {string.Join(" ", this.Arguments)}{Environment.NewLine}" +
        $"STDOUT:{Environment.NewLine}{this.Standard_Output}{Environment.NewLine}" +
        $"STDERR:{Environment.NewLine}{this.Standard_Error}";
    }
  }

  private sealed class Cli_Temporary_Directory : IDisposable
  {
    public string Root { get; } = Path.Combine(Path.GetTempPath(), $"FEMapCreator-CliIntegration-{Guid.NewGuid():N}");

    public Cli_Temporary_Directory()
    {
      Directory.CreateDirectory(this.Root);
    }

    public string path(params string[] segments)
    {
      return segments.Aggregate(this.Root, Path.Combine);
    }

    public void Dispose()
    {
      if (Directory.Exists(this.Root))
        Directory.Delete(this.Root, true);
    }
  }
}
