// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.KeyEventArgs
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using Microsoft.Xna.Framework.Input;
using System;

#nullable disable
namespace FEXNA_Library;

public class KeyEventArgs : EventArgs
{
  private Keys \u0002;

  public KeyEventArgs(Keys keyCode) => this.\u0002 = keyCode;

  public Keys KeyCode => this.\u0002;
}
