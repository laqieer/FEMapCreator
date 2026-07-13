// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.Battle_Frame_Image_Data
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.IO;
using Vector2Extension;

#nullable disable
namespace FEXNA_Library;

public struct Battle_Frame_Image_Data
{
  [ContentSerializer(Optional = true, ElementName = "parent_index")]
  public int? Parent_Index;
  public int frame_id;
  public Vector2 loc;
  public Vector2 scale;
  [ContentSerializer(Optional = true, ElementName = "rotation")]
  public float? Rotation;
  public bool flipped;
  public int opacity;
  public int blend_mode;

  [ContentSerializerIgnore]
  public int parent_index
  {
    get
    {
      if (!this.Parent_Index.HasValue)
        this.Parent_Index = new int?(-1);
      return this.Parent_Index.Value;
    }
    set => this.Parent_Index = new int?(value);
  }

  [ContentSerializerIgnore]
  public float rotation
  {
    get
    {
      if (!this.Rotation.HasValue)
        this.Rotation = new float?(0.0f);
      return this.Rotation.Value;
    }
    set => this.Rotation = new float?(value);
  }

  public static Battle_Frame_Image_Data read(BinaryReader reader)
  {
    return new Battle_Frame_Image_Data()
    {
      parent_index = reader.ReadInt32(),
      frame_id = reader.ReadInt32(),
      loc = new Vector2().read(reader),
      scale = new Vector2().read(reader),
      rotation = (float) reader.ReadDouble(),
      flipped = reader.ReadBoolean(),
      opacity = reader.ReadInt32(),
      blend_mode = reader.ReadInt32()
    };
  }

  public void write(BinaryWriter writer)
  {
    writer.Write(this.parent_index);
    writer.Write(this.frame_id);
    this.loc.write(writer);
    this.scale.write(writer);
    writer.Write((double) this.rotation);
    writer.Write(this.flipped);
    writer.Write(this.opacity);
    writer.Write(this.blend_mode);
  }
}
