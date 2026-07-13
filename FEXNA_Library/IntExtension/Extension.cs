// Decompiled with JetBrains decompiler
// Type: IntExtension.Extension
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System.Collections.Generic;

#nullable disable
namespace IntExtension;

public static class Extension
{
  public static List<int> list_add(this int value, List<int> list)
  {
    List<int> intList1 = new List<int>((IEnumerable<int>) list);
    for (int index1 = 0; index1 < intList1.Count; ++index1)
    {
      List<int> intList2;
      int index2;
      (intList2 = intList1)[index2 = index1] = intList2[index2] + value;
    }
    return intList1;
  }
}
