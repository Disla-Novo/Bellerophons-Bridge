# Bellerophon's Bridge


<img width="298" height="158" alt="bph_plugin" src="https://github.com/user-attachments/assets/8f386045-aac3-489e-b622-c253800cd22f" />


Bellerophon's Bridge is an alpha Grasshopper plugin with two ways to create Bellerophon source: export ordered XYZ toolpath points from Grasshopper, or convert slicer G-code into Bellerophon `MoveTo` commands. The Dimidium Ecosystem's Bellerophon compiler converts the exported input into printer-specific G-code using its compiler and printer-profile settings.

This plugin is an exporter, not a slicer: the point component does not turn a solid into perimeters or infill, and the G-code component converts existing slicer moves rather than generating toolpaths.

> **Alpha software:** Expect changes and test generated output in the Dimidium Ecosystem and with your printer setup before use. Do not assume an export is safe to print without reviewing the compiled G-code.

## Requirements

- Rhino with Grasshopper
- The Dimidium Ecosystem: [Get the latest release](https://github.com/Disla-Novo/Dimidium_Bellerophon). This will not work on versions prior to v1.2.0-alpha.13.

The project references Grasshopper 8. Check the release notes for the Rhino/Grasshopper versions supported by each downloadable plugin build.

## Download and install

Download the `.gha` file for your Rhino/Grasshopper version from the project's [Releases](https://github.com/Disla-Novo/Bellerophons-Bridge/releases) page. Releases include builds targeting `net48`, `net7.0`, and `net7.0-windows`; check the release notes and your Rhino version to choose the compatible file. You do not need to build the plugin from source to use a release.

To install it, copy the downloaded `.gha` file into Grasshopper's Libraries folder (commonly `%AppData%\Grasshopper\Libraries`) or click and drag the `.gha` onto the Grasshopper UI and restart Rhino. If Rhino or Grasshopper reports that the plugin could not be loaded, check the release notes and Rhino's plugin/security prompts for any additional steps for your platform.

To build from source, run `dotnet build BellerophonExport\BellerophonExport.csproj --configuration Release` from the repository root. The `.gha` files are written under `BellerophonExport\bin\Release\<target-framework>\`. Copy the build matching your Rhino version to Grasshopper's Libraries folder and restart Rhino; building does not replace a plugin already loaded by a running Rhino process.

### Choose an input workflow

- **Grasshopper geometry:** Use **Bellerophon Export (BPH)** with ordered XYZ points in a data tree. Each branch is treated as a path.
- **Slicer output:** Use **Bellerophon G-code Export (BPH-G)** with slicer G-code supplied as text lines or as one multi-line string. The component reads ordered `G0`/`G1` moves, follows `G90`/`G91` and `M82`/`M83` coordinate/extrusion modes, and skips comments and other commands.

The point component keeps its original Grasshopper identity, so existing definitions that used it remain linked. The G-code importer is a separate component with its own inputs and settings.

> [!IMPORTANT]
> **You can use the Droid plugin to generate your toolpaths.** Integrating [Droid](https://github.com/steox/droid/tree/master) makes it much easier to visually customize your slicing parameters and physical movements before compiling them into Bellerophon `.bph` files.

<img width="937" height="453" alt="testings" src="https://github.com/user-attachments/assets/30e3b7b2-3e5c-4e3d-9b47-6ee994700ef0" />

> [!TIP]
> Please note that this is a basic example of utilizing Droid. If you build a cleaner Grasshopper workflow, feel free to submit a pull request with a better screenshot!

### Prepare point-based toolpaths

1. **Draw your lines:** Start by turning your 3D shape into 2D printable lines (using tools like **Contour**). Remember, this just creates the outer shell
2. **Turn lines into dots:** 3D printers read points, not curves. Use **Divide Curve** to break your lines down into a series of points. 
3. **Keep them organized:** Plug your points into the **Toolpath points (P)** input in the order you want them to print. 
4. **Close your loops:** If you are printing a closed shape like a cylinder, make sure the last point connects back to the first point so you don't get a gap in the wall. 
5. **Double-check your work:** Look at the points on your screen before exporting. Too many points will make the printer stutter; too few points will make smooth curves look like blocky polygons.

**Point-mode reminder:** Bellerophon's Bridge is not a full slicer. The point exporter reads the exact points you created in Grasshopper and writes them as sequential `MoveTo` commands; it does not generate infill, calculate wall thickness, or optimize travel paths. It embeds the **AutoExtrude** and **AutoRetract** flags for the printer profile. The G-code exporter converts existing slicer moves instead. In both workflows, the Bellerophon compiler handles conversion to printer-ready G-code.



### Component inputs

| Input | Type | Behavior |
|---|---|---|
| **Toolpath points (P)** | Point tree | Ordered XYZ points for **Bellerophon Export**. Each non-empty branch is one path; points within a branch retain their input order. |
| **Title (T)** | Optional text | Used in the `M.title` command and as the saved filename. Defaults to the active Rhino document's name, or `Bellerophon_Export` when no active document name is available. |
| **Save (S)** | Boolean, default `False` | On a false-to-true change, asks for confirmation before saving a `.bph` file. Choose Yes to write or No to cancel. Set it back to False before requesting another save. |
| **Flip Z (F)** | Boolean, default `False` | Sorts whole paths by their average Z: bottom-to-top when false and top-to-bottom when true. It does not mirror or negate Z coordinates, and it does not reverse point order within a path. For best results, make each branch a single path at one layer height. |

### G-code component inputs

| Input | Type | Behavior |
|---|---|---|
| **G-code (G)** | Text list | G-code lines, or a single text item containing multiple lines. Only `G0`/`G1` movement commands are converted; comments and non-move commands are ignored. |
| **Title (T)** | Optional text | Used in `M.title` and as the saved filename. Defaults to the active Rhino document's name or `Bellerophon_Export`. |
| **Save (S)** | Boolean, default `False` | On a false-to-true change, asks for confirmation before saving a `.bph` file. Set it back to `False` before requesting another save. |
| **Flip Z (F)** | Boolean, default `False` | Reverses the parsed move order when true; otherwise preserves slicer order. |
| **Absolute mode (A)** | Boolean, default `True` | Emits `Absolute` in the generated Bellerophon header. |
| **Default speed (V)** | Number, default `2000` | Emits `SetSpeed = N` in the header. Set to `0` to omit it. |
| **Auto extrude (AE)** | Boolean, default `False` | Omits source E values and emits `EnableAutoExtrude = 1` when enabled. |
| **Auto retract (AR)** | Boolean, default `False` | Emits `EnableAutoRetract = 1` when enabled. Either auto option also enables `RelativeExtrusion`. |

### Output

**Bellerophon (B)** provides generated Bellerophon source as text. The point exporter writes its standard title, coordinate/extrusion settings and a `MoveTo` line for each point; the G-code exporter emits moves parsed from the slicer's G-code and supports the additional header options above. Both finish with `M.end`.

The point exporter writes `SetSpeed = 2000`, `RelativeExtrusion`, `EnableAutoExtrude = 1`, and `EnableAutoRetract = 1`. The G-code exporter instead preserves source E values by default and can optionally use the compiler's auto-extrusion and auto-retraction settings. The Bellerophon compiler applies extrusion and retraction according to its behavior and printer profile.

Before output, each component translates the toolpath so the minimum X and Y coordinates become `10` and the minimum Z becomes `0`. These coordinates are written as-is in Rhino document units; the exporter does not convert units. The Bellerophon compiler and printer profile expect millimeters, so set the Rhino document units to millimeters before creating/exporting toolpaths. With millimeter units, the offset places the lowest X and Y at `10 mm` and the lowest Z at `0 mm`. This is only an origin offset: the exporter does not check that the full toolpath fits within the printer's bed dimensions, so verify the resulting bounds.

When saving, a saved Rhino document's folder is used; otherwise, the Desktop is used. The confirmation dialog displays the destination path.

## Example Bellerophon output

```text
M.title "Bellerophon_Export"
Absolute
SetSpeed = 2000
RelativeExtrusion
EnableAutoExtrude = 1
EnableAutoRetract = 1

MoveTo x=10.000 y=10.000 z=0.000
MoveTo x=10.500 y=10.000 z=0.000

M.end
```

## Limitations and feedback

- This plugin is intended for the Dimidium Ecosystem's Bellerophon compiler; the emitted text is not standard G-code.
- The point component exports paths supplied by the Grasshopper definition; it does not generate infill, calculate wall thickness, or optimize travel paths.
- The G-code component only converts `G0`/`G1` movement commands and does not convert every slicer command or metadata field.
- It does not currently emit explicit path/travel markers between branches.
- The point exporter uses fixed speed and auto-extrusion/retraction settings. The G-code exporter exposes speed and auto-extrusion/retraction options as inputs.
- Units are not converted.

Grasshopper workflows and usability suggestions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for how to share ideas, examples, and bug reports.

## License

This project is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE).
