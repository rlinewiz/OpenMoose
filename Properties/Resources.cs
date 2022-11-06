// Decompiled with JetBrains decompiler
// Type: J2534.Properties.Resources
// Assembly: ECM-TECH Suite, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: B30416E2-D8D0-4434-82C3-04779BF15D87
// Assembly location: D:\Users\Vida\Desktop\ME7 suite\ME7 Suite.exe

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace J2534.Properties
{
  [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
  [DebuggerNonUserCode]
  [CompilerGenerated]
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
        if (J2534.Properties.Resources.resourceMan == null)
          J2534.Properties.Resources.resourceMan = new ResourceManager("J2534.Properties.Resources", typeof (J2534.Properties.Resources).Assembly);
        return J2534.Properties.Resources.resourceMan;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get
      {
        return J2534.Properties.Resources.resourceCulture;
      }
      set
      {
        J2534.Properties.Resources.resourceCulture = value;
      }
    }
  }
}
