namespace FEXNA_Library;

public class Data_Terrain
{
  public int Id = 1;
  public string Name = string.Empty;
  public int[][] Move_Costs = new int[3][]
  {
    new int[5] { 1, 1, 1, 1, 1 },
    new int[5] { 1, 1, 1, 1, 1 },
    new int[5] { 1, 1, 1, 1, 1 }
  };
}
