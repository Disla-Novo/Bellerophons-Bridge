using System;
using System.Collections.Generic;
using System.IO; // Required for saving files directly

using Grasshopper;
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
          "Compiles geometry into Bellerophon syntax and exports safely to disk.",
          "Dimidium", "Export")
    {
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {   
        // Auto-flattening added here to prevent data tree freezes
        pManager.AddPointParameter("Toolpath points", "P", "Ordered points from sliced toolpath curves. Keep each path in its own branch.", GH_ParamAccess.tree);
        
        pManager.AddTextParameter("Title", "T", "Custom title (Defaults to Rhino file name)", GH_ParamAccess.item);
        pManager[1].Optional = true; 

        // Saving is triggered once when this input changes from false to true.
        pManager.AddBooleanParameter("Save", "S", "Set to True to confirm and write the file. Set back to False before saving again.", GH_ParamAccess.item, false);

        //  New Flip toggle
        pManager.AddBooleanParameter("Flip Z", "F", "True = Top-to-Bottom, False = Bottom-to-Top", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Bellerophon", "B", "Compiled .bph syntax", GH_ParamAccess.item);
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
        DA.GetData(3, ref flipZ); // Retrieve the Flip Z toggle

        // Determine where to save the file (defaults to Desktop)
        string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var doc = Rhino.RhinoDoc.ActiveDoc;

        if (doc != null && !string.IsNullOrEmpty(doc.Name))
        {
            if (string.IsNullOrWhiteSpace(exportTitle))
            {
                exportTitle = Path.GetFileNameWithoutExtension(doc.Name); 
            }
            if (!string.IsNullOrEmpty(doc.Path))
            {
                folderPath = Path.GetDirectoryName(doc.Path);
            }
        }
        else if (string.IsNullOrWhiteSpace(exportTitle))
        {
            exportTitle = "Bellerophon_Export"; 
        }

        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double minZ = double.MaxValue;

        foreach (Point3d pt in points)
        {
            if (pt.X < minX) minX = pt.X;
            if (pt.Y < minY) minY = pt.Y;
            if (pt.Z < minZ) minZ = pt.Z;
        }

        double shiftX = 10.0 - minX;
        double shiftY = 10.0 - minY;
        double shiftZ = 0.0 - minZ;

        System.Text.StringBuilder bph = new System.Text.StringBuilder();
        bph.AppendLine($"M.title \"{exportTitle}\"");
        bph.AppendLine("Absolute");
        bph.AppendLine("SetSpeed = 2000");
        bph.AppendLine("RelativeExtrusion");
        bph.AppendLine("EnableAutoExtrude = 1");
        bph.AppendLine("EnableAutoRetract = 1"); // Added for the compiler-side retraction logic
        bph.AppendLine("");

        // Sort paths by their average Z while keeping each path's point order intact.
        var orderedPaths = flipZ
            ? System.Linq.Enumerable.OrderByDescending(paths, path => System.Linq.Enumerable.Average(path, pt => pt.Z))
            : System.Linq.Enumerable.OrderBy(paths, path => System.Linq.Enumerable.Average(path, pt => pt.Z));

        foreach (List<Point3d> path in orderedPaths)
        {
            foreach (Point3d pt in path)
            {
                double safeX = pt.X + shiftX;
                double safeY = pt.Y + shiftY;
                double safeZ = pt.Z + shiftZ;
                
                bph.AppendLine($"MoveTo x={safeX:F3} y={safeY:F3} z={safeZ:F3}");
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
                $"Save the Bellerophon file to:\n{filePath}",
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
    protected override System.Drawing.Bitmap Icon => null;

    public override Guid ComponentGuid => new Guid("f7bbfb54-da7b-4d32-9afd-3fffef2184de");
  }
}