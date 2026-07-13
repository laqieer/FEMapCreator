// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.EventInput
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Runtime.InteropServices;

#nullable disable
namespace FEXNA_Library;

public static class EventInput
{
  private static bool \u0008;
  private static IntPtr \u0006;
  private static EventInput.\u0002 \u000E;
  private static IntPtr \u000F;

  public static event CharEnteredHandler CharEntered;

  public static event KeyEventHandler KeyDown;

  public static event KeyEventHandler KeyUp;

  [DllImport("Imm32.dll", EntryPoint = "ImmGetContext")]
  private static extern IntPtr \u0002(IntPtr _param0);

  [DllImport("Imm32.dll", EntryPoint = "ImmAssociateContext")]
  private static extern IntPtr \u0002(IntPtr _param0, IntPtr _param1);

  [DllImport("user32.dll", EntryPoint = "CallWindowProc")]
  private static extern IntPtr \u0002(
    IntPtr _param0,
    IntPtr _param1,
    uint _param2,
    IntPtr _param3,
    IntPtr _param4);

  [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
  private static extern int \u0002(IntPtr _param0, int _param1, int _param2);

  public static void Initialize(GameWindow window)
  {
    if (EventInput.\u0008)
      throw new InvalidOperationException(global::\u0005.\u0002(-1223164824));
    EventInput.\u000E = new EventInput.\u0002(EventInput.\u0002);
    EventInput.\u0006 = (IntPtr) EventInput.\u0002(window.Handle, -4, (int) Marshal.GetFunctionPointerForDelegate((Delegate) EventInput.\u000E));
    EventInput.\u000F = EventInput.\u0002(window.Handle);
    EventInput.\u0008 = true;
  }

  private static IntPtr \u0002(IntPtr _param0, uint _param1, IntPtr _param2, IntPtr _param3)
  {
    IntPtr num = EventInput.\u0002(EventInput.\u0006, _param0, _param1, _param2, _param3);
    switch (_param1)
    {
      case 81:
        EventInput.\u0002(_param0, EventInput.\u000F);
        num = (IntPtr) 1;
        break;
      case 135:
        num = (IntPtr) (num.ToInt32() | 4);
        break;
      case 256 /*0x0100*/:
        if (EventInput.\u0003 != null)
        {
          EventInput.\u0003((object) null, new KeyEventArgs((Keys) (int) _param2));
          break;
        }
        break;
      case 257:
        if (EventInput.\u0005 != null)
        {
          EventInput.\u0005((object) null, new KeyEventArgs((Keys) (int) _param2));
          break;
        }
        break;
      case 258:
        if (EventInput.\u0002 != null)
        {
          EventInput.\u0002((object) null, new CharacterEventArgs((char) (int) _param2, _param3.ToInt32()));
          break;
        }
        break;
      case 641:
        if (_param2.ToInt32() == 1)
        {
          EventInput.\u0002(_param0, EventInput.\u000F);
          break;
        }
        break;
    }
    return num;
  }

  private delegate IntPtr \u0002(IntPtr _param1, uint _param2, IntPtr _param3, IntPtr _param4);
}
