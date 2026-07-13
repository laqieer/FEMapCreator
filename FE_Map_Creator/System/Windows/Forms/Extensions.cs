// Decompiled with JetBrains decompiler
// Type: System.Windows.Forms.Extensions
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System.Runtime.InteropServices;

#nullable disable
namespace System.Windows.Forms;

public static class Extensions
{
  private const uint SW_RESTORE = 9;

  [DllImport("user32.dll")]
  private static extern int ShowWindow(IntPtr hWnd, uint Msg);

  public static void Restore(this Form form)
  {
    if (form.WindowState != FormWindowState.Minimized)
      return;
    Extensions.ShowWindow(form.Handle, 9U);
  }
}
