// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Terrain
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System.Collections.Generic;

#nullable disable
namespace FEXNA_Library;

public class Data_Terrain
{
  public int Id = 1;
  public string Name = \u0005.\u0002(-1223164546);
  public int Avoid;
  public int Def;
  public int Res;
  public bool Stats_Visible = true;
  public int Step_Sound_Group;
  public string Platform_Rename = string.Empty;
  public string Background_Rename = string.Empty;
  public int Dust_Type;
  public bool Fire_Through;
  public int[][] Move_Costs = new int[3][]
  {
    new int[5]{ 1, 1, 1, 1, 1 },
    new int[5]{ 1, 1, 1, 1, 1 },
    new int[5]{ 1, 1, 1, 1, 1 }
  };
  public int[] Heal;
  public int Minimap = 1;
  public List<int> Minimap_Group = new List<int>();

  public override string ToString()
  {
    return string.Format(\u0005.\u0002(-1223164637), (object) this.Id, (object) this.Name, (object) this.Avoid, (object) this.Def, (object) this.Res);
  }
}
