// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Flood_Fill
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;
using System.Collections.Generic;

#nullable disable
namespace FE_Map_Creator;

internal static class Flood_Fill
{
  internal static bool flood_fill<T>(
    T[,] source_array,
    T[,] target_array,
    int base_x,
    int base_y,
    T value)
  {
    T source = source_array[base_x, base_y];
    if (EqualityComparer<T>.Default.Equals(source, value))
      return false;
    int length1 = source_array.GetLength(0);
    int length2 = source_array.GetLength(1);
    bool[] already_tested = new bool[source_array.GetLength(0) * source_array.GetLength(1)];
    Queue<Tuple<int, int, int>> tupleQueue = new Queue<Tuple<int, int, int>>();
    Tuple<int, int, int> tuple1 = Flood_Fill.process_flood_fill<T>(base_x, base_y, already_tested, source, value, source_array, target_array, length1);
    tupleQueue.Enqueue(tuple1);
    while (tupleQueue.Count > 0)
    {
      Tuple<int, int, int> tuple2 = tupleQueue.Dequeue();
      for (int base_x1 = tuple2.Item1; base_x1 <= tuple2.Item2; ++base_x1)
      {
        if (tuple2.Item3 > 0 && !already_tested[length1 * (tuple2.Item3 - 1) + base_x1] && EqualityComparer<T>.Default.Equals(source_array[base_x1, tuple2.Item3 - 1], source))
        {
          Tuple<int, int, int> tuple3 = Flood_Fill.process_flood_fill<T>(base_x1, tuple2.Item3 - 1, already_tested, source, value, source_array, target_array, length1);
          tupleQueue.Enqueue(tuple3);
        }
        if (tuple2.Item3 < length2 - 1 && !already_tested[length1 * (tuple2.Item3 + 1) + base_x1] && EqualityComparer<T>.Default.Equals(source_array[base_x1, tuple2.Item3 + 1], source))
        {
          Tuple<int, int, int> tuple4 = Flood_Fill.process_flood_fill<T>(base_x1, tuple2.Item3 + 1, already_tested, source, value, source_array, target_array, length1);
          tupleQueue.Enqueue(tuple4);
        }
      }
    }
    return true;
  }

  private static Tuple<int, int, int> process_flood_fill<T>(
    int base_x,
    int base_y,
    bool[] already_tested,
    T value_to_overwrite,
    T value_to_write,
    T[,] source,
    T[,] target,
    int width)
  {
    already_tested[width * base_y + base_x] = true;
    target[base_x, base_y] = value_to_write;
    int index1 = base_x;
    int index2 = base_x;
    while (index1 != 0 && EqualityComparer<T>.Default.Equals(source[index1 - 1, base_y], value_to_overwrite))
    {
      --index1;
      already_tested[width * base_y + index1] = true;
      target[index1, base_y] = value_to_write;
    }
    while (index2 != width - 1 && EqualityComparer<T>.Default.Equals(source[index2 + 1, base_y], value_to_overwrite))
    {
      ++index2;
      already_tested[width * base_y + index2] = true;
      target[index2, base_y] = value_to_write;
    }
    return new Tuple<int, int, int>(index1, index2, base_y);
  }
}
