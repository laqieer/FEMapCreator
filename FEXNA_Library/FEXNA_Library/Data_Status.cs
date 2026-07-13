// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Status
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework;
using System.Collections.Generic;

#nullable disable
namespace FEXNA_Library;

public class Data_Status
{
  public int Id;
  public string Name = string.Empty;
  public string Description = string.Empty;
  public int Turns = 1;
  public bool Negative;
  public float Damage_Per_Turn;
  public bool Unselectable;
  public bool Ai_Controlled;
  public bool Attacks_Allies;
  public bool No_Magic;
  public List<int> Skills = new List<int>();
  public int Image_Index;
  public int Map_Anim_Id = -1;
  public Color Battle_Color = new Color(0, 0, 0, 0);
}
