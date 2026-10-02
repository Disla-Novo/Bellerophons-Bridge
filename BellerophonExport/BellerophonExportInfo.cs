using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace BellerophonExport
{
  public class BellerophonExportInfo : GH_AssemblyInfo
  {
    public override string Name => "Bellerophon's Bridge";

    //Return a 24x24 pixel bitmap to represent this GHA library.
    public override Bitmap Icon => null;

    //Return a short string describing the purpose of this GHA library.
    public override string Description => "Exports 3D-printing toolpaths from Grasshopper for conversion to G-code with the Bellerophon compiler.";

    public override Guid Id => new Guid("66e8da52-0f16-4201-ac1f-52c5c14ed8b6");

    //Return a string identifying you or your company.
    public override string AuthorName => "Disla-Novo";

    //Return a string representing your preferred contact details.
    public override string AuthorContact => "https://github.com/Disla-Novo";

    //Return a string representing the version.  This returns the same version as the assembly.
    public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
  }
}