// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Preset_Chapter_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

#nullable disable
namespace FEXNA_Library;

public struct Preset_Chapter_Data
{
  public int Lord_Lvl;
  public int Units;
  public int Gold;
  public int Playtime;

  internal Preset_Chapter_Data(int _param1, int _param2, int _param3, int _param4)
  {
    this.Lord_Lvl = _param1;
    this.Units = _param2;
    this.Gold = _param3;
    this.Playtime = _param4;
  }

  internal Preset_Chapter_Data(Preset_Chapter_Data _param1)
  {
    this.Lord_Lvl = _param1.Lord_Lvl;
    this.Units = _param1.Units;
    this.Gold = _param1.Gold;
    this.Playtime = _param1.Playtime;
  }
}
