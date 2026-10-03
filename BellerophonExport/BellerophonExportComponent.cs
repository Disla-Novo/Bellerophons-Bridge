using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace BellerophonExport
{
  public class BellerophonGCodeComponent : GH_Component
  {
    private bool _saveWasRequested;

    public BellerophonGCodeComponent()
      : base("Bellerophon G-code Export", "BPH-G",
          "Converts slicer G-code into Bellerophon .bph source and exports it to disk.",
          "Dimidium", "Export")
    {
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
        pManager.AddTextParameter("G-code", "G",
            "Slicer G-code as a list of lines (or one big string with newlines). Comments and non-move commands are skipped.",
            GH_ParamAccess.list);

        pManager.AddTextParameter("Title", "T",
            "Custom title. Defaults to the Rhino file name, then to 'Bellerophon_Export'.",
            GH_ParamAccess.item);
        pManager[1].Optional = true;

        pManager.AddBooleanParameter("Save", "S",
            "Set to True to confirm and write the .bph file. Set back to False before saving again.",
            GH_ParamAccess.item, false);

        pManager.AddBooleanParameter("Flip Z", "F",
            "True = reverse the move order (top-to-bottom view). False = keep slicer order.",
            GH_ParamAccess.item, false);

        pManager.AddBooleanParameter("Absolute mode", "A",
            "Emit 'Absolute' at the top of the .bph.",
            GH_ParamAccess.item, true);

        pManager.AddNumberParameter("Default speed", "V",
            "Feedrate emitted as 'SetSpeed = N' at the top of the .bph. Set to 0 to skip.",
            GH_ParamAccess.item, 2000.0);

        pManager.AddBooleanParameter("Auto extrude", "AE",
            "True = strip E values from every move and emit EnableAutoExtrude = 1. False = keep the E values from the source G-code.",
            GH_ParamAccess.item, false);

        pManager.AddBooleanParameter("Auto retract", "AR",
            "True = emit EnableAutoRetract = 1. Requires RelativeExtrusion, which the plugin emits automatically when either AE or AR is on.",
            GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
        pManager.AddTextParameter("Bellerophon", "B",
            "Compiled .bph source.",
            GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        List<string> gcodeLines = new List<string>();
        if (!DA.GetDataList(0, gcodeLines) || gcodeLines.Count == 0) return;

        string exportTitle = "";
        DA.GetData(1, ref exportTitle);

        bool saveFile = false;
        DA.GetData(2, ref saveFile);
        bool shouldPromptToSave = saveFile && !_saveWasRequested;
        _saveWasRequested = saveFile;

        bool flipZ = false;
        DA.GetData(3, ref flipZ);

        bool absoluteMode = true;
        DA.GetData(4, ref absoluteMode);

        double defaultSpeed = 2000.0;
        DA.GetData(5, ref defaultSpeed);

        bool autoExtrude = false;
        DA.GetData(6, ref autoExtrude);

        bool autoRetract = false;
        DA.GetData(7, ref autoRetract);

      

        List<string> lines = new List<string>();
        foreach (string raw in gcodeLines)
        {
            if (raw == null) continue;
            if (raw.IndexOf('\n') >= 0)
            {
                foreach (string sub in raw.Split('\n'))
                    lines.Add(sub);
            }
            else
            {
                lines.Add(raw);
            }
        }

      

        // The parser flattens relative moves into absolute coordinates so the
        // emitted .bph is always absolute, independent of whatever mode the
        // source slicer was using.
        bool sourceAbsolutePos = true;
        bool sourceAbsoluteE = true;

        double currentX = 0, currentY = 0, currentZ = 0, currentE = 0;

        List<MoveRecord> moves = new List<MoveRecord>();

        foreach (string rawLine in lines)
        {
            if (rawLine == null) continue;
            string line = rawLine.Trim();
            if (line.Length == 0) continue;

            int commentIdx = line.IndexOf(';');
            if (commentIdx >= 0) line = line.Substring(0, commentIdx).Trim();
            if (line.Length == 0) continue;

            if (line.Equals("G90", StringComparison.OrdinalIgnoreCase)) { sourceAbsolutePos = true; continue; }
            if (line.Equals("G91", StringComparison.OrdinalIgnoreCase)) { sourceAbsolutePos = false; continue; }
            if (line.Equals("M82", StringComparison.OrdinalIgnoreCase)) { sourceAbsoluteE = true; continue; }
            if (line.Equals("M83", StringComparison.OrdinalIgnoreCase)) { sourceAbsoluteE = false; continue; }

            string upper = line.ToUpperInvariant();
            bool isMove = upper.StartsWith("G0 ") || upper.StartsWith("G1 ")
                       || upper.Equals("G0") || upper.Equals("G1");
            if (!isMove) continue;

            double? px = ExtractParam(line, 'X');
            double? py = ExtractParam(line, 'Y');
            double? pz = ExtractParam(line, 'Z');
            double? pe = ExtractParam(line, 'E');

            if (!px.HasValue && !py.HasValue && !pz.HasValue && !pe.HasValue) continue;

            double newX, newY, newZ, newE;
            if (sourceAbsolutePos)
            {
                newX = px.HasValue ? px.Value : currentX;
                newY = py.HasValue ? py.Value : currentY;
                newZ = pz.HasValue ? pz.Value : currentZ;
            }
            else
            {
                newX = px.HasValue ? currentX + px.Value : currentX;
                newY = py.HasValue ? currentY + py.Value : currentY;
                newZ = pz.HasValue ? currentZ + pz.Value : currentZ;
            }

            if (sourceAbsoluteE)
            {
                newE = pe.HasValue ? pe.Value : currentE;
            }
            else
            {
                newE = pe.HasValue ? currentE + pe.Value : currentE;
            }

            // A move that only sets E (no X/Y/Z) is a retract or prime. Keep
            // it as an E-only move record so the compiler emits a standalone
            // extrusion command.
            bool hasExplicitE = pe.HasValue;
            bool hasPosition = px.HasValue || py.HasValue || pz.HasValue;

            if (!hasPosition && !hasExplicitE) continue;

            currentX = newX;
            currentY = newY;
            currentZ = newZ;
            currentE = newE;

            moves.Add(new MoveRecord
            {
                X = hasPosition ? newX : (double?)null,
                Y = hasPosition ? newY : (double?)null,
                Z = hasPosition ? newZ : (double?)null,
                E = hasExplicitE ? newE : (double?)null
            });
        }

        if (moves.Count == 0) return;

        if (flipZ)
        {
            moves.Reverse();
        }

       
        // Toolpath min X/Y are shifted to sit 10mm in from the printer origin,
        // and min Z to 0. Adjust these constants if your printer homes elsewhere.
        double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
        foreach (var m in moves)
        {
            if (m.X.HasValue && m.X.Value < minX) minX = m.X.Value;
            if (m.Y.HasValue && m.Y.Value < minY) minY = m.Y.Value;
            if (m.Z.HasValue && m.Z.Value < minZ) minZ = m.Z.Value;
        }
        if (minX == double.MaxValue) minX = 0;
        if (minY == double.MaxValue) minY = 0;
        if (minZ == double.MaxValue) minZ = 0;

        double shiftX = 10.0 - minX;
        double shiftY = 10.0 - minY;
        double shiftZ = 0.0 - minZ;

        //  Determine title + save folder 
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

        // Emit .bph source 
        System.Text.StringBuilder bph = new System.Text.StringBuilder();
        bph.AppendLine(string.Format(CultureInfo.InvariantCulture,
            "M.title \"{0}\"", exportTitle));

        if (absoluteMode)
            bph.AppendLine("Absolute");

        if (defaultSpeed > 0)
            bph.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "SetSpeed = {0}", defaultSpeed));

        // Both auto features rely on relative extrusion. Emit the mode switch
        // once if either is enabled.
        if (autoExtrude || autoRetract)
            bph.AppendLine("RelativeExtrusion");

        if (autoExtrude)
            bph.AppendLine("EnableAutoExtrude = 1");

        if (autoRetract)
            bph.AppendLine("EnableAutoRetract = 1");

        bph.AppendLine("");

        foreach (var m in moves)
        {
            bool hasPos = m.X.HasValue || m.Y.HasValue || m.Z.HasValue;

            if (!hasPos && m.E.HasValue)
            {
                // E-only move: retract, unretract, or standalone prime.
                bph.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "MoveTo e={0:F4}", m.E.Value));
                continue;
            }

            double safeX = (m.X ?? 0) + shiftX;
            double safeY = (m.Y ?? 0) + shiftY;
            double safeZ = (m.Z ?? 0) + shiftZ;

            // Auto-extrude computes E from the printer profile, so any E in
            // the source is dropped.
            bool emitE = !autoExtrude && m.E.HasValue;

            if (emitE)
            {
                bph.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "MoveTo x={0:F3} y={1:F3} z={2:F3} e={3:F4}",
                    safeX, safeY, safeZ, m.E.Value));
            }
            else
            {
                bph.AppendLine(string.Format(CultureInfo.InvariantCulture,
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

    private struct MoveRecord
    {
        public double? X;
        public double? Y;
        public double? Z;
        public double? E;
    }

    // Parses a G-code parameter like 'X' out of a line. Returns null if the
    // parameter isn't present or can't be parsed.
    private static double? ExtractParam(string line, char letter)
    {
        for (int i = 0; i < line.Length; i++)
        {
            if (char.ToUpperInvariant(line[i]) != letter) continue;
            int start = i + 1;
            if (start >= line.Length) return null;

            int end = start;
            while (end < line.Length)
            {
                char c = line[end];
                if (char.IsDigit(c) || c == '.' || c == '-' || c == '+') end++;
                else break;
            }
            if (end == start) return null;

            double value;
            if (double.TryParse(line.Substring(start, end - start),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return value;

            return null;
        }
        return null;
    }

    protected override System.Drawing.Bitmap Icon => null;

    public override Guid ComponentGuid => new Guid("a4875090-b21e-4f21-8f7d-b7bd16310d42");
  }
}