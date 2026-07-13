// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.NativeMethods
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Runtime.InteropServices;

#nullable disable
namespace FE_Map_Creator;

internal static class NativeMethods
{
  [DllImport("gdiplus.dll")]
  internal static extern int GdipWindingModeOutline(HandleRef path, IntPtr matrix, float flatness);

  internal static void GdipWindingModeOutline(GraphicsPath path, float flatness)
  {
    NativeMethods.GdipWindingModeOutline(new HandleRef((object) path, (IntPtr) path.GetType().GetField("nativePath", BindingFlags.Instance | BindingFlags.NonPublic).GetValue((object) path)), IntPtr.Zero, flatness);
  }
}
