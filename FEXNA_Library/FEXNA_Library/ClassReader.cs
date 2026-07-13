// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.ClassReader
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;
using System.IO;

#nullable disable
namespace FEXNA_Library;

public class ClassReader : ContentTypeReader<Data_Class>
{
  protected virtual Data_Class Read(ContentReader input, Data_Class existingInstance)
  {
    existingInstance = new Data_Class();
    existingInstance.Id = ((BinaryReader) input).ReadInt32();
    existingInstance.Name = ((BinaryReader) input).ReadString();
    existingInstance.Class_Types = new List<ClassTypes>();
    int num1 = ((BinaryReader) input).ReadInt32();
    for (int index = 0; index < num1; ++index)
      existingInstance.Class_Types.Add((ClassTypes) ((BinaryReader) input).ReadInt32());
    existingInstance.Skills = new List<int>();
    int num2 = ((BinaryReader) input).ReadInt32();
    for (int index = 0; index < num2; ++index)
      existingInstance.Skills.Add(((BinaryReader) input).ReadInt32());
    existingInstance.Description = ((BinaryReader) input).ReadString();
    if (((BinaryReader) input).ReadBoolean())
    {
      int num3 = ((BinaryReader) input).ReadInt32();
      existingInstance.Caps = new List<int>[2];
      existingInstance.Caps[0] = new List<int>();
      for (int index = 0; index < num3; ++index)
        existingInstance.Caps[0].Add(((BinaryReader) input).ReadInt32());
      existingInstance.Caps[1] = new List<int>();
      for (int index = 0; index < num3; ++index)
        existingInstance.Caps[1].Add(((BinaryReader) input).ReadInt32());
    }
    else
      existingInstance.Caps = (List<int>[]) null;
    existingInstance.Max_WLvl = new List<int>();
    int num4 = ((BinaryReader) input).ReadInt32();
    for (int index = 0; index < num4; ++index)
      existingInstance.Max_WLvl.Add(((BinaryReader) input).ReadInt32());
    existingInstance.Promotion = new Dictionary<int, List<int>[]>();
    int num5 = ((BinaryReader) input).ReadInt32();
    for (int index1 = 0; index1 < num5; ++index1)
    {
      int key = ((BinaryReader) input).ReadInt32();
      List<int>[] intListArray = new List<int>[2]
      {
        new List<int>(),
        new List<int>()
      };
      int num6 = ((BinaryReader) input).ReadInt32();
      for (int index2 = 0; index2 < num6; ++index2)
        intListArray[0].Add(((BinaryReader) input).ReadInt32());
      int num7 = ((BinaryReader) input).ReadInt32();
      for (int index3 = 0; index3 < num7; ++index3)
        intListArray[1].Add(((BinaryReader) input).ReadInt32());
      existingInstance.Promotion.Add(key, intListArray);
    }
    existingInstance.Tier = ((BinaryReader) input).ReadInt32();
    existingInstance.Mov = ((BinaryReader) input).ReadInt32();
    existingInstance.Mov_Cap = ((BinaryReader) input).ReadInt32();
    existingInstance.Movement_Type = (MovementTypes) ((BinaryReader) input).ReadInt32();
    existingInstance.Generic_Stats = new List<List<int>[]>();
    int num8 = ((BinaryReader) input).ReadInt32();
    for (int index4 = 0; index4 < num8; ++index4)
    {
      List<int>[] intListArray = new List<int>[2]
      {
        new List<int>(),
        new List<int>()
      };
      int num9 = ((BinaryReader) input).ReadInt32();
      for (int index5 = 0; index5 < num9; ++index5)
        intListArray[0].Add(((BinaryReader) input).ReadInt32());
      int num10 = ((BinaryReader) input).ReadInt32();
      for (int index6 = 0; index6 < num10; ++index6)
        intListArray[1].Add(((BinaryReader) input).ReadInt32());
      existingInstance.Generic_Stats.Add(intListArray);
    }
    return existingInstance;
  }
}
