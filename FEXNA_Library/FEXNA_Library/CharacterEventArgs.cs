// Decompiled with JetBrains decompiler
// Type: FEXNA_Library.CharacterEventArgs
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System;

#nullable disable
namespace FEXNA_Library;

public class CharacterEventArgs : EventArgs
{
  private readonly char \u0002;
  private readonly int \u0003;

  public CharacterEventArgs(char character, int lParam)
  {
    this.\u0002 = character;
    this.\u0003 = lParam;
  }

  public char Character => this.\u0002;

  public int Param => this.\u0003;

  public int RepeatCount => this.\u0003 & (int) ushort.MaxValue;

  public bool ExtendedKey => (this.\u0003 & 16777216 /*0x01000000*/) > 0;

  public bool AltPressed => (this.\u0003 & 536870912 /*0x20000000*/) > 0;

  public bool PreviousState => (this.\u0003 & 1073741824 /*0x40000000*/) > 0;

  public bool TransitionState => (this.\u0003 & int.MinValue) > 0;
}
