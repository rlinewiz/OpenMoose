// Decompiled with JetBrains decompiler
// Type: J2534.Program
// Assembly: ECM-TECH Suite, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: B30416E2-D8D0-4434-82C3-04779BF15D87
// Assembly location: D:\Users\Vida\Desktop\ME7 suite\ME7 Suite.exe

using System;
using System.Windows.Forms;

namespace J2534
{
  internal static class Program
  {
    [STAThread]
    private static void Main()
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);
      Application.Run((Form) new frmMain());
    }
  }
}
