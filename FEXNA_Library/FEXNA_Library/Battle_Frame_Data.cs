// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Battle_Frame_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class Battle_Frame_Data
{
  public List<Battle_Frame_Image_Data> Lower_Frames = new List<Battle_Frame_Image_Data>();
  public List<Battle_Frame_Image_Data> Upper_Frames = new List<Battle_Frame_Image_Data>();
  public int time = 10;
  public int pan = -1;
  public int[] flash = new int[2]{ -1, -1 };
  public List<Sound_Data> sounds = new List<Sound_Data>();

  public void read(BinaryReader reader)
  {
    this.Lower_Frames.read(reader);
    this.Upper_Frames.read(reader);
    this.time = reader.ReadInt32();
    this.pan = reader.ReadInt32();
    this.flash = this.flash.read(reader);
    this.sounds.read(reader);
  }

  public void write(BinaryWriter writer)
  {
    this.Lower_Frames.write(writer);
    this.Upper_Frames.write(writer);
    writer.Write(this.time);
    writer.Write(this.pan);
    this.flash.write(writer);
    this.sounds.write(writer);
  }

  public Battle_Frame_Image_Data image_data(int data_index)
  {
    if (data_index < this.Lower_Frames.Count)
      return this.Lower_Frames[data_index];
    if (data_index - this.Lower_Frames.Count < this.Upper_Frames.Count)
      return this.Upper_Frames[data_index - this.Lower_Frames.Count];
    throw new IndexOutOfRangeException();
  }

  public int lower_frame_parent_index(int data_index)
  {
    int parentIndex = this.Lower_Frames[data_index].parent_index;
    return this.frame_parent_index(data_index, parentIndex);
  }

  public int upper_frame_parent_index(int data_index)
  {
    int parentIndex = this.Upper_Frames[data_index].parent_index;
    return this.frame_parent_index(data_index + this.Lower_Frames.Count, parentIndex);
  }

  public int frame_parent_index(int data_index, int parent_index)
  {
    if (parent_index == -1 || parent_index >= this.Lower_Frames.Count + this.Upper_Frames.Count)
      return -1;
    HashSet<int> intSet = new HashSet<int>();
    intSet.Add(data_index);
    Battle_Frame_Image_Data battleFrameImageData;
    for (data_index = parent_index; !intSet.Contains(data_index); data_index = battleFrameImageData.parent_index)
    {
      intSet.Add(data_index);
      battleFrameImageData = this.image_data(data_index);
      if (battleFrameImageData.parent_index == -1)
        return parent_index;
    }
    return -1;
  }

  public void fix_delete_parent(int data_index)
  {
    for (int index = 0; index < this.Lower_Frames.Count; ++index)
    {
      Battle_Frame_Image_Data lowerFrame = this.Lower_Frames[index];
      this.\u0002(data_index, ref lowerFrame);
      this.Lower_Frames[index] = lowerFrame;
    }
    for (int index = 0; index < this.Upper_Frames.Count; ++index)
    {
      Battle_Frame_Image_Data upperFrame = this.Upper_Frames[index];
      this.\u0002(data_index, ref upperFrame);
      this.Upper_Frames[index] = upperFrame;
    }
  }

  private void \u0002(int _param1, ref Battle_Frame_Image_Data _param2)
  {
    if (_param2.parent_index == _param1)
      _param2.parent_index = this.image_data(_param1).parent_index;
    if (_param2.parent_index < _param1)
      return;
    --_param2.parent_index;
  }

  public void fix_switch_position_parent(int data_index1, int data_index2)
  {
    for (int index = 0; index < this.Lower_Frames.Count; ++index)
    {
      Battle_Frame_Image_Data lowerFrame = this.Lower_Frames[index];
      this.\u0002(data_index1, data_index2, ref lowerFrame);
      this.Lower_Frames[index] = lowerFrame;
    }
    for (int index = 0; index < this.Upper_Frames.Count; ++index)
    {
      Battle_Frame_Image_Data upperFrame = this.Upper_Frames[index];
      this.\u0002(data_index1, data_index2, ref upperFrame);
      this.Upper_Frames[index] = upperFrame;
    }
  }

  private void \u0002(int _param1, int _param2, ref Battle_Frame_Image_Data _param3)
  {
    if (_param3.parent_index == _param1)
    {
      _param3.parent_index = _param2;
    }
    else
    {
      if (_param3.parent_index != _param2)
        return;
      _param3.parent_index = _param1;
    }
  }

  public void fix_switch_to_lower_parent(int data_index)
  {
    for (int index = 0; index < this.Lower_Frames.Count; ++index)
    {
      Battle_Frame_Image_Data lowerFrame = this.Lower_Frames[index];
      this.\u0003(data_index, ref lowerFrame);
      this.Lower_Frames[index] = lowerFrame;
    }
    for (int index = 0; index < this.Upper_Frames.Count; ++index)
    {
      Battle_Frame_Image_Data upperFrame = this.Upper_Frames[index];
      this.\u0003(data_index, ref upperFrame);
      this.Upper_Frames[index] = upperFrame;
    }
  }

  private void \u0003(int _param1, ref Battle_Frame_Image_Data _param2)
  {
    if (_param2.parent_index == _param1)
    {
      _param2.parent_index = this.Lower_Frames.Count;
    }
    else
    {
      if (_param2.parent_index >= _param1 || _param2.parent_index < this.Lower_Frames.Count)
        return;
      ++_param2.parent_index;
    }
  }

  public void fix_switch_to_upper_parent(int data_index)
  {
    for (int index = 0; index < this.Lower_Frames.Count; ++index)
    {
      Battle_Frame_Image_Data lowerFrame = this.Lower_Frames[index];
      this.\u0005(data_index, ref lowerFrame);
      this.Lower_Frames[index] = lowerFrame;
    }
    for (int index = 0; index < this.Upper_Frames.Count; ++index)
    {
      Battle_Frame_Image_Data upperFrame = this.Upper_Frames[index];
      this.\u0005(data_index, ref upperFrame);
      this.Upper_Frames[index] = upperFrame;
    }
  }

  private void \u0005(int _param1, ref Battle_Frame_Image_Data _param2)
  {
    if (_param2.parent_index == _param1)
    {
      _param2.parent_index = this.Upper_Frames.Count + this.Lower_Frames.Count - 1;
    }
    else
    {
      if (_param2.parent_index <= _param1)
        return;
      --_param2.parent_index;
    }
  }
}
