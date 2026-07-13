using System;
using System.IO;
using System.Text.Json;

#nullable disable
namespace FE_Map_Creator;

public sealed class Map_Job_Spec_Reader
{
  private readonly JsonSerializerOptions Options = new JsonSerializerOptions()
  {
    AllowTrailingCommas = true,
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    WriteIndented = true
  };

  public Map_Job_Spec read_job(string filename)
  {
    Map_Job_Spec job = read<Map_Job_Spec>(filename);
    validate_version(job.Version, filename);
    job.validate();
    return job;
  }

  public Map_Job_Manifest read_manifest(string filename)
  {
    Map_Job_Manifest manifest = read<Map_Job_Manifest>(filename);
    validate_version(manifest.Version, filename);
    if (manifest.Jobs == null || manifest.Jobs.Length == 0)
      throw new InvalidDataException("The batch manifest does not contain any jobs.");
    for (int index = 0; index < manifest.Jobs.Length; ++index)
    {
      if (manifest.Jobs[index] == null)
        throw new InvalidDataException($"Batch job {index} is null.");
      validate_version(manifest.Jobs[index].Version, $"{filename} job {index}");
      manifest.Jobs[index].validate();
    }
    return manifest;
  }

  public void write_job(string filename, Map_Job_Spec job)
  {
    if (job == null)
      throw new ArgumentNullException(nameof (job));
    validate_version(job.Version, nameof (job));
    job.validate();
    write(filename, job);
  }

  public void write_manifest(string filename, Map_Job_Manifest manifest)
  {
    if (manifest == null)
      throw new ArgumentNullException(nameof (manifest));
    validate_version(manifest.Version, nameof (manifest));
    if (manifest.Jobs == null || manifest.Jobs.Length == 0)
      throw new InvalidDataException("The batch manifest does not contain any jobs.");
    for (int index = 0; index < manifest.Jobs.Length; ++index)
    {
      if (manifest.Jobs[index] == null)
        throw new InvalidDataException($"Batch job {index} is null.");
      validate_version(manifest.Jobs[index].Version, $"{nameof (manifest)} job {index}");
      manifest.Jobs[index].validate();
    }
    write(filename, manifest);
  }

  private T read<T>(string filename)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A JSON filename is required.", nameof (filename));
    using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
    {
      T value = JsonSerializer.Deserialize<T>(stream, this.Options);
      if (value == null)
        throw new InvalidDataException($"JSON file \"{filename}\" did not contain a valid {typeof (T).Name}.");
      return value;
    }
  }

  private void write<T>(string filename, T value)
  {
    if (string.IsNullOrWhiteSpace(filename))
      throw new ArgumentException("A JSON filename is required.", nameof (filename));
    string directory = Path.GetDirectoryName(Path.GetFullPath(filename));
    Directory.CreateDirectory(directory);
    using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
      JsonSerializer.Serialize(stream, value, this.Options);
  }

  private static void validate_version(int version, string source)
  {
    if (version != 1)
      throw new NotSupportedException($"{source} uses unsupported map job schema version {version}.");
  }
}
