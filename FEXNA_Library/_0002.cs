// Decompiled with JetBrains decompiler
// Type: 
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System;
using System.Reflection;

#nullable disable
internal sealed class \u0002
{
  public static Enum[] \u0002(Type _param0)
  {
    FieldInfo[] fieldInfoArray = _param0.BaseType == typeof (Enum) ? _param0.GetFields(BindingFlags.Static | BindingFlags.Public) : throw new Exception(\u0005.\u0002(-1223164336));
    Enum[] enumArray = new Enum[fieldInfoArray.Length];
    for (int index = 0; index < enumArray.Length; ++index)
      enumArray[index] = (Enum) fieldInfoArray[index].GetValue((object) null);
    return enumArray;
  }

  public static int \u0002(Type _param0)
  {
    return _param0.BaseType == typeof (Enum) ? _param0.GetFields(BindingFlags.Static | BindingFlags.Public).Length : throw new Exception(\u0005.\u0002(-1223164336));
  }
}
