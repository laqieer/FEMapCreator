// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Battle_Animation_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using ListExtension;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#nullable disable
namespace FEXNA_Library;

public class Battle_Animation_Data
{
  public int id;
  public string name = string.Empty;
  public string filename = string.Empty;
  public List<Battle_Frame_Data> Frames = new List<Battle_Frame_Data>();
  public bool loop;
  [ContentSerializer(Optional = true)]
  public List<Battle_Animation_Tween_Data> Tween_Data = new List<Battle_Animation_Tween_Data>();
  [ContentSerializer(Optional = true, ElementName = "pan")]
  public int? Pan = new int?();
  [ContentSerializerIgnore]
  public List<Battle_Animtion_Modifier> modifiers;

  [ContentSerializerIgnore]
  public int pan
  {
    get => this.Pan.Value;
    set => this.Pan = new int?(value);
  }

  public int frame_count => this.Frames.Count;

  public int duration
  {
    get
    {
      int duration = 0;
      foreach (Battle_Frame_Data frame in this.Frames)
        duration += frame.time;
      return duration;
    }
  }

  public bool has_tween_data => true;

  public void read(BinaryReader reader)
  {
    this.id = reader.ReadInt32();
    this.name = reader.ReadString();
    this.filename = reader.ReadString();
    this.Frames.read(reader);
    this.loop = reader.ReadBoolean();
    this.Tween_Data.read(reader);
    this.\u0002();
    this.setup_tweening();
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.id);
    writer.Write(this.name);
    writer.Write(this.filename);
    this.Frames.write(writer);
    writer.Write(this.loop);
    this.Tween_Data.write(writer);
  }

  public override string ToString()
  {
    return string.Format(\u0005.\u0002(-1223164902), (object) this.name, (object) this.filename);
  }

  public Battle_Frame_Data current_frame(int index)
  {
    return this.Frames.Count == 0 ? (Battle_Frame_Data) null : this.Frames[index];
  }

  public int image_index(int frame_id, int tick, int data_index)
  {
    return this.Frames[frame_id].image_data(data_index).frame_id + (int) this.\u0002(Frame_Tween_Types.Frame_Index, frame_id, tick, data_index);
  }

  public Vector2 image_location(int frame_id, int tick, int data_index)
  {
    Func<Battle_Animtion_Modifier, bool> func = (Func<Battle_Animtion_Modifier, bool>) null;
    Battle_Animation_Data.\u0002 obj = new Battle_Animation_Data.\u0002();
    Battle_Frame_Image_Data battleFrameImageData1 = this.Frames[frame_id].image_data(data_index);
    Vector2 vector2_1 = Vector2.Zero;
    Vector2 vector2_2 = Vector2.op_Addition(battleFrameImageData1.loc, new Vector2(this.\u0002(Frame_Tween_Types.X_Pos, frame_id, tick, data_index), this.\u0002(Frame_Tween_Types.Y_Pos, frame_id, tick, data_index)));
    Battle_Frame_Image_Data battleFrameImageData2;
    for (obj.\u0002 = this.Frames[frame_id].frame_parent_index(data_index, battleFrameImageData1.parent_index); obj.\u0002 != -1; obj.\u0002 = this.Frames[frame_id].frame_parent_index(obj.\u0002, battleFrameImageData2.parent_index))
    {
      battleFrameImageData2 = this.Frames[frame_id].image_data(obj.\u0002);
      float num = battleFrameImageData2.rotation + this.\u0002(Frame_Tween_Types.Rotation, frame_id, tick, obj.\u0002);
      if (this.modifiers != null)
      {
        List<Battle_Animtion_Modifier> modifiers = this.modifiers;
        if (func == null)
          func = new Func<Battle_Animtion_Modifier, bool>(obj.\u0002);
        Func<Battle_Animtion_Modifier, bool> predicate = func;
        foreach (Battle_Animtion_Modifier animtionModifier in modifiers.Where<Battle_Animtion_Modifier>(predicate))
          num += animtionModifier.magnitude;
      }
      Matrix matrix1 = Matrix.op_Multiply(Matrix.op_Multiply(Matrix.Identity, Matrix.CreateScale(battleFrameImageData2.scale.X, battleFrameImageData2.scale.Y, 1f)), Matrix.CreateRotationZ((float) ((double) num / 180.0 * 3.1415927410125732)));
      Matrix matrix2 = Matrix.op_Multiply(Matrix.Identity, Matrix.CreateRotationZ((float) ((double) num / 180.0 * 3.1415927410125732)));
      vector2_1 = Vector2.op_Addition(Vector2.Transform(vector2_1, matrix2), Vector2.Transform(vector2_2, matrix1));
      vector2_2 = Vector2.op_Addition(battleFrameImageData2.loc, new Vector2(this.\u0002(Frame_Tween_Types.X_Pos, frame_id, tick, obj.\u0002), this.\u0002(Frame_Tween_Types.Y_Pos, frame_id, tick, obj.\u0002)));
    }
    return Vector2.op_Addition(vector2_1, vector2_2);
  }

  public Vector2 image_scale(int frame_id, int tick, int data_index)
  {
    return Vector2.op_Addition(this.Frames[frame_id].image_data(data_index).scale, Vector2.op_Division(new Vector2(this.\u0002(Frame_Tween_Types.X_Scale, frame_id, tick, data_index), this.\u0002(Frame_Tween_Types.Y_Scale, frame_id, tick, data_index)), 100f));
  }

  public float image_rotation(int frame_id, int tick, int data_index)
  {
    Func<Battle_Animtion_Modifier, bool> func1 = (Func<Battle_Animtion_Modifier, bool>) null;
    Func<Battle_Animtion_Modifier, bool> func2 = (Func<Battle_Animtion_Modifier, bool>) null;
    Battle_Animation_Data.\u0003 obj = new Battle_Animation_Data.\u0003();
    obj.\u0003 = data_index;
    Battle_Frame_Image_Data battleFrameImageData1 = this.Frames[frame_id].image_data(obj.\u0003);
    float num = battleFrameImageData1.rotation + this.\u0002(Frame_Tween_Types.Rotation, frame_id, tick, obj.\u0003);
    if (this.modifiers != null)
    {
      List<Battle_Animtion_Modifier> modifiers = this.modifiers;
      if (func1 == null)
        func1 = new Func<Battle_Animtion_Modifier, bool>(obj.\u0002);
      Func<Battle_Animtion_Modifier, bool> predicate = func1;
      foreach (Battle_Animtion_Modifier animtionModifier in modifiers.Where<Battle_Animtion_Modifier>(predicate))
        num += animtionModifier.magnitude;
    }
    Battle_Frame_Image_Data battleFrameImageData2;
    for (obj.\u0002 = this.Frames[frame_id].frame_parent_index(obj.\u0003, battleFrameImageData1.parent_index); obj.\u0002 != -1; obj.\u0002 = this.Frames[frame_id].frame_parent_index(obj.\u0002, battleFrameImageData2.parent_index))
    {
      battleFrameImageData2 = this.Frames[frame_id].image_data(obj.\u0002);
      num += battleFrameImageData2.rotation + this.\u0002(Frame_Tween_Types.Rotation, frame_id, tick, obj.\u0002);
      if (this.modifiers != null)
      {
        List<Battle_Animtion_Modifier> modifiers = this.modifiers;
        if (func2 == null)
          func2 = new Func<Battle_Animtion_Modifier, bool>(obj.\u0003);
        Func<Battle_Animtion_Modifier, bool> predicate = func2;
        foreach (Battle_Animtion_Modifier animtionModifier in modifiers.Where<Battle_Animtion_Modifier>(predicate))
          num += animtionModifier.magnitude;
      }
    }
    return num;
  }

  public int image_opacity(int frame_id, int tick, int data_index)
  {
    return this.Frames[frame_id].image_data(data_index).opacity + (int) this.\u0002(Frame_Tween_Types.Opacity, frame_id, tick, data_index);
  }

  private float \u0002(Frame_Tween_Types _param1, int _param2, int _param3, int _param4)
  {
    float num1 = 0.0f;
    foreach (Battle_Animation_Tween_Data animationTweenData in this.Tween_Data)
    {
      if (animationTweenData.layer == _param4 && _param1 == animationTweenData.data && _param2 >= animationTweenData.start_frame && animationTweenData.start_frame < animationTweenData.end_frame && animationTweenData.interval > 0)
      {
        int index = animationTweenData.start_frame;
        int num2 = 0;
        int num3 = 0;
        if (index < 0)
        {
          index = 0;
          num3 = 1;
        }
        while (true)
        {
          if (animationTweenData.interval_type == Frame_Tween_Intervals.Tick)
            num2 += animationTweenData.interval;
          else
            index += animationTweenData.interval;
          for (; index < this.Frames.Count && num2 >= this.Frames[index].time; ++index)
            num2 -= this.Frames[index].time;
          if (index < this.Frames.Count && (index != animationTweenData.end_frame || num2 <= 0) && index <= animationTweenData.end_frame && (index != _param2 || num2 <= _param3) && index <= _param2)
            ++num3;
          else
            break;
        }
        switch (animationTweenData.function)
        {
          case Frame_Tween_Functions.Linear:
            num1 += (float) num3 * animationTweenData.magnitude;
            continue;
          case Frame_Tween_Functions.Sinusoidal:
            num1 += (float) ((double) animationTweenData.magnitude / 2.0 * Math.Sin((double) (num3 + animationTweenData.offset) * Math.PI * 2.0 / (double) animationTweenData.period) - (double) animationTweenData.magnitude / 2.0 * Math.Sin((double) animationTweenData.offset * Math.PI * 2.0 / (double) animationTweenData.period));
            continue;
          case Frame_Tween_Functions.Modulo:
            num1 += animationTweenData.magnitude * (float) ((num3 + animationTweenData.offset) % animationTweenData.period);
            continue;
          default:
            continue;
        }
      }
    }
    return num1;
  }

  public int next_frame(int index)
  {
    if (this.Frames.Count <= 0)
      return index;
    int num = index;
    index = (index + 1) % this.frame_count;
    while (this.Frames[index].time == 0)
    {
      if (num == index)
        return num;
      index = (index + 1) % this.frame_count;
      if (num == -1 && index == 0)
        return 0;
    }
    return index;
  }

  private void \u0002()
  {
    this.pan = 0;
    bool flag = false;
    for (int index = 0; index < this.Frames.Count; ++index)
    {
      if (this.Frames[index].pan == -1)
      {
        this.pan += this.Frames[index].time;
      }
      else
      {
        this.pan += this.Frames[index].pan;
        flag = true;
        break;
      }
    }
    if (flag)
      return;
    this.pan = 1;
  }

  public void setup_tweening()
  {
  }

  public bool flash_visible(int frame_id, int tick)
  {
    for (int index1 = frame_id; index1 >= 0; --index1)
    {
      if (this.Frames[index1].flash[0] >= 0 && this.Frames[index1].flash[0] < this.Frames[index1].time && (frame_id != index1 || tick >= this.Frames[index1].flash[0]))
      {
        int index2 = index1;
        int num1 = this.Frames[index1].flash[0];
        int num2 = 0;
        while (true)
        {
          ++num2;
          if (num2 <= this.Frames[index1].flash[1])
          {
            for (; index2 < this.Frames.Count && num1 >= this.Frames[index2].time; ++index2)
              num1 -= this.Frames[index2].time;
            if (index2 < frame_id || num1 < tick)
            {
              if (index2 < this.Frames.Count)
                ++num1;
              else
                goto label_11;
            }
            else
              break;
          }
          else
            goto label_11;
        }
        return true;
      }
      continue;
label_11:;
    }
    return false;
  }

  private sealed class \u0002
  {
    public int \u0002;

    public bool \u0002(Battle_Animtion_Modifier _param1)
    {
      return _param1.layer_id == this.\u0002 && _param1.type == Frame_Tween_Types.Rotation;
    }
  }

  private sealed class \u0003
  {
    public int \u0002;
    public int \u0003;

    public bool \u0002(Battle_Animtion_Modifier _param1)
    {
      return _param1.layer_id == this.\u0003 && _param1.type == Frame_Tween_Types.Rotation;
    }

    public bool \u0003(Battle_Animtion_Modifier _param1)
    {
      return _param1.layer_id == this.\u0002 && _param1.type == Frame_Tween_Types.Rotation;
    }
  }
}
