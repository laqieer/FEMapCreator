using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FE_Map_Creator.Cli.Requests;
using FE_Map_Creator.Generation;

#nullable disable
namespace FE_Map_Creator.Cli.Execution;

/// <summary>
/// Core-backed <see cref="ICli_Executor"/> for <c>generate</c> (single-job and
/// homogeneous <c>--count</c> batches), <c>repair</c> (single-file and homogeneous
/// <c>--input-dir</c> directory batches), heterogeneous <c>batch --manifest</c>
/// orchestration, and <c>tilesets list</c>. All batch modes dispatch to the same
/// single-job <see cref="run_generate_job"/>/<see cref="run_repair_job"/> methods used by
/// direct single-job calls, so generation/repair logic is never duplicated.
/// </summary>
internal sealed class Core_Cli_Executor : ICli_Executor
{
  public async Task<Cli_Execution_Result> generate_async(
    Generate_Request request,
    Cli_Output output,
    CancellationToken cancellation_token)
  {
    if (request.Is_Batch_Mode)
      return await run_generate_count_batch(request, output, cancellation_token).ConfigureAwait(false);

    (Map_Job_Spec spec, string spec_directory) = load_optional_spec(request.Spec, "generate");
    return await run_generate_job(request, spec, spec_directory, output, cancellation_token).ConfigureAwait(false);
  }

  public async Task<Cli_Execution_Result> repair_async(
    Repair_Request request,
    Cli_Output output,
    CancellationToken cancellation_token)
  {
    if (request.Is_Directory_Mode)
      return await run_repair_directory_batch(request, output, cancellation_token).ConfigureAwait(false);

    (Map_Job_Spec spec, string spec_directory) = load_optional_spec(request.Spec, "repair");
    return await run_repair_job(request, spec, spec_directory, output, cancellation_token).ConfigureAwait(false);
  }

  public async Task<Cli_Execution_Result> batch_async(
    Batch_Request request,
    Cli_Output output,
    CancellationToken cancellation_token)
  {
    string manifest_path = resolve_optional_full_path(request.Manifest);
    Map_Job_Manifest manifest = new Map_Job_Spec_Reader().read_manifest(manifest_path);
    string manifest_directory = Path.GetDirectoryName(manifest_path);
    foreach (Map_Job_Spec job in manifest.Jobs)
    {
      if (string.Equals(job.Operation, "repair", StringComparison.OrdinalIgnoreCase))
        Repair_Input_Preflight.validate_manifest_mar_job(job, manifest_directory);
    }

    Batch_Progress progress = new Batch_Progress(manifest.Jobs.Length);
    for (int index = 0; index < manifest.Jobs.Length; ++index)
    {
      Map_Job_Spec job = manifest.Jobs[index];
      int job_number = index + 1;
      string label = $"[{job_number}/{manifest.Jobs.Length}] {job.Operation ?? "(missing operation)"}";

      Batch_Job_Runner.Outcome outcome = await Batch_Job_Runner.run(
        () => dispatch_manifest_job(job, job_number, manifest_directory, output, cancellation_token),
        label, output, progress, cancellation_token).ConfigureAwait(false);

      if (outcome.Cancelled)
      {
        output.Error.WriteLine(progress.summary("Batch"));
        throw new OperationCanceledException(cancellation_token);
      }
      if (request.Fail_Fast && !outcome.Succeeded)
        break;
    }

    return new Cli_Execution_Result(progress.exit_code(), progress.summary("Batch"));
  }

  public Task<Cli_Execution_Result> tilesets_list_async(
    Tilesets_List_Request request,
    Cli_Output output,
    CancellationToken cancellation_token)
  {
    string assets_root = Job_Merge.resolve_path(request.Assets_Dir, null, null) ?? AppContext.BaseDirectory;
    Tileset_Catalog catalog = new Tileset_Catalog(assets_root);
    int count = 0;
    foreach (Tileset_Asset asset in catalog.tilesets)
    {
      ++count;
      string diagnostic = asset.Missing_Pair_Diagnostic;
      string line = string.IsNullOrEmpty(diagnostic)
        ? $"{asset.Name}\n  image: {asset.Image_Path}\n  generation-data: {asset.Generation_Data_Path}"
        : $"{asset.Name}\n  {diagnostic}";
      output.Out.WriteLine(line);
    }
    return Task.FromResult(Cli_Execution_Result.success($"Listed {count} tileset(s) from \"{assets_root}\"."));
  }

  /// <summary>Single generate job, shared by direct single-job calls, --count batches
  /// (per generated job), and manifest jobs (per "generate" job).</summary>
  private static async Task<Cli_Execution_Result> run_generate_job(
    Generate_Request request,
    Map_Job_Spec spec,
    string spec_directory,
    Cli_Output output,
    CancellationToken cancellation_token)
  {
    Map_Codec_Registry registry = new Map_Codec_Registry();
    string template_path = Job_Merge.resolve_path(request.Template, spec?.Template, spec_directory);
    Map_Document template_document = template_path != null ? registry.read(template_path) : null;

    int? width = request.Width ?? spec?.Width ?? template_document?.Width;
    int? height = request.Height ?? spec?.Height ?? template_document?.Height;
    if (!width.HasValue || !height.HasValue)
      throw new InvalidOperationException("--width and --height are required (directly, via --spec, or via --template).");
    if (width.Value <= 0 || height.Value <= 0)
      throw new InvalidOperationException("Map width and height must be positive.");
    if (template_document != null && (template_document.Width != width.Value || template_document.Height != height.Value))
    {
      throw new InvalidOperationException(
        $"--template dimensions {template_document.Width}x{template_document.Height} do not match the requested {width.Value}x{height.Value}.");
    }

    int[,] tiles = new int[width.Value, height.Value];
    if (template_document != null)
      Array.Copy(template_document.Tiles, tiles, template_document.Tiles.Length);

    Map_State state = Map_State_Builder.build_for_generate(tiles, template_document != null, spec, width.Value, height.Value);

    string output_path = Job_Merge.resolve_path(request.Output, spec?.Output, spec_directory);
    if (string.IsNullOrWhiteSpace(output_path))
      throw new InvalidOperationException("--output is required (directly or via --spec).");
    Map_Format output_format = Output_Format_Resolver.resolve(request.Format, spec?.Format, output_path, registry);
    bool require_image = output_format == Map_Format.Tmx;

    string tileset_selector = Job_Merge.merge_string(request.Tileset, spec?.Tileset);
    Resolved_Tileset resolved = Asset_Resolution.resolve(
      request.Assets_Dir, spec?.AssetsDir, spec_directory,
      tileset_selector,
      request.Tileset_Image, spec?.TilesetImage,
      request.Generation_Data, spec?.GenerationData,
      require_image);
    Asset_Resolution.require_terrain_metadata_if_constrained(
      state.Terrain, width.Value, height.Value, resolved.Terrain_Metadata, resolved.Asset.Name);

    Map_Generation_Engine engine = new Map_Generation_Engine(resolved.Generation_Data, resolved.Terrain_Metadata);
    Map_Generation_Options options = new Map_Generation_Options
    {
      Depth = request.Depth ?? spec?.Depth ?? 1,
      Seed = request.Seed ?? spec?.Seed,
    };
    Cli_Map_Progress progress = new Cli_Map_Progress(
      output.Error,
      "Generate",
      output_path,
      checked(width.Value * height.Value));
    Map_Generation_Result result = await Task.Run(
      () => engine.generate(state, options, cancellation_token, null, progress), cancellation_token).ConfigureAwait(false);
    progress.complete();

    (Map_Document document, Map_Write_Options write_options) = Output_Document_Builder.build(state.Tiles, output_format, resolved.Asset);
    return Incomplete_Result_Writer.write(
      output_path, request.Force, request.Allow_Incomplete, request.Require_Complete,
      result.Unresolved_Tile_Count, result.Seed, "Generated",
      temporary_path => registry.write(temporary_path, document, write_options, output_format));
  }

  /// <summary>Single repair job, shared by direct single-file calls, --input-dir batches
  /// (per enumerated file), and manifest jobs (per "repair" job).</summary>
  private static async Task<Cli_Execution_Result> run_repair_job(
    Repair_Request request,
    Map_Job_Spec spec,
    string spec_directory,
    Cli_Output output,
    CancellationToken cancellation_token)
  {
    Map_Codec_Registry registry = new Map_Codec_Registry();
    Repair_Input_Metadata input = Repair_Input_Preflight.resolve(request, spec, spec_directory, registry);
    string selector_override = input.Tileset;
    int? width_override = input.Width;
    int? height_override = input.Height;
    Map_Read_Options read_options = new Map_Read_Options
    {
      Width = width_override,
      Height = height_override,
      Tileset = selector_override,
    };
    Map_Document document = registry.read(input.Input_Path, read_options, input.Input_Format);

    if (width_override.HasValue && width_override.Value != document.Width)
      throw new InvalidOperationException($"--width {width_override.Value} does not match the input map's actual width {document.Width}.");
    if (height_override.HasValue && height_override.Value != document.Height)
      throw new InvalidOperationException($"--height {height_override.Value} does not match the input map's actual height {document.Height}.");

    string effective_selector = !string.IsNullOrWhiteSpace(selector_override) ? selector_override : document.Tileset;

    Map_State state = Map_State_Builder.build_for_repair(document.Tiles, spec, document.Width, document.Height);

    string output_path = request.In_Place ? input.Input_Path : Job_Merge.resolve_path(request.Output, spec?.Output, spec_directory);
    if (string.IsNullOrWhiteSpace(output_path))
      throw new InvalidOperationException("--output is required unless --in-place or --spec supplies an output.");
    Map_Format output_format = Output_Format_Resolver.resolve(null, spec?.Format, output_path, registry);
    bool require_image = output_format == Map_Format.Tmx;

    Resolved_Tileset resolved = Asset_Resolution.resolve(
      request.Assets_Dir, spec?.AssetsDir, spec_directory,
      effective_selector,
      request.Tileset_Image, spec?.TilesetImage,
      request.Generation_Data, spec?.GenerationData,
      require_image);
    Asset_Resolution.require_terrain_metadata_if_constrained(
      state.Terrain, document.Width, document.Height, resolved.Terrain_Metadata, resolved.Asset.Name);

    Map_Generation_Engine engine = new Map_Generation_Engine(resolved.Generation_Data, resolved.Terrain_Metadata);
    Map_Repair_Options options = new Map_Repair_Options
    {
      Radius = request.Repair_Radius ?? spec?.RepairRadius ?? 0,
      Depth = request.Depth ?? spec?.Depth ?? 1,
      Seed = request.Seed ?? spec?.Seed,
    };
    Cli_Map_Progress progress = new Cli_Map_Progress(
      output.Error,
      "Repair",
      input.Input_Path,
      checked(document.Width * document.Height));
    Map_Generation_Result result = await Task.Run(
      () => engine.repair(state, options, cancellation_token, null, progress), cancellation_token).ConfigureAwait(false);
    progress.complete();

    (Map_Document output_document, Map_Write_Options write_options) = Output_Document_Builder.build(state.Tiles, output_format, resolved.Asset);
    return Incomplete_Result_Writer.write(
      output_path, request.Force || request.In_Place, request.Allow_Incomplete, request.Require_Complete,
      result.Unresolved_Tile_Count, result.Seed, "Repaired",
      temporary_path => registry.write(temporary_path, output_document, write_options, output_format));
  }

  /// <summary>
  /// Homogeneous <c>generate --count</c>: precomputes every job's seed and output path up
  /// front (so duplicate output paths from a bad --name-template are caught before any
  /// file is written), then runs each job through the same <see cref="run_generate_job"/>
  /// used for a direct single-job call. There is no --fail-fast for this mode; every job
  /// is attempted.
  /// </summary>
  private static async Task<Cli_Execution_Result> run_generate_count_batch(
    Generate_Request request,
    Cli_Output output,
    CancellationToken cancellation_token)
  {
    (Map_Job_Spec spec, string spec_directory) = load_optional_spec(request.Spec, "generate");

    int count = request.Count.Value;
    if (count <= 0)
      throw new InvalidOperationException("--count must be positive.");
    string output_dir = Job_Merge.resolve_path(request.Output_Dir, null, null);
    if (string.IsNullOrWhiteSpace(output_dir))
      throw new InvalidOperationException("--output-dir is required when --count is used.");
    Directory.CreateDirectory(output_dir);

    string name_template = string.IsNullOrWhiteSpace(request.Name_Template) ? "map-{index}" : request.Name_Template;
    Cli_Map_Format batch_format = Name_Template_Expander.resolve_batch_format(request.Format, spec?.Format, name_template);

    // With no base seed, one shared Random for the whole batch (not one per iteration) so
    // filenames can embed each job's actual seed before that job ever runs, without any
    // risk of the low-entropy "new Random() called in a tight loop" pitfall.
    int? base_seed = request.Seed ?? spec?.Seed;
    Random shared_random = base_seed.HasValue ? null : new Random();

    var planned_jobs = new List<(int index, int seed, string output_path)>(count);
    var seen_output_paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    for (int job_index = 1; job_index <= count; ++job_index)
    {
      int seed = base_seed.HasValue ? Seed_Derivation.derive(base_seed.Value, job_index) : shared_random.Next();
      string file_name = Name_Template_Expander.expand(name_template, job_index, seed, batch_format);
      string full_output_path = Path.GetFullPath(Path.Combine(output_dir, file_name));
      if (!seen_output_paths.Add(full_output_path))
      {
        throw new InvalidOperationException(
          $"--name-template \"{name_template}\" produced duplicate output path \"{full_output_path}\" for job {job_index}; " +
          "include {index} or {seed} in the template so every job's output is unique.");
      }
      planned_jobs.Add((job_index, seed, full_output_path));
    }

    Batch_Progress progress = new Batch_Progress(count);
    foreach ((int job_index, int seed, string full_output_path) in planned_jobs)
    {
      Generate_Request job_request = new Generate_Request
      {
        Width = request.Width,
        Height = request.Height,
        Tileset = request.Tileset,
        Output = full_output_path,
        Format = batch_format,
        Template = request.Template,
        Depth = request.Depth,
        Seed = seed,
        Assets_Dir = request.Assets_Dir,
        Tileset_Image = request.Tileset_Image,
        Generation_Data = request.Generation_Data,
        Force = request.Force,
        Allow_Incomplete = request.Allow_Incomplete,
        Require_Complete = request.Require_Complete,
      };

      string label = $"[{job_index}/{count}] {Path.GetFileName(full_output_path)}";
      Batch_Job_Runner.Outcome outcome = await Batch_Job_Runner.run(
        () => run_generate_job(job_request, spec, spec_directory, output, cancellation_token),
        label, output, progress, cancellation_token).ConfigureAwait(false);

      if (outcome.Cancelled)
      {
        output.Error.WriteLine(progress.summary("Generation"));
        throw new OperationCanceledException(cancellation_token);
      }
    }

    return new Cli_Execution_Result(progress.exit_code(), progress.summary("Generation"));
  }

  /// <summary>
  /// Homogeneous <c>repair --input-dir</c>: deterministically enumerates matching files
  /// (excluding sidecars and anything already under --output-dir, so a repeat run over a
  /// nested output directory never treats prior outputs as new inputs), preserves each
  /// file's relative subdirectory and extension, loads an optional per-file
  /// <c>.mapgen.json</c> sidecar, and runs each file through the same
  /// <see cref="run_repair_job"/> used for a direct single-file call.
  /// </summary>
  private static async Task<Cli_Execution_Result> run_repair_directory_batch(
    Repair_Request request,
    Cli_Output output,
    CancellationToken cancellation_token)
  {
    string input_dir = Job_Merge.resolve_path(request.Input_Dir, null, null);
    if (string.IsNullOrWhiteSpace(input_dir) || !Directory.Exists(input_dir))
      throw new InvalidOperationException($"--input-dir \"{request.Input_Dir}\" does not exist.");
    string output_dir = Job_Merge.resolve_path(request.Output_Dir, null, null);
    if (string.IsNullOrWhiteSpace(output_dir))
      throw new InvalidOperationException("--output-dir is required when --input-dir is used.");

    string output_dir_prefix = with_trailing_separator(output_dir);
    string pattern = string.IsNullOrWhiteSpace(request.Pattern) ? "*.map" : request.Pattern;
    SearchOption search_option = request.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

    string[] input_files = Directory.GetFiles(input_dir, pattern, search_option)
      .Select(Path.GetFullPath)
      .Where(file => !Sidecar_Spec_Loader.is_sidecar(file))
      .Where(file => !file.StartsWith(output_dir_prefix, StringComparison.OrdinalIgnoreCase))
      .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
      .ToArray();
    if (input_files.Length == 0)
      throw new InvalidOperationException($"No files matching pattern \"{pattern}\" were found under \"{input_dir}\".");

    var planned_jobs = new List<(string relative_path, Repair_Request job_request, Map_Job_Spec sidecar_spec, string sidecar_directory)>();
    Map_Codec_Registry registry = new Map_Codec_Registry();
    for (int file_index = 0; file_index < input_files.Length; ++file_index)
    {
      cancellation_token.ThrowIfCancellationRequested();
      string input_file = input_files[file_index];
      string relative_path = Path.GetRelativePath(input_dir, input_file);
      string output_file = Path.GetFullPath(Path.Combine(output_dir, relative_path));

      (Map_Job_Spec sidecar_spec, string sidecar_directory) = Sidecar_Spec_Loader.load(input_file);

      Repair_Request job_request = new Repair_Request
      {
        Input = input_file,
        Output = output_file,
        In_Place = false,
        Tileset = request.Tileset,
        Width = request.Width,
        Height = request.Height,
        Repair_Radius = request.Repair_Radius,
        Depth = request.Depth,
        Seed = request.Seed,
        Assets_Dir = request.Assets_Dir,
        Tileset_Image = request.Tileset_Image,
        Generation_Data = request.Generation_Data,
        Force = request.Force,
        Allow_Incomplete = request.Allow_Incomplete,
        Require_Complete = request.Require_Complete,
      };
      Repair_Input_Preflight.resolve(job_request, sidecar_spec, sidecar_directory, registry);
      planned_jobs.Add((relative_path, job_request, sidecar_spec, sidecar_directory));
    }

    Directory.CreateDirectory(output_dir);
    Batch_Progress progress = new Batch_Progress(planned_jobs.Count);
    for (int job_index = 0; job_index < planned_jobs.Count; ++job_index)
    {
      var job = planned_jobs[job_index];
      string label = $"[{job_index + 1}/{planned_jobs.Count}] {job.relative_path}";
      Batch_Job_Runner.Outcome outcome = await Batch_Job_Runner.run(
        () => run_repair_job(job.job_request, job.sidecar_spec, job.sidecar_directory, output, cancellation_token),
        label, output, progress, cancellation_token).ConfigureAwait(false);

      if (outcome.Cancelled)
      {
        output.Error.WriteLine(progress.summary("Repair"));
        throw new OperationCanceledException(cancellation_token);
      }
      if (request.Fail_Fast && !outcome.Succeeded)
        break;
    }

    return new Cli_Execution_Result(progress.exit_code(), progress.summary("Repair"));
  }

  /// <summary>Dispatches one <c>batch --manifest</c> job by its declared operation to the
  /// same single-job methods used everywhere else, rejecting nested batch operations and
  /// anything unrecognized with a clear message instead of guessing.</summary>
  private static Task<Cli_Execution_Result> dispatch_manifest_job(
    Map_Job_Spec job,
    int job_number,
    string manifest_directory,
    Cli_Output output,
    CancellationToken cancellation_token)
  {
    if (string.Equals(job.Operation, "generate", StringComparison.OrdinalIgnoreCase))
      return run_generate_job(new Generate_Request(), job, manifest_directory, output, cancellation_token);
    if (string.Equals(job.Operation, "repair", StringComparison.OrdinalIgnoreCase))
      return run_repair_job(new Repair_Request(), job, manifest_directory, output, cancellation_token);
    if (string.Equals(job.Operation, "batch", StringComparison.OrdinalIgnoreCase))
    {
      return Task.FromResult(Cli_Execution_Result.failure(
        $"Manifest job {job_number} declares operation \"batch\"; nested batch operations are not supported."));
    }
    return Task.FromResult(Cli_Execution_Result.failure(
      $"Manifest job {job_number} has unsupported operation \"{job.Operation}\"; expected \"generate\" or \"repair\"."));
  }

  private static (Map_Job_Spec spec, string spec_directory) load_optional_spec(string spec_path_raw, string expected_operation)
  {
    string spec_path = resolve_optional_full_path(spec_path_raw);
    if (spec_path == null)
      return (null, null);
    Map_Job_Spec spec = new Map_Job_Spec_Reader().read_job(spec_path);
    Job_Merge.validate_operation(spec.Operation, expected_operation, spec_path);
    return (spec, Path.GetDirectoryName(spec_path));
  }

  private static string with_trailing_separator(string path)
  {
    return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
  }

  private static string resolve_optional_full_path(string path)
  {
    return string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path);
  }
}
