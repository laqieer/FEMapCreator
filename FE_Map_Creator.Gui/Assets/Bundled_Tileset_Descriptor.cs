using Avalonia.Media.Imaging;
using FEXNA_Library;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

#nullable disable
namespace FE_Map_Creator.Gui.Assets;

public sealed class Bundled_Tileset_Descriptor
{
  private readonly Assembly Assembly;
  private readonly string Image_Resource;
  private readonly string Generation_Data_Resource;
  private readonly IReadOnlyDictionary<int, Data_Tileset> Metadata;

  public string Name { get; }

  internal Bundled_Tileset_Descriptor(
    Assembly assembly,
    string name,
    string image_resource,
    string generation_data_resource,
    IReadOnlyDictionary<int, Data_Tileset> metadata)
  {
    this.Assembly = assembly;
    this.Name = name;
    this.Image_Resource = image_resource;
    this.Generation_Data_Resource = generation_data_resource;
    this.Metadata = metadata;
  }

  public Bundled_Tileset load()
  {
    Bitmap image = null;
    using (Stream stream = open_resource(this.Assembly, this.Image_Resource))
      image = new Bitmap(stream);

    try
    {
      Tileset_Generation_Data generation_data;
      using (Stream stream = open_resource(this.Assembly, this.Generation_Data_Resource))
      using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: false))
        generation_data = Tileset_Generation_Data.read(reader);

      Tileset_Metadata_Reader metadata_reader = new Tileset_Metadata_Reader();
      Data_Tileset metadata = metadata_reader.find_for_asset_name(this.Metadata, this.Name);
      return new Bundled_Tileset(this.Name, image, generation_data, metadata);
    }
    catch
    {
      image.Dispose();
      throw;
    }
  }

  public override string ToString() => this.Name;

  private static Stream open_resource(Assembly assembly, string name)
  {
    return assembly.GetManifestResourceStream(name)
      ?? throw new FileNotFoundException($"Embedded resource \"{name}\" was not found.");
  }
}
