using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FE_Map_Creator.Tests;

internal static class Cli_Test_Helpers
{
  internal static async Task<Cli_Process_Result> run_cli_async(params string[] arguments)
  {
    return await run_cli_in_directory_async(repository_root(), arguments);
  }

  internal static async Task<Cli_Process_Result> run_cli_in_directory_async(string working_directory, params string[] arguments)
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

  internal static void assert_success(Cli_Process_Result result)
  {
    Assert.AreEqual(0, result.Exit_Code, result.describe());
    Assert.IsTrue(string.IsNullOrWhiteSpace(result.Standard_Error), result.describe());
  }

  internal static void assert_error(Cli_Process_Result result, string message)
  {
    Assert.AreEqual(1, result.Exit_Code, result.describe());
    StringAssert.Contains(result.All_Output, message);
  }

  internal static void assert_all_tiles(Map_Document document, int expected)
  {
    for (int y = 0; y < document.Height; ++y)
    {
      for (int x = 0; x < document.Width; ++x)
        Assert.AreEqual(expected, document.Tiles[x, y], $"Unexpected tile at ({x},{y}).");
    }
  }

  internal static void create_hole_map(string source, string destination, int x, int y)
  {
    Map_Document document = new Text_Map_Codec().read(source).clone();
    document.Tiles[x, y] = 0;
    new Text_Map_Codec().write(destination, document);
  }

  internal static void create_hole_mar(
    string source,
    string destination,
    int width,
    int height,
    string tileset,
    int x,
    int y)
  {
    Mar_Map_Codec codec = new Mar_Map_Codec();
    Map_Document document = codec.read(source, new Map_Read_Options()
    {
      Width = width,
      Height = height,
      Tileset = tileset
    }).clone();
    document.Tiles[x, y] = 0;
    codec.write(destination, document);
  }

  internal static int[] extract_reported_seeds(string output)
  {
    return Regex.Matches(output ?? "", @"seed (?<seed>-?\d+)")
      .Select(match => int.Parse(match.Groups["seed"].Value, System.Globalization.CultureInfo.InvariantCulture))
      .ToArray();
  }

  internal static string repository_asset_root() => repository_root();

  internal static string repository_root()
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

  private static string test_configuration()
  {
    DirectoryInfo framework_directory = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
    string? configuration = framework_directory.Parent?.Name;
    if (string.IsNullOrWhiteSpace(configuration))
      throw new DirectoryNotFoundException("Could not determine the active test build configuration.");
    return configuration;
  }
}

internal sealed class Cli_Process_Result
{
  internal int Exit_Code { get; }

  internal string Standard_Output { get; }

  internal string Standard_Error { get; }

  internal string All_Output => $"{this.Standard_Output}{this.Standard_Error}";

  private IReadOnlyList<string> Arguments { get; }

  internal Cli_Process_Result(
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

  internal string describe()
  {
    return
      $"Exit code: {this.Exit_Code}{Environment.NewLine}" +
      $"Arguments: {string.Join(" ", this.Arguments)}{Environment.NewLine}" +
      $"STDOUT:{Environment.NewLine}{this.Standard_Output}{Environment.NewLine}" +
      $"STDERR:{Environment.NewLine}{this.Standard_Error}";
  }
}

internal sealed class Cli_Temporary_Directory : IDisposable
{
  internal string Root { get; } = Path.Combine(Path.GetTempPath(), $"FEMapCreator-CliIntegration-{Guid.NewGuid():N}");

  internal Cli_Temporary_Directory()
  {
    Directory.CreateDirectory(this.Root);
  }

  internal string path(params string[] segments)
  {
    return segments.Aggregate(this.Root, Path.Combine);
  }

  public void Dispose()
  {
    if (Directory.Exists(this.Root))
      Directory.Delete(this.Root, true);
  }
}
