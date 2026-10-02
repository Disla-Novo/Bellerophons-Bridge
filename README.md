# Bellerophon's Bridge


<img width="298" height="158" alt="bph_plugin" src="https://github.com/user-attachments/assets/8f386045-aac3-489e-b622-c253800cd22f" />


Bellerophon's Bridge is an alpha Grasshopper plugin for exporting ordered 3D-printing toolpath points to the Bellerophon language. The Dimidium Ecosystem's Bellerophon compiler converts the exported input into printer-specific G-code using its compiler and printer-profile settings.

This plugin is an exporter, not a slicer: it does not turn a solid into perimeters or infill. Create the toolpaths in Grasshopper (or another tool) and provide their ordered points to the component.

> **Alpha software:** Expect changes and test generated output in the Dimidium Ecosystem and with your printer setup before use. Do not assume an export is safe to print without reviewing the compiled G-code.

## Requirements

- Rhino with Grasshopper
- The Dimidium Ecosystem: [Get the latest release](https://github.com/Disla-Novo/Dimidium_Bellerophon). This will not work on versions prior to v1.2.0-alpha.13.

The project references Grasshopper 8. Check the release notes for the Rhino/Grasshopper versions supported by each downloadable plugin build.

## Download and install

Download the `.gha` file for your Rhino/Grasshopper version from the project's [Releases](https://github.com/Disla-Novo/Bellerophons-Bridge/releases) page. Releases include builds targeting `net48`, `net7.0`, and `net7.0-windows`; check the release notes and your Rhino version to choose the compatible file. You do not need to build the plugin from source to use a release.

To install it, copy the downloaded `.gha` file into Grasshopper's Libraries folder (commonly `%AppData%\Grasshopper\Libraries`) or click and drag the `.gha` onto the Grasshopper UI and restart Rhino. If Rhino or Grasshopper reports that the plugin could not be loaded, check the release notes and Rhino's plugin/security prompts for any additional steps for your platform.

Building from source is only necessary if you want to contribute to development or create your own build.

### Prepare your toolpaths

1. **Draw your lines:** Start by turning your 3D shape into 2D printable lines (using tools like **Contour**). Remember, this just creates the outer shell—you have to design your own infill lines if you want them!
2. **Turn lines into dots:** 3D printers read points, not curves. Use **Divide Curve** to break your lines down into a series of points. 
3. **Keep them organized:** Plug your points into the **Toolpath points (P)** input in the order you want them to print. 
4. **Close your loops:** If you are printing a closed shape like a cylinder, make sure the last point connects back to the first point so you don't get a gap in the wall. 
5. **Double-check your work:** Look at the points on your screen before exporting. Too many points will make the printer stutter; too few points will make smooth curves look like blocky polygons.

**A quick reminder on what this plugin does:** 
Bellerophon's Bridge is an exporter, not a full slicer. It will not generate infill, calculate wall thickness, or optimize travel paths across your build plate. It simply reads the exact dots you created in Grasshopper, writes them as sequential `MoveTo` commands, and embeds the **AutoExtrude** and **AutoRetract** flags for your specific printer profile. The main Bellerophon compiler then handles the heavy lifting of calculating the actual plastic flow and turning those instructions into printer-ready G-code! 



### Component inputs

| Input | Type | Behavior |
|---|---|---|
| **Toolpath points (P)** | Point tree | Ordered XYZ points. Each non-empty branch is exported as one path; points within a branch retain their input order. |
| **Title (T)** | Optional text | Used in the `M.title` command and as the saved filename. Defaults to the active Rhino document's name, or `Bellerophon_Export` when no active document name is available. |
| **Save (S)** | Boolean, default `False` | On a false-to-true change, asks for confirmation before saving a `.bph` file. Choose Yes to write or No to cancel. Set it back to False before requesting another save. |
| **Flip Z (F)** | Boolean, default `False` | Sorts whole paths by their average Z: bottom-to-top when false and top-to-bottom when true. It does not mirror or negate Z coordinates, and it does not reverse point order within a path. For best results, make each branch a single path at one layer height. |

### Output

**Bellerophon (B)** provides the generated Bellerophon source as text. The exporter writes a title, coordinate/extrusion mode and compiler settings, then a `MoveTo` line for each point and `M.end`.

The current exporter always writes `SetSpeed = 2000`, `RelativeExtrusion`, `EnableAutoExtrude = 1`, and `EnableAutoRetract = 1`. The Bellerophon compiler applies extrusion and retraction according to its behavior and printer profile. The exporter itself does not calculate E values or select printer-specific G-code.

Before output, it translates the points so the minimum X and Y coordinates become `10` and the minimum Z becomes `0`. These coordinates are written as-is in Rhino document units; the exporter does not convert units. The Bellerophon compiler and printer profile expect millimeters, so set the Rhino document units to millimeters before creating/exporting toolpaths. With millimeter units, the offset places the lowest X and Y at `10 mm` and the lowest Z at `0 mm`. This is only an origin offset: the exporter does not check that the full toolpath fits within the printer's bed dimensions, so verify the resulting bounds.

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
- It exports point paths supplied by the Grasshopper definition; it is not a slicer or solid-to-toolpath generator.
- It does not currently emit explicit path/travel markers between branches.
- Feed rate, auto-extrusion, and auto-retraction commands are currently fixed in the exported header, though it may be altered in the IDE. 
- Units are not converted.

Grasshopper workflows and usability suggestions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for how to share ideas, examples, and bug reports.

## License

This project is licensed under the GNU General Public License v3.0. See [LICENSE](LICENSE).
