using System;
using System.Drawing;
using System.IO;

namespace BellerophonExport
{
  internal static class ComponentIcons
  {
    private static readonly Bitmap PointExportIcon = Load("BP.png");
    private static readonly Bitmap GCodeExportIcon = Load("GC.png");

    public static Bitmap PointExport => PointExportIcon;
    public static Bitmap GCodeExport => GCodeExportIcon;

    private static Bitmap Load(string resourceName)
    {
#if NET7_0_OR_GREATER
      if (!OperatingSystem.IsWindows())
        return null;
#endif

      string fullResourceName = "BellerophonExport.Properties." + resourceName;
      using (Stream stream = typeof(ComponentIcons).Assembly.GetManifestResourceStream(fullResourceName))
      {
        if (stream == null)
          return null;

        using (Bitmap source = new Bitmap(stream))
          return new Bitmap(source, 24, 24);
      }
    }
  }
}
