using System;
using System.Collections.Generic;
using System.IO;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace BellerophonExport
{
  public class BellerophonExportComponent : GH_Component
  {
    private bool _saveWasRequested;

    public BellerophonExportComponent()
      : base("Bellerophon Export", "BPH",
          "Exports ordered Grasshopper toolpath points as Bellerophon .bph source.",
          "Dimidium", "Export")
    {
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddPointParameter("Toolpath points", "P",
            "Ordered points from toolpath curves. Keep each path in its own branch.",
            GH_ParamAccess.tree);

        pManager.AddTextParameter("Title", "T",
            "Custom title. Defaults to the Rhino file name, then to 'Bellerophon_Export'.",
            GH_ParamAccess.item);
        pManager[1].Optional = true;

        pManager.AddBooleanParameter("Save", "S",
            "Set to True to confirm and write the .bph file. Set back to False before saving again.",
            GH_ParamAccess.item, false);

        pManager.AddBooleanParameter("Flip Z", "F",
            "True = sort paths from top-to-bottom. False = sort paths from bottom-to-top.",
            GH_ParamAccess.item, false);

        pManager.AddNumberParameter("First layer Z", "Z1",
            "Minimum exported Z height in the Rhino document's units.",
            GH_ParamAccess.item, 0.2);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Bellerophon", "B",
            "Generated .bph source.",
            GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        GH_Structure<GH_Point> pointTree;
        if (!DA.GetDataTree(0, out pointTree)) return;

        List<List<Point3d>> paths = new List<List<Point3d>>();
        List<Point3d> points = new List<Point3d>();
        foreach (IList<GH_Point> branch in pointTree.Branches)
        {
            List<Point3d> path = new List<Point3d>();
            foreach (GH_Point point in branch)
            {
                path.Add(point.Value);
                points.Add(point.Value);
            }

            if (path.Count > 0)
                paths.Add(path);
        }

        if (points.Count == 0) return;

        string exportTitle = "";
        DA.GetData(1, ref exportTitle);

        bool saveFile = false;
        DA.GetData(2, ref saveFile);
        bool shouldPromptToSave = saveFile && !_saveWasRequested;
        _saveWasRequested = saveFile;

        bool flipZ = false;
        DA.GetData(3, ref flipZ);

        double firstLayerZ = 0.2;
        DA.GetData(4, ref firstLayerZ);
        if (double.IsNaN(firstLayerZ) || double.IsInfinity(firstLayerZ) || firstLayerZ < 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                "First layer Z must be a finite, non-negative number.");
            return;
        }

        string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var doc = Rhino.RhinoDoc.ActiveDoc;

        if (doc != null && !string.IsNullOrEmpty(doc.Name))
        {
            if (string.IsNullOrWhiteSpace(exportTitle))
                exportTitle = Path.GetFileNameWithoutExtension(doc.Name);
            if (!string.IsNullOrEmpty(doc.Path))
                folderPath = Path.GetDirectoryName(doc.Path);
        }
        else if (string.IsNullOrWhiteSpace(exportTitle))
        {
            exportTitle = "Bellerophon_Export";
        }

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double minZ = double.MaxValue;

        foreach (Point3d point in points)
        {
            if (point.X < minX) minX = point.X;
            if (point.Y < minY) minY = point.Y;
            if (point.Z < minZ) minZ = point.Z;
        }

        double shiftX = 10.0 - minX;
        double shiftY = 10.0 - minY;
        double shiftZ = firstLayerZ - minZ;

        System.Text.StringBuilder bph = new System.Text.StringBuilder();
        bph.AppendLine(string.Format("M.title \"{0}\"", FormatTitle(exportTitle)));
        bph.AppendLine("Absolute");
        bph.AppendLine("SetSpeed = 2000");
        bph.AppendLine("RelativeExtrusion");
        bph.AppendLine("EnableAutoExtrude = 1");
        bph.AppendLine("EnableAutoRetract = 1");
        bph.AppendLine("");

        var orderedPaths = flipZ
            ? System.Linq.Enumerable.OrderByDescending(paths, path => System.Linq.Enumerable.Average(path, point => point.Z))
            : System.Linq.Enumerable.OrderBy(paths, path => System.Linq.Enumerable.Average(path, point => point.Z));

        foreach (List<Point3d> path in orderedPaths)
        {
            foreach (Point3d point in path)
            {
                double safeX = point.X + shiftX;
                double safeY = point.Y + shiftY;
                double safeZ = point.Z + shiftZ;

                bph.AppendLine(string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "MoveTo x={0:F3} y={1:F3} z={2:F3}",
                    safeX, safeY, safeZ));
            }
        }

        bph.AppendLine("");
        bph.AppendLine("M.end");

        string finalSyntax = bph.ToString();
        DA.SetData(0, finalSyntax);

        if (shouldPromptToSave)
        {
            string filePath = Path.Combine(folderPath, exportTitle + ".bph");
            Eto.Forms.DialogResult result = Eto.Forms.MessageBox.Show(
                string.Format("Save the Bellerophon file to:\n{0}", filePath),
                "Confirm Bellerophon export",
                Eto.Forms.MessageBoxButtons.YesNo,
                Eto.Forms.MessageBoxType.Warning);

            if (result != Eto.Forms.DialogResult.Yes)
                return;

            try
            {
                File.WriteAllText(filePath, finalSyntax);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Failed to save: " + ex.Message);
            }
        }
    }

    private static string FormatTitle(string title)
    {
        if (title == null)
            return "";

        return title.Replace("\"", "'").Replace('\r', ' ').Replace('\n', ' ');
    }

    protected override System.Drawing.Bitmap Icon => ComponentIcons.PointExport;

    public override Guid ComponentGuid => new Guid("f7bbfb54-da7b-4d32-9afd-3fffef2184de");
  }
}
