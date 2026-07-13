// Decompiled with JetBrains decompiler
// Type: FE_Map_Creator.Properties.Resources
// Assembly: FE_Map_Creator, Version=1.0.3.0, Culture=neutral, PublicKeyToken=null
// MVID: 892ADB68-6185-447F-AABB-429C2C7B2C22
// Assembly location: C:\FEMapCreator\FE_Map_Creator.exe

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

#nullable disable
namespace FE_Map_Creator.Properties;

[DebuggerNonUserCode]
[CompilerGenerated]
[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
internal class Resources
{
  private static ResourceManager resourceMan;
  private static CultureInfo resourceCulture;

  internal Resources()
  {
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  internal static ResourceManager ResourceManager
  {
    get
    {
      if (object.ReferenceEquals((object) FE_Map_Creator.Properties.Resources.resourceMan, (object) null))
        FE_Map_Creator.Properties.Resources.resourceMan = new ResourceManager("FE_Map_Creator.Properties.Resources", typeof (FE_Map_Creator.Properties.Resources).Assembly);
      return FE_Map_Creator.Properties.Resources.resourceMan;
    }
  }

  [EditorBrowsable(EditorBrowsableState.Advanced)]
  internal static CultureInfo Culture
  {
    get => FE_Map_Creator.Properties.Resources.resourceCulture;
    set => FE_Map_Creator.Properties.Resources.resourceCulture = value;
  }
}
