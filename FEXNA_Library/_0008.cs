// Decompiled with JetBrains decompiler
// Type: 
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

#nullable disable
internal static class \u0008
{
  public static byte[] \u0002(byte[] _param0, byte[] _param1)
  {
    byte num1 = _param0[1];
    int length = _param1.Length;
    byte num2 = (byte) (length + 11 ^ (int) num1 + 7);
    uint num3 = (uint) (((int) _param0[0] | (int) _param0[2] << 8) + ((int) num2 << 3));
    ushort num4 = 0;
    for (int index = 0; index < length; ++index)
    {
      if ((index & 1) == 0)
      {
        num3 = (uint) ((int) num3 * 214013 + 2531011);
        num4 = (ushort) (num3 >> 16 /*0x10*/);
      }
      byte num5 = (byte) num4;
      num4 >>= 8;
      byte num6 = _param1[index];
      _param1[index] = (byte) ((uint) ((int) num6 ^ (int) num1 ^ (int) num2 + 3) ^ (uint) num5);
      num2 = num6;
    }
    return _param1;
  }
}
