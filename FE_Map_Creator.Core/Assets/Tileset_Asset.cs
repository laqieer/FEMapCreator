#nullable disable
namespace FE_Map_Creator;

public sealed class Tileset_Asset
{
  public string Name { get; }

  public string Image_Path { get; }

  public string Generation_Data_Path { get; }

  public bool Has_Image => !string.IsNullOrWhiteSpace(this.Image_Path);

  public bool Has_Generation_Data => !string.IsNullOrWhiteSpace(this.Generation_Data_Path);

  public string Missing_Pair_Diagnostic
  {
    get
    {
      if (this.Has_Image && this.Has_Generation_Data)
        return "";
      if (!this.Has_Image && !this.Has_Generation_Data)
        return "Missing PNG image and generation-data file.";
      return this.Has_Image ? "Missing generation-data file." : "Missing PNG image.";
    }
  }

  public Tileset_Asset(string name, string image_path, string generation_data_path)
  {
    this.Name = name ?? "";
    this.Image_Path = image_path ?? "";
    this.Generation_Data_Path = generation_data_path ?? "";
  }
}
