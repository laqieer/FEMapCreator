// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Additional_Math
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;

#nullable disable
namespace FE_Map_Creator;

internal class Additional_Math
{
  public static int int_closer(int source, int target, int val)
  {
    int num = target - source;
    val = (num < 0 ? -1 : 1) * Math.Min(Math.Abs(num), Math.Abs(val));
    return source + val;
  }

  public static double double_closer(double source, double target, double val)
  {
    double num = target - source;
    val = (num < 0.0 ? -1.0 : 1.0) * Math.Min(Math.Abs(num), Math.Abs(val));
    return source + val;
  }

  public static void swap(ref int i, ref int j)
  {
    int num = i;
    i = j;
    j = num;
  }
}
