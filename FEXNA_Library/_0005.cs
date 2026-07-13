// Decompiled with JetBrains decompiler
// Type: 
// Assembly: FEXNA_Library, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 7DBDACEE-B64B-4FB8-B02B-46D2DFF4BC2A
// Assembly location: C:\FEMapCreator\FEXNA_Library.dll

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

#nullable disable
internal static class \u0005
{
  private static readonly Dictionary<int, string> \u0002 = new Dictionary<int, string>(35);
  private static BinaryReader \u0003;
  private static byte[] \u0005;
  private static short \u0008;
  private static int \u0006;
  private static byte[] \u000E;

  [MethodImpl(MethodImplOptions.NoInlining)]
  internal static string \u0002(int _param0)
  {
    lock (global::\u0005.\u0002)
    {
      string str1;
      byte[] numArray1;
      for (; !global::\u0005.\u0002.TryGetValue(_param0, out str1); _param0 = ((int) numArray1[2] | (int) numArray1[3] << 16 /*0x10*/ | (int) numArray1[0] << 8 | (int) numArray1[1] << 24) ^ -_param0)
      {
        if (global::\u0005.\u0003 == null)
        {
          Assembly executingAssembly = Assembly.GetExecutingAssembly();
          Assembly.GetCallingAssembly();
          global::\u0005.\u0006 = 1610370;
          Stream manifestResourceStream = executingAssembly.GetManifestResourceStream("  \u200B          ");
          int index = 1;
          StackTrace stackTrace = new StackTrace();
          global::\u0005.\u0006 ^= 6470 | index;
          StackFrame frame = stackTrace.GetFrame(index);
          int num = index - 1;
          MethodBase method = frame == null ? (MethodBase) null : frame.GetMethod();
          global::\u0005.\u0006 ^= num + 128 /*0x80*/;
          Type declaringType = (object) method == null ? (Type) null : method.DeclaringType;
          if (frame == null)
            global::\u0005.\u0006 ^= 219315;
          bool flag = (object) declaringType == (object) typeof (RuntimeMethodHandle);
          global::\u0005.\u0006 ^= 160 /*0xA0*/;
          if (!flag)
          {
            flag = (object) declaringType == null;
            if (flag)
              global::\u0005.\u0006 ^= 219283;
          }
          if (flag == (stackTrace != null))
            global::\u0005.\u0006 ^= 32 /*0x20*/;
          global::\u0005.\u0006 ^= 6502 | num + 1;
          global::\u0005.\u0003 = new BinaryReader(manifestResourceStream);
          short count = (short) ((int) global::\u0005.\u0003.ReadInt16() ^ (int) (short) ~-~-~--~~-~29477);
          if (count == (short) 0)
            global::\u0005.\u0008 = (short) ((int) global::\u0005.\u0003.ReadInt16() ^ (int) (short) (~--~~--~~-~-1404609326 ^ -1404618441));
          else
            global::\u0005.\u0005 = global::\u0005.\u0003.ReadBytes((int) count);
          global::\u0005.\u000E = executingAssembly.GetName().GetPublicKeyToken();
          if (global::\u0005.\u000E != null && global::\u0005.\u000E.Length == 0)
            global::\u0005.\u000E = (byte[]) null;
          global::\u0005.\u0006 = global::\u0005.\u0006 & 268435314 ^ 6788;
        }
        int num1 = _param0 ^ -1223164898;
        global::\u0005.\u0003.BaseStream.Position = (long) num1;
        byte[] numArray2;
        if (global::\u0005.\u0005 != null)
        {
          numArray2 = global::\u0005.\u0005;
        }
        else
        {
          short count = global::\u0005.\u0008 != (short) -1 ? global::\u0005.\u0008 : (short) ((int) global::\u0005.\u0003.ReadInt16() ^ 2421 ^ num1);
          numArray2 = count != (short) 0 ? global::\u0005.\u0003.ReadBytes((int) count) : (byte[]) null;
        }
        int num2 = global::\u0005.\u0003.ReadInt32() ^ num1 ^ ~-~--~~-~913578354 ^ 187022166;
        if (num2 == -2)
        {
          numArray1 = global::\u0005.\u0003.ReadBytes(4);
          _param0 = -1028760099;
        }
        else
        {
          bool flag1 = (num2 & int.MinValue) != 0;
          bool flag2 = (num2 & 1073741824 /*0x40000000*/) != 0;
          int count = num2 & 1073741823 /*0x3FFFFFFF*/;
          byte[] numArray3 = global::\u0008.\u0002(numArray2, global::\u0005.\u0003.ReadBytes(count));
          if (global::\u0005.\u000E != null != (global::\u0005.\u0006 != 1607814))
          {
            for (int index = 0; index < count; ++index)
            {
              byte num3 = global::\u0005.\u000E[index & 7];
              byte num4 = (byte) ((int) num3 << 3 | (int) num3 >> 5);
              numArray3[index] = (byte) ((uint) numArray3[index] ^ (uint) num4);
            }
          }
          int num5 = global::\u0005.\u0006 - 12;
          byte[] bytes;
          int length;
          if (!flag2)
          {
            bytes = numArray3;
            length = count;
          }
          else
          {
            length = (int) numArray3[2] | (int) numArray3[0] << 16 /*0x10*/ | (int) numArray3[3] << 8 | (int) numArray3[1] << 24;
            bytes = new byte[length];
            global::\u0005.\u0002(numArray3, 4, bytes);
          }
          string str2;
          if (flag1 && num5 == 1607802)
          {
            char[] chArray = new char[length];
            for (int index = 0; index < length; ++index)
              chArray[index] = (char) bytes[index];
            str2 = new string(chArray);
          }
          else
            str2 = Encoding.Unicode.GetString(bytes, 0, bytes.Length);
          int num6 = num5 + ((int) sbyte.MaxValue + (num5 & 3) << 5);
          if (num6 != 1611930)
            str2 = (_param0 + count ^ 936568 ^ num6 & 1293).ToString("X");
          string str3 = string.Intern(str2);
          global::\u0005.\u0002.Add(_param0, str3);
          if (global::\u0005.\u0002.Count == 35)
          {
            global::\u0005.\u0003.Close();
            global::\u0005.\u0003 = (BinaryReader) null;
            global::\u0005.\u0005 = global::\u0005.\u000E = (byte[]) null;
          }
          return str3;
        }
      }
      return str1;
    }
  }

  private static int \u0002(byte[] _param0, int _param1, byte[] _param2)
  {
    int num1 = 0;
    int num2 = 0;
    int num3 = 128 /*0x80*/;
    int length = _param2.Length;
    while (num1 < length)
    {
      if ((num3 <<= 1) == 256 /*0x0100*/)
      {
        num3 = 1;
        num2 = (int) _param0[_param1++];
      }
      if ((num2 & num3) != 0)
      {
        int num4 = ((int) _param0[_param1] >> 2) + 3;
        int num5 = ((int) _param0[_param1] << 8 | (int) _param0[_param1 + 1]) & 1023 /*0x03FF*/;
        _param1 += 2;
        int num6 = num1 - num5;
        if (num6 < 0)
          return -1;
        while (true)
        {
          if (--num4 >= 0 && num1 < length)
            _param2[num1++] = _param2[num6++];
          else
            goto label_9;
        }
      }
      else
      {
        _param2[num1++] = _param0[_param1++];
        continue;
      }
label_9:;
    }
    return 0;
  }
}
