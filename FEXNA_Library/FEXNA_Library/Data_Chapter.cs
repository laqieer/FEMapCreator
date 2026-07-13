// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Data_Chapter
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Data_Chapter
{
  public string Id = string.Empty;
  public List<string> Prior_Chapters = new List<string>();
  [ContentSerializer(Optional = true)]
  public List<string> Prior_Ranking_Chapters = new List<string>();
  [ContentSerializer(Optional = true)]
  public List<string> Completed_Chapters = new List<string>();
  [ContentSerializer(Optional = true)]
  public bool Standalone;
  public string Chapter_Name = string.Empty;
  [ContentSerializer(Optional = true)]
  public string World_Map_Name = string.Empty;
  [ContentSerializer(Optional = true)]
  public Vector2 World_Map_Loc;
  [ContentSerializer(Optional = true)]
  public int World_Map_Lord_Id;
  public List<string> Turn_Themes = new List<string>();
  public List<string> Battle_Themes = new List<string>();
  [ContentSerializer(Optional = true)]
  public int Battalion;
  public string Text_Key = string.Empty;
  public string Event_Data_Id = string.Empty;
  public int Ranking_Turns;
  public int Ranking_Combat;
  public int Ranking_Exp;
  public int Ranking_Completion;
  [ContentSerializer(Optional = true)]
  public Preset_Chapter_Data Preset_Data;
  [ContentSerializer(Optional = true)]
  public List<string> Progression_Ids = new List<string>();

  public Data_Chapter()
  {
  }

  public Data_Chapter(Data_Chapter chapter)
  {
    this.Id = chapter.Id;
    this.Progression_Ids = new List<string>((IEnumerable<string>) chapter.Progression_Ids);
    this.Prior_Chapters = new List<string>((IEnumerable<string>) chapter.Prior_Chapters);
    this.Prior_Ranking_Chapters = new List<string>((IEnumerable<string>) chapter.Prior_Ranking_Chapters);
    this.Completed_Chapters = new List<string>((IEnumerable<string>) chapter.Completed_Chapters);
    this.Standalone = chapter.Standalone;
    this.Chapter_Name = chapter.Chapter_Name;
    this.World_Map_Name = chapter.World_Map_Name;
    this.World_Map_Loc = chapter.World_Map_Loc;
    this.World_Map_Lord_Id = chapter.World_Map_Lord_Id;
    this.Turn_Themes = new List<string>((IEnumerable<string>) chapter.Turn_Themes);
    this.Battle_Themes = new List<string>((IEnumerable<string>) chapter.Battle_Themes);
    this.Battalion = chapter.Battalion;
    this.Text_Key = chapter.Text_Key;
    this.Event_Data_Id = chapter.Event_Data_Id;
    this.Ranking_Turns = chapter.Ranking_Turns;
    this.Ranking_Combat = chapter.Ranking_Combat;
    this.Ranking_Exp = chapter.Ranking_Exp;
    this.Ranking_Completion = chapter.Ranking_Completion;
    this.Preset_Data = new Preset_Chapter_Data(chapter.Preset_Data);
  }

  internal void \u0002(ContentReader _param1)
  {
    this.Id = ((BinaryReader) _param1).ReadString();
    this.Prior_Chapters.read((BinaryReader) _param1);
    this.Prior_Ranking_Chapters.read((BinaryReader) _param1);
    this.Completed_Chapters.read((BinaryReader) _param1);
    this.Standalone = ((BinaryReader) _param1).ReadBoolean();
    this.Chapter_Name = ((BinaryReader) _param1).ReadString();
    this.World_Map_Name = ((BinaryReader) _param1).ReadString();
    this.World_Map_Loc = this.World_Map_Loc.read((BinaryReader) _param1);
    this.World_Map_Lord_Id = ((BinaryReader) _param1).ReadInt32();
    this.Turn_Themes.read((BinaryReader) _param1);
    this.Battle_Themes.read((BinaryReader) _param1);
    this.Battalion = ((BinaryReader) _param1).ReadInt32();
    this.Text_Key = ((BinaryReader) _param1).ReadString();
    this.Event_Data_Id = ((BinaryReader) _param1).ReadString();
    this.Ranking_Turns = ((BinaryReader) _param1).ReadInt32();
    this.Ranking_Combat = ((BinaryReader) _param1).ReadInt32();
    this.Ranking_Exp = ((BinaryReader) _param1).ReadInt32();
    this.Ranking_Completion = ((BinaryReader) _param1).ReadInt32();
    this.Preset_Data = new Preset_Chapter_Data(((BinaryReader) _param1).ReadInt32(), ((BinaryReader) _param1).ReadInt32(), ((BinaryReader) _param1).ReadInt32(), ((BinaryReader) _param1).ReadInt32());
    this.Progression_Ids.read((BinaryReader) _param1);
  }

  public override string ToString()
  {
    return string.Format(\u0005.\u0002(-1223164807), (object) this.Id, (object) this.Chapter_Name);
  }
}
