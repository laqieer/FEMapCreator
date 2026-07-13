#nullable disable
namespace FE_Map_Creator;

public interface IMap_Codec
{
  Map_Format Format { get; }

  Map_Document read(string filename, Map_Read_Options options = null);

  void write(string filename, Map_Document document, Map_Write_Options options = null);
}
